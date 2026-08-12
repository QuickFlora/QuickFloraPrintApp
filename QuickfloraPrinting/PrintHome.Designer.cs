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

        // AB#1327 — interface rebuilt against QuickFlora Brand-Identity Guidelines Edition 2.
        //   PMS 348 C  #036A37  primary: header, primary action, headings
        //   PMS 2269 C #80C56C  surface green: accents and OK states. Never body text.
        //   PMS 486 C  #CC7C68  warnings and attention states
        //   PMS 424 C  #656868  secondary text and captions
        //   Cool Grey 1 #E2DDDB panel washes and dividers
        //
        // Fonts: the guide specifies Montserrat (headings/labels) and Open Sans (body).
        // Neither ships with Windows, and a desktop app cannot fetch webfonts, so this build
        // falls back to Segoe UI. Bundling the real fonts is tracked separately — see AB#1327.
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintHome));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.autoStartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlStatus = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatusSub = new System.Windows.Forms.Label();
            this.lbltimer = new System.Windows.Forms.Label();
            this.btnTestDrawer = new System.Windows.Forms.Button();
            this.btnTestPrint = new System.Windows.Forms.Button();
            this.btnOpenReceipts = new System.Windows.Forms.Button();
            this.btnCopyDiag = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtDivision = new System.Windows.Forms.TextBox();
            this.txtdepartment = new System.Windows.Forms.TextBox();
            this.txtTerminal = new System.Windows.Forms.TextBox();
            this.txtcmp = new System.Windows.Forms.TextBox();
            this.txtadobe = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtdefaultprinter = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblprintfile = new System.Windows.Forms.Label();
            this.lblprintrequest = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.pnlStatus.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            //
            // pictureBox1 — brand header, approved reversed lockup on PMS 348
            //
            this.pictureBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox1.Image = global::QuickfloraPrinting.Properties.Resources.QFHEADER;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(999, 100);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            //
            // notifyIcon1
            //
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "QuickFlora Print App";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            //
            // contextMenuStrip1
            //
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoStartToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(213, 64);
            //
            // autoStartToolStripMenuItem  (AB#1321)
            //
            this.autoStartToolStripMenuItem.CheckOnClick = true;
            this.autoStartToolStripMenuItem.Name = "autoStartToolStripMenuItem";
            this.autoStartToolStripMenuItem.Size = new System.Drawing.Size(212, 30);
            this.autoStartToolStripMenuItem.Text = "Start with Windows";
            this.autoStartToolStripMenuItem.CheckedChanged += new System.EventHandler(this.autoStartToolStripMenuItem_CheckedChanged);
            //
            // exitToolStripMenuItem
            //
            this.exitToolStripMenuItem.Image = global::QuickfloraPrinting.Properties.Resources.delete;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(212, 30);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            //
            // pnlStatus — status at a glance
            //
            this.pnlStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(246)))), ((int)(((byte)(242)))));
            this.pnlStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlStatus.Controls.Add(this.lbltimer);
            this.pnlStatus.Controls.Add(this.lblStatus);
            this.pnlStatus.Controls.Add(this.lblStatusSub);
            this.pnlStatus.Location = new System.Drawing.Point(12, 112);
            this.pnlStatus.Name = "pnlStatus";
            this.pnlStatus.Size = new System.Drawing.Size(975, 62);
            this.pnlStatus.TabIndex = 30;
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.lblStatus.Location = new System.Drawing.Point(14, 8);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(200, 25);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Starting up...";
            //
            // lblStatusSub
            //
            this.lblStatusSub.AutoSize = true;
            this.lblStatusSub.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblStatusSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblStatusSub.Location = new System.Drawing.Point(16, 36);
            this.lblStatusSub.Name = "lblStatusSub";
            this.lblStatusSub.Size = new System.Drawing.Size(120, 19);
            this.lblStatusSub.TabIndex = 1;
            this.lblStatusSub.Text = "Waiting for print jobs";
            //
            // lbltimer
            //
            this.lbltimer.AutoSize = true;
            this.lbltimer.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Regular);
            this.lbltimer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lbltimer.Location = new System.Drawing.Point(795, 16);
            this.lbltimer.Name = "lbltimer";
            this.lbltimer.Size = new System.Drawing.Size(0, 28);
            this.lbltimer.TabIndex = 2;
            //
            // btnTestDrawer — the one that settles hardware vs software in seconds
            //
            this.btnTestDrawer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.btnTestDrawer.FlatAppearance.BorderSize = 0;
            this.btnTestDrawer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestDrawer.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnTestDrawer.ForeColor = System.Drawing.Color.White;
            this.btnTestDrawer.Location = new System.Drawing.Point(12, 186);
            this.btnTestDrawer.Name = "btnTestDrawer";
            this.btnTestDrawer.Size = new System.Drawing.Size(236, 54);
            this.btnTestDrawer.TabIndex = 31;
            this.btnTestDrawer.Text = "Test Cash Drawer";
            this.btnTestDrawer.UseVisualStyleBackColor = false;
            this.btnTestDrawer.Click += new System.EventHandler(this.btnTestDrawer_Click);
            //
            // btnTestPrint
            //
            this.btnTestPrint.BackColor = System.Drawing.Color.White;
            this.btnTestPrint.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(221)))), ((int)(((byte)(219)))));
            this.btnTestPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestPrint.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnTestPrint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(33)))), ((int)(((byte)(29)))));
            this.btnTestPrint.Location = new System.Drawing.Point(256, 186);
            this.btnTestPrint.Name = "btnTestPrint";
            this.btnTestPrint.Size = new System.Drawing.Size(236, 54);
            this.btnTestPrint.TabIndex = 32;
            this.btnTestPrint.Text = "Test Print";
            this.btnTestPrint.UseVisualStyleBackColor = false;
            this.btnTestPrint.Click += new System.EventHandler(this.btnTestPrint_Click);
            //
            // btnOpenReceipts
            //
            this.btnOpenReceipts.BackColor = System.Drawing.Color.White;
            this.btnOpenReceipts.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(221)))), ((int)(((byte)(219)))));
            this.btnOpenReceipts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenReceipts.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnOpenReceipts.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(33)))), ((int)(((byte)(29)))));
            this.btnOpenReceipts.Location = new System.Drawing.Point(500, 186);
            this.btnOpenReceipts.Name = "btnOpenReceipts";
            this.btnOpenReceipts.Size = new System.Drawing.Size(236, 54);
            this.btnOpenReceipts.TabIndex = 33;
            this.btnOpenReceipts.Text = "Open Receipts Folder";
            this.btnOpenReceipts.UseVisualStyleBackColor = false;
            this.btnOpenReceipts.Click += new System.EventHandler(this.btnOpenReceipts_Click);
            //
            // btnCopyDiag
            //
            this.btnCopyDiag.BackColor = System.Drawing.Color.White;
            this.btnCopyDiag.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(221)))), ((int)(((byte)(219)))));
            this.btnCopyDiag.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopyDiag.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCopyDiag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(33)))), ((int)(((byte)(29)))));
            this.btnCopyDiag.Location = new System.Drawing.Point(744, 186);
            this.btnCopyDiag.Name = "btnCopyDiag";
            this.btnCopyDiag.Size = new System.Drawing.Size(243, 54);
            this.btnCopyDiag.TabIndex = 34;
            this.btnCopyDiag.Text = "Copy Details for Support";
            this.btnCopyDiag.UseVisualStyleBackColor = false;
            this.btnCopyDiag.Click += new System.EventHandler(this.btnCopyDiag_Click);
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label1.Location = new System.Drawing.Point(12, 604);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(300, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "QuickFlora POS Windows Print App";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label2.Location = new System.Drawing.Point(12, 624);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(600, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Check printer settings before use. Email support@quickflora.com for assistance.";
            //
            // label6  (CompanyID)
            //
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label6.Location = new System.Drawing.Point(14, 32);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 20);
            this.label6.TabIndex = 14;
            this.label6.Text = "Company";
            //
            // label4  (Division)
            //
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label4.Location = new System.Drawing.Point(14, 66);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 20);
            this.label4.TabIndex = 17;
            this.label4.Text = "Division";
            //
            // label3  (Department)
            //
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label3.Location = new System.Drawing.Point(14, 100);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 20);
            this.label3.TabIndex = 16;
            this.label3.Text = "Department";
            //
            // label5  (Terminal)
            //
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label5.Location = new System.Drawing.Point(14, 134);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 20);
            this.label5.TabIndex = 15;
            this.label5.Text = "Terminal";
            //
            // label7  (Receipt printer)
            //
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label7.Location = new System.Drawing.Point(14, 168);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(90, 20);
            this.label7.TabIndex = 20;
            this.label7.Text = "Printer";
            //
            // txtcmp
            //
            this.txtcmp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtcmp.Location = new System.Drawing.Point(120, 28);
            this.txtcmp.Name = "txtcmp";
            this.txtcmp.ReadOnly = true;
            this.txtcmp.Size = new System.Drawing.Size(240, 27);
            this.txtcmp.TabIndex = 10;
            //
            // txtDivision
            //
            this.txtDivision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDivision.Location = new System.Drawing.Point(120, 62);
            this.txtDivision.Name = "txtDivision";
            this.txtDivision.ReadOnly = true;
            this.txtDivision.Size = new System.Drawing.Size(240, 27);
            this.txtDivision.TabIndex = 11;
            //
            // txtdepartment
            //
            this.txtdepartment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtdepartment.Location = new System.Drawing.Point(120, 96);
            this.txtdepartment.Name = "txtdepartment";
            this.txtdepartment.ReadOnly = true;
            this.txtdepartment.Size = new System.Drawing.Size(240, 27);
            this.txtdepartment.TabIndex = 12;
            //
            // txtTerminal
            //
            this.txtTerminal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTerminal.Location = new System.Drawing.Point(120, 130);
            this.txtTerminal.Name = "txtTerminal";
            this.txtTerminal.ReadOnly = true;
            this.txtTerminal.Size = new System.Drawing.Size(240, 27);
            this.txtTerminal.TabIndex = 13;
            //
            // txtdefaultprinter
            //
            this.txtdefaultprinter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtdefaultprinter.Location = new System.Drawing.Point(120, 164);
            this.txtdefaultprinter.Name = "txtdefaultprinter";
            this.txtdefaultprinter.ReadOnly = true;
            this.txtdefaultprinter.Size = new System.Drawing.Size(240, 27);
            this.txtdefaultprinter.TabIndex = 19;
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.txtdefaultprinter);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.txtcmp);
            this.groupBox1.Controls.Add(this.txtTerminal);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtdepartment);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtDivision);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.groupBox1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.groupBox1.Location = new System.Drawing.Point(12, 254);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(380, 208);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "  This Terminal  ";
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.lblprintfile);
            this.groupBox2.Controls.Add(this.lblprintrequest);
            this.groupBox2.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.groupBox2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.groupBox2.Location = new System.Drawing.Point(404, 254);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(583, 208);
            this.groupBox2.TabIndex = 24;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "  Current Activity  ";
            //
            // lblprintrequest
            //
            this.lblprintrequest.AutoSize = true;
            this.lblprintrequest.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblprintrequest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblprintrequest.Location = new System.Drawing.Point(18, 34);
            this.lblprintrequest.Name = "lblprintrequest";
            this.lblprintrequest.Size = new System.Drawing.Size(0, 21);
            this.lblprintrequest.TabIndex = 0;
            //
            // lblprintfile
            //
            this.lblprintfile.AutoSize = true;
            this.lblprintfile.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblprintfile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblprintfile.Location = new System.Drawing.Point(18, 74);
            this.lblprintfile.MaximumSize = new System.Drawing.Size(545, 0);
            this.lblprintfile.Name = "lblprintfile";
            this.lblprintfile.Size = new System.Drawing.Size(0, 15);
            this.lblprintfile.TabIndex = 1;
            //
            // txtadobe
            //
            this.txtadobe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtadobe.Location = new System.Drawing.Point(120, 476);
            this.txtadobe.Name = "txtadobe";
            this.txtadobe.ReadOnly = true;
            this.txtadobe.Size = new System.Drawing.Size(867, 27);
            this.txtadobe.TabIndex = 21;
            //
            // label8
            //
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.label8.Location = new System.Drawing.Point(14, 480);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 20);
            this.label8.TabIndex = 22;
            this.label8.Text = "Adobe Path";
            //
            // lblVersion
            //
            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new System.Drawing.Font("Consolas", 8.5F);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblVersion.Location = new System.Drawing.Point(760, 624);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(200, 15);
            this.lblVersion.TabIndex = 35;
            this.lblVersion.Text = "";
            //
            // timer1
            //
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // PrintHome
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(999, 660);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.btnCopyDiag);
            this.Controls.Add(this.btnOpenReceipts);
            this.Controls.Add(this.btnTestPrint);
            this.Controls.Add(this.btnTestDrawer);
            this.Controls.Add(this.pnlStatus);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtadobe);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "PrintHome";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QuickFlora Print ";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PrintHome_FormClosing);
            this.Load += new System.EventHandler(this.PrintHome_Load);
            this.Move += new System.EventHandler(this.PrintHome_Move);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.pnlStatus.ResumeLayout(false);
            this.pnlStatus.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoStartToolStripMenuItem;
        private System.Windows.Forms.Panel pnlStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblStatusSub;
        private System.Windows.Forms.Button btnTestDrawer;
        private System.Windows.Forms.Button btnTestPrint;
        private System.Windows.Forms.Button btnOpenReceipts;
        private System.Windows.Forms.Button btnCopyDiag;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtDivision;
        private System.Windows.Forms.TextBox txtdepartment;
        private System.Windows.Forms.TextBox txtTerminal;
        private System.Windows.Forms.TextBox txtcmp;
        private System.Windows.Forms.TextBox txtadobe;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lbltimer;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblprintrequest;
        private System.Windows.Forms.Label lblprintfile;
        private System.Windows.Forms.TextBox txtdefaultprinter;
        private System.Windows.Forms.Label label7;
    }
}
