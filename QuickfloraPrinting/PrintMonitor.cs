using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3164 (v4-3): tell the Print Monitor (Supabase project print-monitor, edge function
    /// print-ingest) how every ticket ended - Printed or Not printed, with the reason - plus a
    /// heartbeat every 30 s and any errors. Same endpoint and key PrinterWatch already uses, so
    /// no server or database change.
    ///
    /// A shop is often offline exactly when something goes wrong, so job and error rows are kept
    /// in Logs\monitor-queue.txt until the endpoint accepts them, and resent on the next beat.
    /// Everything here is wrapped: the Print Monitor being down can never slow or stop printing.
    /// </summary>
    internal static class PrintMonitor
    {
        private const string IngestUrl = "https://lvybdxahpekdpdvxmyzz.supabase.co/functions/v1/print-ingest";
        private const int MaxQueued = 500;     // per kind; the endpoint takes at most 500 per call
        private const int MaxPerSend = 200;

        private static readonly object Lock = new object();
        private static readonly List<string> jobs = new List<string>();     // JSON objects
        private static readonly List<string> errors = new List<string>();
        private static readonly DateTime started = DateTime.Now;
        private static bool loaded;
        private static bool sending;
        private static string key;
        private static DateTime keyLookedAt = DateTime.MinValue;
        private static string lastPrinters = "";
        private static DateTime lastPrintersSent = DateTime.MinValue;

        /// <summary>One finished ticket. slno 0 = a local reprint, not a server job.</summary>
        public static void Job(DateTime sentAt, int slno, string form, string file, string printer, bool printed, string detail)
        {
            try
            {
                Add(jobs, "{" + J("printed_at", Iso(sentAt)) + "," + J("terminal_name", Program.TerminalName)
                    + "," + J("doc_type", form) + "," + J("file_name", file) + ",\"slno\":" + slno.ToString(CultureInfo.InvariantCulture)
                    + "," + J("printer_name", printer) + ",\"sent_ok\":" + (printed ? "true" : "false")
                    + "," + J("detail", detail) + "}");
            }
            catch { }
        }

        public static void Error(string location, string order, string printer, Exception ex)
        {
            try
            {
                Add(errors, "{" + J("occurred_at", Iso(DateTime.Now)) + "," + J("terminal_name", Program.TerminalName)
                    + "," + J("location", location) + "," + J("order_ref", order) + "," + J("printer_name", printer)
                    + "," + J("error_type", ex == null ? "" : ex.GetType().Name) + "," + J("message", ex == null ? "" : ex.Message)
                    + "," + J("detail", ex == null ? "" : ex.ToString()) + "}");
            }
            catch { }
        }

        /// <summary>
        /// Called every 30 s from the UI thread with a snapshot of the screen's state; the POST runs
        /// on a pool thread. printers = name, state, severity (as shown on the Printers list). They
        /// are sent only when they change or every 15 min, to keep printer_status small.
        /// </summary>
        public static void Beat(bool serverReachable, DateTime lastPrint, int waitingForPrinter, int notPrintedLastHour,
                                string defaultPrinter, List<string[]> printers)
        {
            try
            {
                lock (Lock) { if (sending) return; sending = true; }
                string pj = PrintersJson(defaultPrinter, printers);
                bool sendPrinters = pj.Length > 2 && (pj != lastPrinters || (DateTime.Now - lastPrintersSent).TotalMinutes >= 15);
                string hb = "{\"server_reachable\":" + (serverReachable ? "true" : "false")
                    + (lastPrint == DateTime.MinValue ? "" : "," + J("last_print_at", Iso(lastPrint)))
                    + ",\"uptime_seconds\":" + ((long)(DateTime.Now - started).TotalSeconds).ToString(CultureInfo.InvariantCulture)
                    + ",\"waiting_for_printer\":" + waitingForPrinter.ToString(CultureInfo.InvariantCulture)
                    + ",\"not_printed_last_hour\":" + notPrintedLastHour.ToString(CultureInfo.InvariantCulture)
                    + "," + J("source", "print-app") + "}";
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        if (Send(hb, sendPrinters ? pj : null, defaultPrinter) && sendPrinters)
                        {
                            lastPrinters = pj;
                            lastPrintersSent = DateTime.Now;
                        }
                    }
                    catch { }
                    finally { lock (Lock) { sending = false; } }
                });
            }
            catch { lock (Lock) { sending = false; } }
        }

        private static bool Send(string heartbeat, string printersJson, string defaultPrinter)
        {
            string k = Key();
            if (k == null) return false;
            Load();
            List<string> j, e;
            lock (Lock)
            {
                j = jobs.GetRange(0, Math.Min(MaxPerSend, jobs.Count));
                e = errors.GetRange(0, Math.Min(MaxPerSend, errors.Count));
            }
            string body = "{" + J("company_id", Program.CompanyID) + "," + J("division_id", Program.DivisionID)
                + "," + J("department_id", Program.DepartmentID) + "," + J("machine_name", Environment.MachineName)
                + "," + J("terminal_name", Program.TerminalName) + "," + J("app_version", Program.AppVersion.TrimStart('v'))
                + "," + J("os_version", Environment.OSVersion.VersionString) + "," + J("default_printer", defaultPrinter)
                + ",\"heartbeat\":" + heartbeat
                + (printersJson == null ? "" : ",\"printers\":" + printersJson)
                + ",\"jobs\":[" + string.Join(",", j.ToArray()) + "]"
                + ",\"errors\":[" + string.Join(",", e.ToArray()) + "]}";
            try
            {
                // .NET 4.0 has no Tls12 name; 3072 is TLS 1.2 (present when 4.5+ is installed, as on Windows 10/11).
                try { System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072; } catch { }
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(IngestUrl);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 15000;
                req.Headers["x-ingest-key"] = k;
                byte[] data = Encoding.UTF8.GetBytes(body);
                req.ContentLength = data.Length;
                using (System.IO.Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
                {
                    if ((int)resp.StatusCode != 200) return false;
                }
            }
            catch (System.Net.WebException ex)
            {
                // 401 = wrong key: look for a new one next time instead of every 30 s with the old one.
                System.Net.HttpWebResponse r = ex.Response as System.Net.HttpWebResponse;
                if (r != null && (int)r.StatusCode == 401) { key = null; keyLookedAt = DateTime.MinValue; }
                return false;
            }
            catch { return false; }

            if (j.Count > 0 || e.Count > 0)
            {
                lock (Lock)
                {
                    jobs.RemoveRange(0, Math.Min(j.Count, jobs.Count));
                    errors.RemoveRange(0, Math.Min(e.Count, errors.Count));
                }
                Save();
            }
            return true;
        }

        /// <summary>
        /// The fleet's write-only ingest key. The app runs as the shop user, who cannot read
        /// PrinterWatch's copy (SYSTEM and Administrators only), so install-printapp.ps1 puts a copy
        /// beside the exe. No key = no reporting; printing is unaffected. Looked up every 10 min.
        /// </summary>
        private static string Key()
        {
            if (key != null) return key;
            if ((DateTime.Now - keyLookedAt).TotalMinutes < 10) return null;
            keyLookedAt = DateTime.Now;
            string[] places = {
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "monitor.key"),
                @"C:\ProgramData\PrinterWatch\ingest.key"
            };
            foreach (string p in places)
            {
                try
                {
                    if (!System.IO.File.Exists(p)) continue;
                    string k = System.IO.File.ReadAllText(p).Trim();
                    if (k.Length >= 16) { key = k; return key; }
                }
                catch { }
            }
            return null;
        }

        private static string QueueFile
        {
            get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), @"Logs\monitor-queue.txt"); }
        }

        private static void Add(List<string> list, string row)
        {
            Load();
            lock (Lock)
            {
                list.Add(row);
                if (list.Count > MaxQueued) list.RemoveAt(0);   // oldest first; keep the recent picture
            }
            Save();
        }

        /// <summary>Lines are "J {json}" or "E {json}". Read once, after a restart.</summary>
        private static void Load()
        {
            lock (Lock)
            {
                if (loaded) return;
                loaded = true;
                try
                {
                    if (!System.IO.File.Exists(QueueFile)) return;
                    foreach (string l in System.IO.File.ReadAllLines(QueueFile))
                    {
                        if (l.StartsWith("J {")) jobs.Add(l.Substring(2));
                        else if (l.StartsWith("E {")) errors.Add(l.Substring(2));
                    }
                    while (jobs.Count > MaxQueued) jobs.RemoveAt(0);
                    while (errors.Count > MaxQueued) errors.RemoveAt(0);
                }
                catch { }
            }
        }

        private static void Save()
        {
            lock (Lock)
            {
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(QueueFile);
                    if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                    StringBuilder sb = new StringBuilder();
                    foreach (string r in jobs) sb.Append("J ").Append(r).Append("\r\n");
                    foreach (string r in errors) sb.Append("E ").Append(r).Append("\r\n");
                    System.IO.File.WriteAllText(QueueFile, sb.ToString());
                }
                catch { }
            }
        }

        private static string PrintersJson(string defaultPrinter, List<string[]> printers)
        {
            StringBuilder sb = new StringBuilder("[");
            if (printers != null)
            {
                foreach (string[] p in printers)
                {
                    if (p == null || p.Length < 2) continue;
                    if (sb.Length > 1) sb.Append(",");
                    string st = p[1] ?? "";
                    sb.Append("{" + J("printer_name", p[0])
                        + ",\"is_default\":" + (string.Equals(p[0], defaultPrinter, StringComparison.OrdinalIgnoreCase) ? "true" : "false")
                        + ",\"is_offline\":" + (st.IndexOf("Offline", StringComparison.OrdinalIgnoreCase) >= 0 ? "true" : "false")
                        + ",\"paper_out\":" + (st.IndexOf("Out of paper", StringComparison.OrdinalIgnoreCase) >= 0 ? "true" : "false")
                        + ",\"jammed\":" + (st.IndexOf("Jammed", StringComparison.OrdinalIgnoreCase) >= 0 ? "true" : "false")
                        + "," + J("error_state", st) + "}");
                }
            }
            return sb.Append("]").ToString();
        }

        private static string Iso(DateTime t)
        {
            return t.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>"name":"value" with JSON escaping (no JSON library on .NET 4.0 without extra references).</summary>
        private static string J(string name, string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"').Append(name).Append("\":");
            if (value == null) return sb.Append("null").ToString();
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
