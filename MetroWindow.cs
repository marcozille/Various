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
    public partial class MetroWindow : Form, IMetroWindow, IDisposable
    {
        #region Eventi

        public delegate void OnCombinazioneColoriChangedEventHandler(object sender, EventArgs e);
        public delegate void OnStileMetroChangedEventHandler(object sender, EventArgs e);
        public event OnCombinazioneColoriChangedEventHandler OnCombinazioneColoriChanged;
        public event OnStileMetroChangedEventHandler OnStileMetroChanged;

        #endregion

        #region Costruttore
        public MetroWindow()
        {
            try
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint |
                         ControlStyles.SupportsTransparentBackColor, true);

                FormBorderStyle = FormBorderStyle.None;
                Name = "MetroWindow";
                StartPosition = FormStartPosition.CenterScreen;

                MetroWindowButtons = new Dictionary<MetroWindowButton.TipoMetroWindowButton, MetroWindowButton>();

                _visualManager = new MetroVisualManager();
                PosizioneTitolo = MetroWindowTextPos.Centro;
                StileBordo = MetroWindowBorderStyle.Ridimensionabile;
                AltezzaBarraTitolo = 30;
                LarghezzaPulsanti = 30;

                BackColor = Color.Black;
                TransparencyKey = Color.Black;

                CloseBox = true;

                ShowShadowForm = true;
                MostraBordo = true;
                _bIsMaximizing = false;

                MostraBarraTitolo = true;

                _mdiClientController = new MdiClientController();
                _mdiClientController.ParentForm = this;
                _mdiClientController.BorderStyle = BorderStyle.None;

                if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
                {
                    _metroShadowForm = new MetroShadowForm(this);
                    _metroShadowForm.Visible = true;
                }

                _refreshTimer = new Timer();
                //_refreshTimer.Interval = 1000;
                //_refreshTimer.Tick += _refreshTimer_Tick;
                //_refreshTimer.Enabled = true;

                OnCombinazioneColoriChanged = null;
                OnStileMetroChanged = null;

                Icona = null;
                MostraIcona = false;
                MDIFormsTab = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void _refreshTimer_Tick(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
                if (ShowShadowForm && _metroShadowForm != null)
                {
                    _metroShadowForm.UpdateDimension();
                    UpdateDisplay();
                }
        }
        #endregion

        #region Componenti
        private Dictionary<MetroWindowButton.TipoMetroWindowButton, MetroWindowButton> MetroWindowButtons;
        #endregion
        
        #region Disegno Client Area

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                DrawClientArea(e.Graphics);
                //DrawResizeButton(e.Graphics);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void DrawClientArea(Graphics g)
        {
            DrawManager.DrawWindowBack(g, VisualManager(), DisplayRectangle, IsMdiContainer, IsMdiChild);
        }
        
        protected virtual void DrawResizeButton(Graphics g)
        {
            if (StileBordo == MetroWindowBorderStyle.Fisso || WindowState == FormWindowState.Maximized)
                return;

            DrawManager.DrawResizeWindowButton(g, VisualManager(), this, DisplayRectangle);
        }
        
        #endregion
        
        #region Utilità
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                BackColor = Color.AliceBlue;
                TransparencyKey = Color.AliceBlue;

                UpdateMetroWindowButtons();
                UpdateStatusBarPosition();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void UpdateMetroWindowButtons()
        {
            try
            {
                if (CloseBox)
                    AddWindowButton(MetroWindowButton.TipoMetroWindowButton.Close);
                else
                    RemoveWindowButton(MetroWindowButton.TipoMetroWindowButton.Close);

                if (MinimizeBox)
                    AddWindowButton(MetroWindowButton.TipoMetroWindowButton.Minimize);
                else
                    RemoveWindowButton(MetroWindowButton.TipoMetroWindowButton.Minimize);

                if (MaximizeBox)
                    AddWindowButton(MetroWindowButton.TipoMetroWindowButton.Maximize);
                else
                    RemoveWindowButton(MetroWindowButton.TipoMetroWindowButton.Maximize);

                UpdateWindowButtonPosition();
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void AddWindowButton(MetroWindowButton.TipoMetroWindowButton tipo)
        {
            try
            {
                if (MetroWindowButtons == null)
                    MetroWindowButtons = new Dictionary<MetroWindowButton.TipoMetroWindowButton, MetroWindowButton>();

                if (MetroWindowButtons.ContainsKey(tipo))
                    return;

                MetroWindowButton button = new MetroWindowButton();
                button.Parent = this;
                button.MetroWindowButtonType = tipo;
                button.Size = new Size(LarghezzaPulsanti, AltezzaBarraTitolo);

                MetroWindowButtons.Add(tipo, button);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void RemoveWindowButton(MetroWindowButton.TipoMetroWindowButton tipo)
        {
            try
            {
                if (MetroWindowButtons == null)
                    return;

                MetroWindowButton button = null;

                if (MetroWindowButtons.TryGetValue(tipo, out button))
                {
                    //Controls.Remove(button);
                    MetroWindowButtons.Remove(tipo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void UpdateWindowButtonPosition()
        {
            try
            {
                if (MetroWindowButtons == null)
                    return;

                int yOffset = (WindowState == FormWindowState.Maximized || !MostraBordo) ? 0 : 1;

                Dictionary<int, MetroWindowButton.TipoMetroWindowButton> priorityOrder = new Dictionary<int, MetroWindowButton.TipoMetroWindowButton>(4) 
                { 
                    { 0, MetroWindowButton.TipoMetroWindowButton.Close }, 
                    { 1, MetroWindowButton.TipoMetroWindowButton.Maximize },
                    { 2, MetroWindowButton.TipoMetroWindowButton.Minimize }
                };

                int index = 1;

                if (MetroWindowButtons.Count == 1)
                {
                    foreach (KeyValuePair<MetroWindowButton.TipoMetroWindowButton, MetroWindowButton> button in MetroWindowButtons)
                    {
                        if (WindowState == FormWindowState.Normal && MostraBordo)
                        {
                            button.Value.Location = new Point(WindowRectangle.Width - (LarghezzaPulsanti * index) - 1, yOffset);
                            button.Value.Size = new Size(button.Value.Size.Width, AltezzaBarraTitolo - 1);
                        }
                        else
                        {
                            button.Value.Location = new Point(WindowRectangle.Width - (LarghezzaPulsanti * index), yOffset);
                            button.Value.Size = new Size(button.Value.Size.Width, AltezzaBarraTitolo);
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, MetroWindowButton.TipoMetroWindowButton> button in priorityOrder)
                    {
                        bool buttonExists = MetroWindowButtons.ContainsKey(button.Value);

                        if (buttonExists)
                        {
                            if (WindowState == FormWindowState.Normal && MostraBordo)
                            {
                                MetroWindowButtons[button.Value].Location = new Point(WindowRectangle.Width - (LarghezzaPulsanti * index) - 1, yOffset);
                                MetroWindowButtons[button.Value].Size = new Size(MetroWindowButtons[button.Value].Size.Width, AltezzaBarraTitolo - 1);
                            }
                            else
                            {
                                MetroWindowButtons[button.Value].Location = new Point(WindowRectangle.Width - (LarghezzaPulsanti * index), yOffset);
                                MetroWindowButtons[button.Value].Size = new Size(MetroWindowButtons[button.Value].Size.Width, AltezzaBarraTitolo);
                            }
                            index++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public virtual void UpdateStatusBarPosition()
        {
            //MetroStatusBar barra = null;

            //foreach (Control c in Controls)
            //{
            //    if (c is MetroStatusBar)
            //    {
            //        barra = c as MetroStatusBar;
            //        break;
            //    }
            //}

            //if (barra == null)
            //    return;

            //Point location = WindowState == FormWindowState.Normal ? new Point(7, ClientRectangle.Height - barra.Altezza - 7) : new Point(0, ClientRectangle.Height - barra.Altezza);
            //Size size = new Size(WindowState == FormWindowState.Normal ? DisplayRectangle.Width - 2 : DisplayRectangle.Width, barra.Altezza);

            //barra.Location = location;
            //barra.Size = size;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            try
            {
                UpdateStatusBarPosition();
                UpdateWindowButtonPosition();
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            _isActive = true;

            try
            {
                if (WindowState == FormWindowState.Normal && _metroShadowForm != null)
                    _metroShadowForm.Invalidate();

                if (IsMdiChild)
                {
                    MdiClient parent = Parent as MdiClient;

                    if(parent != null)
                    {
                        MetroWindow grandParent = parent.Parent as MetroWindow;

                        if (grandParent != null)
                            WinApi.SendMessage(grandParent.MDIClientController.Handle, grandParent.MDIClientController.WM_FORM_ACTIVE_CHANGED, Handle, IntPtr.Zero);
                    }
                }

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            _isActive = false;

            //if (ShowShadowForm)
            //{
            //    if (_metroShadowForm != null)
            //        _metroShadowForm.Visible = false;
            //}

            try
            {
                if (_metroShadowForm != null)
                    _metroShadowForm.Invalidate();

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            UpdateDisplay();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
        }
        #endregion

        #region WndProc
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)WinApi.Messages.WM_SYSCOMMAND:
                    if (m.WParam.ToInt32() == (int)WinApi.Messages.SC_RESTORE)
                    {
                        this.Height = _iStoreHeight;
                    }
                    else if (m.WParam.ToInt32() == (int)WinApi.Messages.SC_MAXIMIZE)
                    {
                        _iStoreHeight = this.Height;
                    }
                    break;
                case (int)WinApi.Messages.WM_NCLBUTTONDBLCLK:
                case (int)WinApi.Messages.WM_LBUTTONDBLCLK:
                    if (!MaximizeBox) return;
                    break;

                case (int)WinApi.Messages.WM_NCHITTEST:

                    WinApi.HitTest ht = HitTestNCA(m.HWnd, m.WParam, m.LParam);
                    if (ht != WinApi.HitTest.HTCLIENT)
                    {
                        m.Result = (IntPtr)ht;
                        return;
                    }
                    break;
                case (int)WinApi.Messages.WM_NCCALCSIZE:
                    NCCalcSize(ref m);
                    return;
                case (int)WinApi.Messages.WM_NCPAINT:
                    NCPaint(ref m);
                    return;
                case (int)WinApi.Messages.WM_NCLBUTTONDOWN:
                    if(NCMouseLButtonDown(ref m))
                        return;
                    break;
                case (int)WinApi.Messages.WM_NCMOUSELEAVE:
                    ResetWindowButtons();
                    UpdateDisplay();
                    break;
                case (int)WinApi.Messages.WM_NCLBUTTONUP:
                    if (NCMouseLButtonUp(ref m))
                        return;
                    break;
                case (int)WinApi.Messages.WM_SETTEXT:
                    base.WndProc(ref m);
                    NCPaint(ref m);
                    return;
                case (int)WinApi.Messages.WM_ACTIVATE:
                    bool active = (m.WParam != IntPtr.Zero);

                    if (WindowState == FormWindowState.Minimized)
                    {
                        base.WndProc(ref m);
                        return;
                    }
                    else
                    {
                        base.WndProc(ref m);
                        NCPaint(ref m);
                        m.Result = new IntPtr(1);
                        return;
                    }
            }

            base.WndProc(ref m);

            if (m.Msg == (int)WinApi.Messages.WM_NCLBUTTONDBLCLK || m.Msg == (int)WinApi.Messages.WM_LBUTTONDBLCLK)
            {
                MetroWindowButton button = new MetroWindowButton();

                if (MetroWindowButtons.TryGetValue(MetroWindowButton.TipoMetroWindowButton.Maximize, out button))
                {
                    button.MetroWindowButtonType = MetroWindowButton.TipoMetroWindowButton.Maximize;
                    UpdateDisplay();
                }
            }
        }
        #endregion
        
        public void UpdateDisplay(bool redrawNC = true)
        {
            Refresh();

            if(redrawNC)
                WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_NCPAINT, (int)(IntPtr)1, (int)(IntPtr)0); 
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            WinApi.SetWindowTheme(this.Handle, "", "");
            WinApi.DisableProcessWindowsGhosting();
            base.OnHandleCreated(e);
        }

        protected override void Dispose(bool disposing)
        {
            _refreshTimer.Enabled = false;

            if (_mdiClientController != null)
            {
                _mdiClientController.DestroyHandle();
                _mdiClientController = null;
            }

            if (_metroShadowForm != null)
                _metroShadowForm = null;

            base.Dispose(disposing);
        }

        public void Maximize()
        {
            if (_metroShadowForm != null && _metroShadowForm.Visible)
                _metroShadowForm.Visible = false;

            _bIsMaximizing = true;
            WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_SYSCOMMAND, (int)WinApi.Messages.SC_MAXIMIZE, 0);
            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].MetroWindowButtonType = MetroWindowButton.TipoMetroWindowButton.Maximize;
            WindowState = FormWindowState.Maximized;
            UpdateDisplay();
        }

        public void Restore()
        {
            if (_metroShadowForm != null && ShowShadowForm && MostraBordo)
                _metroShadowForm.Visible = true;

            WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_SYSCOMMAND, (int)WinApi.Messages.SC_RESTORE, 0);
            MetroWindowButtons[MetroWindowButton.TipoMetroWindowButton.Maximize].MetroWindowButtonType = MetroWindowButton.TipoMetroWindowButton.Maximize;
        }

        public void Minimize()
        {
            if (_metroShadowForm != null && _metroShadowForm.Visible)
                _metroShadowForm.Visible = false;

            WinApi.SendMessage(this.Handle, (int)WinApi.Messages.WM_SYSCOMMAND, (int)WinApi.Messages.SC_MINIMIZE, 0); 
        }

        protected void UpdateShadow()
        {
            if (ShowShadowForm && MostraBordo && _metroShadowForm != null)
                _metroShadowForm.UpdateDimension();
        }
    }
}
