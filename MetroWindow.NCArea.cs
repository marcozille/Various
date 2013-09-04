using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

using MetroUI.Interfacce;
using MetroUI.Componenti;
using MetroUI.Nativo;
using MetroUI.Controlli.ControlliUtente;

namespace MetroUI.Controlli.Finestre
{
    public partial class MetroWindow
    {
        #region Disegno
        protected void NCPaint(ref Message m)
        {
            IntPtr hRgn = (IntPtr)m.WParam;

            IntPtr hDC = WinApi.GetWindowDC(m.HWnd);

            if (hDC == IntPtr.Zero)
                return;

            using (Graphics g = Graphics.FromHdc(hDC))
            {
                DrawCaption(g);
                DrawWindowButtons(g);
                DrawBorder(g);
            }

            WinApi.ReleaseDC(m.HWnd, hDC);
        }

        protected virtual void DrawBorder(Graphics g)
        {
            if (WindowState == FormWindowState.Normal && MostraBordo)
            {
                DrawManager.DrawWindowBorder(g, VisualManager(), WindowRectangle, IsActive);

                if(_metroShadowForm != null)
                    _metroShadowForm.Invalidate();
            }
        }

        protected virtual void DrawWindowButtons(Graphics g)
        {
            if (!MostraBarraTitolo)
                return;

            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].Paint(g);
            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].Paint(g);
            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].Paint(g);
        }

        protected virtual void DrawCaption(Graphics g)
        {
            if (!MostraBarraTitolo)
                return;

            Rectangle rectTitolo = new Rectangle(new Point(0, 0), new Size(WindowRectangle.Width - (MetroWindowButtons.Count * LarghezzaPulsanti), AltezzaBarraTitolo));
            g.FillRectangle(new SolidBrush(VisualManager().WindowBackColor), rectTitolo);
            
            int xTesto = 15;

            if (MostraIcona && Icona != null)
            {
                xTesto += AltezzaBarraTitolo + 3;
                int xIcona, yIcona;
                xIcona = (AltezzaBarraTitolo - Icona.Width) / 2 + 3;
                yIcona = (AltezzaBarraTitolo - Icona.Height) / 2;

                Rectangle rcIcona = new Rectangle(new Point(xIcona, yIcona), Icona.Size);
                g.DrawImage(Icona, rcIcona);
            }

            Rectangle rcTesto = new Rectangle(new Point(xTesto, 0), new Size(WindowRectangle.Width - (MetroWindowButtons.Count * LarghezzaPulsanti) - 15, AltezzaBarraTitolo));

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

            if (PosizioneTitolo == MetroWindowTextPos.Centro)
                flags |= TextFormatFlags.HorizontalCenter;
            else if (PosizioneTitolo == MetroWindowTextPos.Destra)
                flags |= TextFormatFlags.Right;
            else if (PosizioneTitolo == MetroWindowTextPos.Sinistra)
                flags |= TextFormatFlags.Left;

            TextRenderer.DrawText(g, Text, VisualManager().WindowFont, rcTesto, VisualManager().WindowTextColor, flags);
        }
        #endregion

        #region Controllo
        private WinApi.HitTest HitTestNCA(IntPtr hwnd, IntPtr wparam, IntPtr lparam)
        {
            if (DesignMode)
                return WinApi.HitTest.HTCLIENT;

            Rectangle windowRect = new Rectangle();
            Point cursorPoint = new Point();
            WinApi.GetCursorPos(out cursorPoint);
            WinApi.GetWindowRect(this.Handle, ref windowRect);
            cursorPoint.X -= windowRect.Left;
            cursorPoint.Y -= windowRect.Top;
            int width = windowRect.Right - windowRect.Left;
            int height = windowRect.Bottom - windowRect.Top;

            bool bUpdate = false;
            WinApi.HitTest ht = WinApi.HitTest.HTNOWHERE;

            if (MostraBarraTitolo)
            {
                if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                {
                    if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].ClientRectangle.Contains(cursorPoint))
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover == false)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover = true;
                            bUpdate = true;
                        }

                        ht = WinApi.HitTest.HTCLOSE;
                    }
                    else
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover == true)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover = false;
                            bUpdate = true;
                        }

                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick == true)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick = false;
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover = true;
                            bUpdate = true;
                        }
                    }
                }
                if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                {
                    if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].ClientRectangle.Contains(cursorPoint))
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover == false)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover = true;
                            bUpdate = true;
                        }

                        ht = WinApi.HitTest.HTMAXBUTTON;
                    }
                    else
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover == true)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover = false;
                            bUpdate = true;
                        }

                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick == true)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick = false;
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover = true;
                            bUpdate = true;
                        }
                    }
                }
                if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                {
                    if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].ClientRectangle.Contains(cursorPoint))
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover == false)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover = true;
                            bUpdate = true;
                        }

                        ht = WinApi.HitTest.HTMINBUTTON;
                    }
                    else
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover == true)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover = false;
                            bUpdate = true;
                        }

                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick == true)
                        {
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick = false;
                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover = true;
                            bUpdate = true;
                        }
                    }
                }
            }

            if (StileBordo == MetroWindowBorderStyle.Fisso)
                return WinApi.HitTest.HTCLIENT;

            if (bUpdate)
                UpdateDisplay();

            if (ht != WinApi.HitTest.HTNOWHERE)
                return ht;

            if (new Rectangle(1, 1, WindowRectangle.Width - 2, AltezzaBarraTitolo - 1).Contains(cursorPoint))
                return WinApi.HitTest.HTCAPTION;
            else
                ResetWindowButtons();

            return WinApi.HitTest.HTCLIENT;
        }

        private void NCCalcSize(ref Message m)
        {
            if (m.WParam == IntPtr.Zero)
            {
                WinApi.RECT ncRect = (WinApi.RECT)m.GetLParam(typeof(WinApi.RECT));
                Rectangle proposed = ncRect.Rect;

                CalcSizeNCRectangle(ref proposed);

                ncRect.Rect = proposed;
                Marshal.StructureToPtr(ncRect, m.LParam, false);
                m.Result = IntPtr.Zero;
            }
            else if (m.WParam != IntPtr.Zero)
            {
                WinApi.NCCALCSIZE_PARAMS ncParams =
                    (WinApi.NCCALCSIZE_PARAMS)m.GetLParam(typeof(WinApi.NCCALCSIZE_PARAMS));
                Rectangle proposed = ncParams.rect0.Rect;

                CalcSizeNCRectangle(ref proposed);

                ncParams.rect0.Rect = proposed;
                Marshal.StructureToPtr(ncParams, m.LParam, false);
            }
        }

        protected void CalcSizeNCRectangle(ref Rectangle rect)
        {
            if (MdiParent == null)
            {
                if (MostraBarraTitolo)
                {
                    if (_bIsMaximizing || !MostraBordo)
                    {
                        rect.Location = new Point(rect.Location.X, rect.Location.Y + AltezzaBarraTitolo);
                        rect.Size = new Size(rect.Width, rect.Height - AltezzaBarraTitolo - 1);
                    }
                    else if (WindowState == FormWindowState.Normal)
                    {
                        rect.Location = new Point(rect.Location.X + 1, rect.Location.Y + AltezzaBarraTitolo);
                        rect.Size = new Size(rect.Width - 2, rect.Height - AltezzaBarraTitolo);
                    }
                }
                else
                {
                    if (_bIsMaximizing || !MostraBordo)
                    {
                        rect.Location = new Point(rect.Location.X, rect.Location.Y);
                        rect.Size = new Size(rect.Width, rect.Height);
                    }
                    else if (WindowState == FormWindowState.Normal)
                    {
                        rect.Location = new Point(rect.Location.X + 1, rect.Location.Y);
                        rect.Size = new Size(rect.Width - 2, rect.Height);
                    }
                }
            }
            else
            {
                rect.Location = new Point(rect.Location.X, rect.Location.Y);
                rect.Size = new Size(rect.Width, rect.Height);
            }
        }

        private void NCMouseLeave(ref Message m)
        {
            ResetWindowButtons();
            m.Result = IntPtr.Zero;
        }

        private bool NCMouseLButtonDown(ref Message m)
        {
            bool rv = true;

            switch (m.WParam.ToInt32())
            {
                case (int)WinApi.HitTest.HTNOWHERE:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick = false;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick = false;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick = false;
                    break;
                case (int)WinApi.HitTest.HTCLOSE:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick = true;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick = false;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick = false;
                    break;
                case (int)WinApi.HitTest.HTMAXBUTTON:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick = false;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick = false;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick = true;
                    break;
                case (int)WinApi.HitTest.HTMINBUTTON:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick = false;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick = true;
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                        MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick = false;
                    break;
                default:
                    rv = false;
                    break;
            }

            UpdateDisplay();

            return rv;
        }

        private bool NCMouseLButtonUp(ref Message m)
        {
            bool rv = true;

            switch (m.WParam.ToInt32())
            {
                case (int)WinApi.HitTest.HTCLOSE:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick)
                        {
                            Close();
                        }
                    }
                    break;
                case (int)WinApi.HitTest.HTMAXBUTTON:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick)
                        {
                            if (WindowState == FormWindowState.Normal)
                            {
                                if (_metroShadowForm != null && _metroShadowForm.Visible)
                                    _metroShadowForm.Visible = false;

                                _bIsMaximizing = true;
                                WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_SYSCOMMAND, (int)WinApi.Messages.SC_MAXIMIZE, 0);
                            }
                            else if (WindowState == FormWindowState.Maximized)
                            {
                                if (_metroShadowForm != null && ShowShadowForm && MostraBordo)
                                    _metroShadowForm.Visible = true;

                                _bIsMaximizing = false;
                                WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_SYSCOMMAND, (int)WinApi.Messages.SC_RESTORE, 0);
                            }

                            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].MetroWindowButtonType = MetroWindowButton.TipoMetroWindowButton.Maximize;

                            UpdateDisplay();
                        }
                    }
                    break;
                case (int)WinApi.HitTest.HTMINBUTTON:
                    if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                    {
                        if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick)
                        {
                            if (_metroShadowForm != null && _metroShadowForm.Visible)
                                _metroShadowForm.Visible = false;

                            WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_SYSCOMMAND, (int)WinApi.Messages.SC_MINIMIZE, 0);
                            UpdateDisplay();
                        }
                    }
                    break;
                default:
                    rv = false;
                    break;
            }

            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
                MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsClick = false;
            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
                MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsClick = false;
            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
                MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsClick = false;

            return rv;
        }

        private void ResetWindowButtons()
        {
            bool bUpdate = false;

            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Close))
            {
                if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover == true)
                {
                    MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Close].IsHover = false;
                    bUpdate = true;
                }
            }
            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Maximize))
            {
                if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover == true)
                {
                    MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].IsHover = false;
                    bUpdate = true;
                }
            }
            if (MetroWindowButtons.ContainsKey(MetroWindowButton.TipoMetroWindowButton.Minimize))
            {
                if (MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover == true)
                {
                    MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Minimize].IsHover = false;
                    bUpdate = true;
                }
            }

            if (bUpdate)
                UpdateDisplay();
        }
        #endregion
    }
}
