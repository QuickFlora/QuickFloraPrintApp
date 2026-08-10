using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

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

        private const string SingleInstanceMutexName = "QuickfloraPrintingSingleInstance";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "QuickfloraPrinting";
        private const string PrefKeyPath = @"Software\QuickfloraPrinting";
        private const string AutoStartDisabledValueName = "AutoStartDisabled";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            bool startMinimized = false;
            foreach (string arg in args)
            {
                if (arg.Equals("/autostart", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-autostart", StringComparison.OrdinalIgnoreCase))
                {
                    startMinimized = true;
                }
            }

            // Single instance: if another copy is already running, bring its
            // window to the front and exit this one.
            bool createdNew;
            using (Mutex singleInstance = new Mutex(true, SingleInstanceMutexName, out createdNew))
            {
                if (!createdNew)
                {
                    BringExistingInstanceToFront();
                    return;
                }

                // Register to run at user login (idempotent, respects an
                // explicit opt-out made via the tray menu).
                EnsureAutoStart();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PrintHome(startMinimized));
            }
        }

        private static void BringExistingInstanceToFront()
        {
            try
            {
                // FindWindow matches hidden windows too, which matters because
                // the running copy is usually minimised to the tray.
                IntPtr hWnd = FindWindow(null, "QuickFlora Print ");
                if (hWnd == IntPtr.Zero)
                    return;

                const int SW_RESTORE = 9;
                ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }
            catch
            {
                // Best effort only - the new instance exits either way.
            }
        }

        // Registry value written to HKCU Run key. The /autostart switch makes
        // the app start minimised to the tray when Windows launches it.
        private static string AutoStartCommand
        {
            get { return "\"" + Application.ExecutablePath + "\" /autostart"; }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                {
                    return run != null && run.GetValue(RunValueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        // Called on every launch. Safe to re-run: it only rewrites the value
        // when it is missing or points at a stale path, and it does nothing
        // at all once the user has turned auto-start off.
        public static void EnsureAutoStart()
        {
            try
            {
                using (RegistryKey pref = Registry.CurrentUser.OpenSubKey(PrefKeyPath))
                {
                    if (pref != null &&
                        Convert.ToInt32(pref.GetValue(AutoStartDisabledValueName, 0)) != 0)
                    {
                        return;
                    }
                }

                using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (run == null)
                        return;

                    object current = run.GetValue(RunValueName);
                    if (current == null || current.ToString() != AutoStartCommand)
                    {
                        run.SetValue(RunValueName, AutoStartCommand);
                    }
                }
            }
            catch
            {
                // Never let a registration failure stop printing.
            }
        }

        // Backs the "Start with Windows" tray menu item.
        public static void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey pref = Registry.CurrentUser.CreateSubKey(PrefKeyPath))
                {
                    if (pref != null)
                        pref.SetValue(AutoStartDisabledValueName, enable ? 0 : 1);
                }

                using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (run == null)
                        return;

                    if (enable)
                    {
                        run.SetValue(RunValueName, AutoStartCommand);
                    }
                    else if (run.GetValue(RunValueName) != null)
                    {
                        run.DeleteValue(RunValueName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update the auto-start setting:\r\n" + ex.Message,
                    "QuickFlora Printing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
