using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QuickFloraEMV;
using System.Management;

namespace QuickfloraPrinting
{
    public partial class Printing : Form
    {
        public Printing()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //QuickFloraEMV.RawPrinterHelper.SendStringToPrinter("zebra", tbValue.Text);
            lblrefresh.Text = "Running..." + DateTime.Now.ToString();
            timer1.Start();
            lblrefresh.ForeColor = Color.Green;



        }

        bool printprocess;

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblrefresh.Text = "Running..." + DateTime.Now.ToString();
            QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient();
            //QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient()  ;

            if (printprocess == true)
            {
                return;
            }




            bool chk;
            chk = false;

            try
            {
                chk = obj.CheckPOSForPrintingstatus(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
            }

            catch (Exception ex)
            {
                lblrefresh.Text += " Wait...";
            }


            if (chk)
            {
                printprocess = true;

                lblrefresh.Text += " Print Request In Process";
                DataTable dt = new DataTable();
                try
                {
                    dt = obj.CheckPOSForPrinting(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
                }
                catch (Exception ex)
                {
                    lblrefresh.Text += " Wait...";
                    //return; 
                }


                if (dt.Rows.Count > 0)
                {
                    //PrintText
                    string PrintText = "";
                    int slno = 0;

                    try
                    {
                        PrintText = dt.Rows[0]["PrintText"].ToString();
                        slno = Convert.ToInt32(dt.Rows[0]["slno"].ToString());

                    }
                    catch (Exception ex)
                    {

                    }

                    tbValue.Text = PrintText + "  Printing..";

                    if (PrintText != "")
                    {
                        string receipt = "";

                        try
                        {
                            receipt = obj.ReceiptPrintData(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), PrintText);
                        }
                        catch (Exception ex)
                        {
                            lblrefresh.Text += " Wait...";
                            // return;
                        }



                        // tbValue.Text = receipt;

                        if (receipt.Trim() != "")
                        {
                            QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(txtprinter.Text, receipt);

                            string stslno = "";
                            stslno = slno.ToString();

                            System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\QuickfloraPrintingUpdated\\Receipts\\" + PrintText + "_" + stslno + ".txt");
                            file.WriteLine(receipt);
                            file.Close();

                        }




                        DataTable objDataTable = new DataTable();
                        if ("668822" != PrintText)
                        {
                            try
                            {
                                objDataTable = obj.ReceiptPDFPrintDataPickscreen(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), PrintText, txtTerminal.Text.ToString(), slno.ToString());
                            }
                            catch (Exception ex)
                            {
                                lblrefresh.Text += " Wait...";
                                // return;
                            }
                        }


                        if (objDataTable.Rows.Count > 0)
                        {
                            string strurl = "";

                            strurl = "https://secure.localflorist.com/PDF/";

                            string txturlmc = "";
                            string txturlwt = "";



                            try
                            {
                                txturlmc = objDataTable.Rows[0]["txturlmc"].ToString();
                                txturlwt = objDataTable.Rows[0]["txturlwt"].ToString();

                            }
                            catch (Exception ex)
                            {

                            }

                            tbValue.Text = tbValue.Text + Environment.NewLine + txturlmc;
                            tbValue.Text = tbValue.Text + Environment.NewLine;
                            tbValue.Text = tbValue.Text + Environment.NewLine + txturlwt;

                            System.Net.WebClient wc = new System.Net.WebClient();

                            if (txturlmc != "")
                            {
                                try
                                {
                                    wc.DownloadFile(strurl + txturlmc, "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlmc);
                                    // QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(txtcardprinter.Text, "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlmc); 
                                    Pdf.PrintPDFs("C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlmc, txtadobe.Text, txtcardprinter.Text);
                                }
                                catch (Exception ex)
                                {

                                }

                            }


                            if (txturlwt != "")
                            {


                                try
                                {

                                    //  MessageBox.Show("Printer:" + txtpdfprinter.Text);

                                    wc.DownloadFile(strurl + txturlwt, "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlwt);

                                    // MessageBox.Show("Printer:" + txtpdfprinter.Text);
                                    // MessageBox.Show("Path:" + "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlwt);  


                                    // QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(txtpdfprinter.Text, "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlwt);

                                    //printDocument1.PrinterSettings.PrinterName = txtpdfprinter.Text;
                                    //printDocument1.PrinterSettings.PrintFileName = "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlwt;
                                    //printDocument1.Print();

                                    Pdf.PrintPDFs("C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlwt, txtadobe.Text, txtpdfprinter.Text);
                                    // QuickFloraEMV.RawPrinterHelper.SendFileToPrinter(txtcardprinter.Text, "C:\\QuickfloraPrintingUpdated\\PDF\\" + txturlmc); 
                                }
                                catch (Exception ex)
                                {

                                }



                            }


                            object printerName = txtdefaultprinter.Text;
                            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
                            ManagementObjectCollection collection = searcher.Get();

                            foreach (ManagementObject currentObject in collection)
                            {

                                if (currentObject["name"].ToString() == printerName.ToString())
                                {

                                    currentObject.InvokeMethod("SetDefaultPrinter", new object[] { printerName });

                                }

                            }

                        }



                        obj.UpdatePOSForPrinting(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), slno);
                        printprocess = false;

                         
                    }

                }
            }
            else
            {
                lblrefresh.Text += " Print Request Checking";
            }

            try
            {
                clsQFPrinting.PrintProcessforShift();
            }
            catch (Exception ex)
            {

            }


            try
            {
                clsQFPrinting.PrintProcessforOrderReturn();
            }
            catch (Exception ex)
            {

            }


            try
            {
                clsQFPrinting.PrintProcessforPaidOut();
            }
            catch (Exception ex)
            {

            }


        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Printing_Load(object sender, EventArgs e)
        {
            timer1.Start();
            lblrefresh.ForeColor = Color.Green;
            string[] lines = System.IO.File.ReadAllLines(@"C:\QuickfloraPrintingUpdated\QuickfloraPrinting\Config.txt");
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
                    txtprinter.Text = line;
                    txtprinter.Enabled = false;
                    Program.rawPrinter = txtprinter.Text;
                }

                if (n == 6)
                {
                    txtpdfprinter.Text = line;
                    txtpdfprinter.Enabled = false;
                }

                if (n == 7)
                {
                    txtcardprinter.Text = line;
                    txtcardprinter.Enabled = false;
                }

                if (n == 8)
                {
                    txtadobe.Text = line;
                    txtadobe.Enabled = false;
                }

                if (n == 9)
                {
                    txtdefaultprinter.Text = line;
                    txtdefaultprinter.Enabled = false;
                }

                n = n + 1;
            }


            //timer1.Stop();
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

        private void Printing_FormClosing(object sender, FormClosingEventArgs e)
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

        private void Printing_Move(object sender, EventArgs e)
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

        private void button1_Click_1(object sender, EventArgs e)
        {
            timer1.Stop();
            lblrefresh.Text = "App stopped Please Click Start Button";
            lblrefresh.ForeColor = Color.Red;


        }

        private void button2_Click(object sender, EventArgs e)
        {

            QuickFloraEMV.RawPrinterHelper.SendFileToPrinter("CutePDF Writer", "C:\\temp.pdf");
        }
    }
}
