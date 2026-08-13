using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#1326 — first-run setup.
    ///
    /// Replaces the old process, which asked a florist to copy files to an exact folder and then
    /// edit Config.txt BY LINE NUMBER with six values they have no way of knowing. That is how a
    /// machine ended up configured as "QuickfloraDemo" printing to "Snagit 2020" — silently
    /// producing no receipts, with nothing on screen to say why.
    ///
    /// This form writes Config.txt itself, in the same six-line format, so nothing else in the
    /// application had to change.
    /// </summary>
    public partial class SetupForm : Form
    {
        private static readonly Color Pms348 = Color.FromArgb(3, 106, 55);
        private static readonly Color Pms424 = Color.FromArgb(101, 104, 104);
        private static readonly Color Pms486 = Color.FromArgb(204, 124, 104);

        /// <summary>Full path of the Config.txt this form will write.</summary>
        public string ConfigPath { get; private set; }

        public SetupForm(string configPath)
        {
            ConfigPath = configPath;
            InitializeComponent();
        }

        private void SetupForm_Load(object sender, EventArgs e)
        {
            LoadPrinters();
            LoadExistingIfAny();
            DetectAdobe();
            UpdateReadiness();
        }

        /// <summary>Populate the printer dropdowns from what Windows can actually see.</summary>
        private void LoadPrinters()
        {
            cboPrinter.Items.Clear();
            try
            {
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    cboPrinter.Items.Add(p);
                }
            }
            catch { }

            if (cboPrinter.Items.Count == 0)
            {
                lblPrinterHint.Text = "No printers found. Add the receipt printer in Windows first, then reopen this.";
                lblPrinterHint.ForeColor = Pms486;
                return;
            }

            // Best guess: a receipt printer is usually a Star, Epson TM, or has "receipt"/"POS"
            // in the name. Only a suggestion — the user confirms.
            foreach (object item in cboPrinter.Items)
            {
                string n = Convert.ToString(item).ToLowerInvariant();
                if (n.Contains("star") || n.Contains("tsp") || n.Contains("receipt")
                    || n.Contains("tm-t") || n.Contains("pos"))
                {
                    cboPrinter.SelectedItem = item;
                    lblPrinterHint.Text = "Suggested — this looks like a receipt printer. Change it if that's wrong.";
                    lblPrinterHint.ForeColor = Pms424;
                    return;
                }
            }
            lblPrinterHint.Text = "Choose the printer that prints customer receipts.";
            lblPrinterHint.ForeColor = Pms424;
        }

        /// <summary>If a Config.txt already exists, pre-fill from it so this doubles as an edit screen.</summary>
        private void LoadExistingIfAny()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                string[] lines = File.ReadAllLines(ConfigPath);
                if (lines.Length > 0) txtCompany.Text = lines[0].Trim();
                if (lines.Length > 1) txtDivision.Text = lines[1].Trim();
                if (lines.Length > 2) txtDepartment.Text = lines[2].Trim();
                if (lines.Length > 3) txtTerminal.Text = lines[3].Trim();
                if (lines.Length > 4) txtAdobe.Text = lines[4].Trim();
                if (lines.Length > 5)
                {
                    string p = lines[5].Trim();
                    if (cboPrinter.Items.Contains(p)) cboPrinter.SelectedItem = p;
                }
            }
            catch { }
        }

        /// <summary>Find Adobe Reader rather than asking a florist for a file path.</summary>
        private void DetectAdobe()
        {
            if (txtAdobe.Text.Trim().Length > 0 && File.Exists(txtAdobe.Text.Trim())) return;

            List<string> candidates = new List<string>();
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            foreach (string root in new string[] { pf86, pf })
            {
                if (string.IsNullOrEmpty(root)) continue;
                candidates.Add(Path.Combine(root, @"Adobe\Acrobat Reader DC\Reader\AcroRd32.exe"));
                candidates.Add(Path.Combine(root, @"Adobe\Acrobat DC\Acrobat\Acrobat.exe"));
                candidates.Add(Path.Combine(root, @"Adobe\Reader 11.0\Reader\AcroRd32.exe"));
                candidates.Add(Path.Combine(root, @"Adobe\Reader 10.0\Reader\AcroRd32.exe"));
                candidates.Add(Path.Combine(root, @"Adobe\Reader 9.0\Reader\AcroRd32.exe"));
            }

            foreach (string c in candidates)
            {
                try
                {
                    if (File.Exists(c))
                    {
                        txtAdobe.Text = c;
                        lblAdobeHint.Text = "Found automatically.";
                        lblAdobeHint.ForeColor = Pms424;
                        return;
                    }
                }
                catch { }
            }

            lblAdobeHint.Text = "Adobe Reader not found. Worksheets may not print, but receipts will.";
            lblAdobeHint.ForeColor = Pms486;
        }

        /// <summary>
        /// Validate an activation code against the server. The server records the activation
        /// against this machine's MAC address.
        /// </summary>
        private void btnActivate_Click(object sender, EventArgs e)
        {
            string token = txtCode.Text.Trim();
            if (token.Length == 0)
            {
                MessageBox.Show("Enter the activation code supplied by QuickFlora.",
                    "No code entered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnActivate.Enabled = false;
            lblCodeStatus.Text = "Checking with QuickFlora...";
            lblCodeStatus.ForeColor = Pms424;
            Application.DoEvents();

            try
            {
                string mac = GetMacAddress();
                QFPrintService.QFPrintService svc = new QFPrintService.QFPrintService();
                bool ok = svc.InsertMacRequest(token, mac);

                if (ok)
                {
                    lblCodeStatus.Text = "Code accepted. This PC is registered with QuickFlora.";
                    lblCodeStatus.ForeColor = Pms348;
                    // NOTE: the server validates the code and records the activation, but does not
                    // yet return the company/terminal settings — GetTokenData exists server-side but
                    // is not exposed as a WebMethod. Until it is, the fields below are still filled
                    // in manually. Tracked on AB#1326.
                    MessageBox.Show(
                        "Activation code accepted and this PC has been registered.\r\n\r\n" +
                        "Please still confirm the details below, then choose your receipt printer.",
                        "Activated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblCodeStatus.Text = "That code was not recognised.";
                    lblCodeStatus.ForeColor = Pms486;
                    MessageBox.Show(
                        "QuickFlora did not recognise that activation code.\r\n\r\n" +
                        "Check it was typed correctly, or email support@quickflora.com for a new one.\r\n\r\n" +
                        "You can still set this up manually below.",
                        "Code not recognised", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                lblCodeStatus.Text = "Could not reach QuickFlora.";
                lblCodeStatus.ForeColor = Pms486;
                MessageBox.Show(
                    "Could not reach QuickFlora to check the code.\r\n\r\n" +
                    "Check this PC is on the internet, or set up manually below.\r\n\r\n" + ex.Message,
                    "No connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnActivate.Enabled = true;
                UpdateReadiness();
            }
        }

        private static string GetMacAddress()
        {
            try
            {
                foreach (System.Net.NetworkInformation.NetworkInterface nic
                         in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                        && nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        string m = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(m)) return m;
                    }
                }
            }
            catch { }
            return Environment.MachineName;
        }

        /// <summary>Send only the drawer-kick byte, so hardware can be proved before finishing setup.</summary>
        private void btnTestDrawer_Click(object sender, EventArgs e)
        {
            string printer = Convert.ToString(cboPrinter.SelectedItem);
            if (string.IsNullOrEmpty(printer)) { NeedPrinter(); return; }
            try
            {
                QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(printer, "\u0007");
                MessageBox.Show("Open command sent to:\r\n\r\n    " + printer +
                    "\r\n\r\nDid the cash drawer open?\r\n\r\n" +
                    "YES - the printer, cable and drawer are all working.\r\n" +
                    "NO  - check the drawer cable is plugged into the printer.",
                    "Cash drawer test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not send to that printer.\r\n\r\n" + ex.Message,
                    "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTestPrint_Click(object sender, EventArgs e)
        {
            string printer = Convert.ToString(cboPrinter.SelectedItem);
            if (string.IsNullOrEmpty(printer)) { NeedPrinter(); return; }
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\r\n     QUICKFLORA SETUP TEST\r\n     ---------------------\r\n\r\n");
                sb.Append("  Company : " + txtCompany.Text.Trim() + "\r\n");
                sb.Append("  Terminal: " + txtTerminal.Text.Trim() + "\r\n");
                sb.Append("  Printer : " + printer + "\r\n");
                sb.Append("  Time    : " + DateTime.Now.ToString("dd MMM yyyy  h:mm:ss tt") + "\r\n\r\n");
                sb.Append("  If you can read this, printing works.\r\n\r\n\r\n");
                sb.Append("\u001B\u0064\u0030");   // cut. No drawer byte.
                QuickFloraEMV.RawPrinterHelper.SendStringToPrinter(printer, sb.ToString());
                MessageBox.Show("Test receipt sent to:\r\n\r\n    " + printer +
                    "\r\n\r\nDid it print and cut?",
                    "Test print", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not print.\r\n\r\n" + ex.Message,
                    "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NeedPrinter()
        {
            MessageBox.Show("Choose the receipt printer first.", "No printer chosen",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Field_Changed(object sender, EventArgs e)
        {
            UpdateReadiness();
        }

        /// <summary>Finish is only enabled when the settings are actually usable.</summary>
        private void UpdateReadiness()
        {
            bool ready =
                txtCompany.Text.Trim().Length > 0 &&
                txtDivision.Text.Trim().Length > 0 &&
                txtDepartment.Text.Trim().Length > 0 &&
                txtTerminal.Text.Trim().Length > 0 &&
                cboPrinter.SelectedItem != null;

            btnFinish.Enabled = ready;
            btnFinish.BackColor = ready ? Pms348 : Color.FromArgb(200, 200, 200);
        }

        /// <summary>
        /// Write Config.txt in the existing six-line format. Deliberately unchanged so the rest of
        /// the application, and every shop already running, is unaffected.
        /// </summary>
        private void btnFinish_Click(object sender, EventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Working folders the app expects.
                foreach (string sub in new string[] { "Receipts", "PDF" })
                {
                    try
                    {
                        string full = Path.Combine(dir, sub);
                        if (!Directory.Exists(full)) Directory.CreateDirectory(full);
                    }
                    catch { }
                }

                string[] lines = new string[]
                {
                    txtCompany.Text.Trim(),
                    txtDivision.Text.Trim(),
                    txtDepartment.Text.Trim(),
                    txtTerminal.Text.Trim(),
                    txtAdobe.Text.Trim(),
                    Convert.ToString(cboPrinter.SelectedItem).Trim()
                };
                File.WriteAllLines(ConfigPath, lines);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not save the settings to:\r\n\r\n    " + ConfigPath + "\r\n\r\n" +
                    "You may need to run QuickFlora Print as administrator, or install it somewhere " +
                    "your account can write to.\r\n\r\n" + ex.Message,
                    "Could not save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
