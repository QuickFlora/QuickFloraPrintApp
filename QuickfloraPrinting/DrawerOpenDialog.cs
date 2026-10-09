using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3399 (5.0.2): before the cash drawer opens, the person must give their employee ID and a reason.
    /// PrintHome records both, with the time, for the audit.
    /// </summary>
    public class DrawerOpenDialog : Form
    {
        private readonly TextBox txtEmployee = new TextBox();
        private readonly ComboBox cboReason = new ComboBox();
        private readonly TextBox txtNote = new TextBox();
        private readonly Label lblError = new Label();

        public string EmployeeID { get { return txtEmployee.Text.Trim(); } }
        public string Reason
        {
            get
            {
                string r = Convert.ToString(cboReason.SelectedItem);
                string n = txtNote.Text.Trim();
                return n.Length == 0 ? r : r + ": " + n;
            }
        }

        public DrawerOpenDialog()
        {
            Text = Program.Caption("Open cash drawer");
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 330);
            Padding = new Padding(24, 20, 24, 16);

            Label title = new Label();
            title.Text = "Open cash drawer";
            title.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            title.Dock = DockStyle.Top; title.Height = 40;

            Label sub = new Label();
            sub.Text = "Every drawer opening is recorded with your employee ID, the reason and the time.";
            sub.Dock = DockStyle.Top; sub.Height = 40;

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Top; t.ColumnCount = 2; t.AutoSize = true;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            cboReason.DropDownStyle = ComboBoxStyle.DropDownList;
            cboReason.Items.AddRange(new object[] { "Make change", "Customer payment", "Count the drawer", "Other" });
            txtEmployee.Dock = DockStyle.Fill; cboReason.Dock = DockStyle.Fill; txtNote.Dock = DockStyle.Fill;
            AddRow(t, "Employee ID", txtEmployee);
            AddRow(t, "Reason", cboReason);
            AddRow(t, "Note", txtNote);

            lblError.ForeColor = Color.FromArgb(161, 35, 27);
            lblError.Dock = DockStyle.Top; lblError.Height = 28;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom; buttons.FlowDirection = FlowDirection.RightToLeft; buttons.Height = 44;
            Button ok = new Button(); ok.Text = "Open drawer"; ok.Width = 130; ok.Height = 34;
            Button cancel = new Button(); cancel.Text = "Cancel"; cancel.Width = 100; cancel.Height = 34;
            cancel.DialogResult = DialogResult.Cancel;
            ok.Click += new EventHandler(ok_Click);
            buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;

            Controls.Add(buttons);
            Controls.Add(lblError);
            Controls.Add(t);
            Controls.Add(sub);
            Controls.Add(title);
        }

        private static void AddRow(TableLayoutPanel t, string label, Control c)
        {
            Label l = new Label();
            l.Text = label; l.AutoSize = true; l.Anchor = AnchorStyles.Left;
            l.Margin = new Padding(0, 8, 8, 8);
            c.Margin = new Padding(0, 4, 0, 4);
            t.Controls.Add(l); t.Controls.Add(c);
        }

        private void ok_Click(object sender, EventArgs e)
        {
            if (EmployeeID.Length == 0) { lblError.Text = "Enter your employee ID."; txtEmployee.Focus(); return; }
            if (cboReason.SelectedIndex < 0) { lblError.Text = "Choose a reason."; cboReason.Focus(); return; }
            if (Convert.ToString(cboReason.SelectedItem) == "Other" && txtNote.Text.Trim().Length == 0)
            { lblError.Text = "Type the reason in Note."; txtNote.Focus(); return; }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
