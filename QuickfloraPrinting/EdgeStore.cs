using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Microsoft.Win32;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3963 (epic AB#3961): the shop PC's own store - one SQLite file, no server, no port, no login.
    ///
    /// Three table groups:
    ///   queue      every print job this PC picked up, and how it ended (survives a crash or restart)
    ///   cache_rows reference data pulled from QuickFlora (items, customers, companies ...) - filled by EdgeSync
    ///   sync_state when each cache table was last pulled and up to which rowversion
    ///
    /// Rules (epic AB#3961): never store card numbers, card tokens, passwords or API keys here.
    /// Every call is wrapped - a store problem can never stop a receipt printing.
    /// Kill switch: HKCU\Software\QuickfloraPrinting\EdgeStoreDisabled = 1.
    /// </summary>
    internal static class EdgeStore
    {
        public const int SchemaVersion = 1;
        private const string PrefKeyPath = @"Software\QuickfloraPrinting";
        private const string DisabledValueName = "EdgeStoreDisabled";

        private static readonly object Gate = new object();
        private static string connString;
        private static string dbPath;
        private static Action<string> log = delegate { };
        private static bool opened;

        /// <summary>C:\QFPrintApp\edge\edge.db - the installer creates the folder with users-modify.</summary>
        public static string DbPath
        {
            get { return dbPath ?? Path.Combine(DataRoot, "edge.db"); }
        }

        public static string DataRoot
        {
            get { return @"C:\QFPrintApp\edge"; }
        }

        public static bool Enabled
        {
            get { return opened; }
        }

        public static bool IsDisabledByRegistry()
        {
            try
            {
                using (RegistryKey pref = Registry.CurrentUser.OpenSubKey(PrefKeyPath))
                {
                    return pref != null && Convert.ToInt32(pref.GetValue(DisabledValueName, 0)) != 0;
                }
            }
            catch { return false; }
        }

        /// <summary>Open (and create or migrate) the store. Safe to call more than once.</summary>
        public static void Open(Action<string> logger)
        {
            if (logger != null) log = logger;
            lock (Gate)
            {
                if (opened) return;
                try
                {
                    if (IsDisabledByRegistry())
                    {
                        log("EDGE store: disabled by registry (" + PrefKeyPath + "\\" + DisabledValueName + ")");
                        return;
                    }
                    if (!Directory.Exists(DataRoot)) Directory.CreateDirectory(DataRoot);
                    dbPath = Path.Combine(DataRoot, "edge.db");
                    SQLiteConnectionStringBuilder b = new SQLiteConnectionStringBuilder();
                    b.DataSource = dbPath;
                    b.Version = 3;
                    b.JournalMode = SQLiteJournalModeEnum.Wal;
                    b.DefaultTimeout = 5;
                    b.BusyTimeout = 5000;
                    b.FailIfMissing = false;
                    connString = b.ToString();
                    Migrate();
                    opened = true;
                    log("EDGE store: open " + dbPath + " schema v" + SchemaVersion + " | " + Summary());
                }
                catch (Exception ex)
                {
                    opened = false;
                    log("EDGE store: could not open " + DbPath + " - running without it: " + ex.Message);
                }
            }
        }

        private static SQLiteConnection Connect()
        {
            SQLiteConnection c = new SQLiteConnection(connString);
            c.Open();
            return c;
        }

        private static void Migrate()
        {
            using (SQLiteConnection c = Connect())
            {
                int v = Convert.ToInt32(Scalar(c, "PRAGMA user_version;"));
                if (v < 1)
                {
                    Exec(c,
                        "CREATE TABLE IF NOT EXISTS queue (" +
                        "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
                        "  slno INTEGER NOT NULL," +
                        "  company TEXT NOT NULL, division TEXT NOT NULL, department TEXT NOT NULL, terminal TEXT NOT NULL," +
                        "  file_name TEXT, printer TEXT, source_url TEXT," +
                        "  received_at TEXT NOT NULL," +
                        "  status TEXT NOT NULL," +          // queued | printed | failed
                        "  done_at TEXT, note TEXT);" +
                        "CREATE INDEX IF NOT EXISTS ix_queue_status ON queue(status, received_at);" +
                        "CREATE UNIQUE INDEX IF NOT EXISTS ux_queue_slno ON queue(slno) WHERE slno > 0;" +

                        "CREATE TABLE IF NOT EXISTS cache_rows (" +
                        "  table_name TEXT NOT NULL, row_key TEXT NOT NULL," +
                        "  payload TEXT NOT NULL," +           // JSON from the sync source
                        "  rowversion INTEGER NOT NULL DEFAULT 0," +
                        "  updated_at TEXT NOT NULL," +
                        "  PRIMARY KEY (table_name, row_key));" +

                        "CREATE TABLE IF NOT EXISTS sync_state (" +
                        "  table_name TEXT PRIMARY KEY," +
                        "  rowversion INTEGER NOT NULL DEFAULT 0," +
                        "  synced_at TEXT, rows INTEGER NOT NULL DEFAULT 0, note TEXT);" +

                        "PRAGMA user_version = 1;");
                }
            }
        }

        // ---------------------------------------------------------------- queue

        /// <summary>A job was taken off the server for this PC. Idempotent per slno.</summary>
        public static void JobReceived(int slno, string fileName, string printer, string sourceUrl)
        {
            if (!opened || slno <= 0) return;
            try
            {
                using (SQLiteConnection c = Connect())
                using (SQLiteCommand cmd = c.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT OR IGNORE INTO queue (slno, company, division, department, terminal, file_name, printer, source_url, received_at, status) " +
                        "VALUES (@slno, @c, @d, @p, @t, @f, @pr, @src, @now, 'queued');";
                    cmd.Parameters.AddWithValue("@slno", slno);
                    cmd.Parameters.AddWithValue("@c", Program.CompanyID ?? "");
                    cmd.Parameters.AddWithValue("@d", Program.DivisionID ?? "");
                    cmd.Parameters.AddWithValue("@p", Program.DepartmentID ?? "");
                    cmd.Parameters.AddWithValue("@t", Program.TerminalName ?? "");
                    cmd.Parameters.AddWithValue("@f", fileName ?? "");
                    cmd.Parameters.AddWithValue("@pr", printer ?? "");
                    cmd.Parameters.AddWithValue("@src", sourceUrl ?? "");
                    cmd.Parameters.AddWithValue("@now", Now());
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { log("EDGE store: JobReceived slno=" + slno + " failed: " + ex.Message); }
        }

        /// <summary>How the job ended. printed=true closes it; false leaves a 'failed' row with the reason.</summary>
        public static void JobDone(int slno, bool printed, string note)
        {
            if (!opened || slno <= 0) return;
            try
            {
                using (SQLiteConnection c = Connect())
                using (SQLiteCommand cmd = c.CreateCommand())
                {
                    cmd.CommandText = "UPDATE queue SET status=@s, done_at=@now, note=@n WHERE slno=@slno;";
                    cmd.Parameters.AddWithValue("@s", printed ? "printed" : "failed");
                    cmd.Parameters.AddWithValue("@now", Now());
                    cmd.Parameters.AddWithValue("@n", note ?? "");
                    cmd.Parameters.AddWithValue("@slno", slno);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { log("EDGE store: JobDone slno=" + slno + " failed: " + ex.Message); }
        }

        /// <summary>Jobs this PC received but never finished (crash, power cut). slno, file, printer, source_url, received_at.</summary>
        public static List<string[]> UnfinishedJobs()
        {
            List<string[]> rows = new List<string[]>();
            if (!opened) return rows;
            try
            {
                using (SQLiteConnection c = Connect())
                using (SQLiteCommand cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT slno, file_name, printer, source_url, received_at FROM queue WHERE status='queued' ORDER BY received_at;";
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            rows.Add(new string[] { r[0].ToString(), r[1].ToString(), r[2].ToString(), r[3].ToString(), r[4].ToString() });
                }
            }
            catch (Exception ex) { log("EDGE store: UnfinishedJobs failed: " + ex.Message); }
            return rows;
        }

        /// <summary>Keep the queue small: finished rows older than 90 days go.</summary>
        public static void Trim()
        {
            if (!opened) return;
            try
            {
                using (SQLiteConnection c = Connect())
                    Exec(c, "DELETE FROM queue WHERE status<>'queued' AND received_at < '" + Now(DateTime.Now.AddDays(-90)) + "';");
            }
            catch (Exception ex) { log("EDGE store: Trim failed: " + ex.Message); }
        }

        // ---------------------------------------------------------------- cache

        /// <summary>Write or replace one cached row. Payload is whatever the sync source sent (JSON).</summary>
        public static void UpsertCache(string table, string key, string payload, long rowversion)
        {
            if (!opened) return;
            using (SQLiteConnection c = Connect())
            using (SQLiteCommand cmd = c.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO cache_rows (table_name, row_key, payload, rowversion, updated_at) VALUES (@t, @k, @p, @v, @now) " +
                    "ON CONFLICT(table_name, row_key) DO UPDATE SET payload=excluded.payload, rowversion=excluded.rowversion, updated_at=excluded.updated_at;";
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@k", key);
                cmd.Parameters.AddWithValue("@p", payload ?? "");
                cmd.Parameters.AddWithValue("@v", rowversion);
                cmd.Parameters.AddWithValue("@now", Now());
                cmd.ExecuteNonQuery();
            }
        }

        public static string ReadCache(string table, string key)
        {
            if (!opened) return null;
            try
            {
                using (SQLiteConnection c = Connect())
                using (SQLiteCommand cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT payload FROM cache_rows WHERE table_name=@t AND row_key=@k;";
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.Parameters.AddWithValue("@k", key);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? null : o.ToString();
                }
            }
            catch (Exception ex) { log("EDGE store: ReadCache " + table + "/" + key + " failed: " + ex.Message); return null; }
        }

        public static long GetSyncRowversion(string table)
        {
            if (!opened) return 0;
            try
            {
                using (SQLiteConnection c = Connect())
                using (SQLiteCommand cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT rowversion FROM sync_state WHERE table_name=@t;";
                    cmd.Parameters.AddWithValue("@t", table);
                    object o = cmd.ExecuteScalar();
                    return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
                }
            }
            catch { return 0; }
        }

        public static void SetSyncState(string table, long rowversion, int rows, string note)
        {
            if (!opened) return;
            try
            {
                using (SQLiteConnection c = Connect())
                using (SQLiteCommand cmd = c.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO sync_state (table_name, rowversion, synced_at, rows, note) VALUES (@t, @v, @now, @r, @n) " +
                        "ON CONFLICT(table_name) DO UPDATE SET rowversion=excluded.rowversion, synced_at=excluded.synced_at, rows=excluded.rows, note=excluded.note;";
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.Parameters.AddWithValue("@v", rowversion);
                    cmd.Parameters.AddWithValue("@now", Now());
                    cmd.Parameters.AddWithValue("@r", rows);
                    cmd.Parameters.AddWithValue("@n", note ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { log("EDGE store: SetSyncState " + table + " failed: " + ex.Message); }
        }

        // ---------------------------------------------------------------- status

        /// <summary>One line for the log and the health view.</summary>
        public static string Summary()
        {
            if (!opened) return "edge store off";
            try
            {
                using (SQLiteConnection c = Connect())
                {
                    long queued = Convert.ToInt64(Scalar(c, "SELECT COUNT(*) FROM queue WHERE status='queued';"));
                    long printed = Convert.ToInt64(Scalar(c, "SELECT COUNT(*) FROM queue WHERE status='printed';"));
                    long failed = Convert.ToInt64(Scalar(c, "SELECT COUNT(*) FROM queue WHERE status='failed';"));
                    long cached = Convert.ToInt64(Scalar(c, "SELECT COUNT(*) FROM cache_rows;"));
                    long size = 0;
                    try { size = new FileInfo(dbPath).Length; } catch { }
                    return "queue " + queued + " open / " + printed + " printed / " + failed + " failed | cache rows " + cached
                        + " | " + (size / 1024) + " KB";
                }
            }
            catch (Exception ex) { return "edge store error: " + ex.Message; }
        }

        private static void Exec(SQLiteConnection c, string sql)
        {
            using (SQLiteCommand cmd = c.CreateCommand()) { cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
        }

        private static object Scalar(SQLiteConnection c, string sql)
        {
            using (SQLiteCommand cmd = c.CreateCommand()) { cmd.CommandText = sql; return cmd.ExecuteScalar(); }
        }

        private static string Now() { return Now(DateTime.Now); }
        private static string Now(DateTime t) { return t.ToString("yyyy-MM-dd HH:mm:ss"); }
    }
}
