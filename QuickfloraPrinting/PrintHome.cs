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
                notifyIcon1.ShowBalloonTip(3000, "QuickFlora Printing",
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
                notifyIcon1.ShowBalloonTip(3000, "QuickFlora Printing App",
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
            y = MessageBox.Show("Are you sure to Exit?", "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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
                "Setup not completed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

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

            string[] lines = System.IO.File.ReadAllLines(ResolveConfigPath());
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
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            lbltimer.Text = DateTime.Now.ToLongTimeString() ; 
 
            QFPrintService.QFPrintService obj = new QFPrintService.QFPrintService();
            obj.PingPOSForPrintingCompleted += new QFPrintService.PingPOSForPrintingCompletedEventHandler(obj_PingPOSForPrintingCompleted);
            obj.PingPOSForPrintingAsync(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName );
            timer1.Enabled = false;
         

        }

        void obj_PingPOSForPrintingCompleted(object sender, QFPrintService.PingPOSForPrintingCompletedEventArgs e)
        {
            string chk = "";


            try
            {
                chk = e.Result.ToString();
            }
            catch (Exception ex)
            {
                ReportError("obj_PingPOSForPrintingCompleted", "", ex);
                //   label1.Text += " Wait...";
                // return;
            }

            if (chk == "True")
            {
                lblprintrequest.Text = "New Print Request Found";
                lblprintrequest.ForeColor = Color.Green;
                lbltimer.ForeColor = Color.Red;
                SetStatus("Printing", "Sending a receipt to " + txtdefaultprinter.Text, false);
                
                processprint();
            }
            else
            {
                timer1.Enabled = true;
                lblprintrequest.Text = "No Print Request Present";
                lblprintrequest.ForeColor = Color.Red ;
                SetStatus("Printing is working", "Connected. Waiting for the next receipt.", false);
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

            try
            {
                PrintText = objDataTable.Rows[0]["PrintText"].ToString();
                PrintText1 = objDataTable.Rows[0]["PrintText1"].ToString();
                PrintText2 = objDataTable.Rows[0]["PrintText2"].ToString();
                FileName = objDataTable.Rows[0]["FileName"].ToString();
                slno = Convert.ToInt32(objDataTable.Rows[0]["slno"].ToString());

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
                    bool sentOk = QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(PrintText2, "C:\\QFPrintApp\\Receipts\\" + filename);
                    LogPrintJob(PrintText, filename, slno, PrintText2, detail, hadDrawer, fileSize, sentOk);
                }

                if (PrintText == "PDF")
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
                   // MessageBox.Show("Printer <" + sPrinterName + "> is invalid. \n The default printer will be used.", "Error with Printer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Microsoft.VisualBasic.Interaction.CallByName(WshNetwork, "SetDefaultPrinter", Microsoft.VisualBasic.CallType.Method, param);
                }

                // Specify the printer to use.
                pd.PrinterSettings.PrinterName = sPrinterName;
            }
            catch
            {
                //Revert to original default
                param[0] = sOldPrinter;
               // MessageBox.Show("Printer <" + sPrinterName + "> is invalid. \n The default printer will be used.", "Error with Printer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Microsoft.VisualBasic.Interaction.CallByName(WshNetwork, "SetDefaultPrinter", Microsoft.VisualBasic.CallType.Method, param);

            }
        }



        void obj_UpdatePOSForPrintingCompleted(object sender, QFPrintService.UpdatePOSForPrintingCompletedEventArgs e)
        {
            lblprintfile.Text = lblprintfile.Text + "\r\n" + "3.Print Completed.";
            timer1.Enabled = true;
        }







    
        // ==================== AB#1327 — new interface behaviour ====================

        /// <summary>Sets the status banner. Green = fine, amber = attention needed.</summary>
        private void SetStatus(string headline, string detail, bool attention)
        {
            try
            {
                lblStatus.Text = headline;
                lblStatusSub.Text = detail;
                // PMS 486 for attention states, PMS 348 otherwise — per Brand Guidelines Ed.2
                lblStatus.ForeColor = attention
                    ? Color.FromArgb(204, 124, 104)
                    : Color.FromArgb(3, 106, 55);
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
                    "No printer set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        "Cash drawer test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SetStatus("Cash drawer test failed", "Windows would not accept the job for " + printer, true);
                    MessageBox.Show("Windows would not send to this printer:\r\n\r\n    " + printer +
                        "\r\n\r\nCheck the printer is switched on and the name matches exactly.",
                        "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                ReportError("btnTestDrawer_Click", "", ex);
                MessageBox.Show("Could not send to the printer.\r\n\r\n" + ex.Message,
                    "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Prints a short sample receipt, then cuts. Does not open the drawer.</summary>
        private void btnTestPrint_Click(object sender, EventArgs e)
        {
            string printer = txtdefaultprinter.Text.Trim();
            if (printer.Length == 0)
            {
                MessageBox.Show("No printer is configured for this terminal.\r\n\r\nCheck line 6 of Config.txt.",
                    "No printer set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                ReportError("btnTestPrint_Click", "", ex);
                MessageBox.Show("Could not print.\r\n\r\n" + ex.Message,
                    "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    "Could not open folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ReportError("btnCopyDiag_Click", "", ex);
                MessageBox.Show("Could not gather details.\r\n\r\n" + ex.Message,
                    "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

}
}
