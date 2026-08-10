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

        private void PrintHome_Load(object sender, EventArgs e)
        {
            // Reflect the current auto-start state in the tray menu without
            // firing the CheckedChanged handler while we set it.
            loadingSettings = true;
            autoStartToolStripMenuItem.Checked = Program.IsAutoStartEnabled();
            loadingSettings = false;

            // Launched by the Windows auto-start entry: go straight to the
            // tray so staff are not interrupted.
            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }

            timer1.Enabled = true;

            string[] lines = System.IO.File.ReadAllLines(@"C:\\QFPrintApp\QuickfloraPrinting\Config.txt");
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
                //   label1.Text += " Wait...";
                // return;
            }

            if (chk == "True")
            {
                lblprintrequest.Text = "New Print Request Found";
                lblprintrequest.ForeColor = Color.Green;
                lbltimer.ForeColor = Color.Red;
                
                processprint();
            }
            else
            {
                timer1.Enabled = true;
                lblprintrequest.Text = "No Print Request Present";
                lblprintrequest.ForeColor = Color.Red ;
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
                    wc.DownloadFile(PrintText1, "C:\\QFPrintApp\\Receipts\\" + filename);
                    lblprintfile.Text = lblprintfile.Text + "\r\n" + "2.Printing file on printer :" + PrintText2;
                    QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(PrintText2, "C:\\QFPrintApp\\Receipts\\" + filename); 
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

                    wc.DownloadFile(PrintText1, "C:\\QFPrintApp\\PDF\\" + filename);
                    lblprintfile.Text = lblprintfile.Text + "\r\n" + "2.Printing file on printer :" + PrintText2;
                    SetDefaultSystemPrinter(PrintText2);
                    try
                    {
                        Pdf.PrintPDFs("C:\\QFPrintApp\\PDF\\" + filename, txtadobe.Text, PrintText2);
                    }
                    catch (Exception ex)
                    {
                        var str = "";
                        str = ex.Message;
                      //  MessageBox.Show(str);

                    }
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







    }
}
