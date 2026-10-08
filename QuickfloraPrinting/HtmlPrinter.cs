using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3170 (v4-8): print the server's HTML work ticket through Microsoft Edge instead of the
    /// Crystal PDF through Adobe Reader. Alex chose Edge ("B") on 7 Oct 2026 after comparing both
    /// HTML routes on the Lenovo + Canon G6010: ~3.4 s to reach the printer vs ~10-11 s for Adobe,
    /// with the modern rendering his approved layout (wt-v2-forms) was designed in.
    ///
    /// Edge is on every Windows 10/11 PC. --kiosk-printing prints silently to the DEFAULT printer,
    /// so the caller sets the default printer first (the PDF path already does the same for Adobe).
    /// Edge runs with its own profile folder so it never touches the shop's own Edge.
    /// </summary>
    public static class HtmlPrinter
    {
        private static readonly string ProfileDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"QuickFloraPrint\edge");

        public static string EdgePath()
        {
            string[] candidates = new string[] {
                Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? @"C:\Program Files (x86)", @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files", @"Microsoft\Edge\Application\msedge.exe") };
            foreach (string c in candidates)
                if (File.Exists(c)) return c;
            return null;
        }

        /// <summary>Downloads the HTML twin of a PDF. False when it does not exist (older report) or looks empty.</summary>
        public static bool TryDownload(string url, string path)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    byte[] data = wc.DownloadData(url);
                    if (data == null || data.Length < 200) return false;
                    string head = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 4000)).ToLowerInvariant();
                    if (head.IndexOf("<html") < 0 && head.IndexOf("<!doctype html") < 0) return false;
                    string dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllBytes(path, data);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>The page title, which is the document name Edge gives the print job.</summary>
        public static string TitleOf(string path)
        {
            try
            {
                Match m = Regex.Match(File.ReadAllText(path, Encoding.UTF8), @"<title>\s*(.*?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (m.Success && m.Groups[1].Value.Trim().Length > 0) return System.Net.WebUtility.HtmlDecode(m.Groups[1].Value.Trim());
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Prints through Edge and waits (max 25 s) until Windows has the job, then closes Edge.
        /// Returns false if the job never reached the queue, so the caller can fall back to the PDF.
        /// </summary>
        public static bool Print(string htmlPath, string printer, string docTitle, out string detail)
        {
            DateTime started = DateTime.Now;
            string edge = EdgePath();
            if (edge == null) { detail = " html=EDGE NOT FOUND"; return false; }

            string printFile = htmlPath + ".print.html";
            try
            {
                string html = File.ReadAllText(htmlPath, Encoding.UTF8);
                const string trigger = "<script>window.addEventListener('load',function(){setTimeout(function(){window.print();},50);});</script>";
                int at = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                html = at >= 0 ? html.Insert(at, trigger) : html + trigger;
                File.WriteAllText(printFile, html, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                detail = " html=PREPARE FAILED(" + ex.GetType().Name + ")";
                return false;
            }

            try
            {
                if (!Directory.Exists(ProfileDir)) Directory.CreateDirectory(ProfileDir);
                ProcessStartInfo psi = new ProcessStartInfo(edge,
                    "--kiosk-printing --no-first-run --no-default-browser-check --disable-sync" +
                    " --window-position=-2000,-2000 --user-data-dir=\"" + ProfileDir + "\"" +
                    " --app=\"" + new Uri(printFile).AbsoluteUri + "\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                detail = " html=EDGE START FAILED(" + ex.GetType().Name + ")";
                return false;
            }

            bool seen = false;
            while ((DateTime.Now - started).TotalSeconds < 25)
            {
                int mask;
                if (JobInQueue(printer, docTitle, started, out mask) || SeenByWatcher(printer, docTitle))
                {
                    seen = true;
                    // Let Edge finish spooling (JOB_STATUS_SPOOLING = 8) before closing it.
                    DateTime spoolStart = DateTime.Now;
                    while ((DateTime.Now - spoolStart).TotalSeconds < 10 && JobInQueue(printer, docTitle, started, out mask) && (mask & 8) != 0)
                        Thread.Sleep(200);
                    break;
                }
                Thread.Sleep(200);
            }
            CloseEdge();
            try { File.Delete(printFile); } catch { }
            detail = seen
                ? " html=edge spooled " + (DateTime.Now - started).TotalSeconds.ToString("0.0") + "s"
                : " html=edge NO JOB after 25s";
            return seen;
        }

        private static bool SeenByWatcher(string printer, string docTitle)
        {
            try
            {
                PrintHome.SpoolWatch.Sighting s = PrintHome.SpoolWatch.Get(printer, docTitle);
                return s != null && s.Seen;
            }
            catch { return false; }
        }

        private static bool JobInQueue(string printer, string docTitle, DateTime since, out int mask)
        {
            mask = 0;
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, Document, StatusMask, TimeSubmitted FROM Win32_PrintJob"))
                {
                    foreach (ManagementObject j in s.Get())
                    {
                        string name = Convert.ToString(j["Name"]);
                        int comma = name.LastIndexOf(',');
                        if (comma <= 0 || !name.Substring(0, comma).Equals(printer, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!Convert.ToString(j["Document"]).Equals(docTitle, StringComparison.OrdinalIgnoreCase)) continue;
                        DateTime t = ManagementDateTimeConverter.ToDateTime(Convert.ToString(j["TimeSubmitted"]));
                        if (t < since.AddSeconds(-5)) continue;
                        mask = Convert.ToInt32(j["StatusMask"]);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>Closes only the Edge started by the print app (identified by its profile folder).</summary>
        private static void CloseEdge()
        {
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='msedge.exe'"))
                {
                    foreach (ManagementObject p in s.Get())
                    {
                        string cmd = Convert.ToString(p["CommandLine"]);
                        if (cmd.IndexOf(ProfileDir, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        try { Process.GetProcessById(Convert.ToInt32(p["ProcessId"])).Kill(); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
