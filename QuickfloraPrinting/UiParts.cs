using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuickfloraPrinting
{
    /// <summary>AB#3189: white panel with a 1px grey border, used for every card on the main window.</summary>
    public class CardPanel : Panel
    {
        public CardPanel()
        {
            BackColor = Color.White;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(221, 226, 220)))
            {
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    /// <summary>
    /// AB#3189: the round badge at the left of the status card. Green tick when printing is
    /// working, amber "!" for a warning (e.g. no remote support), red "!" when not printing. Drawn rather than an image so it stays sharp at
    /// any display scale.
    /// </summary>
    public class StatusBadge : Control
    {
        private int level;

        public StatusBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }

        /// <summary>0 = green tick, 1 = amber "!", 2 = red "!".</summary>
        public int Level
        {
            get { return level; }
            set { level = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = Math.Min(Math.Min(Width - 16, Height), (int)(52 * g.DpiX / 96f));
            if (d <= 0) return;
            int x = 0, y = (Height - d) / 2;
            bool attention = level > 0;
            Color fill = level == 2 ? Color.FromArgb(246, 211, 207)
                       : level == 1 ? Color.FromArgb(252, 235, 211)
                       : Color.FromArgb(227, 241, 232);
            Color mark = level == 2 ? Color.FromArgb(161, 35, 27)
                       : level == 1 ? Color.FromArgb(166, 90, 8)
                       : Color.FromArgb(11, 107, 55);
            using (SolidBrush b = new SolidBrush(fill)) g.FillEllipse(b, x, y, d, d);
            using (Pen p = new Pen(mark, Math.Max(2f, d / 20f)))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                float cx = x + d / 2f, cy = y + d / 2f, u = d / 52f;
                if (attention)
                {
                    g.DrawLine(p, cx, cy - 11 * u, cx, cy + 3 * u);
                    g.DrawLine(p, cx, cy + 10 * u, cx, cy + 10.5f * u);
                }
                else
                {
                    g.DrawLines(p, new PointF[] {
                        new PointF(cx - 10 * u, cy + 1 * u),
                        new PointF(cx - 3 * u, cy + 8 * u),
                        new PointF(cx + 11 * u, cy - 7 * u) });
                }
            }
        }
    }
}
