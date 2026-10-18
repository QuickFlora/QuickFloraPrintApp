using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Win32;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3958 (epic AB#3961): the push side. One persistent SignalR connection to qf-edge-hub on the
    /// QuickFlora web server; the hub pushes print jobs (and "a request is being turned into a job"
    /// nudges) for this terminal the moment they exist. While the hub is connected the app makes NO
    /// WaitForPrintJob / PingPOSForPrinting calls; the moment the connection drops the app goes back to
    /// the 6.0.0 long-poll, and comes back to push when the hub answers again.
    ///
    /// Hub URL: the print web service's host + /edgehub (so a PC pointed at staging talks to the
    /// staging hub). Override: HKCU\Software\QuickfloraPrinting\EdgeHubUrl. Kill switch: EdgeHubDisabled = 1.
    /// Optional shared key: EdgeHubKey (only needed when the hub's HubKey is set).
    /// Every callback is marshalled to the UI thread by the owner; nothing here touches the print path.
    /// </summary>
    internal sealed class EdgeHubClient
    {
        /// <summary>What the hub sends for one POSPrintProcess row (same fields CheckPOSForPrinting returns).</summary>
        public sealed class JobMessage
        {
            public long Slno;
            public string PrintText;
            public string PrintText1;
            public string PrintText2;
            public string FileName;
            public string OrderNumber;
            public DateTime QueuedAt;
        }

        private const string PrefKeyPath = @"Software\QuickfloraPrinting";
        private const int ReconnectSeconds = 15;

        private readonly Action<string> log;
        private readonly string url;
        private readonly string key;
        private HubConnection conn;
        private IHubProxy hub;
        private Timer retry;
        private bool stopped;
        private int starting;

        public event Action<JobMessage> JobReceived;
        public event Action<string> Nudged;
        public event Action<string> Alert;
        /// <summary>true = push is live (polling must stop); false = hub gone (polling must run).</summary>
        public event Action<bool> ConnectedChanged;

        public bool IsConnected { get { return conn != null && conn.State == ConnectionState.Connected; } }
        public string Url { get { return url; } }
        public DateTime ConnectedSince = DateTime.MinValue;
        public DateTime LastDrop = DateTime.MinValue;
        public int Drops;
        public long JobsPushed;

        public EdgeHubClient(string hubUrl, string hubKey, Action<string> logger)
        {
            url = hubUrl; key = hubKey ?? ""; log = logger ?? delegate { };
        }

        /// <summary>Where the hub is for this PC, or null when switched off.</summary>
        public static string ResolveUrl(string printServiceUrl)
        {
            try
            {
                using (RegistryKey pref = Registry.CurrentUser.OpenSubKey(PrefKeyPath))
                {
                    if (pref != null)
                    {
                        if (Convert.ToInt32(pref.GetValue("EdgeHubDisabled", 0)) != 0) return null;
                        string o = Convert.ToString(pref.GetValue("EdgeHubUrl", ""));
                        if (!string.IsNullOrEmpty(o)) return o.TrimEnd('/');
                    }
                }
            }
            catch { }
            if (string.IsNullOrEmpty(printServiceUrl)) return null;
            try
            {
                Uri u = new Uri(printServiceUrl);
                return u.Scheme + "://" + u.Authority + "/edgehub";
            }
            catch { return null; }
        }

        public static string ResolveKey()
        {
            try
            {
                using (RegistryKey pref = Registry.CurrentUser.OpenSubKey(PrefKeyPath))
                    return pref == null ? "" : Convert.ToString(pref.GetValue("EdgeHubKey", ""));
            }
            catch { return ""; }
        }

        public void Start()
        {
            stopped = false;
            Connect();
        }

        public void Stop()
        {
            stopped = true;
            try { if (retry != null) retry.Dispose(); } catch { }
            try { if (conn != null) conn.Stop(); } catch { }
        }

        private void Connect()
        {
            if (stopped) return;
            if (Interlocked.Exchange(ref starting, 1) == 1) return;
            try
            {
                Dictionary<string, string> qs = new Dictionary<string, string>();
                qs["c"] = Program.CompanyID ?? "";
                qs["d"] = Program.DivisionID ?? "";
                qs["p"] = Program.DepartmentID ?? "";
                qs["t"] = Program.TerminalName ?? "";
                if (key.Length > 0) qs["k"] = key;
                HubConnection c = new HubConnection(url, qs);
                c.TransportConnectTimeout = TimeSpan.FromSeconds(15);
                IHubProxy h = c.CreateHubProxy("EdgeHub");
                h.On<JobMessage>("Job", OnJob);
                h.On<string>("Nudge", delegate (string reason) { if (Nudged != null) Nudged(reason); });
                h.On<string>("Alert", delegate (string text) { if (Alert != null) Alert(text); });
                h.On<string>("Reject", delegate (string why) { log("HUB rejected this PC: " + why); });
                c.StateChanged += OnStateChanged;
                c.Closed += OnClosed;
                c.Error += delegate (Exception ex) { log("HUB error: " + Short(ex)); };
                conn = c; hub = h;
                log("HUB connecting to " + url + " as " + Program.CompanyID + "/" + Program.DivisionID + "/" + Program.DepartmentID + "/" + Program.TerminalName);
                c.Start().ContinueWith(delegate (Task t)
                {
                    Interlocked.Exchange(ref starting, 0);
                    if (t.IsFaulted)
                    {
                        log("HUB not reachable (" + Short(t.Exception) + ") - staying on the long-poll, retry in " + ReconnectSeconds + " s");
                        ScheduleRetry();
                        return;
                    }
                    try { h.Invoke("Hello", Program.AppVersion, Environment.MachineName); } catch { }
                });
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref starting, 0);
                log("HUB start failed: " + ex.Message);
                ScheduleRetry();
            }
        }

        private void ScheduleRetry()
        {
            if (stopped) return;
            try { if (retry != null) retry.Dispose(); } catch { }
            retry = new Timer(delegate { Connect(); }, null, TimeSpan.FromSeconds(ReconnectSeconds), TimeSpan.FromMilliseconds(-1));
        }

        private void OnStateChanged(StateChange s)
        {
            if (s.NewState == ConnectionState.Connected)
            {
                ConnectedSince = DateTime.Now;
                log("HUB connected (" + (conn.Transport == null ? "?" : conn.Transport.Name) + ") - push mode, polling stopped");
                if (ConnectedChanged != null) ConnectedChanged(true);
            }
            else if (s.OldState == ConnectionState.Connected)
            {
                LastDrop = DateTime.Now; Drops++;
                log("HUB connection lost (" + s.NewState + ") - back to the long-poll until it returns");
                if (ConnectedChanged != null) ConnectedChanged(false);
            }
        }

        private void OnClosed()
        {
            // The client gave up reconnecting on its own (hub down longer than its reconnect window).
            if (stopped) return;
            log("HUB closed - retry in " + ReconnectSeconds + " s");
            ScheduleRetry();
        }

        private void OnJob(JobMessage j)
        {
            JobsPushed++;
            if (JobReceived != null) JobReceived(j);
        }

        /// <summary>Tell the hub this PC stored and claimed the job (so it is never offered again).</summary>
        public void Claimed(long slno)
        {
            try { if (IsConnected) hub.Invoke("Claimed", slno); } catch (Exception ex) { log("HUB Claimed(" + slno + ") failed: " + ex.Message); }
        }

        private static string Short(Exception ex)
        {
            if (ex == null) return "";
            AggregateException ag = ex as AggregateException;
            if (ag != null && ag.InnerException != null) ex = ag.InnerException;
            if (ex.InnerException != null && ex.InnerException.Message.Length > 0) return ex.InnerException.Message;
            return ex.Message;
        }
    }
}
