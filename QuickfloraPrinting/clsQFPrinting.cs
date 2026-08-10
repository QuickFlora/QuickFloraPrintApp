using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QuickFloraEMV;

namespace QuickfloraPrinting
{
    class clsQFPrinting
    {

        public static bool printprocess;

         //Program.CompanyID = dt.Rows[0]["CompanyID"].ToString();
         //   Program.DivisionID = dt.Rows[0]["DivisionID"].ToString();
         //   Program.DepartmentID = dt.Rows[0]["DepartmentID"].ToString();
         //   Program.CompanyName = dt.Rows[0]["CompanyName"].ToString();
         //   Program.TerminalName = dt.Rows[0]["TerminalName"].ToString();
         //   Program.rawPrinter = dt.Rows[0]["PrinterName1"].ToString();


        public static void PrintProcessforShift()
        {

            QFPrint.ServiceSoap   obj = new QFPrint.ServiceSoapClient() ;

            

            //QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient()  ;

            if (printprocess == true )
            {
                //return; 
            }

            bool chk;
            chk = false;

            try
            {
                 
                chk = obj.CheckPOSShiftForPrintingstatus(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);
                //chk = obj.CheckPOSForPrintingstatus(Program.CompanyID,Program.DivisionID , Program.DepartmentID , Program.TerminalName);
      
            }
             
            catch (Exception ex)
            {
                //lblrefresh.Text += " Wait...";
            }


            if (chk)
            {
                printprocess = true;

                // lblrefresh.Text += " Print Request In Process";
                DataTable dt = new DataTable();
                try
                {
                    //dt = obj.CheckPOSForPrinting(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
                    dt = obj.CheckPOSShiftForPrinting(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);
                }
                catch (Exception ex)
                {
                    //lblrefresh.Text += " Wait...";
                    return;
                    printprocess = false  ;
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
                        printprocess = false ;
                    }

                    //tbValue.Text = PrintText;

                    if (PrintText != "")
                    {
                        string receipt = "";

                        try
                        {
                            receipt = obj.GETPOSShiftForPrinting(Program.CompanyID, Program.DivisionID, Program.DepartmentID, PrintText, Program.TerminalName);

                            // tbValue.Text = receipt;

                            QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(Program.rawPrinter, receipt);
                            obj.UpdatePOSShiftForPrinting(Program.CompanyID, Program.DivisionID, Program.DepartmentID, slno);
                            printprocess = false;

                            string flname = "";
                            flname = DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Millisecond.ToString();
                            System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\QuickfloraPrintingUpdated\\Receipts\\" + PrintText + "-" + flname + ".txt");

                            receipt = receipt.Replace("\n", Environment.NewLine);

                            file.WriteLine(receipt);
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            printprocess = false;
                        }
                       // receipt = obj.ReceiptPrintData(Program.CompanyID, Program.DivisionID, Program.DepartmentID, PrintText);
                       
                    }

                }


            }




        }




