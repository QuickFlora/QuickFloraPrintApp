namespace QuickfloraPrinting
{
    partial class PrintHome
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // AB#3189 (v3.5) — main window rebuilt to the approved 3.5 design. UI only: the print loop,
        // web service calls and Config.txt handling are unchanged from 3.4.
        //
        // Why the rebuild: 3.4 was laid out with AutoScaleMode.Font at AutoScaleDimensions 9x20, i.e.
        // on a 150% display. On a 125% laptop Windows shrank the window to ~83% while some text did
        // not shrink, so the clock and "Copy Details for Support" were cut off and the status lines
        // overlapped (Lenovo test PC, 7 Oct 2026). This layout is authored at 96 DPI with
        // AutoScaleMode.Dpi, the exe declares itself DPI-aware (app.manifest), and every region is
        // docked rather than placed at fixed pixels, so it reflows at any size or scale.
        //
        // Colours: PMS 348 C #036A37 primary; status greens/reds kept dark enough for 4.5:1 text.
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintHome));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.autoStartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timerHealth = new System.Windows.Forms.Timer(this.components);
            this.timerConfirm = new System.Windows.Forms.Timer(this.components);

            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pnlHeaderRight = new System.Windows.Forms.TableLayoutPanel();
            this.lblShop = new System.Windows.Forms.Label();
            this.lblTerminalName = new System.Windows.Forms.Label();
            this.lblConn = new System.Windows.Forms.Label();

            this.pnlFooter = new System.Windows.Forms.Panel();
            this.lblHelp = new System.Windows.Forms.Label();
            this.flowFooter = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnCopyDiag = new System.Windows.Forms.Button();
            this.btnTestDrawer = new System.Windows.Forms.Button();

            this.pnlBody = new System.Windows.Forms.Panel();
            this.pnlStatus = new QuickfloraPrinting.CardPanel();
            this.badgeStatus = new QuickfloraPrinting.StatusBadge();
            this.pnlStatusText = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatusSub = new System.Windows.Forms.Label();
            this.btnTestPrint = new System.Windows.Forms.Button();
            this.pnlGap = new System.Windows.Forms.Panel();
            this.tblMain = new System.Windows.Forms.TableLayoutPanel();
            this.cardJobs = new QuickfloraPrinting.CardPanel();
            this.lblJobsTitle = new System.Windows.Forms.Label();
            this.lstJobs = new System.Windows.Forms.ListView();
            this.colTime = new System.Windows.Forms.ColumnHeader();
            this.colFile = new System.Windows.Forms.ColumnHeader();
            this.colForm = new System.Windows.Forms.ColumnHeader();
            this.colPrinter = new System.Windows.Forms.ColumnHeader();
            this.colTook = new System.Windows.Forms.ColumnHeader();
            this.colResult = new System.Windows.Forms.ColumnHeader();
            this.imgRowHeight = new System.Windows.Forms.ImageList(this.components);
            this.pnlRight = new System.Windows.Forms.TableLayoutPanel();
            this.cardPrinters = new QuickfloraPrinting.CardPanel();
            this.lblPrintersTitle = new System.Windows.Forms.Label();
            this.lstPrinters = new System.Windows.Forms.ListView();
            this.colPrinterName = new System.Windows.Forms.ColumnHeader();
            this.colPrinterState = new System.Windows.Forms.ColumnHeader();
            this.cardComputer = new QuickfloraPrinting.CardPanel();
            this.lblComputerTitle = new System.Windows.Forms.Label();
            this.tblComputer = new System.Windows.Forms.TableLayoutPanel();
            this.lblPcNameCap = new System.Windows.Forms.Label();
            this.lblPcName = new System.Windows.Forms.Label();
            this.lblIpCap = new System.Windows.Forms.Label();
            this.lblIp = new System.Windows.Forms.Label();
            this.lblUserCap = new System.Windows.Forms.Label();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblVersionCap = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblRmmCap = new System.Windows.Forms.Label();
            this.lblRmm = new System.Windows.Forms.Label();

            // Not shown on screen. The print loop and the support copy still read and write these
            // exactly as in 3.4 (Config.txt values, last activity text), so they stay as fields.
            this.txtcmp = new System.Windows.Forms.TextBox();
            this.txtDivision = new System.Windows.Forms.TextBox();
            this.txtdepartment = new System.Windows.Forms.TextBox();
            this.txtTerminal = new System.Windows.Forms.TextBox();
            this.txtadobe = new System.Windows.Forms.TextBox();
            this.txtdefaultprinter = new System.Windows.Forms.TextBox();
            this.lbltimer = new System.Windows.Forms.Label();
            this.lblprintrequest = new System.Windows.Forms.Label();
            this.lblprintfile = new System.Windows.Forms.Label();
            this.btnOpenReceipts = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.pnlHeaderRight.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.flowFooter.SuspendLayout();
            this.pnlBody.SuspendLayout();
            this.pnlStatus.SuspendLayout();
            this.pnlStatusText.SuspendLayout();
            this.tblMain.SuspendLayout();
            this.cardJobs.SuspendLayout();
            this.pnlRight.SuspendLayout();
            this.cardPrinters.SuspendLayout();
            this.cardComputer.SuspendLayout();
            this.tblComputer.SuspendLayout();
            this.SuspendLayout();

            System.Drawing.Color green = System.Drawing.Color.FromArgb(3, 106, 55);
            System.Drawing.Color ink = System.Drawing.Color.FromArgb(28, 33, 29);
            System.Drawing.Color grey = System.Drawing.Color.FromArgb(90, 97, 91);
            System.Drawing.Color ground = System.Drawing.Color.FromArgb(243, 245, 242);
            System.Drawing.Color line = System.Drawing.Color.FromArgb(201, 207, 200);
            System.Drawing.Font fBody = new System.Drawing.Font("Segoe UI", 10F);
            System.Drawing.Font fBold = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            System.Drawing.Font fTitle = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);

            //
            // notifyIcon1 / tray menu (unchanged behaviour)
            //
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = Program.WindowTitle;
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.autoStartToolStripMenuItem,
                this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.autoStartToolStripMenuItem.CheckOnClick = true;
            this.autoStartToolStripMenuItem.Name = "autoStartToolStripMenuItem";
            this.autoStartToolStripMenuItem.Text = "Start with Windows";
            this.autoStartToolStripMenuItem.CheckedChanged += new System.EventHandler(this.autoStartToolStripMenuItem_CheckedChanged);
            this.exitToolStripMenuItem.Image = global::QuickfloraPrinting.Properties.Resources.delete;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            //
            // timer1 — the print poll (unchanged: 5 s, re-armed by the poll callbacks)
            //
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // timerHealth — refreshes printer state and the "This computer" box every 30 s
            //
            this.timerHealth.Interval = 30000;
            this.timerHealth.Tick += new System.EventHandler(this.timerHealth_Tick);
            //
            // timerConfirm — AB#3164 (v4): every 2 s while jobs wait for Windows to say "printed"
            //
            this.timerConfirm.Interval = 2000;
            this.timerConfirm.Tick += new System.EventHandler(this.timerConfirm_Tick);

            //
            // ===== Header: logo left, shop + connection right =====
            //
            this.pnlHeader.BackColor = green;
            this.pnlHeader.Controls.Add(this.pnlHeaderRight);
            this.pnlHeader.Controls.Add(this.pictureBox1);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 64;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(8, 4, 20, 4);
            this.pnlHeader.Name = "pnlHeader";
            // Approved QuickFlora lockup (Resources\QFHEADER.jpg), not redrawn.
            this.pictureBox1.BackColor = green;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.pictureBox1.Image = global::QuickfloraPrinting.Properties.Resources.QFHEADER;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Width = 430;
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabStop = false;
            // Right side: two stacked lines + pill
            this.pnlHeaderRight.AutoSize = true;
            this.pnlHeaderRight.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlHeaderRight.BackColor = green;
            this.pnlHeaderRight.ColumnCount = 2;
            this.pnlHeaderRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pnlHeaderRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.pnlHeaderRight.RowCount = 2;
            this.pnlHeaderRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pnlHeaderRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pnlHeaderRight.Controls.Add(this.lblShop, 0, 0);
            this.pnlHeaderRight.Controls.Add(this.lblTerminalName, 0, 1);
            this.pnlHeaderRight.Controls.Add(this.lblConn, 1, 0);
            this.pnlHeaderRight.SetRowSpan(this.lblConn, 2);
            this.pnlHeaderRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlHeaderRight.Name = "pnlHeaderRight";
            this.lblShop.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.lblShop.AutoSize = true;
            this.lblShop.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblShop.ForeColor = System.Drawing.Color.White;
            this.lblShop.Margin = new System.Windows.Forms.Padding(0);
            this.lblShop.Name = "lblShop";
            this.lblShop.Text = "";
            this.lblTerminalName.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lblTerminalName.AutoSize = true;
            this.lblTerminalName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTerminalName.ForeColor = System.Drawing.Color.FromArgb(213, 233, 220);
            this.lblTerminalName.Margin = new System.Windows.Forms.Padding(0);
            this.lblTerminalName.Name = "lblTerminalName";
            this.lblTerminalName.Text = "";
            this.lblConn.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblConn.AutoSize = true;
            this.lblConn.BackColor = System.Drawing.Color.White;
            this.lblConn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblConn.ForeColor = System.Drawing.Color.FromArgb(11, 90, 48);
            this.lblConn.Margin = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblConn.Name = "lblConn";
            this.lblConn.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblConn.Text = "●  Connecting...";

            //
            // ===== Footer: help left, three buttons right =====
            //
            this.pnlFooter.BackColor = System.Drawing.Color.White;
            this.pnlFooter.Controls.Add(this.flowFooter);
            this.pnlFooter.Controls.Add(this.lblHelp);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Height = 56;
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(24, 0, 20, 0);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlFooter_Paint);
            this.lblHelp.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblHelp.AutoSize = false;
            this.lblHelp.Width = 420;
            this.lblHelp.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblHelp.ForeColor = grey;
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Text = "QuickFlora Print App " + Program.AppVersion + "   ·   Help: support@quickflora.com";
            this.lblHelp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.flowFooter.AutoSize = true;
            this.flowFooter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowFooter.Controls.Add(this.btnTestDrawer);
            this.flowFooter.Controls.Add(this.btnCopyDiag);
            this.flowFooter.Controls.Add(this.btnSettings);
            this.flowFooter.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowFooter.Padding = new System.Windows.Forms.Padding(0, 9, 0, 0);
            this.flowFooter.WrapContents = false;
            this.flowFooter.Name = "flowFooter";
            StyleSecondary(this.btnTestDrawer, "Open cash drawer", line, ink);
            this.btnTestDrawer.Name = "btnTestDrawer";
            this.btnTestDrawer.Click += new System.EventHandler(this.btnTestDrawer_Click);
            StyleSecondary(this.btnCopyDiag, "Copy details for support", line, ink);
            this.btnCopyDiag.Name = "btnCopyDiag";
            this.btnCopyDiag.Click += new System.EventHandler(this.btnCopyDiag_Click);
            StyleSecondary(this.btnSettings, "Settings", line, ink);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

            //
            // ===== Body =====
            //
            this.pnlBody.BackColor = ground;
            this.pnlBody.Controls.Add(this.tblMain);
            this.pnlBody.Controls.Add(this.pnlGap);
            this.pnlBody.Controls.Add(this.pnlStatus);
            this.pnlBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBody.Padding = new System.Windows.Forms.Padding(24, 18, 24, 18);
            this.pnlBody.Name = "pnlBody";
            //
            // Status card: badge, two lines, Print test page
            //
            this.pnlStatus.Controls.Add(this.pnlStatusText);
            this.pnlStatus.Controls.Add(this.btnTestPrint);
            this.pnlStatus.Controls.Add(this.badgeStatus);
            this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatus.Height = 88;
            this.pnlStatus.Padding = new System.Windows.Forms.Padding(20, 16, 20, 16);
            this.pnlStatus.Name = "pnlStatus";
            this.badgeStatus.Dock = System.Windows.Forms.DockStyle.Left;
            this.badgeStatus.Width = 72;
            this.badgeStatus.Name = "badgeStatus";
            this.pnlStatusText.Controls.Add(this.lblStatusSub);
            this.pnlStatusText.Controls.Add(this.lblStatus);
            this.pnlStatusText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlStatusText.Name = "pnlStatusText";
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = ink;
            this.lblStatus.Height = 32;
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Starting up";
            this.lblStatusSub.AutoEllipsis = true;
            this.lblStatusSub.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatusSub.Font = fBody;
            this.lblStatusSub.ForeColor = grey;
            this.lblStatusSub.Name = "lblStatusSub";
            this.lblStatusSub.Text = "Connecting to QuickFlora";
            this.btnTestPrint.BackColor = green;
            this.btnTestPrint.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnTestPrint.FlatAppearance.BorderSize = 0;
            this.btnTestPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestPrint.Font = fBold;
            this.btnTestPrint.ForeColor = System.Drawing.Color.White;
            this.btnTestPrint.Name = "btnTestPrint";
            this.btnTestPrint.Text = "Print test page";
            this.btnTestPrint.UseVisualStyleBackColor = false;
            this.btnTestPrint.Width = 160;
            this.btnTestPrint.Click += new System.EventHandler(this.btnTestPrint_Click);
            this.pnlGap.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGap.Height = 18;
            this.pnlGap.Name = "pnlGap";
            //
            // Main grid: jobs (fill) | printers + this computer (316)
            //
            this.tblMain.ColumnCount = 2;
            this.tblMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 372F));
            this.tblMain.RowCount = 1;
            this.tblMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMain.Controls.Add(this.cardJobs, 0, 0);
            this.tblMain.Controls.Add(this.pnlRight, 1, 0);
            this.tblMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMain.Margin = new System.Windows.Forms.Padding(0);
            this.tblMain.Name = "tblMain";
            // Recent print jobs
            this.cardJobs.Controls.Add(this.lstJobs);
            this.cardJobs.Controls.Add(this.lblJobsTitle);
            this.cardJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardJobs.Margin = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this.cardJobs.Padding = new System.Windows.Forms.Padding(1);
            this.cardJobs.Name = "cardJobs";
            StyleCardTitle(this.lblJobsTitle, "Recent print jobs on this computer", fTitle, ink);
            this.lblJobsTitle.Name = "lblJobsTitle";
            this.imgRowHeight.ImageSize = new System.Drawing.Size(1, 34);
            this.lstJobs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstJobs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colTime, this.colForm, this.colFile, this.colPrinter, this.colTook, this.colResult});
            this.colTime.Text = "Time"; this.colTime.Width = 90;
            this.colForm.Text = "Form"; this.colForm.Width = 120;
            this.colFile.Text = "File"; this.colFile.Width = 200;
            this.colPrinter.Text = "Printer"; this.colPrinter.Width = 150;
            this.colTook.Text = "Took"; this.colTook.Width = 64;
            this.colResult.Text = "Status"; this.colResult.Width = 112;
            this.lstJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstJobs.Font = fBody;
            this.lstJobs.ForeColor = ink;
            this.lstJobs.FullRowSelect = true;
            this.lstJobs.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstJobs.MultiSelect = false;
            this.lstJobs.Name = "lstJobs";
            this.lstJobs.SmallImageList = this.imgRowHeight;
            this.lstJobs.UseCompatibleStateImageBehavior = false;
            this.lstJobs.View = System.Windows.Forms.View.Details;
            this.lstJobs.Resize += new System.EventHandler(this.lstJobs_Resize);
            // Right column
            this.pnlRight.ColumnCount = 1;
            this.pnlRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlRight.RowCount = 2;
            this.pnlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.pnlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlRight.Controls.Add(this.cardPrinters, 0, 0);
            this.pnlRight.Controls.Add(this.cardComputer, 0, 1);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Margin = new System.Windows.Forms.Padding(0);
            this.pnlRight.Name = "pnlRight";
            // Printers
            this.cardPrinters.Controls.Add(this.lstPrinters);
            this.cardPrinters.Controls.Add(this.lblPrintersTitle);
            this.cardPrinters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardPrinters.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            this.cardPrinters.Padding = new System.Windows.Forms.Padding(1);
            this.cardPrinters.Name = "cardPrinters";
            StyleCardTitle(this.lblPrintersTitle, "Printers", fTitle, ink);
            this.lblPrintersTitle.Name = "lblPrintersTitle";
            this.lstPrinters.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstPrinters.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colPrinterName, this.colPrinterState});
            this.colPrinterName.Width = 170;
            this.colPrinterState.Width = 140;
            this.lstPrinters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPrinters.Font = fBody;
            this.lstPrinters.ForeColor = ink;
            this.lstPrinters.FullRowSelect = true;
            this.lstPrinters.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lstPrinters.Name = "lstPrinters";
            this.lstPrinters.SmallImageList = this.imgRowHeight;
            this.lstPrinters.UseCompatibleStateImageBehavior = false;
            this.lstPrinters.View = System.Windows.Forms.View.Details;
            // This computer
            this.cardComputer.Controls.Add(this.tblComputer);
            this.cardComputer.Controls.Add(this.lblComputerTitle);
            this.cardComputer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardComputer.Margin = new System.Windows.Forms.Padding(0);
            this.cardComputer.Padding = new System.Windows.Forms.Padding(1);
            this.cardComputer.Name = "cardComputer";
            StyleCardTitle(this.lblComputerTitle, "This computer", fTitle, ink);
            this.lblComputerTitle.Name = "lblComputerTitle";
            this.tblComputer.BackColor = System.Drawing.Color.White;
            this.tblComputer.ColumnCount = 2;
            this.tblComputer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tblComputer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblComputer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblComputer.Padding = new System.Windows.Forms.Padding(16, 10, 12, 8);
            this.tblComputer.Name = "tblComputer";
            this.tblComputer.RowCount = 6;
            for (int r = 0; r < 5; r++)
                this.tblComputer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tblComputer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            AddInfoRow(this.tblComputer, 0, this.lblPcNameCap, "PC name", this.lblPcName, fBody, grey, ink);
            AddInfoRow(this.tblComputer, 1, this.lblIpCap, "IP address", this.lblIp, fBody, grey, ink);
            AddInfoRow(this.tblComputer, 2, this.lblUserCap, "Signed in as", this.lblUser, fBody, grey, ink);
            AddInfoRow(this.tblComputer, 3, this.lblVersionCap, "App version", this.lblVersion, fBody, grey, ink);
            AddInfoRow(this.tblComputer, 4, this.lblRmmCap, "Remote support", this.lblRmm, fBody, grey, ink);

            //
            // PrintHome
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = ground;
            this.ClientSize = new System.Drawing.Size(1100, 700);
            this.Controls.Add(this.pnlBody);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.Font = fBody;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(940, 620);
            this.Name = "PrintHome";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            // Program.BringExistingInstanceToFront finds the running copy by this title
            // (single-instance, AB#1321), so both use Program.WindowTitle. Includes the version.
            this.Text = Program.WindowTitle;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PrintHome_FormClosing);
            this.Load += new System.EventHandler(this.PrintHome_Load);
            this.Move += new System.EventHandler(this.PrintHome_Move);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.pnlHeaderRight.ResumeLayout(false);
            this.pnlHeaderRight.PerformLayout();
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.flowFooter.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.pnlStatusText.ResumeLayout(false);
            this.pnlStatus.ResumeLayout(false);
            this.tblComputer.ResumeLayout(false);
            this.tblComputer.PerformLayout();
            this.cardComputer.ResumeLayout(false);
            this.cardPrinters.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            this.cardJobs.ResumeLayout(false);
            this.tblMain.ResumeLayout(false);
            this.pnlBody.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private static void StyleSecondary(System.Windows.Forms.Button b, string text,
                                           System.Drawing.Color border, System.Drawing.Color ink)
        {
            b.AutoSize = true;
            b.BackColor = System.Drawing.Color.White;
            b.FlatAppearance.BorderColor = border;
            b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            b.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            b.ForeColor = ink;
            b.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            b.MinimumSize = new System.Drawing.Size(0, 38);
            b.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            b.Text = text;
            b.UseVisualStyleBackColor = false;
        }

        private static void StyleCardTitle(System.Windows.Forms.Label l, string text,
                                           System.Drawing.Font font, System.Drawing.Color ink)
        {
            l.BackColor = System.Drawing.Color.White;
            l.Dock = System.Windows.Forms.DockStyle.Top;
            l.Font = font;
            l.ForeColor = ink;
            l.Height = 44;
            l.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            l.Text = text;
            l.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        private static void AddInfoRow(System.Windows.Forms.TableLayoutPanel t, int row,
                                       System.Windows.Forms.Label cap, string caption,
                                       System.Windows.Forms.Label value, System.Drawing.Font font,
                                       System.Drawing.Color grey, System.Drawing.Color ink)
        {
            cap.AutoSize = true; cap.Anchor = System.Windows.Forms.AnchorStyles.Left;
            cap.Font = font; cap.ForeColor = grey; cap.Text = caption;
            value.AutoEllipsis = true; value.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            value.Font = font; value.ForeColor = ink; value.Text = "";
            t.Controls.Add(cap, 0, row);
            t.Controls.Add(value, 1, row);
        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoStartToolStripMenuItem;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timerHealth;
        private System.Windows.Forms.Timer timerConfirm;

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TableLayoutPanel pnlHeaderRight;
        private System.Windows.Forms.Label lblShop;
        private System.Windows.Forms.Label lblTerminalName;
        private System.Windows.Forms.Label lblConn;

        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Label lblHelp;
        private System.Windows.Forms.FlowLayoutPanel flowFooter;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnCopyDiag;
        private System.Windows.Forms.Button btnTestDrawer;

        private System.Windows.Forms.Panel pnlBody;
        private QuickfloraPrinting.CardPanel pnlStatus;
        private QuickfloraPrinting.StatusBadge badgeStatus;
        private System.Windows.Forms.Panel pnlStatusText;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblStatusSub;
        private System.Windows.Forms.Button btnTestPrint;
        private System.Windows.Forms.Panel pnlGap;
        private System.Windows.Forms.TableLayoutPanel tblMain;
        private QuickfloraPrinting.CardPanel cardJobs;
        private System.Windows.Forms.Label lblJobsTitle;
        private System.Windows.Forms.ListView lstJobs;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ColumnHeader colFile;
        private System.Windows.Forms.ColumnHeader colForm;
        private System.Windows.Forms.ColumnHeader colPrinter;
        private System.Windows.Forms.ColumnHeader colTook;
        private System.Windows.Forms.ColumnHeader colResult;
        private System.Windows.Forms.ImageList imgRowHeight;
        private System.Windows.Forms.TableLayoutPanel pnlRight;
        private QuickfloraPrinting.CardPanel cardPrinters;
        private System.Windows.Forms.Label lblPrintersTitle;
        private System.Windows.Forms.ListView lstPrinters;
        private System.Windows.Forms.ColumnHeader colPrinterName;
        private System.Windows.Forms.ColumnHeader colPrinterState;
        private QuickfloraPrinting.CardPanel cardComputer;
        private System.Windows.Forms.Label lblComputerTitle;
        private System.Windows.Forms.TableLayoutPanel tblComputer;
        private System.Windows.Forms.Label lblPcNameCap;
        private System.Windows.Forms.Label lblPcName;
        private System.Windows.Forms.Label lblIpCap;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.Label lblUserCap;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblVersionCap;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblRmmCap;
        private System.Windows.Forms.Label lblRmm;

        private System.Windows.Forms.TextBox txtcmp;
        private System.Windows.Forms.TextBox txtDivision;
        private System.Windows.Forms.TextBox txtdepartment;
        private System.Windows.Forms.TextBox txtTerminal;
        private System.Windows.Forms.TextBox txtadobe;
        private System.Windows.Forms.TextBox txtdefaultprinter;
        private System.Windows.Forms.Label lbltimer;
        private System.Windows.Forms.Label lblprintrequest;
        private System.Windows.Forms.Label lblprintfile;
        private System.Windows.Forms.Button btnOpenReceipts;
    }
}
