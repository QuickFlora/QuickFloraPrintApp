using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3189 (v3.5): the Settings button on the main window. Shows the same Config.txt values
    /// 3.4 displayed on the main screen (read-only, as they were), plus Start with Windows and
    /// the receipts folder. Nothing here changes how printing works.
    /// </summary>
    public class SettingsView : Form
    {
        private readonly CheckBox chkAutoStart = new CheckBox();

        public SettingsView(string company, string division, string department, string terminal,
                            string printer, string adobe, string configPath, EventHandler openReceipts)
        {
            Text = Program.Caption("Settings");
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 470);
            Padding = new Padding(24, 20, 24, 16);

            Label title = new Label();
            title.Text = "Settings";
            title.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            title.Dock = DockStyle.Top; title.Height = 40;

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Top; t.ColumnCount = 2; t.AutoSize = true;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            AddRow(t, "Company", company);
            AddRow(t, "Division", division);
            AddRow(t, "Department", department);
            AddRow(t, "Terminal", terminal);
            AddRow(t, "Printer", printer);
            AddRow(t, "Adobe path", adobe);

            Label note = new Label();
            note.Text = "QuickFlora Print App " + Program.AppVersion + ". These come from " + configPath + ". To change them, email support@quickflora.com.";
            note.ForeColor = Color.FromArgb(90, 97, 91);
            note.Font = new Font("Segoe UI", 9F);
            note.Dock = DockStyle.Top; note.Height = 44; note.Padding = new Padding(0, 8, 0, 0);

            chkAutoStart.Text = "Start the print app when Windows starts";
            chkAutoStart.Checked = Program.IsAutoStartEnabled();
            chkAutoStart.Dock = DockStyle.Top; chkAutoStart.Height = 36;
            chkAutoStart.CheckedChanged += delegate { Program.SetAutoStart(chkAutoStart.Checked); };

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom; buttons.Height = 46;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            Button close = MakeButton("Close", true);
            close.Click += delegate { Close(); };
            Button receipts = MakeButton("Open receipts folder", false);
            receipts.Click += openReceipts;
            buttons.Controls.Add(close);
            buttons.Controls.Add(receipts);
            AcceptButton = close; CancelButton = close;

            Controls.Add(chkAutoStart);
            Controls.Add(note);
            Controls.Add(t);
            Controls.Add(title);
            Controls.Add(buttons);
        }

        private static void AddRow(TableLayoutPanel t, string caption, string value)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            Label c = new Label();
            c.Text = caption; c.AutoSize = true; c.Anchor = AnchorStyles.Left;
            c.ForeColor = Color.FromArgb(90, 97, 91);
            TextBox v = new TextBox();
            v.Text = value; v.ReadOnly = true; v.BackColor = Color.FromArgb(247, 249, 246);
            v.BorderStyle = BorderStyle.FixedSingle; v.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            t.Controls.Add(c, 0, r);
            t.Controls.Add(v, 1, r);
        }

        private static Button MakeButton(string text, bool primary)
        {
            Button b = new Button();
            b.Text = text; b.AutoSize = true; b.MinimumSize = new Size(0, 38);
            b.Padding = new Padding(10, 0, 10, 0); b.Margin = new Padding(10, 0, 0, 0);
            b.FlatStyle = FlatStyle.Flat; b.UseVisualStyleBackColor = false;
            b.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            if (primary)
            {
                b.BackColor = Color.FromArgb(3, 106, 55); b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
            }
            else
            {
                b.BackColor = Color.White; b.ForeColor = Color.FromArgb(28, 33, 29);
                b.FlatAppearance.BorderColor = Color.FromArgb(201, 207, 200);
            }
            return b;
        }
    }
}
