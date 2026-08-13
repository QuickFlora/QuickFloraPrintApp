namespace QuickfloraPrinting
{
    partial class SetupForm
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

        // AB#1326 — first-run setup, on Brand-Identity Guidelines Edition 2.
        //   PMS 348 #036A37 primary / PMS 2269 #80C56C surface / PMS 424 #656868 secondary text
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();

            this.grpCode = new System.Windows.Forms.GroupBox();
            this.lblCodeHelp = new System.Windows.Forms.Label();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.btnActivate = new System.Windows.Forms.Button();
            this.lblCodeStatus = new System.Windows.Forms.Label();

            this.grpDetails = new System.Windows.Forms.GroupBox();
            this.lblCompany = new System.Windows.Forms.Label();
            this.txtCompany = new System.Windows.Forms.TextBox();
            this.lblDivision = new System.Windows.Forms.Label();
            this.txtDivision = new System.Windows.Forms.TextBox();
            this.lblDepartment = new System.Windows.Forms.Label();
            this.txtDepartment = new System.Windows.Forms.TextBox();
            this.lblTerminal = new System.Windows.Forms.Label();
            this.txtTerminal = new System.Windows.Forms.TextBox();

            this.grpPrinter = new System.Windows.Forms.GroupBox();
            this.lblPrinter = new System.Windows.Forms.Label();
            this.cboPrinter = new System.Windows.Forms.ComboBox();
            this.lblPrinterHint = new System.Windows.Forms.Label();
            this.btnTestPrint = new System.Windows.Forms.Button();
            this.btnTestDrawer = new System.Windows.Forms.Button();
            this.lblAdobe = new System.Windows.Forms.Label();
            this.txtAdobe = new System.Windows.Forms.TextBox();
            this.lblAdobeHint = new System.Windows.Forms.Label();

            this.btnFinish = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.pnlHeader.SuspendLayout();
            this.grpCode.SuspendLayout();
            this.grpDetails.SuspendLayout();
            this.grpPrinter.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlHeader
            //
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(720, 72);
            this.pnlHeader.TabIndex = 0;
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(22, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(300, 28);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Set up QuickFlora Print";
            //
            // lblSubtitle
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(197)))), ((int)(((byte)(108)))));
            this.lblSubtitle.Location = new System.Drawing.Point(24, 45);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(400, 19);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "This only needs doing once on this till.";
            //
            // grpCode
            //
            this.grpCode.Controls.Add(this.lblCodeHelp);
            this.grpCode.Controls.Add(this.txtCode);
            this.grpCode.Controls.Add(this.btnActivate);
            this.grpCode.Controls.Add(this.lblCodeStatus);
            this.grpCode.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpCode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.grpCode.Location = new System.Drawing.Point(18, 86);
            this.grpCode.Name = "grpCode";
            this.grpCode.Size = new System.Drawing.Size(684, 108);
            this.grpCode.TabIndex = 1;
            this.grpCode.TabStop = false;
            this.grpCode.Text = "  Step 1 — Activation code  ";
            //
            // lblCodeHelp
            //
            this.lblCodeHelp.AutoSize = true;
            this.lblCodeHelp.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCodeHelp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblCodeHelp.Location = new System.Drawing.Point(18, 30);
            this.lblCodeHelp.Name = "lblCodeHelp";
            this.lblCodeHelp.Size = new System.Drawing.Size(600, 20);
            this.lblCodeHelp.TabIndex = 0;
            this.lblCodeHelp.Text = "Enter the code QuickFlora gave you for this till. No code? Fill in Step 2 by hand.";
            //
            // txtCode
            //
            this.txtCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCode.Font = new System.Drawing.Font("Consolas", 12F);
            this.txtCode.Location = new System.Drawing.Point(22, 56);
            this.txtCode.Name = "txtCode";
            this.txtCode.Size = new System.Drawing.Size(300, 29);
            this.txtCode.TabIndex = 1;
            //
            // btnActivate
            //
            this.btnActivate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.btnActivate.FlatAppearance.BorderSize = 0;
            this.btnActivate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActivate.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnActivate.ForeColor = System.Drawing.Color.White;
            this.btnActivate.Location = new System.Drawing.Point(332, 56);
            this.btnActivate.Name = "btnActivate";
            this.btnActivate.Size = new System.Drawing.Size(120, 29);
            this.btnActivate.TabIndex = 2;
            this.btnActivate.Text = "Activate";
            this.btnActivate.UseVisualStyleBackColor = false;
            this.btnActivate.Click += new System.EventHandler(this.btnActivate_Click);
            //
            // lblCodeStatus
            //
            this.lblCodeStatus.AutoSize = true;
            this.lblCodeStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCodeStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblCodeStatus.Location = new System.Drawing.Point(464, 62);
            this.lblCodeStatus.Name = "lblCodeStatus";
            this.lblCodeStatus.Size = new System.Drawing.Size(200, 20);
            this.lblCodeStatus.TabIndex = 3;
            this.lblCodeStatus.Text = "";
            //
            // grpDetails
            //
            this.grpDetails.Controls.Add(this.lblCompany);
            this.grpDetails.Controls.Add(this.txtCompany);
            this.grpDetails.Controls.Add(this.lblDivision);
            this.grpDetails.Controls.Add(this.txtDivision);
            this.grpDetails.Controls.Add(this.lblDepartment);
            this.grpDetails.Controls.Add(this.txtDepartment);
            this.grpDetails.Controls.Add(this.lblTerminal);
            this.grpDetails.Controls.Add(this.txtTerminal);
            this.grpDetails.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpDetails.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.grpDetails.Location = new System.Drawing.Point(18, 202);
            this.grpDetails.Name = "grpDetails";
            this.grpDetails.Size = new System.Drawing.Size(684, 130);
            this.grpDetails.TabIndex = 2;
            this.grpDetails.TabStop = false;
            this.grpDetails.Text = "  Step 2 — Shop details  ";
            //
            // lblCompany
            //
            this.lblCompany.AutoSize = true;
            this.lblCompany.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCompany.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblCompany.Location = new System.Drawing.Point(18, 34);
            this.lblCompany.Name = "lblCompany";
            this.lblCompany.Size = new System.Drawing.Size(80, 20);
            this.lblCompany.TabIndex = 0;
            this.lblCompany.Text = "Company";
            //
            // txtCompany
            //
            this.txtCompany.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCompany.Location = new System.Drawing.Point(110, 30);
            this.txtCompany.Name = "txtCompany";
            this.txtCompany.Size = new System.Drawing.Size(230, 27);
            this.txtCompany.TabIndex = 1;
            this.txtCompany.TextChanged += new System.EventHandler(this.Field_Changed);
            //
            // lblDivision
            //
            this.lblDivision.AutoSize = true;
            this.lblDivision.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDivision.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblDivision.Location = new System.Drawing.Point(360, 34);
            this.lblDivision.Name = "lblDivision";
            this.lblDivision.Size = new System.Drawing.Size(70, 20);
            this.lblDivision.TabIndex = 2;
            this.lblDivision.Text = "Division";
            //
            // txtDivision
            //
            this.txtDivision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDivision.Location = new System.Drawing.Point(452, 30);
            this.txtDivision.Name = "txtDivision";
            this.txtDivision.Size = new System.Drawing.Size(210, 27);
            this.txtDivision.TabIndex = 3;
            this.txtDivision.Text = "DEFAULT";
            this.txtDivision.TextChanged += new System.EventHandler(this.Field_Changed);
            //
            // lblDepartment
            //
            this.lblDepartment.AutoSize = true;
            this.lblDepartment.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDepartment.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblDepartment.Location = new System.Drawing.Point(18, 78);
            this.lblDepartment.Name = "lblDepartment";
            this.lblDepartment.Size = new System.Drawing.Size(90, 20);
            this.lblDepartment.TabIndex = 4;
            this.lblDepartment.Text = "Department";
            //
            // txtDepartment
            //
            this.txtDepartment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDepartment.Location = new System.Drawing.Point(110, 74);
            this.txtDepartment.Name = "txtDepartment";
            this.txtDepartment.Size = new System.Drawing.Size(230, 27);
            this.txtDepartment.TabIndex = 5;
            this.txtDepartment.Text = "DEFAULT";
            this.txtDepartment.TextChanged += new System.EventHandler(this.Field_Changed);
            //
            // lblTerminal
            //
            this.lblTerminal.AutoSize = true;
            this.lblTerminal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTerminal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblTerminal.Location = new System.Drawing.Point(360, 78);
            this.lblTerminal.Name = "lblTerminal";
            this.lblTerminal.Size = new System.Drawing.Size(80, 20);
            this.lblTerminal.TabIndex = 6;
            this.lblTerminal.Text = "This till";
            //
            // txtTerminal
            //
            this.txtTerminal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTerminal.Location = new System.Drawing.Point(452, 74);
            this.txtTerminal.Name = "txtTerminal";
            this.txtTerminal.Size = new System.Drawing.Size(210, 27);
            this.txtTerminal.TabIndex = 7;
            this.txtTerminal.TextChanged += new System.EventHandler(this.Field_Changed);
            //
            // grpPrinter
            //
            this.grpPrinter.Controls.Add(this.lblPrinter);
            this.grpPrinter.Controls.Add(this.cboPrinter);
            this.grpPrinter.Controls.Add(this.lblPrinterHint);
            this.grpPrinter.Controls.Add(this.btnTestPrint);
            this.grpPrinter.Controls.Add(this.btnTestDrawer);
            this.grpPrinter.Controls.Add(this.lblAdobe);
            this.grpPrinter.Controls.Add(this.txtAdobe);
            this.grpPrinter.Controls.Add(this.lblAdobeHint);
            this.grpPrinter.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpPrinter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.grpPrinter.Location = new System.Drawing.Point(18, 340);
            this.grpPrinter.Name = "grpPrinter";
            this.grpPrinter.Size = new System.Drawing.Size(684, 176);
            this.grpPrinter.TabIndex = 3;
            this.grpPrinter.TabStop = false;
            this.grpPrinter.Text = "  Step 3 — Receipt printer  ";
            //
            // lblPrinter
            //
            this.lblPrinter.AutoSize = true;
            this.lblPrinter.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPrinter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblPrinter.Location = new System.Drawing.Point(18, 34);
            this.lblPrinter.Name = "lblPrinter";
            this.lblPrinter.Size = new System.Drawing.Size(70, 20);
            this.lblPrinter.TabIndex = 0;
            this.lblPrinter.Text = "Printer";
            //
            // cboPrinter
            //
            this.cboPrinter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPrinter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboPrinter.Location = new System.Drawing.Point(110, 30);
            this.cboPrinter.Name = "cboPrinter";
            this.cboPrinter.Size = new System.Drawing.Size(552, 28);
            this.cboPrinter.TabIndex = 1;
            this.cboPrinter.SelectedIndexChanged += new System.EventHandler(this.Field_Changed);
            //
            // lblPrinterHint
            //
            this.lblPrinterHint.AutoSize = true;
            this.lblPrinterHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPrinterHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblPrinterHint.Location = new System.Drawing.Point(110, 62);
            this.lblPrinterHint.Name = "lblPrinterHint";
            this.lblPrinterHint.Size = new System.Drawing.Size(500, 17);
            this.lblPrinterHint.TabIndex = 2;
            this.lblPrinterHint.Text = "";
            //
            // btnTestPrint
            //
            this.btnTestPrint.BackColor = System.Drawing.Color.White;
            this.btnTestPrint.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(221)))), ((int)(((byte)(219)))));
            this.btnTestPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestPrint.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnTestPrint.Location = new System.Drawing.Point(110, 88);
            this.btnTestPrint.Name = "btnTestPrint";
            this.btnTestPrint.Size = new System.Drawing.Size(160, 38);
            this.btnTestPrint.TabIndex = 3;
            this.btnTestPrint.Text = "Test print";
            this.btnTestPrint.UseVisualStyleBackColor = false;
            this.btnTestPrint.Click += new System.EventHandler(this.btnTestPrint_Click);
            //
            // btnTestDrawer
            //
            this.btnTestDrawer.BackColor = System.Drawing.Color.White;
            this.btnTestDrawer.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(221)))), ((int)(((byte)(219)))));
            this.btnTestDrawer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestDrawer.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnTestDrawer.Location = new System.Drawing.Point(280, 88);
            this.btnTestDrawer.Name = "btnTestDrawer";
            this.btnTestDrawer.Size = new System.Drawing.Size(160, 38);
            this.btnTestDrawer.TabIndex = 4;
            this.btnTestDrawer.Text = "Test cash drawer";
            this.btnTestDrawer.UseVisualStyleBackColor = false;
            this.btnTestDrawer.Click += new System.EventHandler(this.btnTestDrawer_Click);
            //
            // lblAdobe
            //
            this.lblAdobe.AutoSize = true;
            this.lblAdobe.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAdobe.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblAdobe.Location = new System.Drawing.Point(18, 140);
            this.lblAdobe.Name = "lblAdobe";
            this.lblAdobe.Size = new System.Drawing.Size(80, 20);
            this.lblAdobe.TabIndex = 5;
            this.lblAdobe.Text = "Adobe";
            //
            // txtAdobe
            //
            this.txtAdobe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAdobe.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.txtAdobe.Location = new System.Drawing.Point(110, 138);
            this.txtAdobe.Name = "txtAdobe";
            this.txtAdobe.Size = new System.Drawing.Size(400, 25);
            this.txtAdobe.TabIndex = 6;
            //
            // lblAdobeHint
            //
            this.lblAdobeHint.AutoSize = true;
            this.lblAdobeHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblAdobeHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(101)))), ((int)(((byte)(104)))), ((int)(((byte)(104)))));
            this.lblAdobeHint.Location = new System.Drawing.Point(518, 142);
            this.lblAdobeHint.Name = "lblAdobeHint";
            this.lblAdobeHint.Size = new System.Drawing.Size(150, 17);
            this.lblAdobeHint.TabIndex = 7;
            this.lblAdobeHint.Text = "";
            //
            // btnFinish
            //
            this.btnFinish.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(106)))), ((int)(((byte)(55)))));
            this.btnFinish.FlatAppearance.BorderSize = 0;
            this.btnFinish.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFinish.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.btnFinish.ForeColor = System.Drawing.Color.White;
            this.btnFinish.Location = new System.Drawing.Point(502, 528);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new System.Drawing.Size(200, 46);
            this.btnFinish.TabIndex = 4;
            this.btnFinish.Text = "Finish setup";
            this.btnFinish.UseVisualStyleBackColor = false;
            this.btnFinish.Click += new System.EventHandler(this.btnFinish_Click);
            //
            // btnCancel
            //
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(221)))), ((int)(((byte)(219)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCancel.Location = new System.Drawing.Point(370, 528);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 46);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // SetupForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(720, 592);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnFinish);
            this.Controls.Add(this.grpPrinter);
            this.Controls.Add(this.grpDetails);
            this.Controls.Add(this.grpCode);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QuickFlora Print — Setup";
            this.Load += new System.EventHandler(this.SetupForm_Load);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.grpCode.ResumeLayout(false);
            this.grpCode.PerformLayout();
            this.grpDetails.ResumeLayout(false);
            this.grpDetails.PerformLayout();
            this.grpPrinter.ResumeLayout(false);
            this.grpPrinter.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.GroupBox grpCode;
        private System.Windows.Forms.Label lblCodeHelp;
        private System.Windows.Forms.TextBox txtCode;
        private System.Windows.Forms.Button btnActivate;
        private System.Windows.Forms.Label lblCodeStatus;
        private System.Windows.Forms.GroupBox grpDetails;
        private System.Windows.Forms.Label lblCompany;
        private System.Windows.Forms.TextBox txtCompany;
        private System.Windows.Forms.Label lblDivision;
        private System.Windows.Forms.TextBox txtDivision;
        private System.Windows.Forms.Label lblDepartment;
        private System.Windows.Forms.TextBox txtDepartment;
        private System.Windows.Forms.Label lblTerminal;
        private System.Windows.Forms.TextBox txtTerminal;
        private System.Windows.Forms.GroupBox grpPrinter;
        private System.Windows.Forms.Label lblPrinter;
        private System.Windows.Forms.ComboBox cboPrinter;
        private System.Windows.Forms.Label lblPrinterHint;
        private System.Windows.Forms.Button btnTestPrint;
        private System.Windows.Forms.Button btnTestDrawer;
        private System.Windows.Forms.Label lblAdobe;
        private System.Windows.Forms.TextBox txtAdobe;
        private System.Windows.Forms.Label lblAdobeHint;
        private System.Windows.Forms.Button btnFinish;
        private System.Windows.Forms.Button btnCancel;
    }
}