        public static void PrintProcessforOrderReturn()
        {

            QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient();
            //QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient()  ;

            if (printprocess == true)
            {
                //return; 
            }

            bool chk;
            chk = false;

            try
            {
                //chk = obj.CheckPOSForPrintingstatus(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
                chk = obj.CheckPOSReturnOrderPrintRequeststatus(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);

            }

            catch (Exception ex)
            {
                //lblrefresh.Text += " Wait...";
            }


            if (chk)
            {
                printprocess = true;

                // lblrefresh.Text += " Print Request In Process";
                DataTable dt = new DataTable();
                try
                {
                    //dt = obj.CheckPOSForPrinting(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
                    dt = obj.CheckPOSReturnOrderPrintRequestPrinting(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);
                }
                catch (Exception ex)
                {
                    //lblrefresh.Text += " Wait...";
                    return;
                    printprocess = false;
                }


                if (dt.Rows.Count > 0)
                {
                    //PrintText
                    string PrintText = "";
                    string shiftid = "";
                    int slno = 0;

                    try
                    {
                        PrintText = dt.Rows[0]["PrintText"].ToString();
                        slno = Convert.ToInt32(dt.Rows[0]["slno"].ToString());
                        shiftid = dt.Rows[0]["shiftid"].ToString();
                    }
                    catch (Exception ex)
                    {
                        printprocess = false;
                    }

                    //tbValue.Text = PrintText;

                    if (PrintText != "")
                    {
                        string receipt = "";

                        try
                        {
                            receipt = obj.GETPOSReturnOrderPrintRequestForPrinting(Program.CompanyID, Program.DivisionID, Program.DepartmentID, PrintText, shiftid, Program.TerminalName);

                            // tbValue.Text = receipt;

                            QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(Program.rawPrinter, receipt);
                            obj.UpdatePOSReturnOrderPrintRequest(Program.CompanyID, Program.DivisionID, Program.DepartmentID, slno);
                            printprocess = false;

                            string flname = "";
                            flname = DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Millisecond.ToString();
                            System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\QuickfloraPrintingUpdated\\Receipts\\" + PrintText + "-" + flname + ".txt");

                            receipt = receipt.Replace("\n", Environment.NewLine);

                            file.WriteLine(receipt);
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            printprocess = false;
                        }
                        // receipt = obj.ReceiptPrintData(Program.CompanyID, Program.DivisionID, Program.DepartmentID, PrintText);

                    }

                }


            }




        }



        public static void PrintProcessforPaidOut()
        {

            QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient();
            //QFPrint.ServiceSoap obj = new QFPrint.ServiceSoapClient()  ;

            if (printprocess == true)
            {
                //return; 
            }

            bool chk;
            chk = false;

            try
            {
                //chk = obj.CheckPOSForPrintingstatus(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
                chk = obj.CheckPOSPaidOutPrintRequestststatus(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);

            }

            catch (Exception ex)
            {
               string ex1;
               ex1 = ex.Message ;
            }


            if (chk)
            {
                printprocess = true;

                // lblrefresh.Text += " Print Request In Process";
                DataTable dt = new DataTable();
                try
                {
                    //dt = obj.CheckPOSForPrinting(txtcmp.Text.ToString(), txtDivision.Text.ToString(), txtdepartment.Text.ToString(), txtTerminal.Text.ToString());
                    dt = obj.CheckPOSPaidOutPrintRequestPrinting(Program.CompanyID, Program.DivisionID, Program.DepartmentID, Program.TerminalName);
                }
                catch (Exception ex)
                {
                    //lblrefresh.Text += " Wait...";
                    return;
                    printprocess = false;
                }


                if (dt.Rows.Count > 0)
                {
                    //PrintText
                    string PrintText = "";
                    string shiftid = "";
                    int slno = 0;

                    try
                    {
                        PrintText = dt.Rows[0]["PrintText"].ToString();
                        slno = Convert.ToInt32(dt.Rows[0]["slno"].ToString());
                        shiftid = dt.Rows[0]["shiftid"].ToString();
                    }
                    catch (Exception ex)
                    {
                        printprocess = false;
                    }

                    //tbValue.Text = PrintText;

                    if (PrintText != "")
                    {
                        string receipt = "";

                        try
                        {
                            receipt = obj.GETPOSPaidOutPrintRequest(Program.CompanyID, Program.DivisionID, Program.DepartmentID, PrintText, shiftid, Program.TerminalName);

                            // tbValue.Text = receipt;

                            QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(Program.rawPrinter, receipt);
                            obj.UpdatePOSPaidOutPrintRequest(Program.CompanyID, Program.DivisionID, Program.DepartmentID, slno);
                            printprocess = false;

                            string flname = "";
                            flname = DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Millisecond.ToString();
                            System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\QuickfloraPrintingUpdated\\Receipts\\" + PrintText + "-" + flname + ".txt");

                            receipt = receipt.Replace("\n", Environment.NewLine);

                            file.WriteLine(receipt);
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            printprocess = false;
                        }
                        // receipt = obj.ReceiptPrintData(Program.CompanyID, Program.DivisionID, Program.DepartmentID, PrintText);

                    }

                }


            }




        }





    }
}
