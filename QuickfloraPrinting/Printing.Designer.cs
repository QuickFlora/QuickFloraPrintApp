namespace QuickfloraPrinting
{
    partial class Printing
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Printing));
            this.btnprint = new System.Windows.Forms.Button();
            this.tbValue = new System.Windows.Forms.TextBox();
            this.txtcmp = new System.Windows.Forms.TextBox();
            this.txtTerminal = new System.Windows.Forms.TextBox();
            this.txtdepartment = new System.Windows.Forms.TextBox();
            this.txtDivision = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.lblrefresh = new System.Windows.Forms.Label();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label5 = new System.Windows.Forms.Label();
            this.txtprinter = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.txtpdfprinter = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtcardprinter = new System.Windows.Forms.TextBox();
            this.printDocument1 = new System.Drawing.Printing.PrintDocument();
            this.printDialog1 = new System.Windows.Forms.PrintDialog();
            this.label8 = new System.Windows.Forms.Label();
            this.txtadobe = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtdefaultprinter = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnprint
            // 
            this.btnprint.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnprint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnprint.Location = new System.Drawing.Point(126, 431);
            this.btnprint.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnprint.Name = "btnprint";
            this.btnprint.Size = new System.Drawing.Size(112, 48);
            this.btnprint.TabIndex = 6;
            this.btnprint.Text = "Start";
            this.btnprint.UseVisualStyleBackColor = true;
            this.btnprint.Click += new System.EventHandler(this.button1_Click);
            // 
            // tbValue
            // 
            this.tbValue.Location = new System.Drawing.Point(40, 83);
            this.tbValue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbValue.Multiline = true;
            this.tbValue.Name = "tbValue";
            this.tbValue.Size = new System.Drawing.Size(547, 319);
            this.tbValue.TabIndex = 1;
            // 
            // txtcmp
            // 
            this.txtcmp.Location = new System.Drawing.Point(286, 495);
            this.txtcmp.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtcmp.Name = "txtcmp";
            this.txtcmp.Size = new System.Drawing.Size(240, 26);
            this.txtcmp.TabIndex = 2;
            // 
            // txtTerminal
            // 
            this.txtTerminal.Location = new System.Drawing.Point(286, 626);
            this.txtTerminal.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtTerminal.Name = "txtTerminal";
            this.txtTerminal.Size = new System.Drawing.Size(240, 26);
            this.txtTerminal.TabIndex = 5;
            // 
            // txtdepartment
            // 
            this.txtdepartment.Location = new System.Drawing.Point(286, 578);
            this.txtdepartment.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtdepartment.Name = "txtdepartment";
            this.txtdepartment.Size = new System.Drawing.Size(240, 26);
            this.txtdepartment.TabIndex = 4;
            // 
            // txtDivision
            // 
            this.txtDivision.Location = new System.Drawing.Point(286, 535);
            this.txtDivision.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtDivision.Name = "txtDivision";
            this.txtDivision.Size = new System.Drawing.Size(240, 26);
            this.txtDivision.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(66, 503);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "CompanyID";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(66, 637);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "TerminalID";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(66, 589);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "DepartmetID";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(66, 540);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 20);
            this.label4.TabIndex = 9;
            this.label4.Text = "DivisionId";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // lblrefresh
            // 
            this.lblrefresh.AutoSize = true;
            this.lblrefresh.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblrefresh.Location = new System.Drawing.Point(76, 25);
            this.lblrefresh.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblrefresh.Name = "lblrefresh";
            this.lblrefresh.Size = new System.Drawing.Size(0, 29);
            this.lblrefresh.TabIndex = 10;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Quickflora Printing App\r\n";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(112, 34);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Image = global::QuickfloraPrinting.Properties.Resources.delete;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(111, 30);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(68, 680);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(92, 20);
            this.label5.TabIndex = 12;
            this.label5.Text = "POS Printer";
            // 
            // txtprinter
            // 
            this.txtprinter.Location = new System.Drawing.Point(288, 669);
            this.txtprinter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtprinter.Name = "txtprinter";
            this.txtprinter.Size = new System.Drawing.Size(240, 26);
            this.txtprinter.TabIndex = 11;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.Red;
            this.button1.Location = new System.Drawing.Point(302, 431);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 48);
            this.button1.TabIndex = 13;
            this.button1.Text = "Stop";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(230, 1017);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(112, 35);
            this.button2.TabIndex = 15;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Visible = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(66, 720);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(87, 20);
            this.label6.TabIndex = 17;
            this.label6.Text = " WT Printer";
            // 
            // txtpdfprinter
            // 
            this.txtpdfprinter.Location = new System.Drawing.Point(286, 714);
            this.txtpdfprinter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtpdfprinter.Name = "txtpdfprinter";
            this.txtpdfprinter.Size = new System.Drawing.Size(240, 26);
            this.txtpdfprinter.TabIndex = 16;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(68, 768);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(93, 20);
            this.label7.TabIndex = 19;
            this.label7.Text = "Card Printer";
            // 
            // txtcardprinter
            // 
            this.txtcardprinter.Location = new System.Drawing.Point(288, 762);
            this.txtcardprinter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtcardprinter.Name = "txtcardprinter";
            this.txtcardprinter.Size = new System.Drawing.Size(240, 26);
            this.txtcardprinter.TabIndex = 18;
            // 
            // printDialog1
            // 
            this.printDialog1.Document = this.printDocument1;
            this.printDialog1.UseEXDialog = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(225, 926);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(93, 20);
            this.label8.TabIndex = 21;
            this.label8.Text = "Adobe Path";
            // 
            // txtadobe
            // 
            this.txtadobe.Location = new System.Drawing.Point(8, 968);
            this.txtadobe.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtadobe.Name = "txtadobe";
            this.txtadobe.Size = new System.Drawing.Size(674, 26);
            this.txtadobe.TabIndex = 20;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(62, 817);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(111, 20);
            this.label9.TabIndex = 23;
            this.label9.Text = "Default Printer";
            // 
            // txtdefaultprinter
            // 
            this.txtdefaultprinter.Location = new System.Drawing.Point(282, 811);
            this.txtdefaultprinter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtdefaultprinter.Name = "txtdefaultprinter";
            this.txtdefaultprinter.Size = new System.Drawing.Size(240, 26);
            this.txtdefaultprinter.TabIndex = 22;
            // 
            // Printing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(723, 1038);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.txtdefaultprinter);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtadobe);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtcardprinter);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtpdfprinter);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtprinter);
            this.Controls.Add(this.lblrefresh);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtDivision);
            this.Controls.Add(this.txtdepartment);
            this.Controls.Add(this.txtTerminal);
            this.Controls.Add(this.txtcmp);
            this.Controls.Add(this.tbValue);
            this.Controls.Add(this.btnprint);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Printing";
            this.Text = "QuickFlora Printing";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Printing_FormClosing);
            this.Load += new System.EventHandler(this.Printing_Load);
            this.Move += new System.EventHandler(this.Printing_Move);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnprint;
        private System.Windows.Forms.TextBox tbValue;
        private System.Windows.Forms.TextBox txtcmp;
        private System.Windows.Forms.TextBox txtTerminal;
        private System.Windows.Forms.TextBox txtdepartment;
        private System.Windows.Forms.TextBox txtDivision;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblrefresh;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtprinter;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtpdfprinter;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtcardprinter;
        private System.Drawing.Printing.PrintDocument printDocument1;
        private System.Windows.Forms.PrintDialog printDialog1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtadobe;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtdefaultprinter;
    }
}

