using System;
using System.Threading;

namespace QuickfloraPrinting
{
    /// <summary>
    /// Where cached reference data comes from. One implementation per server-side endpoint.
    /// Pull(sinceRowversion) returns how many rows it wrote through EdgeStore.UpsertCache and the
    /// highest rowversion it saw, so the next pull is incremental.
    /// </summary>
    internal interface ICacheSource
    {
        string Table { get; }
        int Pull(long sinceRowversion, out long newRowversion);
    }

    /// <summary>
    /// AB#3963 (epic AB#3961): the print app is the shop PC's sync agent.
    /// Runs one cache pull per source every hour (first one a minute after start), on a background
    /// thread, and records the result in sync_state. Sources are registered by the code that owns
    /// their server endpoint; until the push hub (AB#3958) and the pull endpoints exist there are none,
    /// and the agent only keeps a heartbeat row so a support person can see it is alive.
    /// Never touches the print path.
    /// </summary>
    internal static class EdgeSync
    {
        private static readonly object Gate = new object();
        private static readonly System.Collections.Generic.List<ICacheSource> sources = new System.Collections.Generic.List<ICacheSource>();
        private static Timer timer;
        private static Action<string> log = delegate { };
        private static bool running;
        private static DateTime lastRun = DateTime.MinValue;

        public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public static DateTime LastRun { get { return lastRun; } }

        public static void Register(ICacheSource source)
        {
            lock (Gate) { if (source != null) sources.Add(source); }
        }

        public static void Start(Action<string> logger)
        {
            if (logger != null) log = logger;
            if (!EdgeStore.Enabled) { log("EDGE sync: not started (store is off)"); return; }
            lock (Gate)
            {
                if (timer != null) return;
                timer = new Timer(delegate { RunNow(); }, null, TimeSpan.FromMinutes(1), Interval);
            }
            log("EDGE sync: scheduled every " + Interval.TotalMinutes + " min, " + sources.Count + " source(s)");
        }

        public static void Stop()
        {
            lock (Gate)
            {
                if (timer != null) { timer.Dispose(); timer = null; }
            }
        }

        /// <summary>One pass over every source. Safe to call from any thread; overlapping calls are skipped.</summary>
        public static void RunNow()
        {
            lock (Gate)
            {
                if (running) return;
                running = true;
            }
            try
            {
                ICacheSource[] list;
                lock (Gate) { list = sources.ToArray(); }
                int total = 0;
                foreach (ICacheSource s in list)
                {
                    try
                    {
                        long since = EdgeStore.GetSyncRowversion(s.Table);
                        long upTo;
                        int n = s.Pull(since, out upTo);
                        EdgeStore.SetSyncState(s.Table, upTo, n, "ok");
                        total += n;
                        log("EDGE sync: " + s.Table + " +" + n + " rows (rowversion " + since + " -> " + upTo + ")");
                    }
                    catch (Exception ex)
                    {
                        EdgeStore.SetSyncState(s.Table, EdgeStore.GetSyncRowversion(s.Table), 0, "error: " + ex.Message);
                        log("EDGE sync: " + s.Table + " failed: " + ex.Message);
                    }
                }
                EdgeStore.SetSyncState("_heartbeat", 0, list.Length, Program.AppVersion + " pc=" + Environment.MachineName);
                EdgeStore.Trim();
                lastRun = DateTime.Now;
                if (list.Length == 0) log("EDGE sync: heartbeat only - no cache sources yet (pull endpoints come with the push hub, AB#3958)");
                else log("EDGE sync: done, " + total + " rows | " + EdgeStore.Summary());
            }
            finally
            {
                lock (Gate) { running = false; }
            }
        }
    }
}
