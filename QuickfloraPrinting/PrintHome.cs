using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Drawing.Printing;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace QuickfloraPrinting
{
    public partial class PrintHome : Form
    {
        private bool startMinimized;
        private bool loadingSettings;

        // AB#3189 (v3.5) — screen state. Display only; the print loop does not read any of this.
        private string configPath = "";
        private DateTime pingStarted = DateTime.MinValue;
        private DateTime lastPingOk = DateTime.MinValue;
        private bool pingFailing;
        private int pingFailures;
        private double lastPingSeconds;
        private JobRow lastJob;
        private string printerProblem;      // fault on a printer orders are going to, or null when fine
        private string problemPrinter;      // which printer that is
        private int problemWaiting;         // jobs Windows is still holding for it
        private string rmmState;            // null until the first check has run
        private readonly List<JobRow> jobs = new List<JobRow>();
        private readonly List<JobRow> pendingConfirm = new List<JobRow>();   // AB#3164
        private bool confirmBusy;
        private System.Windows.Forms.Timer timerMonitor;   // AB#3164: Print Monitor heartbeat
        private List<string[]> monitorPrinters;            // last Printers list shown, for the heartbeat

        public PrintHome(bool startMinimized)
        {
            this.startMinimized = startMinimized;
            InitializeComponent();
        }

       
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }

            // Activate the form.
            this.Activate();
            this.Focus();
        }

        private void PrintHome_FormClosing(object sender, FormClosingEventArgs e)
        {
            //There are several ways to close an application.
            //We are trying to find the click of the X in the upper right hand corner
            //We will only allow the closing of this app if it is minized.


            if (this.WindowState != FormWindowState.Minimized)
            {
                //we don't close the app...
                e.Cancel = true;
                //minimize the app and then display a message to the user so
                //they understand they didn't close the app they just sent it to the tray.
                this.WindowState = FormWindowState.Minimized;
                //Show the message.
                notifyIcon1.ShowBalloonTip(3000, Program.WindowTitle,
                    "QuickFlora Printing Process is running." +
                    (Char)(13) + "It has be moved to the tray." +
                    (Char)(13) + "Right click the Icon to exit.",
                    ToolTipIcon.Info);
            }
        }

        private void PrintHome_Move(object sender, EventArgs e)
        {
            //This code causes the form to not show up on the task bar only in the tray.
            //NOTE there is now a form property that will allow you to keep the application
            //from every showing up in the task bar.
            if (this == null)
            { //This happen on create.
                return;
            }
            //If we are minimizing the form then hide it so it doesn't show up on the task bar
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.ShowBalloonTip(3000, Program.WindowTitle,
                    "QuickFlora Printing Process is running.",
                    ToolTipIcon.Info);
            }
            else
            {//any other windows state show it.
                this.Show();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult y;
            y = MessageBox.Show("Are you sure to Exit?", Program.Caption("Please confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (y.ToString().ToUpper() == "YES")
                Application.ExitThread();
        }

        private void autoStartToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings)
                return;
            Program.SetAutoStart(autoStartToolStripMenuItem.Checked);
        }

        /// <summary>
        /// AB#1325: locate Config.txt without a hardcoded C: path.
        ///
        /// This was pinned to C:\QFPrintApp\QuickfloraPrinting\Config.txt since 2020, so the app
        /// only worked if installed to that exact location. A tester hit it immediately when the
        /// files were unpacked one folder higher (AB#1327, 12 Aug 2026).
        ///
        /// Now: look beside the exe first, then the legacy locations, so existing installs keep
        /// working untouched while new ones can live anywhere.
        /// </summary>
        /// <summary>
        /// AB#1326: make sure a download target folder exists before writing to it.
        ///
        /// Receipt and PDF paths are hardcoded to C:\QFPrintApp\Receipts and C:\QFPrintApp\PDF.
        /// On a fresh install those folders may not exist, WebClient.DownloadFile throws, the
        /// exception is swallowed, and NOTHING PRINTS with no visible error. That is exactly what
        /// a tester hit on a clean machine while an older machine worked, because the folders had
        /// been created by earlier manual installs.
        /// </summary>
        /// <summary>
        /// AB#1323 — local rolling log beside the exe. Survives with no internet, which is often
        /// exactly when it is needed. One file per day, 14 days kept.
        /// </summary>
        private static readonly object LogLock = new object();
        private static void WriteToFile(string message)
        {
            try
            {
                string dir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.ExecutablePath), "Logs");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                try
                {
                    foreach (string f in System.IO.Directory.GetFiles(dir, "AppLog_*.txt"))
                    {
                        if (System.IO.File.GetLastWriteTime(f) < DateTime.Now.AddDays(-14))
                            System.IO.File.Delete(f);
                    }
                }
                catch { }
                string path = System.IO.Path.Combine(dir,
                    "AppLog_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt");
                lock (LogLock)
                {
                    System.IO.File.AppendAllText(path,
                        DateTime.Now.ToString("HH:mm:ss") + "  " + message + "\r\n");
                }
            }
            catch { }
        }

        /// <summary>
        /// AB#1323 — report an exception to the server instead of discarding it.
        /// Every catch in this file previously swallowed its exception; POSPrintAppError had
        /// recorded nothing since February 2020. Two faults this week were found only because a
        /// tester noticed, not because the app told anyone.
        /// Wrapped so a telemetry failure can NEVER stop a receipt printing.
        /// </summary>
        private void ReportError(string location, string order, Exception ex)
        {
            try { WriteToFile("ERROR [" + location + "] order=" + order + " : " + ex.ToString()); }
            catch { }
            try
            {
                QFPrintService.QFPrintService svc = new QFPrintService.QFPrintService();
                svc.InsertErrorDetails(
                    Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName,
                    order == null ? "" : order, ex.Message,
                    location + " | printer=" + txtdefaultprinter.Text + " | " + ex.StackTrace);
            }
            catch { }
            PrintMonitor.Error(location, order, txtdefaultprinter.Text, ex);
        }

        /// <summary>
        /// 4.0.2: tell the server what the shop PC knows, for the POSN Order Print Logs page.
        /// Goes through the existing InsertErrorDetails web method into POSPrintAppError, so no server or
        /// database change. [Error] holds the event name the page looks for; Details is "key=value | ...".
        /// Only exceptions to the normal flow are sent (not every printed job) plus a status line every
        /// 30 minutes, to keep the table small. Asynchronous and wrapped: never slows or stops printing.
        /// </summary>
        private void ReportEvent(string evt, string file, string details)
        {
            try
            {
                // POSPrintAppError.[Order] is nvarchar(50); a longer value makes the insert fail, so the
                // full file name also goes into Details.
                string f = file == null ? "" : file;
                string order = f.Length > 50 ? f.Substring(0, 50) : f;
                QFPrintService.QFPrintService svc = new QFPrintService.QFPrintService();
                svc.InsertErrorDetailsAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName,
                    order, evt, "v=" + Program.AppVersion.TrimStart('v') + (f.Length > 0 ? " | file=" + f : "") + " | " + details);
            }
            catch { }
        }

        /// <summary>Why a job did not print, from Windows' job flags (StatusMask) and whether it was ever seen.</summary>
        private static string NotPrintedReason(JobRow j, double age)
        {
            if ((j.LastMask & 32) != 0) return "printer offline";
            if ((j.LastMask & 64) != 0) return "printer out of paper";
            if ((j.LastMask & 1024) != 0) return "printer needs attention";
            if ((j.LastMask & 2) != 0) return "printer error";
            if ((j.LastMask & 512) != 0) return "print queue blocked";
            if (!j.Seen) return "no print job reached Windows within 2 minutes";
            if (j.LastWasError) return "removed from the Windows queue after an error";
            if (age > 1800) return "still not printed after 30 minutes";
            return "not confirmed";
        }

        /// <summary>
        /// AB#1323 — record what was actually sent to the printer, including whether the
        /// cash-drawer byte (0x07) was present. A production incident took a day to answer that
        /// question by reading raw bytes off a server; this line answers it in seconds.
        /// Reads the file only — the bytes sent to the printer are never altered.
        /// </summary>
        /// <summary>
        /// AB#1323 — inspect a downloaded print file.
        ///
        /// MUST be called BEFORE the file is sent to the printer. v3.2 read it afterwards, when the
        /// spooler still holds the handle: ReadAllBytes threw, the failure was swallowed, and the
        /// line reported bytes=0 — which then read as drawerByte=no. In Daman's 15 Aug test that
        /// looked like a finding about the cash drawer when in truth nothing had been read at all.
        /// A diagnostic that invents an answer is worse than no diagnostic.
        ///
        /// Never reports 0 for an unread file: NOFILE and UNREADABLE are distinct from a real zero.
        /// </summary>
        private static string InspectPrintFile(string filePath, out bool hasDrawer, out long size)
        {
            hasDrawer = false;
            size = -1;
            bool hasStarCut = false, hasEpsonCut = false;
            try
            {
                if (!System.IO.File.Exists(filePath)) return " bytes=NOFILE";
                byte[] b = System.IO.File.ReadAllBytes(filePath);
                size = b.Length;
                for (int i = 0; i < b.Length; i++)
                {
                    if (b[i] == 0x07) hasDrawer = true;
                    if (i + 2 < b.Length && b[i] == 0x1B && b[i + 1] == 0x64 && b[i + 2] == 0x30) hasStarCut = true;
                    if (i + 1 < b.Length && b[i] == 0x1D && b[i + 1] == 0x56) hasEpsonCut = true;
                }
            }
            catch (Exception ex)
            {
                return " bytes=UNREADABLE(" + ex.GetType().Name + ")";
            }
            return " bytes=" + size
                + " drawerByte=" + (hasDrawer ? "YES" : "no")
                + " starCut=" + (hasStarCut ? "YES" : "no")
                + " epsonCut=" + (hasEpsonCut ? "YES" : "no");
        }

        /// <summary>
        /// AB#1323 — record one print job.
        ///
        /// `type` is the document kind the server queued ("Text" = raw receipt, "PDF" = worksheet or
        /// card), NOT an order number. v3.2 labelled this field order= and so logged "order=Text",
        /// which told the reader nothing and implied a lookup that does not exist. The queue row
        /// carries no order number; the filename and slno are what identify the job.
        /// </summary>
        private void LogPrintJob(string type, string fileName, int slno, string printer,
                                 string detail, bool hasDrawer, long size, bool sent)
        {
            try
            {
                string line = "PRINT type=" + type + " file=" + fileName + " slno=" + slno
                    + " printer=" + printer + detail
                    + " sent=" + (sent ? "ok" : "FAILED");
                WriteToFile(line);

                // Only a raw receipt can carry a drawer command — a PDF never does — and an unread
                // file proves nothing either way. Reporting those would be a false alarm, which is
                // how v3.2 produced a drawerByte=no that meant nothing.
                if (type == "Text" && size > 0 && !hasDrawer)
                {
                    try
                    {
                        QFPrintService.QFPrintService svc = new QFPrintService.QFPrintService();
                        svc.InsertErrorDetails(
                            Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName,
                            fileName == null ? "" : fileName,
                            "Receipt contained no cash-drawer command", line);
                    }
                    catch { }
                }
            }
            catch { }
        }
        private static void EnsureFolderFor(string filePath)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
            }
            catch { }
        }

        private static string ResolveConfigPath()
        {
            System.Collections.Generic.List<string> candidates = new System.Collections.Generic.List<string>();
            string exeDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath);

            candidates.Add(System.IO.Path.Combine(exeDir, "Config.txt"));
            candidates.Add(System.IO.Path.Combine(exeDir, "QuickfloraPrinting"));
            candidates[1] = System.IO.Path.Combine(candidates[1], "Config.txt");
            candidates.Add("C:\\QFPrintApp\\QuickfloraPrinting\\Config.txt");
            candidates.Add("C:\\QFPrintApp\\Config.txt");

            foreach (string c in candidates)
            {
                try { if (System.IO.File.Exists(c)) return c; }
                catch { }
            }

            // AB#1326: nothing configured yet — offer setup rather than showing an error
            // about a file the user has never heard of.
            string preferred = candidates[0];
            using (SetupForm setup = new SetupForm(preferred))
            {
                if (setup.ShowDialog() == DialogResult.OK && System.IO.File.Exists(preferred))
                {
                    return preferred;
                }
            }

            MessageBox.Show(
                "QuickFlora Print has not been set up yet, so it cannot start.\r\n\r\n" +
                "Run it again and complete the setup, or email support@quickflora.com.",
                Program.Caption("Setup not completed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return candidates[0];
        }

        private void PrintHome_Load(object sender, EventArgs e)
        {
            // Reflect the current auto-start state in the tray menu without
            // firing the CheckedChanged handler while we set it.
            loadingSettings = true;
            autoStartToolStripMenuItem.Checked = Program.IsAutoStartEnabled();
            loadingSettings = false;

            // AB#1327: version visible so a support call can start with
            // "what does the bottom right say?" instead of guessing the build.
            lblVersion.Text = "v" + Application.ProductVersion;
            SetStatus("Starting up", "Connecting to QuickFlora", false);

            // Launched by the Windows auto-start entry: go straight to the
            // tray so staff are not interrupted.
            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }

            timer1.Enabled = true;

            configPath = ResolveConfigPath();
            string[] lines = System.IO.File.ReadAllLines(configPath);
            // Display the file contents by using a foreach loop.
            int n = 1;

            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                if (n == 1)
                {
                    txtcmp.Text = line;
                    txtcmp.Enabled = false;
                    Program.CompanyID = txtcmp.Text;
                }

                if (n == 2)
                {
                    txtDivision.Text = line;
                    txtDivision.Enabled = false;
                    Program.DivisionID = txtDivision.Text;
                }

                if (n == 3)
                {
                    txtdepartment.Text = line;
                    txtdepartment.Enabled = false;
                    Program.DepartmentID = txtdepartment.Text;
                }

                if (n == 4)
                {
                    txtTerminal.Text = line;
                    txtTerminal.Enabled = false;
                    Program.TerminalName = txtTerminal.Text;
                }

                
                if (n == 5)
                {
                    txtadobe.Text = line;
                    txtadobe.Enabled = false;
                }

                if (n == 6)
                {
                    txtdefaultprinter.Text = line;
                    txtdefaultprinter.Enabled = false;
                }

                n = n + 1;
            }

            // AB#3189: fill the new screen from the values just read.
            lblShop.Text = txtcmp.Text;
            lblTerminalName.Text = "Terminal " + txtTerminal.Text;
            ScaleListColumns();
            LoadTodaysJobsFromLog();
            RefreshJobList();
            RefreshHealthAsync();
            timerHealth.Enabled = true;

            // AB#3164: heartbeat to the Print Monitor every 30 s (also sends any queued job results).
            timerMonitor = new System.Windows.Forms.Timer();
            timerMonitor.Interval = 30000;
            timerMonitor.Tick += delegate { MonitorBeat(); };
            timerMonitor.Enabled = true;
        }

        private void MonitorBeat()
        {
            try
            {
                int notPrinted = 0;
                foreach (JobRow j in jobs)
                    if ((j.Waiting || j.Confirm == "Not printed") && (DateTime.Now - j.When).TotalMinutes <= 60) notPrinted++;
                PrintMonitor.Beat(!pingFailing && lastPingOk != DateTime.MinValue,
                    lastJob == null ? DateTime.MinValue : lastJob.When, pendingConfirm.Count, notPrinted,
                    txtdefaultprinter.Text.Trim(), monitorPrinters);
            }
            catch { }
        }


        // ==================== AB#3163 (v4-2): live pickup ====================
        // 4.0 asks the server once and the server holds the request (up to 25 s), answering the
        // moment an order or print job exists for this terminal. On "yes" the normal ping/print
        // flow below runs unchanged. A server without WaitForPrintJob (not yet upgraded) makes the
        // app fall back to the 3.x 5-second ping for the rest of the session.
        private bool useLivePickup = true;
        private bool lastWaitSaidYes;
        private int emptyYesCount;          // "yes" from the wait but the ping found nothing
        private const int LiveWaitSeconds = 25;

        private void StartLiveWait()
        {
            timer1.Enabled = false;
            pingStarted = DateTime.Now;
            QFPrintService.QFPrintService obj = new QFPrintService.QFPrintService();
            obj.Timeout = (LiveWaitSeconds + 20) * 1000;
            obj.WaitForPrintJobCompleted += new QFPrintService.WaitForPrintJobCompletedEventHandler(obj_WaitForPrintJobCompleted);
            obj.WaitForPrintJobAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName, LiveWaitSeconds);
        }

        void obj_WaitForPrintJobCompleted(object sender, QFPrintService.WaitForPrintJobCompletedEventArgs e)
        {
            bool hasWork = false;
            try
            {
                hasWork = e.Result;
            }
            catch (Exception ex)
            {
                // Server without the method (old web service): SOAP "did not recognize ... SOAPAction"
                // or "Unable to handle request without a valid action". Fall back for this session.
                // 5.0.1: look only at the server's own error message. ex.ToString() includes our stack
                // trace, which always contains "WaitForPrintJob", so any network hiccup switched live
                // pickup off until restart (Lenovo, 8 Oct 2026 20:36).
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                if (msg.IndexOf("WaitForPrintJob", StringComparison.OrdinalIgnoreCase) >= 0
                    || msg.IndexOf("SOAPAction", StringComparison.OrdinalIgnoreCase) >= 0
                    || msg.IndexOf("valid action", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    useLivePickup = false;
                    WriteToFile("LIVE PICKUP not available on this server - using 5-second checks: " + ex.Message);
                    timer1.Interval = 5000;
                    timer1.Enabled = true;
                    return;
                }
                ReportError("obj_WaitForPrintJobCompleted", "", ex);
                NoteConnection(false);
                ShowCurrentStatus();
                timer1.Interval = 5000;   // network trouble: retry in 5 s, not in a tight loop
                timer1.Enabled = true;
                return;
            }

            NoteConnection(true);
            lblConn.Text = "\u25CF  Connected \u00B7 live";
            lbltimer.Text = DateTime.Now.ToLongTimeString();
            if (hasWork)
            {
                lastWaitSaidYes = true;
                PingNow();             // the normal 3.x flow: server creates the jobs, app prints them
            }
            else
            {
                lastWaitSaidYes = false;
                ShowCurrentStatus();
                StartLiveWait();       // nothing yet: ask again straight away
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (useLivePickup) { StartLiveWait(); return; }
            PingNow();
        }

        private void PingNow()
        {
            lbltimer.Text = DateTime.Now.ToLongTimeString() ; 
 
            pingStarted = DateTime.Now;
            QFPrintService.QFPrintService obj = new QFPrintService.QFPrintService();
            obj.PingPOSForPrintingCompleted += new QFPrintService.PingPOSForPrintingCompletedEventHandler(obj_PingPOSForPrintingCompleted);
            obj.PingPOSForPrintingAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName );
            timer1.Enabled = false;
         

        }

        void obj_PingPOSForPrintingCompleted(object sender, QFPrintService.PingPOSForPrintingCompletedEventArgs e)
        {
            string chk = "";
            bool reached = false;


            try
            {
                chk = e.Result.ToString();
                reached = true;
            }
            catch (Exception ex)
            {
                ReportError("obj_PingPOSForPrintingCompleted", "", ex);
                //   label1.Text += " Wait...";
                // return;
            }
            NoteConnection(reached);

            if (chk == "True")
            {
                emptyYesCount = 0;
                if (useLivePickup) timer1.Interval = 300;   // after this job, go straight back to waiting
                lblprintrequest.Text = "New Print Request Found";
                lblprintrequest.ForeColor = Color.Green;
                lbltimer.ForeColor = Color.Red;
                SetStatus("Printing", "Sending a receipt to " + txtdefaultprinter.Text, false);
                
                processprint();
            }
            else
            {
                // AB#3163: if the server said "yes" but the ping found nothing (e.g. an order the
                // server cannot turn into a job), don't spin: wait 5 s before asking again.
                if (useLivePickup)
                {
                    if (lastWaitSaidYes) emptyYesCount++;
                    timer1.Interval = emptyYesCount > 0 ? 5000 : 300;
                    if (emptyYesCount == 3) WriteToFile("LIVE PICKUP: server keeps reporting work the ping cannot find - backing off to 5 s");
                }
                lastWaitSaidYes = false;
                timer1.Enabled = true;
                lblprintrequest.Text = "No Print Request Present";
                lblprintrequest.ForeColor = Color.Red ;
                // AB#3189: 3.4 said "Printing is working" here even when the server could not be
                // reached (a failed ping lands in this branch too). ShowCurrentStatus tells the truth.
                ShowCurrentStatus();
                lbltimer.ForeColor = Color.Green;
            }

            
        }

        void processprint()
        {

            QFPrintService.QFPrintService obj = new QFPrintService.QFPrintService();
            obj.CheckPOSForPrintingCompleted += new QFPrintService.CheckPOSForPrintingCompletedEventHandler(obj_CheckPOSForPrintingCompleted);
            obj.CheckPOSForPrintingAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);


        }

        void obj_CheckPOSForPrintingCompleted(object sender, QFPrintService.CheckPOSForPrintingCompletedEventArgs e)
        {
            DataTable objDataTable = new DataTable();

            try
            {
                objDataTable = e.Result;
            }
            catch (Exception ex)
            {
                ReportError("obj_CheckPOSForPrintingCompleted", "", ex);
                //   label1.Text += " Wait...";
                // return;
            }

         
            string PrintText = "";
            string PrintText1 = "";
            string PrintText2 = "";
            string FileName = "";
            int slno = 0;
            DateTime jobStarted = DateTime.Now;   // AB#3189: for the "Took" column
            bool jobShown = false;
            JobRow sentJob = null;                // AB#3164: the row to confirm once Windows prints it

            try
            {
                PrintText = objDataTable.Rows[0]["PrintText"].ToString();
                PrintText1 = objDataTable.Rows[0]["PrintText1"].ToString();
                PrintText2 = objDataTable.Rows[0]["PrintText2"].ToString();
                FileName = objDataTable.Rows[0]["FileName"].ToString();
                slno = Convert.ToInt32(objDataTable.Rows[0]["slno"].ToString());
                // AB#3163: when the job reached this PC, and how (live pickup or 5-second check).
                WriteToFile("PICKUP slno=" + slno + " file=" + FileName + " via " + (useLivePickup ? "live" : "poll"));

                System.Net.WebClient wc = new System.Net.WebClient();

                if (PrintText == "Text")
                {
                    string filename = "";
                    //filename = PrintText1.Replace("https://secure.quickflora.com/FAX/", "");
                    filename = FileName;
                    lblprintfile.Text = "1.Downloading file:";
                    lblprintfile.Text = lblprintfile.Text + "\r\n" + filename;
                   // lblprintfile.Text = lblprintfile.Text + "\r\nPrinter Name :" + PrintText2;
                    EnsureFolderFor("C:\\QFPrintApp\\Receipts\\" + filename);
                    wc.DownloadFile(PrintText1, "C:\\QFPrintApp\\Receipts\\" + filename);
                    lblprintfile.Text = lblprintfile.Text + "\r\n" + "2.Printing file on printer :" + PrintText2;
                    bool hadDrawer; long fileSize;
                    string detail = InspectPrintFile("C:\\QFPrintApp\\Receipts\\" + filename, out hadDrawer, out fileSize);
                    SpoolWatch.Start(PrintText2, "QuickFlora-Print", DateTime.Now);   // AB#3164: watch before sending
                    bool sentOk = QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(PrintText2, "C:\\QFPrintApp\\Receipts\\" + filename);
                    LogPrintJob(PrintText, filename, slno, PrintText2, detail, hadDrawer, fileSize, sentOk);
                    sentJob = AddJob(PrintText, filename, PrintText2, sentOk, (DateTime.Now - jobStarted).TotalSeconds);
                    if (sentJob != null) sentJob.Doc = "QuickFlora-Print";   // raw jobs are spooled under this name (clsPrinting)
                    jobShown = true;
                }

                // AB#3170 (v4-8): HTML work ticket through Edge when the server saved one beside the
                // PDF (QFReports WTReportV2RetailUS). Falls back to the PDF + Adobe below otherwise.
                bool printedHtml = false;
                if (PrintText == "PDF" && PrintText1.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    string htmlName = System.IO.Path.ChangeExtension(FileName, ".html");
                    string htmlPath = "C:\\QFPrintApp\\PDF\\" + htmlName;
                    if (HtmlPrinter.TryDownload(PrintText1.Substring(0, PrintText1.Length - 4) + ".html", htmlPath))
                    {
                        lblprintfile.Text = "1.Downloaded HTML: " + htmlName + "\r\n2.Printing with Edge on " + PrintText2;
                        SetDefaultSystemPrinter(PrintText2);      // Edge kiosk printing uses the default printer
                        string title = HtmlPrinter.TitleOf(htmlPath);
                        if (string.IsNullOrEmpty(title)) title = htmlName;
                        SpoolWatch.Start(PrintText2, title, DateTime.Now);
                        string htmlDetail;
                        printedHtml = HtmlPrinter.Print(htmlPath, PrintText2, title, out htmlDetail);
                        LogPrintJob("HTML", htmlName, slno, PrintText2, " bytes=" + new System.IO.FileInfo(htmlPath).Length + htmlDetail, false, 0, printedHtml);
                        if (printedHtml)
                        {
                            sentJob = AddJob(PrintText, htmlName, PrintText2, true, (DateTime.Now - jobStarted).TotalSeconds);
                            if (sentJob != null) sentJob.Doc = title;
                            jobShown = true;
                        }
                        else
                        {
                            WriteToFile("HTML print failed for " + htmlName + " - falling back to the PDF");
                            ReportEvent("HTML print failed, PDF used", htmlName, "slno=" + slno + " | printer=" + PrintText2 + " |" + htmlDetail);
                        }
                    }
                }

                if (PrintText == "PDF" && !printedHtml)
                {
                    string filename = "";
                    //filename = PrintText1.Replace("https://secure.localflorist.com/PDF/", "");
                    filename = FileName;
                    lblprintfile.Text = "1.Downloading file:";
                    lblprintfile.Text = lblprintfile.Text + "\r\n" + filename;
                    //lblprintfile.Text = lblprintfile.Text + "\r\nPrinter Name :" + PrintText2;
                    //filename = PrintText2 + "_" + filename;
                    //filename.Replace("\\", "");
                    //filename.Replace(" ", "");
                    //filename.Replace(" ", "");

                    EnsureFolderFor("C:\\QFPrintApp\\PDF\\" + filename);
                    wc.DownloadFile(PrintText1, "C:\\QFPrintApp\\PDF\\" + filename);
                    lblprintfile.Text = lblprintfile.Text + "\r\n" + "2.Printing file on printer :" + PrintText2;
                    SetDefaultSystemPrinter(PrintText2);
                    // AB#1323 — log worksheets and cards too. v3.2 logged only the receipt, so
                    // Daman's 15 Aug test printed three documents and produced a single line,
                    // which made a complete log look like a broken one.
                    bool pdfDrawer; long pdfSize;
                    string pdfDetail = InspectPrintFile("C:\\QFPrintApp\\PDF\\" + filename, out pdfDrawer, out pdfSize);
                    bool pdfSent = true;
                    SpoolWatch.Start(PrintText2, filename, DateTime.Now);   // AB#3164: watch before Adobe spools it
                    try
                    {
                        Pdf.PrintPDFs("C:\\QFPrintApp\\PDF\\" + filename, txtadobe.Text, PrintText2);
                    }
                    catch (Exception ex)
                    {
                        pdfSent = false;
                        ReportError("obj_CheckPOSForPrintingCompleted", filename, ex);
                        var str = "";
                        str = ex.Message;
                      //  MessageBox.Show(str);

                    }
                    LogPrintJob(PrintText, filename, slno, PrintText2, pdfDetail, pdfDrawer, pdfSize, pdfSent);
                    sentJob = AddJob(PrintText, filename, PrintText2, pdfSent, (DateTime.Now - jobStarted).TotalSeconds);
                    if (sentJob != null) sentJob.Doc = filename;              // Adobe spools the PDF under its file name
                    jobShown = true;
                    //Pdf.PrintPDFs("C:\\QFPrintApp\\PDF\\" + PrintText2 + "_" + filename, txtadobe.Text, PrintText2);

                }

                //QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(txtprinter.Text, receipt);
                //Pdf.PrintPDFs("C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlmc, txtadobe.Text, txtcardprinter.Text);

                SetDefaultSystemPrinter(txtdefaultprinter.Text);

                //object printerName = txtdefaultprinter.Text;
                //ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
                //ManagementObjectCollection collection = searcher.Get();

                //foreach (ManagementObject currentObject in collection)
                //{

                //    if (currentObject["name"].ToString() == printerName.ToString())
                //    {

                //        currentObject.InvokeMethod("SetDefaultPrinter", new object[] { printerName });

                //    }

                //}


            }
            catch (Exception ex)
            {
                ReportError("obj_CheckPOSForPrintingCompleted", "", ex);
                // AB#3189: a download or send that threw used to vanish from the screen. Show it.
                if (!jobShown && FileName.Length > 0)
                    AddJob(PrintText, FileName, PrintText2, false, (DateTime.Now - jobStarted).TotalSeconds);

            }

            // AB#3164 (v4): tell the server "done" only once Windows says the page printed.
            // CheckPOSForPrinting already marked this row taken ([Read]=0), so it is not sent again
            // while we wait, and it does not block the jobs behind it. Until 4.0 the row was marked
            // done here even when the printer was off or the wrong printer (qfdemo 36319, 7 Oct 2026).
            if (slno > 0 && sentJob != null && sentJob.Ok)
            {
                sentJob.Slno = slno;
                sentJob.Since = jobStarted;
                sentJob.Confirm = "Printing";
                pendingConfirm.Add(sentJob);
                timerConfirm.Enabled = true;
                RefreshJobList();
                lblprintfile.Text = lblprintfile.Text + "\r\n" + "3.Waiting for the printer.";
                timer1.Enabled = true;
                return;
            }
            if (slno > 0)
            {
                // Could not be sent at all: leave it open on the server (taken, not done) so it shows
                // as not printed instead of being marked done. 4.0.4: also when it failed before the
                // file name was known (staging test 8 Oct 2026: such a job fell through and was marked done).
                WriteToFile("NOT PRINTED slno=" + slno + " file=" + FileName + " - left open on the server");
                PrintMonitor.Job(jobStarted, slno, FormName(PrintText, FileName), FileName, PrintText2, false,
                    "Not printed: could not be sent to the printer");
                timer1.Enabled = true;
                return;
            }

            QFPrintService.QFPrintService obj = new QFPrintService.QFPrintService();
            obj.UpdatePOSForPrintingCompleted += new QFPrintService.UpdatePOSForPrintingCompletedEventHandler(obj_UpdatePOSForPrintingCompleted);
            obj.UpdatePOSForPrintingAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID , slno);


        }


        private void SetDefaultSystemPrinter(string sPrinterName)
        {
            // ======================================================
            // Function: Change the default printer
            //
            // History: Shantell Hausleitner
            // ======================================================      
            //Declations
            // ======================================================                        
            string sOldPrinter;
            PrintDocument pd = new PrintDocument();
            object WshNetwork;
            object[] param = new object[1];
            // ======================================================                        

            //Add the parameters to an array
            param[0] = sPrinterName;

            // Get the system default printer
            sOldPrinter = pd.PrinterSettings.PrinterName;

            //Create the object
            WshNetwork = Microsoft.VisualBasic.Interaction.CreateObject("WScript.Network", "");

            try
            {
                //Call the method to set the default printer
                Microsoft.VisualBasic.Interaction.CallByName(WshNetwork, "SetDefaultPrinter", Microsoft.VisualBasic.CallType.Method, param);

                // Check that the printer exists, revert if not.
                if (pd.PrinterSettings.IsValid == false)
                {
                    param[0] = sOldPrinter;
                   // MessageBox.Show("Printer <" + sPrinterName + "> is invalid. \n The default printer will be used.", Program.Caption("Error with Printer"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Microsoft.VisualBasic.Interaction.CallByName(WshNetwork, "SetDefaultPrinter", Microsoft.VisualBasic.CallType.Method, param);
                }

                // Specify the printer to use.
                pd.PrinterSettings.PrinterName = sPrinterName;
            }
            catch
            {
                //Revert to original default
                param[0] = sOldPrinter;
               // MessageBox.Show("Printer <" + sPrinterName + "> is invalid. \n The default printer will be used.", Program.Caption("Error with Printer"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Microsoft.VisualBasic.Interaction.CallByName(WshNetwork, "SetDefaultPrinter", Microsoft.VisualBasic.CallType.Method, param);

            }
        }



        void obj_UpdatePOSForPrintingCompleted(object sender, QFPrintService.UpdatePOSForPrintingCompletedEventArgs e)
        {
            lblprintfile.Text = lblprintfile.Text + "\r\n" + "3.Print Completed.";
            timer1.Enabled = true;
        }

        // ==================== AB#3164 (v4) — confirm the page printed before telling the server ====================

        private class SpoolJob { public string Printer; public string Doc; public int Mask; public DateTime Submitted; }

        private void timerConfirm_Tick(object sender, EventArgs e)
        {
            if (confirmBusy) return;
            if (pendingConfirm.Count == 0) { timerConfirm.Enabled = false; return; }
            confirmBusy = true;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                List<SpoolJob> queue = ReadSpoolQueue();
                try { BeginInvoke(new MethodInvoker(delegate { CheckPending(queue); confirmBusy = false; })); }
                catch { confirmBusy = false; }
            });
        }

        private static List<SpoolJob> ReadSpoolQueue()
        {
            List<SpoolJob> list = new List<SpoolJob>();
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, Document, StatusMask, TimeSubmitted FROM Win32_PrintJob"))
                {
                    foreach (ManagementObject j in s.Get())
                    {
                        string name = Convert.ToString(j["Name"]);
                        int comma = name.LastIndexOf(',');
                        if (comma <= 0) continue;
                        SpoolJob sj = new SpoolJob();
                        sj.Printer = name.Substring(0, comma);
                        sj.Doc = Convert.ToString(j["Document"]);
                        sj.Mask = Convert.ToInt32(j["StatusMask"]);
                        sj.Submitted = ManagementDateTimeConverter.ToDateTime(Convert.ToString(j["TimeSubmitted"]));
                        list.Add(sj);
                    }
                }
            }
            catch { return null; }
            return list;
        }

        /// <summary>
        /// One pass over the jobs waiting for confirmation. Windows' job flags decide:
        ///  printed/complete = printed; error/offline/paper-out/blocked = not printed (keep watching);
        ///  gone after being seen without an error = printed (Windows deletes printed jobs).
        /// A raw receipt can print and vanish before we look, so after 10 s unseen it counts as
        /// printed if its printer shows no fault. A PDF is spooled by Adobe, which can fail silently,
        /// so unseen after 2 minutes it is NOT printed.
        /// </summary>
        private void CheckPending(List<SpoolJob> queue)
        {
            if (queue == null) return;   // WMI failed this time; try again next tick
            bool changed = false;
            for (int i = pendingConfirm.Count - 1; i >= 0; i--)
            {
                JobRow j = pendingConfirm[i];
                double age = (DateTime.Now - j.Since).TotalSeconds;
                SpoolJob match = null;
                foreach (SpoolJob q in queue)
                {
                    if (q.Printer.Equals(j.Printer, StringComparison.OrdinalIgnoreCase)
                        && q.Doc.Equals(j.Doc, StringComparison.OrdinalIgnoreCase)
                        && q.Submitted >= j.Since.AddSeconds(-5)) { match = q; break; }
                }

                // AB#3164: the background watcher may have seen it while the send was still running
                // (Adobe spools and a fast printer finishes before this timer first looks).
                SpoolWatch.Sighting seenEarly = SpoolWatch.Get(j.Printer, j.Doc);
                if (seenEarly != null && seenEarly.Seen)
                {
                    j.Seen = true;
                    if (seenEarly.LastWasError && !j.LastWasError) { j.LastWasError = true; j.Confirm = "Not printed"; changed = true; }
                }

                string outcome = null;   // "Printed" / "Not printed" when settled
                if (match != null)
                {
                    j.Seen = true;
                    if ((match.Mask & (128 | 4096)) != 0) outcome = "Printed";
                    else if ((match.Mask & (2 | 32 | 64 | 512 | 1024)) != 0)
                    {
                        j.LastMask = match.Mask;
                        if (!j.LastWasError) { j.LastWasError = true; j.Confirm = "Not printed"; changed = true; }
                    }
                    else if (j.LastWasError) { j.LastWasError = false; j.Confirm = "Printing"; changed = true; }
                }
                else if (j.Seen)
                {
                    // Left the queue. After a fault that means someone cancelled it.
                    outcome = j.LastWasError ? "Not printed" : "Printed";
                }
                else if (j.Doc == "QuickFlora-Print" && age > 10 && !PrinterHasFault(j.Printer)) outcome = "Printed";
                else if (age > 120) outcome = "Not printed";

                if (age > 1800 && outcome == null) outcome = "Not printed";   // stop watching after 30 min

                if (outcome != null)
                {
                    SpoolWatch.Stop(j.Printer, j.Doc);
                    pendingConfirm.RemoveAt(i);
                    j.Confirm = outcome;
                    j.Waiting = false;
                    changed = true;
                    WriteToFile("CONFIRM slno=" + j.Slno + " file=" + j.File + " printer=" + j.Printer
                        + " result=" + (outcome == "Printed" ? "printed" : "NOT PRINTED") + " after " + age.ToString("0") + "s"
                        + (outcome == "Printed" ? "" : " - left open on the server"));
                    PrintMonitor.Job(j.Since, j.Slno, j.Form, j.File, j.Printer, outcome == "Printed",
                        (outcome == "Printed" ? "Printed" : "Not printed: " + NotPrintedReason(j, age))
                        + " after " + age.ToString("0") + "s" + (j.Slno == 0 ? " (reprint)" : ""));
                    if (outcome != "Printed")
                        ReportEvent("Not printed", j.File, "slno=" + j.Slno + " | printer=" + j.Printer + " | form=" + j.Form
                            + " | reason=" + NotPrintedReason(j, age) + " | after=" + age.ToString("0") + "s");
                    if (outcome == "Printed" && j.Slno > 0)   // a reprint (slno 0) is not a server job
                    {
                        QFPrintService.QFPrintService obj = new QFPrintService.QFPrintService();
                        obj.UpdatePOSForPrintingAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID, j.Slno);
                    }
                }
            }
            if (changed) { RefreshJobList(); ShowCurrentStatus(); }
            if (pendingConfirm.Count == 0) timerConfirm.Enabled = false;
        }

        /// <summary>
        /// AB#3164: background sightings of a job in Windows' queue, started just BEFORE the job is
        /// sent. Polls every 0.4 s on its own thread so it keeps looking while the UI thread is busy in
        /// the send (Adobe takes ~10 s). Staging test 7 Oct 2026: without this, a PDF the Canon printed
        /// during the send was never seen and was wrongly reported "not printed".
        /// </summary>
        internal static class SpoolWatch
        {
            public class Sighting
            {
                public string Printer; public string Doc; public DateTime Since;
                public bool Seen; public bool LastWasError; public DateTime StartedAt;
            }
            private static readonly object Gate = new object();
            private static readonly Dictionary<string, Sighting> Watched = new Dictionary<string, Sighting>(StringComparer.OrdinalIgnoreCase);
            private static bool running;

            private static string Key(string printer, string doc) { return (printer ?? "") + "|" + (doc ?? ""); }

            public static void Start(string printer, string doc, DateTime since)
            {
                try
                {
                    lock (Gate)
                    {
                        Sighting s = new Sighting();
                        s.Printer = printer; s.Doc = doc; s.Since = since; s.StartedAt = DateTime.Now;
                        Watched[Key(printer, doc)] = s;
                        if (running) return;
                        running = true;
                    }
                    System.Threading.Thread t = new System.Threading.Thread(Loop);
                    t.IsBackground = true; t.Name = "QF SpoolWatch";
                    t.Start();
                }
                catch { }
            }

            public static Sighting Get(string printer, string doc)
            {
                lock (Gate) { Sighting s; return Watched.TryGetValue(Key(printer, doc), out s) ? s : null; }
            }

            public static void Stop(string printer, string doc)
            {
                lock (Gate) { Watched.Remove(Key(printer, doc)); }
            }

            private static void Loop()
            {
                while (true)
                {
                    List<Sighting> list;
                    lock (Gate)
                    {
                        // Drop anything watched for over 35 minutes; CheckPending has settled it by then.
                        List<string> old = new List<string>();
                        foreach (KeyValuePair<string, Sighting> kv in Watched)
                            if ((DateTime.Now - kv.Value.StartedAt).TotalMinutes > 35) old.Add(kv.Key);
                        foreach (string k in old) Watched.Remove(k);
                        if (Watched.Count == 0) { running = false; return; }
                        list = new List<Sighting>(Watched.Values);
                    }
                    List<SpoolJob> queue = ReadSpoolQueue();
                    if (queue != null)
                    {
                        foreach (Sighting s in list)
                            foreach (SpoolJob q in queue)
                                if (q.Printer.Equals(s.Printer, StringComparison.OrdinalIgnoreCase)
                                    && q.Doc.Equals(s.Doc, StringComparison.OrdinalIgnoreCase)
                                    && q.Submitted >= s.Since.AddSeconds(-5))
                                {
                                    lock (Gate)
                                    {
                                        s.Seen = true;
                                        s.LastWasError = (q.Mask & (2 | 32 | 64 | 512 | 1024)) != 0;
                                    }
                                }
                    }
                    System.Threading.Thread.Sleep(400);
                }
            }
        }

        private bool PrinterHasFault(string printer)
        {
            return printer != null && problemPrinter != null
                && printer.Equals(problemPrinter, StringComparison.OrdinalIgnoreCase);
        }







    
        // ==================== AB#1327 — new interface behaviour ====================

        /// <summary>Sets the status banner. Green = fine, amber = attention needed.</summary>
        private void SetStatus(string headline, string detail, bool attention)
        {
            SetStatus(headline, detail, attention ? 2 : 0);
        }

        /// <summary>AB#3189: level 0 = fine (green tick), 1 = needs attention soon (amber), 2 = not printing (red).</summary>
        private void SetStatus(string headline, string detail, int level)
        {
            try
            {
                lblStatus.Text = headline;
                lblStatusSub.Text = detail;
                // Dark red / dark amber / near-black so the headline passes 4.5:1 on white.
                lblStatus.ForeColor = level == 2 ? Color.FromArgb(126, 27, 21)
                                    : level == 1 ? Color.FromArgb(122, 66, 6)
                                    : Color.FromArgb(28, 33, 29);
                badgeStatus.Level = level;
            }
            catch { }
        }

        /// <summary>
        /// Sends ONLY the cash-drawer kick byte (0x07) to the configured receipt printer.
        /// This is the fastest way to tell a cabling/hardware fault from a software one:
        /// if the drawer opens here, the hardware is fine and the problem is upstream.
        /// Deliberately does NOT send a cut or any receipt text.
        /// </summary>
        private void btnTestDrawer_Click(object sender, EventArgs e)
        {
            string printer = txtdefaultprinter.Text.Trim();
            if (printer.Length == 0)
            {
                MessageBox.Show("No printer is configured for this terminal.\r\n\r\nCheck line 6 of Config.txt.",
                    Program.Caption("No printer set"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 0x07 = BEL = the drawer-kick byte on Star printers.
                bool ok = QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(printer, "\u0007");
                if (ok)
                {
                    SetStatus("Cash drawer test sent", "Sent the open command to " + printer, false);
                    MessageBox.Show(
                        "The open command was sent to:\r\n\r\n    " + printer + "\r\n\r\n" +
                        "DID THE DRAWER OPEN?\r\n\r\n" +
                        "YES  - the drawer and cabling are fine. Any problem is in the receipt itself.\r\n" +
                        "NO   - the problem is the printer, the cable, or the drawer. Not the software.",
                        Program.Caption("Cash drawer test"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SetStatus("Cash drawer test failed", "Windows would not accept the job for " + printer, true);
                    MessageBox.Show("Windows would not send to this printer:\r\n\r\n    " + printer +
                        "\r\n\r\nCheck the printer is switched on and the name matches exactly.",
                        Program.Caption("Test failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                ReportError("btnTestDrawer_Click", "", ex);
                MessageBox.Show("Could not send to the printer.\r\n\r\n" + ex.Message,
                    Program.Caption("Test failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Prints a short sample receipt, then cuts. Does not open the drawer.</summary>
        private void btnTestPrint_Click(object sender, EventArgs e)
        {
            string printer = txtdefaultprinter.Text.Trim();
            if (printer.Length == 0)
            {
                MessageBox.Show("No printer is configured for this terminal.\r\n\r\nCheck line 6 of Config.txt.",
                    Program.Caption("No printer set"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // AB#3189: raw receipt-printer bytes only make sense on a receipt printer. Sent to an
            // office printer (Canon G6010 on the Lenovo, 7 Oct 2026) Windows says "complete", nothing
            // prints, and the printer can be left in an error state. Office printers get a normal page.
            if (!IsReceiptPrinter(printer))
            {
                PrintNormalTestPage(printer);
                return;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\r\n");
                sb.Append("     QUICKFLORA TEST PRINT\r\n");
                sb.Append("     ---------------------\r\n\r\n");
                sb.Append("  Company : " + txtcmp.Text + "\r\n");
                sb.Append("  Terminal: " + txtTerminal.Text + "\r\n");
                sb.Append("  Printer : " + printer + "\r\n");
                sb.Append("  Time    : " + DateTime.Now.ToString("dd MMM yyyy  h:mm:ss tt") + "\r\n");
                sb.Append("  Version : " + Application.ProductVersion + "\r\n\r\n");
                sb.Append("  If you can read this, printing works.\r\n");
                sb.Append("\r\n\r\n\r\n");
                sb.Append("\u001B\u0064\u0030");   // ESC d 0 = cut. No drawer byte.

                bool ok = QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(printer, sb.ToString());
                SetStatus(ok ? "Test print sent" : "Test print failed",
                          ok ? "Sent to " + printer : "Windows rejected the job for " + printer, !ok);
                if (!ok)
                {
                    MessageBox.Show("Windows would not send to this printer:\r\n\r\n    " + printer,
                        Program.Caption("Test failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                ReportError("btnTestPrint_Click", "", ex);
                MessageBox.Show("Could not print.\r\n\r\n" + ex.Message,
                    Program.Caption("Test failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Receipt (thermal / POS) printer, judged by its Windows driver. Unknown or unreadable is
        /// treated as a receipt printer so the 3.4 behaviour is kept when in doubt.
        /// </summary>
        private static bool IsReceiptPrinter(string printer)
        {
            try
            {
                string driver = "";
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, DriverName FROM Win32_Printer"))
                {
                    foreach (ManagementObject o in s.Get())
                        if (Convert.ToString(o["Name"]).Equals(printer, StringComparison.OrdinalIgnoreCase))
                            driver = Convert.ToString(o["DriverName"]);
                }
                if (driver.Length == 0) return true;
                string d = driver.ToUpperInvariant();
                foreach (string k in new string[] { "TM-", "TM ", "STAR", "TSP", "POS", "RECEIPT", "THERMAL",
                                                    "TEXT ONLY", "80MM", "58MM", "BIXOLON", "CITIZEN", "SNBC", "XPRINTER", "ZJ-" })
                    if (d.Contains(k)) return true;
                return false;
            }
            catch { return true; }
        }

        /// <summary>A normal one-page test for an office printer, sent through its Windows driver.</summary>
        private void PrintNormalTestPage(string printer)
        {
            try
            {
                string[] lines = new string[] {
                    "QuickFlora test print",
                    "",
                    "Company:   " + txtcmp.Text,
                    "Terminal:  " + txtTerminal.Text,
                    "Printer:   " + printer,
                    "Time:      " + DateTime.Now.ToString("dd MMM yyyy  h:mm:ss tt"),
                    "Version:   QuickFlora Print App " + Program.AppVersion,
                    "",
                    "If you can read this, this printer works from QuickFlora.",
                    "Note: this is not a receipt printer. Receipts are made for",
                    "receipt printers and may not print here; worksheets and",
                    "card messages will." };
                using (PrintDocument pd = new PrintDocument())
                {
                    pd.PrinterSettings.PrinterName = printer;
                    pd.DocumentName = "QuickFlora test print";
                    pd.PrintPage += delegate(object s, PrintPageEventArgs ev)
                    {
                        using (Font title = new Font("Segoe UI", 18F, FontStyle.Bold))
                        using (Font body = new Font("Consolas", 11F))
                        {
                            float x = ev.MarginBounds.Left, y = ev.MarginBounds.Top;
                            ev.Graphics.DrawString(lines[0], title, Brushes.Black, x, y);
                            y += title.GetHeight(ev.Graphics) * 1.6f;
                            for (int i = 1; i < lines.Length; i++)
                            {
                                ev.Graphics.DrawString(lines[i], body, Brushes.Black, x, y);
                                y += body.GetHeight(ev.Graphics) * 1.3f;
                            }
                        }
                        ev.HasMorePages = false;
                    };
                    pd.Print();
                }
                SetStatus("Test print sent", "Sent a normal test page to " + printer + " (office printer, not a receipt printer)", false);
            }
            catch (Exception ex)
            {
                ReportError("PrintNormalTestPage", "", ex);
                SetStatus("Test print failed", "Windows rejected the job for " + printer, true);
                MessageBox.Show("Could not print.\r\n\r\n" + ex.Message,
                    Program.Caption("Test failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Opens the folder holding every receipt this PC has printed.</summary>
        private void btnOpenReceipts_Click(object sender, EventArgs e)
        {
            string folder = @"C:\QFPrintApp\Receipts";
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception ex)
            {
                ReportError("btnOpenReceipts_Click", "", ex);
                MessageBox.Show("Could not open " + folder + "\r\n\r\n" + ex.Message,
                    Program.Caption("Could not open folder"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Copies everything support normally has to ask for onto the clipboard, so a problem
        /// report arrives with the facts attached instead of "printing isn't working".
        /// </summary>
        private void btnCopyDiag_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("QuickFlora Print App - support details");
                sb.AppendLine("Generated : " + DateTime.Now.ToString("dd MMM yyyy h:mm:ss tt"));
                sb.AppendLine("Version   : " + Application.ProductVersion);
                sb.AppendLine("Machine   : " + Environment.MachineName);
                sb.AppendLine("Windows   : " + Environment.OSVersion.VersionString);
                sb.AppendLine();
                sb.AppendLine("Company   : " + txtcmp.Text);
                sb.AppendLine("Division  : " + txtDivision.Text);
                sb.AppendLine("Department: " + txtdepartment.Text);
                sb.AppendLine("Terminal  : " + txtTerminal.Text);
                sb.AppendLine("Printer   : " + txtdefaultprinter.Text);
                sb.AppendLine("Adobe     : " + txtadobe.Text);
                sb.AppendLine();
                sb.AppendLine("Status    : " + lblStatus.Text + " - " + lblStatusSub.Text);
                sb.AppendLine("Activity  : " + lblprintrequest.Text);
                sb.AppendLine("Server    : " + lblConn.Text.Replace("●", "").Trim()
                    + (lastPingOk == DateTime.MinValue ? "" : " (last reached " + lastPingOk.ToString("h:mm:ss tt") + ")"));
                sb.AppendLine("IP address: " + lblIp.Text);
                sb.AppendLine("Remote support: " + lblRmm.Text);
                sb.AppendLine();
                sb.AppendLine("Recent print jobs:");
                for (int i = 0; i < jobs.Count && i < 10; i++)
                {
                    JobRow j = jobs[i];
                    sb.AppendLine("   " + j.When.ToString("h:mm:ss tt") + "  " + j.Form + "  " + j.File
                        + "  -> " + j.Printer + "  " + (j.Ok ? "sent" : "FAILED") + "  " + j.TookText);
                }
                sb.AppendLine();
                sb.AppendLine("Printers this PC can see:");
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    sb.AppendLine("   " + p + (p == txtdefaultprinter.Text ? "   <-- configured for receipts" : ""));
                }
                sb.AppendLine();
                try
                {
                    string rf = @"C:\QFPrintApp\Receipts";
                    if (Directory.Exists(rf))
                    {
                        string[] files = Directory.GetFiles(rf);
                        sb.AppendLine("Receipt files stored: " + files.Length);
                    }
                }
                catch { }

                Clipboard.SetText(sb.ToString());
                MessageBox.Show(
                    "Support details copied to the clipboard.\r\n\r\n" +
                    "Paste them into an email to support@quickflora.com along with what went wrong.",
                    Program.Caption("Copied"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ReportError("btnCopyDiag_Click", "", ex);
                MessageBox.Show("Could not gather details.\r\n\r\n" + ex.Message,
                    Program.Caption("Failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== AB#3189 — v3.5 screen ====================
        // Everything below only reads state and updates the screen. None of it is on the print
        // path: if any of it fails, printing carries on exactly as in 3.4.

        private class JobRow
        {
            public DateTime When;
            public string Form;
            public string File;
            public string Printer;
            public bool Ok;
            public bool Waiting;          // Windows still holds it and the printer has a fault
            // AB#3164 (v4) confirmation of a server job
            public int Slno;
            public string Doc;            // name Windows spools it under
            public DateTime Since;
            public bool Seen;             // found in Windows' queue at least once
            public bool LastWasError;
            public int LastMask;          // 4.0.2: Windows job flags at the last fault, for the reason sent to the server
            public string Confirm;        // null (not tracked), "Printing", "Printed", "Not printed"
            public double Seconds = -1;   // -1 = unknown (rows read back from the log)

            public string TookText
            {
                get { return Seconds < 0 ? "\u2014" : Seconds.ToString("0.0") + " s"; }
            }
        }

        /// <summary>"Text" = raw receipt; a PDF is named after its form, e.g. Card_..., Worksheet_...</summary>
        private static string FormName(string type, string file)
        {
            if (type == "Text") return "Receipt";
            string f = file == null ? "" : file;
            int u = f.IndexOf('_');
            string prefix = u > 0 ? f.Substring(0, u) : "";
            if (prefix.Equals("Card", StringComparison.OrdinalIgnoreCase)) return "Card message";
            if (prefix.Length > 0 && prefix.Length <= 20) return prefix;
            return "PDF";
        }

        private JobRow AddJob(string type, string file, string printer, bool ok, double seconds)
        {
            JobRow added = null;
            try
            {
                JobRow j = new JobRow();
                j.When = DateTime.Now; j.Form = FormName(type, file); j.File = file;
                j.Printer = printer; j.Ok = ok; j.Seconds = seconds;
                jobs.Insert(0, j);
                if (jobs.Count > 50) jobs.RemoveAt(jobs.Count - 1);
                lastJob = j;
                added = j;
                RefreshJobList();
                ShowCurrentStatus();
            }
            catch { }
            return added;
        }

        /// <summary>
        /// Rebuilds today's list from the AB#1323 log after a restart, so the screen is not empty
        /// the morning after a reboot. Time taken is not in the log, so those rows show a dash.
        /// </summary>
        private void LoadTodaysJobsFromLog()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "Logs"),
                    "AppLog_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt");
                if (!System.IO.File.Exists(path)) return;
                System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(
                    @"^(\d\d:\d\d:\d\d)\s+PRINT type=(\S+) file=(.*?) slno=(\d+) printer=(.*?) bytes=.* sent=(ok|FAILED)\s*$");
                // AB#3164: 4.0 also logs how each job ended, so a restart keeps Printed / Not printed.
                System.Text.RegularExpressions.Regex rxConfirm = new System.Text.RegularExpressions.Regex(
                    @"^\d\d:\d\d:\d\d\s+CONFIRM slno=(\d+) .* result=(printed|NOT PRINTED)");
                Dictionary<int, JobRow> bySlno = new Dictionary<int, JobRow>();
                foreach (string l in System.IO.File.ReadAllLines(path))
                {
                    System.Text.RegularExpressions.Match c = rxConfirm.Match(l);
                    if (c.Success)
                    {
                        JobRow done;
                        if (bySlno.TryGetValue(int.Parse(c.Groups[1].Value), out done))
                            done.Confirm = c.Groups[2].Value == "printed" ? "Printed" : "Not printed";
                        continue;
                    }
                    System.Text.RegularExpressions.Match m = rx.Match(l);
                    if (!m.Success) continue;
                    JobRow j = new JobRow();
                    j.When = DateTime.Today + TimeSpan.Parse(m.Groups[1].Value);
                    j.Form = FormName(m.Groups[2].Value, m.Groups[3].Value);
                    j.File = m.Groups[3].Value; j.Printer = m.Groups[5].Value;
                    j.Slno = int.Parse(m.Groups[4].Value);
                    j.Ok = m.Groups[6].Value == "ok";
                    bySlno[j.Slno] = j;
                    jobs.Insert(0, j);
                }
                while (jobs.Count > 50) jobs.RemoveAt(jobs.Count - 1);
                if (jobs.Count > 0) lastJob = jobs[0];
            }
            catch { }
        }

        private void RefreshJobList()
        {
            // keep the user's selection: the list is rebuilt every second while a job is confirming
            object keep = lstJobs.SelectedItems.Count == 1 ? lstJobs.SelectedItems[0].Tag : null;
            lstJobs.BeginUpdate();
            try
            {
                lstJobs.Items.Clear();
                if (jobs.Count == 0)
                {
                    ListViewItem none = new ListViewItem("");
                    none.SubItems.Add("");
                    none.SubItems.Add("No print jobs on this computer yet today.");
                    none.ForeColor = Color.FromArgb(90, 97, 91);
                    lstJobs.Items.Add(none);
                    return;
                }
                foreach (JobRow j in jobs)
                {
                    ListViewItem it = new ListViewItem(j.When.ToString("h:mm tt"));
                    it.Tag = j;   // Reprint uses the row's job
                    it.UseItemStyleForSubItems = false;
                    it.SubItems.Add(j.Form);
                    it.SubItems.Add(j.File);
                    it.SubItems.Add(j.Printer);
                    it.SubItems.Add(j.TookText);
                    // "Sent" is what the app knows: Windows accepted the job. Whether paper came out
                    // is not reported back to the app in 3.x, so it does not claim "Printed".
                    bool bad = !j.Ok || j.Waiting || j.Confirm == "Not printed";
                    string label = !j.Ok ? "Failed" : (j.Waiting || j.Confirm == "Not printed") ? "Not printed"
                                 : j.Confirm == "Printed" ? "Printed" : j.Confirm == "Printing" ? "Printing\u2026" : "Sent";
                    ListViewItem.ListViewSubItem s = it.SubItems.Add(label);
                    s.ForeColor = bad ? Color.FromArgb(161, 35, 27)
                                : j.Confirm == "Printing" ? Color.FromArgb(122, 66, 6) : Color.FromArgb(11, 90, 48);
                    s.Font = new Font(lstJobs.Font, FontStyle.Bold);
                    if (bad)
                        foreach (ListViewItem.ListViewSubItem sub in it.SubItems) sub.BackColor = Color.FromArgb(253, 241, 240);
                    lstJobs.Items.Add(it);
                    if (keep != null && ReferenceEquals(keep, j)) it.Selected = true;
                }
            }
            finally { lstJobs.EndUpdate(); btnReprint.Enabled = lstJobs.SelectedItems.Count == 1 && lstJobs.SelectedItems[0].Tag is JobRow; }
        }

        // ==================== Reprint (4.0) ====================

        private void lstJobs_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnReprint.Enabled = lstJobs.SelectedItems.Count == 1 && lstJobs.SelectedItems[0].Tag is JobRow;
        }

        private void btnReprint_Click(object sender, EventArgs e)
        {
            if (lstJobs.SelectedItems.Count != 1) return;
            JobRow src = lstJobs.SelectedItems[0].Tag as JobRow;
            if (src == null || string.IsNullOrEmpty(src.File) || string.IsNullOrEmpty(src.Printer)) return;
            if (MessageBox.Show(this, "Print this again?\r\n\r\n" + src.Form + "\r\n" + src.File + "\r\non " + src.Printer,
                    Program.Caption("Reprint"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Cursor = Cursors.WaitCursor;
            try { Reprint(src); }
            finally { Cursor = Cursors.Default; }
        }

        /// <summary>
        /// Prints a job from today's list again on the same printer. Uses the copy this PC downloaded
        /// (C:\QFPrintApp\PDF or \Receipts); a work ticket or card missing there is fetched again from
        /// reports.quickflora.com/PDF. Local only: the server's queue row is not touched, and a receipt
        /// is reprinted WITHOUT its cash-drawer kick. The new row is confirmed like any 4.0 job.
        /// </summary>
        private void Reprint(JobRow src)
        {
            string ext = System.IO.Path.GetExtension(src.File).ToLowerInvariant();
            bool isHtml = ext == ".html", isPdf = ext == ".pdf";
            string path = (isHtml || isPdf ? "C:\\QFPrintApp\\PDF\\" : "C:\\QFPrintApp\\Receipts\\") + src.File;
            if (!System.IO.File.Exists(path) && (isHtml || isPdf))
            {
                string url = "https://reports.quickflora.com/PDF/" + src.File;
                if (isHtml) HtmlPrinter.TryDownload(url, path);
                else try { EnsureFolderFor(path); new System.Net.WebClient().DownloadFile(url, path); } catch { }
            }
            if (!System.IO.File.Exists(path))
            {
                WriteToFile("REPRINT file=" + src.File + " printer=" + src.Printer + " FAILED - file not on this computer");
                MessageBox.Show(this, "This document is no longer on this computer, so it can't be reprinted here.\r\nPrint the order again from QuickFlora.",
                    Program.Caption("Reprint"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime started = DateTime.Now;
            string doc, detail = "";
            bool ok;
            if (isHtml)
            {
                SetDefaultSystemPrinter(src.Printer);
                doc = HtmlPrinter.TitleOf(path);
                if (string.IsNullOrEmpty(doc)) doc = src.File;
                SpoolWatch.Start(src.Printer, doc, started);
                ok = HtmlPrinter.Print(path, src.Printer, doc, out detail);
            }
            else if (isPdf)
            {
                SetDefaultSystemPrinter(src.Printer);
                doc = src.File;   // Adobe spools the PDF under its file name
                SpoolWatch.Start(src.Printer, doc, started);
                ok = true;
                try { Pdf.PrintPDFs(path, txtadobe.Text, src.Printer); }
                catch (Exception ex) { ok = false; ReportError("Reprint", src.File, ex); }
            }
            else
            {
                doc = "QuickFlora-Print";   // raw jobs are spooled under this name (clsPrinting)
                string copy = path + ".reprint";
                ok = false;
                try
                {
                    System.IO.File.WriteAllBytes(copy, WithoutDrawerKick(System.IO.File.ReadAllBytes(path)));
                    SpoolWatch.Start(src.Printer, doc, started);
                    ok = QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(src.Printer, copy);
                }
                catch (Exception ex) { ReportError("Reprint", src.File, ex); }
                try { System.IO.File.Delete(copy); } catch { }
            }
            SetDefaultSystemPrinter(txtdefaultprinter.Text);

            WriteToFile("REPRINT file=" + src.File + " printer=" + src.Printer + detail + " sent=" + (ok ? "ok" : "FAILED"));
            ReportEvent("Reprint", src.File, "printer=" + src.Printer + " | form=" + src.Form + " | sent=" + (ok ? "ok" : "FAILED") + detail);
            JobRow j = AddJob(isHtml || isPdf ? "PDF" : "Text", src.File, src.Printer, ok, (DateTime.Now - started).TotalSeconds);
            if (j == null) return;
            j.Form = src.Form + " (reprint)";
            j.Doc = doc;
            if (ok)
            {
                j.Slno = 0;
                j.Since = started;
                j.Confirm = "Printing";
                pendingConfirm.Add(j);
                timerConfirm.Enabled = true;
            }
            RefreshJobList();
        }

        /// <summary>Drops cash-drawer kicks (BEL 0x07, ESC p m t1 t2) so a reprinted receipt does not open the drawer.</summary>
        private static byte[] WithoutDrawerKick(byte[] b)
        {
            List<byte> outBytes = new List<byte>(b.Length);
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] == 0x07) continue;
                if (b[i] == 0x1B && i + 4 < b.Length && b[i + 1] == 0x70) { i += 4; continue; }
                outBytes.Add(b[i]);
            }
            return outBytes.ToArray();
        }

        /// <summary>
        /// AB#3164: ListView column widths are in raw pixels and are NOT scaled by AutoScaleMode.Dpi,
        /// so at 125% the printer names and statuses were cut off (Lenovo, 7 Oct 2026). Scale once.
        /// </summary>
        private void ScaleListColumns()
        {
            try
            {
                float f;
                using (Graphics g = CreateGraphics()) f = g.DpiX / 96f;
                if (f <= 1.01f) return;
                foreach (ListView lv in new ListView[] { lstJobs, lstPrinters })
                    foreach (ColumnHeader c in lv.Columns) c.Width = (int)(c.Width * f);
                // The printers card only has room for both columns at their scaled size; let the name take the rest.
                colPrinterName.Width = Math.Max(120, lstPrinters.ClientSize.Width - colPrinterState.Width - 4);
                lstJobs_Resize(this, EventArgs.Empty);
            }
            catch { }
        }

        private void lstJobs_Resize(object sender, EventArgs e)
        {
            try
            {
                int others = colTime.Width + colForm.Width + colPrinter.Width + colTook.Width + colResult.Width;
                int w = lstJobs.ClientSize.Width - others - 4;
                colFile.Width = Math.Max(120, w);
            }
            catch { }
        }

        private void pnlFooter_Paint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(Color.FromArgb(221, 226, 220)))
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
        }

        private void NoteConnection(bool reached)
        {
            try
            {
                if (reached)
                {
                    lastPingOk = DateTime.Now;
                    lastPingSeconds = (DateTime.Now - pingStarted).TotalSeconds;
                    pingFailing = false; pingFailures = 0;
                    lblConn.ForeColor = Color.FromArgb(11, 90, 48);
                    lblConn.Text = "\u25CF  Connected \u00B7 " + lastPingSeconds.ToString("0.0") + " s";
                }
                else
                {
                    pingFailures++;
                    // One slow or dropped call is normal on shop Wi-Fi; two in a row is a problem.
                    pingFailing = pingFailures >= 2;
                    if (pingFailing)
                    {
                        lblConn.ForeColor = Color.FromArgb(161, 35, 27);
                        lblConn.Text = "\u25CF  Not connected";
                    }
                }
            }
            catch { }
        }

        /// <summary>The status card, worst problem first.</summary>
        private void ShowCurrentStatus()
        {
            if (pingFailing)
            {
                SetStatus("Can't reach QuickFlora",
                    (lastPingOk == DateTime.MinValue ? "Not connected since the app started." :
                        "Last connected at " + lastPingOk.ToString("h:mm:ss tt") + ".")
                    + " Orders will not print until this PC is back online. Retrying every 5 seconds.", true);
                return;
            }
            if (lastPingOk == DateTime.MinValue)
            {
                SetStatus("Starting up", "Connecting to QuickFlora", false);
                return;
            }
            int notPrinted = 0; List<string> notPrintedOn = new List<string>();
            foreach (JobRow j in jobs)
                if ((j.Waiting || j.Confirm == "Not printed") && (DateTime.Now - j.When).TotalMinutes <= 60)
                {
                    notPrinted++;
                    if (!string.IsNullOrEmpty(j.Printer) && !notPrintedOn.Contains(j.Printer)) notPrintedOn.Add(j.Printer);
                }
            string notPrintedText = notPrinted == 0 ? "" : notPrinted + (notPrinted == 1 ? " job" : " jobs") + " not printed";

            if (printerProblem != null)
            {
                // AB#3189: any printer orders are actually going to, not just the one in Config.txt.
                // 7 Oct 2026: QuickFlora sent a qfdemo order to the Lenovo's old Epson (offline) while
                // Config.txt named the Canon; 3.5 said "Printing is working". Never again.
                int shown = Math.Max(notPrinted, problemWaiting);
                SetStatus(problemPrinter + ": " + printerProblem
                          + (shown > 0 ? " \u2014 " + shown + (shown == 1 ? " job" : " jobs") + " not printed" : ""),
                    "Orders sent to this printer are not printing. Turn it on and check paper and cable"
                    + (problemPrinter != txtdefaultprinter.Text ? ", or ask QuickFlora to send this terminal's orders to " + txtdefaultprinter.Text : "")
                    + (notPrintedOn.Count > 1 ? ". Also not printed on: " + string.Join(", ", notPrintedOn.FindAll(delegate(string x) { return x != problemPrinter; }).ToArray()) : "")
                    + ".", true);
                return;
            }
            if (otherCopies.Count > 0)
            {
                SetStatus("Another copy of the print app is running",
                    "Also running: " + string.Join("; ", otherCopies.ToArray()) + ". Two copies both pick up this terminal's orders. "
                    + "Close the other copy, or call QuickFlora support (support@quickflora.com).", 1);
                return;
            }
            if (notPrinted > 0)
            {
                // AB#3164: the printer looks fine but jobs never printed - e.g. Adobe Reader 9 printing
                // nothing to an IPP-class driver (staging test, 7 Oct 2026).
                SetStatus(notPrintedText,
                    "Sent to " + string.Join(", ", notPrintedOn.ToArray()) + " but Windows never printed "
                    + (notPrinted == 1 ? "it" : "them") + ". Use Print test page, then call QuickFlora support.", true);
                return;
            }
            if (lastJob != null && !lastJob.Ok && (DateTime.Now - lastJob.When).TotalMinutes < 30)
            {
                SetStatus("Last print job failed",
                    lastJob.Form + " " + lastJob.File + " could not be sent to " + lastJob.Printer
                    + " at " + lastJob.When.ToString("h:mm tt") + ". Use Print test page to check the printer.", true);
                return;
            }
            // v4 rule: the RMM agent on every print PC. Printing still works, so amber, not red —
            // but every shop without it sees this and can tell us.
            if (rmmState != null && rmmState != "Connected" && rmmState != "Unknown")
            {
                SetStatus(rmmState == "Not installed" ? "Remote support is not set up on this PC"
                                                      : "Remote support is not connected",
                    "Printing is working. Call QuickFlora support (support@quickflora.com) so we can fix problems on this PC remotely.", 1);
                return;
            }
            string sub = lastJob == null
                ? "Connected. Waiting for the next order."
                : "Last job: " + lastJob.Form.ToLower() + " sent to " + lastJob.Printer
                  + (lastJob.Seconds >= 0 ? " in " + lastJob.Seconds.ToString("0.0") + " s" : "")
                  + " (" + lastJob.When.ToString("h:mm tt") + ")";
            SetStatus("Printing is working", sub, false);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsView v = new SettingsView(txtcmp.Text, txtDivision.Text, txtdepartment.Text,
                       txtTerminal.Text, txtdefaultprinter.Text, txtadobe.Text, configPath,
                       new EventHandler(btnOpenReceipts_Click)))
            {
                v.ShowDialog(this);
            }
            loadingSettings = true;
            autoStartToolStripMenuItem.Checked = Program.IsAutoStartEnabled();
            loadingSettings = false;
        }

        private void timerHealth_Tick(object sender, EventArgs e)
        {
            RefreshHealthAsync();
        }

        private class HealthInfo
        {
            public List<string[]> Printers = new List<string[]>();   // name, state, severity 0/1/2
            public string ProblemPrinter;
            public string ProblemState;
            public int ProblemWaiting;
            public Dictionary<string, int> Waiting = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public string Ip = "";
            public string Rmm = "";
            public List<string> OtherCopies = new List<string>();   // AB#3384: other print-app copies running
            public bool? ShiftOpen;                                  // AB#3399: null = could not ask the server
        }

        /// <summary>WMI and DNS can take a second or two on a busy PC, so they run off the UI thread.</summary>
        private void RefreshHealthAsync()
        {
            lblPcName.Text = Environment.MachineName;
            lblUser.Text = Environment.UserName;
            lblVersion.Text = Program.AppVersion;

            List<string> names = new List<string>();
            if (txtdefaultprinter.Text.Trim().Length > 0) names.Add(txtdefaultprinter.Text.Trim());
            foreach (JobRow j in jobs)
                if (!string.IsNullOrEmpty(j.Printer) && !names.Contains(j.Printer) && names.Count < 4) names.Add(j.Printer);
            string configured = txtdefaultprinter.Text.Trim();
            // Printers that matter right now: the configured one, plus any that got a job in the last 30 min.
            List<string> live = new List<string>();
            if (configured.Length > 0) live.Add(configured);
            foreach (JobRow j in jobs)
                if (!string.IsNullOrEmpty(j.Printer) && (DateTime.Now - j.When).TotalMinutes <= 30 && !live.Contains(j.Printer)) live.Add(j.Printer);

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                HealthInfo h = ReadHealth(names, live);
                try { BeginInvoke(new MethodInvoker(delegate { ShowHealth(h); })); }
                catch { }
            });
        }

        private static HealthInfo ReadHealth(List<string> names, List<string> live)
        {
            HealthInfo h = new HealthInfo();
            try
            {
                foreach (System.Net.IPAddress a in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
                {
                    if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a))
                    { h.Ip = a.ToString(); break; }
                }
            }
            catch { }

            Dictionary<string, ManagementObject> found = new Dictionary<string, ManagementObject>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, PortName, WorkOffline, PrinterStatus, DetectedErrorState FROM Win32_Printer"))
                {
                    foreach (ManagementObject o in s.Get()) found[Convert.ToString(o["Name"])] = o;
                }
            }
            catch { }

            // AB#3189: also any printer Windows is holding QuickFlora jobs for, even if this app has
            // no record of them (restarted, log cleared). Windows' queue is the truth, not our memory.
            foreach (string stuck in PrintersWithStuckQuickFloraJobs())
            {
                if (!names.Contains(stuck)) names.Add(stuck);
                if (!live.Contains(stuck)) live.Add(stuck);
            }

            foreach (string n in names)
            {
                string state; int sev;
                ManagementObject o;
                if (!found.TryGetValue(n, out o)) { state = "Not installed"; sev = 2; }
                else
                {
                    PrinterState(o, out state, out sev);
                    // Windows often says "Ready" for a network printer that is actually stopped
                    // (Canon G6010 on the Lenovo, 7 Oct 2026: Windows Ready, printer in error).
                    // Two checks Windows does not do for us:
                    if (sev < 2) LastJobState(n, ref state, ref sev);
                    if (sev < 2) NetworkState(Convert.ToString(o["PortName"]), ref state, ref sev);
                }
                int waiting = WaitingJobs(n);
                h.Waiting[n] = waiting;
                // The list shows "Offline \u00B7 3 waiting"; the headline uses the plain state and its own count.
                h.Printers.Add(new string[] { n, waiting > 0 ? state + " \u00B7 " + waiting + " waiting" : state, sev.ToString() });
                // Worst live printer wins: a fault with jobs stuck beats a fault with none.
                if (sev == 2 && live.Contains(n) && (h.ProblemPrinter == null || waiting > h.ProblemWaiting))
                {
                    h.ProblemPrinter = n; h.ProblemState = state; h.ProblemWaiting = waiting;
                }
            }

            h.Rmm = RmmState();
            h.OtherCopies = OtherCopies();
            h.ShiftOpen = ShiftOpenHere();
            return h;
        }

        /// <summary>
        /// AB#3384: other copies of the print app running on this PC. V5 copies cannot start twice, but
        /// an older version (or one in another folder) can, and then two copies serve one terminal
        /// (Berkeley BFS-HP-6, 8 Oct 2026: an old copy in bin\Release beside the real one).
        /// </summary>
        private static List<string> OtherCopies()
        {
            List<string> result = new List<string>();
            try
            {
                int me = Process.GetCurrentProcess().Id;
                foreach (Process p in Process.GetProcessesByName("QuickfloraPrinting"))
                {
                    if (p.Id == me) continue;
                    string where;
                    try { where = p.MainModule.FileName; }
                    catch { where = "another Windows user's session"; }   // no access to other users' processes
                    result.Add(where + " (session " + p.SessionId + ")");
                }
            }
            catch { }
            return result;
        }

        /// <summary>Printers holding a QuickFlora job (receipt, worksheet, card...) unprinted for over 30 s.</summary>
        private static List<string> PrintersWithStuckQuickFloraJobs()
        {
            List<string> result = new List<string>();
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, Document, StatusMask, TimeSubmitted FROM Win32_PrintJob"))
                {
                    foreach (ManagementObject j in s.Get())
                    {
                        int mask = Convert.ToInt32(j["StatusMask"]);
                        if ((mask & (128 | 256 | 4096)) != 0) continue;
                        DateTime t = ManagementDateTimeConverter.ToDateTime(Convert.ToString(j["TimeSubmitted"]));
                        if ((DateTime.Now - t).TotalSeconds <= 30 || (DateTime.Now - t).TotalHours > 24) continue;
                        string doc = Convert.ToString(j["Document"]);
                        // Raw receipts are named QuickFlora-Print (clsPrinting); PDFs keep the server's
                        // file name, which always contains the company ID.
                        bool ours = doc.StartsWith("QuickFlora", StringComparison.OrdinalIgnoreCase)
                            || (Program.CompanyID.Length > 0 && doc.IndexOf(Program.CompanyID, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (!ours) continue;
                        string name = Convert.ToString(j["Name"]);
                        int comma = name.LastIndexOf(',');
                        if (comma > 0 && !result.Contains(name.Substring(0, comma))) result.Add(name.Substring(0, comma));
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>Jobs Windows is still holding for this printer (not printed, not complete) for over 30 s.</summary>
        private static int WaitingJobs(string printer)
        {
            int n = 0;
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, StatusMask, TimeSubmitted FROM Win32_PrintJob"))
                {
                    foreach (ManagementObject j in s.Get())
                    {
                        string name = Convert.ToString(j["Name"]);
                        int comma = name.LastIndexOf(',');
                        if (comma <= 0 || !name.Substring(0, comma).Equals(printer, StringComparison.OrdinalIgnoreCase)) continue;
                        int mask = Convert.ToInt32(j["StatusMask"]);
                        // 128 printed, 256 deleted, 4096 complete
                        if ((mask & (128 | 256 | 4096)) != 0) continue;
                        DateTime t = ManagementDateTimeConverter.ToDateTime(Convert.ToString(j["TimeSubmitted"]));
                        if ((DateTime.Now - t).TotalSeconds > 30) n++;
                    }
                }
            }
            catch { }
            return n;
        }

        /// <summary>
        /// The newest job Windows still holds for this printer. If it ended in error, offline or out
        /// of paper in the last hour, the printer has a problem whatever its status says. A later
        /// good job clears it, because only the newest job counts.
        /// </summary>
        private static void LastJobState(string printer, ref string state, ref int severity)
        {
            try
            {
                DateTime newest = DateTime.MinValue; int mask = 0;
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, StatusMask, TimeSubmitted FROM Win32_PrintJob"))
                {
                    foreach (ManagementObject j in s.Get())
                    {
                        string name = Convert.ToString(j["Name"]);   // "Printer Name, 12"
                        int comma = name.LastIndexOf(',');
                        if (comma <= 0 || !name.Substring(0, comma).Equals(printer, StringComparison.OrdinalIgnoreCase)) continue;
                        DateTime t = ManagementDateTimeConverter.ToDateTime(Convert.ToString(j["TimeSubmitted"]));
                        if (t > newest) { newest = t; mask = Convert.ToInt32(j["StatusMask"]); }
                    }
                }
                if (newest == DateTime.MinValue || (DateTime.Now - newest).TotalMinutes > 60) return;
                // JOB_STATUS_* flags: 2 error, 32 offline, 64 paper out, 512 blocked, 1024 user intervention
                if ((mask & 64) != 0) { state = "Out of paper"; severity = 2; }
                else if ((mask & 32) != 0) { state = "Offline"; severity = 2; }
                else if ((mask & (2 | 512 | 1024)) != 0) { state = "Error on last job"; severity = 2; }
            }
            catch { }
        }

        /// <summary>For a printer on a TCP/IP port: does anything answer on a printing port? Same
        /// ports and spirit as agent\PrinterWatch.ps1. USB and other ports are skipped.</summary>
        private static void NetworkState(string portName, ref string state, ref int severity)
        {
            try
            {
                if (string.IsNullOrEmpty(portName)) return;
                string host = null;
                System.Net.IPAddress ip;
                if (System.Net.IPAddress.TryParse(portName, out ip)) host = portName;
                else
                {
                    using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                        "SELECT HostAddress FROM Win32_TCPIPPrinterPort WHERE Name='" + portName.Replace("\\", "\\\\").Replace("'", "\\'") + "'"))
                    {
                        foreach (ManagementObject p in s.Get()) host = Convert.ToString(p["HostAddress"]);
                    }
                }
                if (string.IsNullOrEmpty(host)) return;
                foreach (int port in new int[] { 9100, 631, 515 })
                {
                    using (System.Net.Sockets.TcpClient c = new System.Net.Sockets.TcpClient())
                    {
                        IAsyncResult ar = c.BeginConnect(host, port, null, null);
                        if (ar.AsyncWaitHandle.WaitOne(800) && c.Connected) return;
                    }
                }
                state = "Not reachable on the network"; severity = 2;
            }
            catch { }
        }

        /// <summary>
        /// Tactical RMM agent: installed? running? and actually connected to our RMM server (it
        /// keeps an open connection while connected)? "Running" alone does not mean we can reach
        /// the PC — a shop firewall can block it.
        /// </summary>
        private static string RmmState()
        {
            try
            {
                int pid = 0; string svcState = null;
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT State, ProcessId FROM Win32_Service WHERE Name='tacticalrmm'"))
                {
                    foreach (ManagementObject o in s.Get())
                    {
                        svcState = Convert.ToString(o["State"]);
                        pid = Convert.ToInt32(o["ProcessId"]);
                    }
                }
                if (svcState == null) return "Not installed";
                if (svcState != "Running" || pid <= 0) return "Installed, not running";

                ProcessStartInfo psi = new ProcessStartInfo("netstat.exe", "-ano -p TCP");
                psi.UseShellExecute = false; psi.RedirectStandardOutput = true; psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    string pidText = pid.ToString();
                    foreach (string line in output.Split('\n'))
                    {
                        string[] f = line.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        // Proto  Local  Foreign  State  PID
                        if (f.Length >= 5 && f[3] == "ESTABLISHED" && f[4] == pidText &&
                            !f[2].StartsWith("127.") && !f[2].StartsWith("[::1]"))
                            return "Connected";
                    }
                }
                return "Running, not connected";
            }
            catch { return "Unknown"; }
        }

        /// <summary>Same wording as agent\PrinterWatch.ps1 so the shop and support see the same words.</summary>
        private static void PrinterState(ManagementObject o, out string state, out int severity)
        {
            state = "Ready"; severity = 0;
            try
            {
                if (o["WorkOffline"] != null && (bool)o["WorkOffline"]) { state = "Offline"; severity = 2; return; }
                int err = o["DetectedErrorState"] == null ? 0 : Convert.ToInt32(o["DetectedErrorState"]);
                switch (err)
                {
                    case 3: state = "Low paper"; severity = 1; return;
                    case 4: state = "Out of paper"; severity = 2; return;
                    case 5: state = "Low toner"; severity = 1; return;
                    case 6: state = "Out of toner"; severity = 2; return;
                    case 7: case 16: state = "Door open"; severity = 2; return;
                    case 8: state = "Jammed"; severity = 2; return;
                    case 9: state = "Offline"; severity = 2; return;
                    case 10: state = "Service needed"; severity = 2; return;
                    case 12: state = "Paper problem"; severity = 2; return;
                }
                int st = o["PrinterStatus"] == null ? 3 : Convert.ToInt32(o["PrinterStatus"]);
                if (st == 7) { state = "Offline"; severity = 2; }
                else if (st == 6) { state = "Stopped"; severity = 2; }
                else if (st == 4) { state = "Printing"; }
            }
            catch { }
        }

        private void ShowHealth(HealthInfo h)
        {
            try
            {
                lblIp.Text = h.Ip;
                lblRmm.Text = h.Rmm;
                monitorPrinters = h.Printers;
                lblRmm.ForeColor = h.Rmm == "Connected" ? Color.FromArgb(11, 90, 48) : Color.FromArgb(161, 35, 27);
                rmmState = h.Rmm;

                lstPrinters.BeginUpdate();
                lstPrinters.Items.Clear();
                foreach (string[] p in h.Printers)
                {
                    ListViewItem it = new ListViewItem("\u25CF  " + p[0]);
                    it.UseItemStyleForSubItems = false;
                    int sev = int.Parse(p[2]);
                    Color c = sev == 0 ? Color.FromArgb(11, 90, 48) : sev == 1 ? Color.FromArgb(122, 66, 6) : Color.FromArgb(161, 35, 27);
                    ListViewItem.ListViewSubItem s = it.SubItems.Add(p[1]);
                    s.ForeColor = c;
                    s.Font = new Font(lstPrinters.Font, FontStyle.Bold);
                    lstPrinters.Items.Add(it);
                }
                if (h.Printers.Count == 0) lstPrinters.Items.Add("No printer set in Config.txt");
                lstPrinters.EndUpdate();

                printerProblem = h.ProblemPrinter == null ? null : h.ProblemState;
                problemPrinter = h.ProblemPrinter;
                problemWaiting = h.ProblemWaiting;
                AlertPrinterProblem(h);
                // AB#3399: the drawer button only while a POS shift is open on this PC. If the server
                // could not be asked, keep what it was (hidden until the first answer).
                if (h.ShiftOpen.HasValue && btnTestDrawer.Visible != h.ShiftOpen.Value) btnTestDrawer.Visible = h.ShiftOpen.Value;
                otherCopies = h.OtherCopies;
                string copies = string.Join("; ", h.OtherCopies.ToArray());
                if (copies != reportedCopies)
                {
                    if (copies.Length > 0)
                        ReportEvent("Two print apps running", "", "pc=" + Environment.MachineName + " | also running=" + copies);
                    reportedCopies = copies;
                }
                bool changed = false;
                foreach (JobRow j in jobs)
                {
                    int w; h.Waiting.TryGetValue(j.Printer ?? "", out w);
                    bool waitingNow = j.Ok && w > 0 && h.ProblemPrinter != null
                        && j.Printer.Equals(h.ProblemPrinter, StringComparison.OrdinalIgnoreCase)
                        && (DateTime.Now - j.When).TotalMinutes <= 60;
                    if (waitingNow != j.Waiting) { j.Waiting = waitingNow; changed = true; }
                }
                if (changed) RefreshJobList();
                ShowCurrentStatus();

                // 4.0.2: status line for the POSN Order Print Logs page, at start and every 30 minutes.
                if ((DateTime.Now - lastStatusReport).TotalMinutes >= 30)
                {
                    lastStatusReport = DateTime.Now;
                    List<string> ps = new List<string>();
                    foreach (string[] p in h.Printers) ps.Add(p[0] + ": " + p[1]);
                    ReportEvent("Print app status", "", "pc=" + Environment.MachineName + " | ip=" + h.Ip
                        + " | pickup=" + (useLivePickup ? "live" : "poll") + " | rmm=" + h.Rmm
                        + " | printers=" + string.Join("; ", ps.ToArray()));
                }
            }
            catch { }
        }

        private DateTime lastStatusReport = DateTime.MinValue;

        // ==================== V5 (AB#3384, AB#3385) ====================

        private List<string> otherCopies = new List<string>();
        private string reportedCopies = "";
        private string reportedProblem;                 // "printer|state" last sent to the server, null when fine
        private string reportedProblemPrinter;
        private DateTime problemSince;
        private DateTime lastProblemBalloon = DateTime.MinValue;

        /// <summary>
        /// AB#3399: is a POS shift open on this terminal? Most shops only use the phone order form and
        /// never open a shift, so the "Open cash drawer" button is shown only while one is open.
        /// Null when the server cannot be asked (offline, or a print service without HasOpenShift).
        /// </summary>
        private static bool? ShiftOpenHere()
        {
            try
            {
                QFPrintService.QFPrintService svc = new QFPrintService.QFPrintService();
                svc.Timeout = 10000;
                return svc.HasOpenShift(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);
            }
            catch { return null; }
        }

        /// <summary>
        /// AB#3385: tell people the moment a printer orders go to stops printing. Before V5 the red
        /// banner was the only sign, and the window is usually hidden in the tray: Berkeley BFS-HP-6's
        /// Dell jammed on 8 Oct 2026 with 12 work tickets waiting and nobody knew.
        ///  - "Printer problem" goes to the server at once (Order Print Logs page), "Printer OK again" when it clears;
        ///  - a Windows notification from the tray icon, repeated every 10 minutes while it lasts.
        /// Never opens the window, so it cannot take over the screen during order entry.
        /// </summary>
        private void AlertPrinterProblem(HealthInfo h)
        {
            try
            {
                string key = h.ProblemPrinter == null ? null : h.ProblemPrinter + "|" + h.ProblemState;
                if (key != reportedProblem)
                {
                    if (key != null)
                    {
                        if (reportedProblem == null) problemSince = DateTime.Now;
                        ReportEvent("Printer problem", "", "pc=" + Environment.MachineName + " | printer=" + h.ProblemPrinter
                            + " | state=" + h.ProblemState + " | waiting=" + h.ProblemWaiting);
                        lastProblemBalloon = DateTime.MinValue;   // new or changed problem: notify now
                    }
                    else
                    {
                        ReportEvent("Printer OK again", "", "pc=" + Environment.MachineName + " | printer=" + reportedProblemPrinter
                            + " | after=" + (DateTime.Now - problemSince).TotalMinutes.ToString("0") + "min");
                    }
                    reportedProblem = key;
                    reportedProblemPrinter = h.ProblemPrinter;
                }
                if (key != null && (DateTime.Now - lastProblemBalloon).TotalMinutes >= 10)
                {
                    lastProblemBalloon = DateTime.Now;
                    notifyIcon1.ShowBalloonTip(10000, "Printer problem: " + h.ProblemPrinter,
                        h.ProblemState + (h.ProblemWaiting > 0 ? " \u2014 " + h.ProblemWaiting + (h.ProblemWaiting == 1 ? " order" : " orders") + " waiting" : "")
                        + ". Orders are not printing on this printer. Check it is on, has paper and no paper is stuck.",
                        ToolTipIcon.Error);
                }
            }
            catch { }
        }

}
}
