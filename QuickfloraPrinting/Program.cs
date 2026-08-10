using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QuickfloraPrinting
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 


        public static string CompanyID = "Greene and Greene";
        public static string DivisionID = "DEFAULT";
        public static string DepartmentID = "DEFAULT";
        public static string CompanyName = "DEFAULT";
        public static string TerminalName = "DEFAULT";
        public static string rawPrinter = "";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PrintHome());
        }
    }
}