using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using MetroUI.Componenti;
using MetroUI.Nativo;

namespace MetroUI.Controlli.Finestre
{
    public partial class MetroWindow
    {
        protected class MetroShadowForm : Form
        {
            protected MetroWindow TargetForm { get; private set; }
            private MetroShadowStruct MetroShadow;

            private readonly int wsExStyle;

            public MetroShadowForm(MetroWindow targetForm)
            {
                TargetForm = targetForm;
                this.wsExStyle = WS_EX_LAYERED | WS_EX_TOOLWINDOW;

                TargetForm.Move += OnTargetFormMove;
                TargetForm.FormClosing += TargetForm_FormClosing;

                //if (TargetForm.Owner != null)
                //    Owner = TargetForm.Owner;

                Owner = targetForm;

                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ShowIcon = false;
                FormBorderStyle = FormBorderStyle.None;
                MetroShadow = null;

                Bounds = GetShadowBounds();
            }

            void TargetForm_FormClosing(object sender, FormClosingEventArgs e)
            {
                this.Close();
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= wsExStyle;
                    return cp;
                }
            }

            private Rectangle GetShadowBounds()
            {
                Rectangle r = TargetForm.Bounds;
                r.Inflate(6, 6);
                r.Location = new Point(r.Location.X, r.Location.Y);

                return r;
            }

            public void UpdateDimension()
            {
                Bounds = GetShadowBounds();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);

                if (!DesignMode)
                {
                    Rectangle rc = this.Bounds;
                    rc.Inflate(-6, -6);
                    TargetForm.Bounds = rc;
                }
            }

            private void OnTargetFormMove(object sender, EventArgs e)
            {
                Bounds = GetShadowBounds();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (DesignMode == false && TargetForm.ShowShadowForm && TargetForm.MostraBordo)
                    DrawManager.DrawMetroWindowShadow(e.Graphics, TargetForm.VisualManager(), ref MetroShadow, this.Bounds, this);
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case (int)WinApi.Messages.WM_NCHITTEST:

                        WinApi.HitTest ht = HitTestNCA(m.HWnd, m.WParam, m.LParam);
                        if (ht != WinApi.HitTest.HTCLIENT)
                        {
                            m.Result = (IntPtr)ht;
                            return;
                        }
                        break;
                    case (int)WinApi.Messages.WM_NCLBUTTONUP:
                        TargetForm.Activate();
                        break;
                }

                base.WndProc(ref m);
            }

            private WinApi.HitTest HitTestNCA(IntPtr hwnd, IntPtr wparam, IntPtr lparam)
            {
                Rectangle windowRect = Bounds;
                Point cursorPoint = Cursor.Position;

                Rectangle top = new Rectangle(new Point(-6, -6), new Size(windowRect.Width, 6));
                Rectangle bottom = new Rectangle(new Point(-6, windowRect.Height - 6), new Size(windowRect.Width - 6, 6));
                Rectangle left = new Rectangle(new Point(-6, 0), new Size(6, windowRect.Height - 12));
                Rectangle right = new Rectangle(new Point(windowRect.Width - 6, 0), new Size(6, windowRect.Height - 12));
                Rectangle br = new Rectangle(new Point(windowRect.Width - 6, windowRect.Height - 6), new Size(6, 6));

                cursorPoint = PointToClient(cursorPoint);

                if (top.Contains(cursorPoint))
                    return WinApi.HitTest.HTTOP;

                if (bottom.Contains(cursorPoint))
                    return WinApi.HitTest.HTBOTTOM;

                if (left.Contains(cursorPoint))
                    return WinApi.HitTest.HTLEFT;

                if (right.Contains(cursorPoint))
                    return WinApi.HitTest.HTRIGHT;

                if (br.Contains(cursorPoint))
                    return WinApi.HitTest.HTBOTTOMRIGHT;

                return WinApi.HitTest.HTNOWHERE;
            }

            protected const int WS_EX_TRANSPARENT = 0x20;
            protected const int WS_EX_LAYERED = 0x80000;
            protected const int WS_EX_NOACTIVATE = 0x8000000;
            protected const int WS_EX_TOOLWINDOW = 0x80;
        }
    }
}
