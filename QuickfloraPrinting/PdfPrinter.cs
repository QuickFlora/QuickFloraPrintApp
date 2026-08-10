using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Management;

namespace QuickfloraPrinting
{
    public class Pdf
    {

       

        public static Boolean PrintPDFs(string pdfFileName, string adobepath, string useprintername)
        {
            try
            {


                //object printerName = useprintername;
                //ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
                //ManagementObjectCollection collection = searcher.Get();

                //foreach (ManagementObject currentObject in collection)
                //{

                //    if (currentObject["name"].ToString() == printerName.ToString())
                //    {

                //        currentObject.InvokeMethod("SetDefaultPrinter", new object[] { printerName });

                //    }

                //}

                Process proc = new Process();
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;


                proc.StartInfo.Verb = "print";


                //Define location of adobe reader/command line
                //switches to launch adobe in "print" mode
                proc.StartInfo.FileName = adobepath;
                proc.StartInfo.Arguments = String.Format(@"/p /h {0}", pdfFileName);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

               

                proc.Start();
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (proc.HasExited == false)
                {
                    proc.WaitForExit(10000);
                }

                proc.EnableRaisingEvents = true;

                proc.Close();
                KillAdobe("AcroRd32");
                return true;
            }
            catch (Exception ex)
            {
                var str = "";
                   str= ex.Message ;

                return false;
            }
        }

        //For whatever reason, sometimes adobe likes to be a stage 5 clinger.
        //So here we kill it with fire.
        private static bool KillAdobe(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses().Where(
                         clsProcess => clsProcess.ProcessName.StartsWith(name)))
            {
                clsProcess.Kill();
                return true;
            }
            return false;
        }
    }//END Class
}
