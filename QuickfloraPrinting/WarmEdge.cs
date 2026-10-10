using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3471 (5.0.5): one Microsoft Edge kept running in the background per printer, told which
    /// file to print over Edge's own DevTools connection. Starting Edge for every ticket cost
    /// 3.4-4.0 s; a running Edge has the ticket in the Windows queue in about 1 s (Lenovo bench,
    /// 10 Oct 2026). One Edge per printer because Edge's silent (kiosk) printing keeps using the
    /// printer it printed to first and ignores later default-printer changes; each Edge is started
    /// while its own printer is the Windows default, so it always prints there.
    /// </summary>
    public static class WarmEdge
    {
        public static readonly string ProfileRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"QuickFloraPrint\edge-warm");

        private class Instance
        {
            public string Printer;
            public Process Proc;
            public ClientWebSocket Socket;
            public int NextId;
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Instance> Instances = new Dictionary<string, Instance>(StringComparer.OrdinalIgnoreCase);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDefaultPrinter(string name);

        /// <summary>Opens a page or file in the printer's warm Edge. False if Edge could not be reached.</summary>
        public static bool Navigate(string printer, string url, out string detail)
        {
            return Send(printer, "Page.navigate", "{\"url\":\"" + Escape(url) + "\"}", out detail);
        }

        /// <summary>Runs a line of script in the page currently open in the printer's warm Edge.</summary>
        public static bool Evaluate(string printer, string script, out string detail)
        {
            return Send(printer, "Runtime.evaluate", "{\"expression\":\"" + Escape(script) + "\"}", out detail);
        }

        private static bool Send(string printer, string method, string paramsJson, out string detail)
        {
            detail = "";
            lock (Gate)
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    Instance inst = null;
                    try
                    {
                        inst = GetOrStart(printer, ref detail);
                        if (inst == null) return false;
                        inst.NextId++;
                        string msg = "{\"id\":" + inst.NextId + ",\"method\":\"" + method + "\",\"params\":" + paramsJson + "}";
                        byte[] b = Encoding.UTF8.GetBytes(msg);
                        if (!inst.Socket.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, CancellationToken.None).Wait(5000))
                            throw new TimeoutException("send");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        detail += " warm-edge " + method + " failed(" + ex.GetType().Name + ")";
                        Drop(printer);   // dead Edge or connection: start a fresh one on the second try
                    }
                }
                return false;
            }
        }

        private static Instance GetOrStart(string printer, ref string detail)
        {
            Instance inst;
            if (Instances.TryGetValue(printer, out inst))
            {
                bool alive = false;
                try { alive = !inst.Proc.HasExited && inst.Socket.State == WebSocketState.Open; } catch { }
                if (alive) return inst;
                Drop(printer);
            }

            string edge = HtmlPrinter.EdgePath();
            if (edge == null) { detail += " EDGE NOT FOUND"; return null; }

            string profile = Path.Combine(ProfileRoot, SafeName(printer));
            KillEdgesUsing(profile);
            Directory.CreateDirectory(profile);

            // The first print of this Edge must happen with its printer as the Windows default.
            SetDefaultPrinter(printer);

            int port = FreePort();
            ProcessStartInfo psi = new ProcessStartInfo(edge,
                "--kiosk-printing --no-first-run --no-default-browser-check --disable-sync" +
                " --remote-debugging-address=127.0.0.1 --remote-debugging-port=" + port +
                " --window-position=-2000,-2000 --user-data-dir=\"" + profile + "\" --app=about:blank");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process proc = Process.Start(psi);

            string wsUrl = null;
            DateTime until = DateTime.Now.AddSeconds(15);
            while (wsUrl == null && DateTime.Now < until)
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        string list = wc.DownloadString("http://127.0.0.1:" + port + "/json");
                        Match m = Regex.Match(list, "\"webSocketDebuggerUrl\"\\s*:\\s*\"(ws://[^\"]+/devtools/page/[^\"]+)\"");
                        if (m.Success) wsUrl = m.Groups[1].Value;
                    }
                }
                catch { }
                if (wsUrl == null) Thread.Sleep(150);
            }
            if (wsUrl == null) { detail += " warm-edge did not start"; try { proc.Kill(); } catch { } return null; }

            ClientWebSocket ws = new ClientWebSocket();
            if (!ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None).Wait(5000)) { detail += " warm-edge connect timeout"; try { proc.Kill(); } catch { } return null; }

            inst = new Instance();
            inst.Printer = printer; inst.Proc = proc; inst.Socket = ws;
            Instances[printer] = inst;
            StartDraining(ws);
            detail += " warm-edge started";
            return inst;
        }

        /// <summary>Edge answers every command; read and discard the answers so its buffer never fills.</summary>
        private static void StartDraining(ClientWebSocket ws)
        {
            Thread t = new Thread(delegate ()
            {
                byte[] buf = new byte[16384];
                try
                {
                    while (ws.State == WebSocketState.Open)
                        ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).Wait();
                }
                catch { }
            });
            t.IsBackground = true; t.Name = "QF warm Edge reader";
            t.Start();
        }

        private static void Drop(string printer)
        {
            Instance inst;
            if (!Instances.TryGetValue(printer, out inst)) return;
            Instances.Remove(printer);
            try { inst.Socket.Abort(); } catch { }
            try { if (!inst.Proc.HasExited) inst.Proc.Kill(); } catch { }
        }

        /// <summary>Closes every warm Edge (app exit) and any left over from a previous run.</summary>
        public static void Shutdown()
        {
            lock (Gate)
            {
                foreach (string p in new List<string>(Instances.Keys)) Drop(p);
            }
            KillEdgesUsing(ProfileRoot);
        }

        private static void KillEdgesUsing(string folder)
        {
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='msedge.exe'"))
                {
                    foreach (ManagementObject p in s.Get())
                    {
                        string cmd = Convert.ToString(p["CommandLine"]);
                        if (cmd.IndexOf(folder, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        try { Process.GetProcessById(Convert.ToInt32(p["ProcessId"])).Kill(); } catch { }
                    }
                }
            }
            catch { }
        }

        private static int FreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private static string SafeName(string printer)
        {
            StringBuilder sb = new StringBuilder("p-");
            foreach (char c in printer) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            uint h = 2166136261;   // FNV-1a: same folder for the same printer on every run
            foreach (char c in printer.ToLowerInvariant()) { h ^= c; h *= 16777619; }
            sb.Append('-').Append(h.ToString("x8"));
            return sb.ToString();
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
