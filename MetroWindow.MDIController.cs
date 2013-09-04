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
        public class MdiClientController : NativeWindow, IComponent, IDisposable, IMetroControl
        {
            public int Opacity { get; set; }
            private enum EDropDownFileListButtonState { NORMAL, HOVER, OPENED }

            private EDropDownFileListButtonState _dropDownFileListButtonState;
            private MetroDropDownPanel _dropDownFileList;
            private Rectangle _dropDownFileListButtonRect;

            private Font _dropDownFileListSymbolFont;

            private WinApi.HookProc MouseHook;
            private IntPtr _hook;

            public uint WM_FORM_ACTIVE_CHANGED = WinApi.RegisterWindowMessage("WM_FORM_ACTIVE_CHANGED");

            private List<MDITab> _mdiTabs = null;
            private Rectangle _tabsRectangle;

            private bool _bDropDownFileListVisible;

            public virtual MetroVisualManager VisualManager(bool bSet = false, MetroVisualManager vm = null)
            {
                MetroWindow wnd = ParentForm as MetroWindow;

                if (wnd == null)
                    throw new ArgumentException();

                if (bSet)
                {
                    wnd.VisualManager(true, vm);
                    return null;
                }
                else
                    return wnd.VisualManager();
            }

            #region Private Fields

            private Form parentForm;
            private MdiClient mdiClient;
            private BorderStyle borderStyle;
            private bool autoScroll;
            private ISite site;

            #endregion // Private Fields

            #region Public Constructors

            public MdiClientController() : this(null)
            {
            }

            public MdiClientController(Form parentForm)
            {
                // Initialize the variables.
                this.site = null;
                this.parentForm = null;
                this.mdiClient = null;
                this.borderStyle = BorderStyle.Fixed3D;
                this.autoScroll = true;

                _hook = IntPtr.Zero;
                _dropDownFileList = new MetroDropDownPanel();
                _dropDownFileListSymbolFont = new Font("Webdings", 9);
                _dropDownFileListButtonState = EDropDownFileListButtonState.NORMAL;
                _bDropDownFileListVisible = false;
                
                // Set the ParentForm property.
                this.ParentForm = parentForm;
            }
            
            #endregion // Public Constructors

            #region Public Events

            [Browsable(false)]
            public event EventHandler Disposed;

            [Browsable(false)]
            public event EventHandler HandleAssigned;

            #endregion // Public Events

            #region Public Properties

            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public ISite Site
            {
                get { return site; }
                set
                {
                    site = value;

                    if (site == null)
                        return;

                    IDesignerHost host = (value.GetService(typeof(IDesignerHost)) as IDesignerHost);
                    if (host != null)
                    {
                        Form parent = host.RootComponent as Form;
                        if (parent != null)
                            ParentForm = parent;
                    }
                }
            }

            [Browsable(false)]
            public Form ParentForm
            {
                get { return parentForm; }
                set
                {
                    if (parentForm != null)
                        parentForm.HandleCreated -= new EventHandler(ParentFormHandleCreated);

                    parentForm = value;

                    if (parentForm == null)
                        return;

                    if (parentForm.IsHandleCreated)
                    {
                        InitializeMdiClient();
                        RefreshProperties();
                    }
                    else
                        parentForm.HandleCreated += new EventHandler(ParentFormHandleCreated);
                }
            }

            [Browsable(false)]
            public MdiClient MdiClient
            {
                get { return mdiClient; }
            }

            [DefaultValue(BorderStyle.Fixed3D), Category("Appearance")]
            [Description("Indicates whether the MDI area should have a border.")]
            public BorderStyle BorderStyle
            {
                get { return borderStyle; }
                set
                {
                    if (!Enum.IsDefined(typeof(BorderStyle), value))
                        throw new InvalidEnumArgumentException("value", (int)value, typeof(BorderStyle));

                    borderStyle = value;

                    if (mdiClient == null)
                        return;

                    if (site != null && site.DesignMode)
                        return;

                    int style = GetWindowLong(mdiClient.Handle, GWL_STYLE);
                    int exStyle = GetWindowLong(mdiClient.Handle, GWL_EXSTYLE);

                    switch (borderStyle)
                    {
                        case BorderStyle.Fixed3D:
                            exStyle |= WS_EX_CLIENTEDGE;
                            style &= ~WS_BORDER;
                            break;

                        case BorderStyle.FixedSingle:
                            exStyle &= ~WS_EX_CLIENTEDGE;
                            style |= WS_BORDER;
                            break;

                        case BorderStyle.None:
                            style &= ~WS_BORDER;
                            exStyle &= ~WS_EX_CLIENTEDGE;
                            break;
                    }

                    // Set the styles using Win32 calls
                    SetWindowLong(mdiClient.Handle, GWL_STYLE, style);
                    SetWindowLong(mdiClient.Handle, GWL_EXSTYLE, exStyle);

                    // Cause an update of the non-client area.
                    UpdateStyles();
                }
            }

            [DefaultValue(true), Category("Layout")]
            [Description("Determines whether scrollbars will automatically appear if controls are placed outside the MDI client area.")]
            public bool AutoScroll
            {
                get { return autoScroll; }
                set
                {
                    autoScroll = value;
                    if (mdiClient != null)
                        UpdateStyles();
                }
            }

            [Browsable(false)]
            public new IntPtr Handle
            {
                get { return base.Handle; }
            }

            #endregion // Public Properties

            #region Public Methods

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void RenewMdiClient()
            {
                InitializeMdiClient();
                RefreshProperties();
            }

            #endregion // Public Methods

            #region Protected Methods

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    lock (this)
                    {
                        if (site != null && site.Container != null)
                            site.Container.Remove(this);

                        if (Disposed != null)
                            Disposed(this, EventArgs.Empty);
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_ERASEBKGND:
                        return;
                    case (int)WinApi.Messages.WM_NCPAINT:
                        NCPaint(ref m);
                        return;
                    case WM_PAINT:
                        PAINTSTRUCT paintStruct = new PAINTSTRUCT();
                        IntPtr screenHdc = BeginPaint(m.HWnd, ref paintStruct);

                        using (Graphics screenGraphics = Graphics.FromHdc(screenHdc))
                        {
                            Rectangle clipRect = new Rectangle(paintStruct.rcPaint.left,
                                                               paintStruct.rcPaint.top,
                                                               paintStruct.rcPaint.right - paintStruct.rcPaint.left,
                                                               paintStruct.rcPaint.bottom - paintStruct.rcPaint.top);

                            int width = (mdiClient.ClientRectangle.Width > 0 ? mdiClient.ClientRectangle.Width : 0);
                            int height = (mdiClient.ClientRectangle.Height > 0 ? mdiClient.ClientRectangle.Height : 0);

                            using (Image i = new Bitmap(width, height))
                            {
                                using (Graphics g = Graphics.FromImage(i))
                                {
                                    IntPtr hdc = g.GetHdc();
                                    Message printClientMessage = Message.Create(m.HWnd, WM_PRINTCLIENT, hdc, IntPtr.Zero);
                                    DefWndProc(ref printClientMessage);
                                    g.ReleaseHdc(hdc);

                                    OnPaint(new PaintEventArgs(g, clipRect));
                                }

                                screenGraphics.DrawImage(i, mdiClient.ClientRectangle);
                            }
                        }

                        EndPaint(m.HWnd, ref paintStruct);
                        return;
                    case WM_SIZE:
                        mdiClient.Invalidate();
                        break;
                    case WM_NCCALCSIZE:
                        NCCalcSize(ref m);
                        return;
                }

                if (m.Msg == (int)WM_FORM_ACTIVE_CHANGED)
                    OnUpdateMDITabsState(m.WParam);

                base.WndProc(ref m);
            }

            protected void NCPaint(ref Message m)
            {
                IntPtr hRgn = (IntPtr)m.WParam;

                IntPtr hDC = WinApi.GetWindowDC(m.HWnd);

                if (hDC == IntPtr.Zero)
                    return;
                
                MetroWindow pWnd = ParentForm as MetroWindow;

                using (Graphics g = Graphics.FromHdc(hDC))
                {
                    if (pWnd.MDIFormsTab)
                    {
                        DrawChildrenTabs(g);
                        DrawDropDownFilesButton(g);
                    }
                }

                WinApi.ReleaseDC(m.HWnd, hDC);
            }

            public Color GetControlColor()
            {
                if (mdiClient.Parent is MetroWindow)
                {
                    MetroWindow wnd = mdiClient.Parent as MetroWindow;

                    if (wnd.IsMdiContainer)
                        return wnd.VisualManager().WindowForeColor;
                    else
                        return wnd.VisualManager().WindowBackColor;
                }
                else if (mdiClient.Parent is IMetroControl)
                {
                    MetroWindowControlBase ctrl = mdiClient.Parent as MetroWindowControlBase;

                    return ctrl.GetControlColor();
                }
                else
                {
                    return mdiClient.Parent.BackColor;
                }
            }

            protected virtual void DrawDropDownFilesButton(Graphics g)
            {
                MetroVisualManager vm = VisualManager();
                Rectangle rc = _dropDownFileListButtonRect;
                rc.Location = new Point(rc.Location.X + 1, rc.Location.Y - 1);

                if (_dropDownFileListButtonState == EDropDownFileListButtonState.NORMAL)
                {
                    g.FillRectangle(new SolidBrush(vm.MDIDropDownFilesButtonNormal), _dropDownFileListButtonRect);
                    TextRenderer.DrawText(g, "6", _dropDownFileListSymbolFont, rc, vm.MDIDropDownFilesButtonTextNormal, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (_dropDownFileListButtonState == EDropDownFileListButtonState.HOVER)
                {
                    g.FillRectangle(new SolidBrush(vm.MDIDropDownFilesButtonHover), _dropDownFileListButtonRect);
                    TextRenderer.DrawText(g, "6", _dropDownFileListSymbolFont, rc, vm.MDIDropDownFilesButtonTextHover, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (_dropDownFileListButtonState == EDropDownFileListButtonState.OPENED)
                {
                    g.FillRectangle(new SolidBrush(vm.MDIDropDownFilesButtonPressed), _dropDownFileListButtonRect);
                    TextRenderer.DrawText(g, "6", _dropDownFileListSymbolFont, rc, vm.MDIDropDownFilesButtonTextPressed, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            protected virtual void DrawChildrenTabs(Graphics g)
            {
                MetroVisualManager vm = VisualManager();

                Rectangle totalRC = new Rectangle(0, 0, MdiClient.Width, 23);
                Rectangle rcBordoInf = new Rectangle(0, 21, MdiClient.Width, 2);

                g.FillRectangle(new SolidBrush(vm.WindowBackColor), totalRC);
                g.FillRectangle(new SolidBrush(vm.WindowBorderColor), rcBordoInf);

                foreach (MDITab tab in _mdiTabs)
                {
                    if (tab.TabRect.Location.X + tab.TabRect.Width > MdiClient.Width - 25)
                        continue;

                    MetroWindow mw = tab.AssociateForm;

                    if (tab.State == MDITab.EMDITabState.SELECTED)
                    {
                        g.FillRectangle(new SolidBrush(vm.MDIWindowTabSelected), tab.TabRect);
                        TextRenderer.DrawText(g, mw.Text, vm.MDITabFont, tab.TabRect, vm.MDIWindowTextSelected, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                        if (tab.CloseButtonState == MDITab.EMDITabCloseButtonState.HOVER)
                            g.FillRectangle(new SolidBrush(vm.MDIWindowTabButtonHover), tab.CloseTabRect);
                        else if (tab.CloseButtonState == MDITab.EMDITabCloseButtonState.PRESSED)
                            g.FillRectangle(new SolidBrush(vm.MDIWindowTabButtonPressed), tab.CloseTabRect);

                        TextRenderer.DrawText(g, "r", vm.SmallSystemButtonFont, tab.CloseTabRect, vm.MDIWindowTextSelected, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    else if (tab.State == MDITab.EMDITabState.HOVER)
                    {
                        g.FillRectangle(new SolidBrush(vm.MDIWindowTabHover), tab.TabRect);
                        TextRenderer.DrawText(g, mw.Text, vm.MDITabFont, tab.TabRect, vm.MDIWindowTextHover, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                        if (tab.CloseButtonState == MDITab.EMDITabCloseButtonState.HOVER)
                            g.FillRectangle(new SolidBrush(vm.MDIWindowTabButtonHover), tab.CloseTabRect);
                        else if (tab.CloseButtonState == MDITab.EMDITabCloseButtonState.PRESSED)
                            g.FillRectangle(new SolidBrush(vm.MDIWindowTabButtonPressed), tab.CloseTabRect);

                        TextRenderer.DrawText(g, "r", vm.SmallSystemButtonFont, tab.CloseTabRect, vm.MDIWindowTextSelected, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    else
                    {
                        g.FillRectangle(new SolidBrush(vm.MDIWindowTabNormal), tab.TabRect);
                        TextRenderer.DrawText(g, mw.Text, vm.MDITabFont, tab.TabRect, vm.MDIWindowTextNormal, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    }                    
                }
            }

            private void NCCalcSize(ref Message m)
            {
                if (!autoScroll)
                    ShowScrollBar(m.HWnd, SB_BOTH, 0);

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
                MetroWindow pWnd = ParentForm as MetroWindow;

                if (ParentForm.WindowState == FormWindowState.Maximized)
                {
                    if (pWnd.MDIFormsTab)
                    {
                        _tabsRectangle = new Rectangle(rect.Location, new Size(rect.Width, 23));
                        rect.Location = new Point(rect.Location.X, rect.Location.Y + 23);
                        rect.Size = new Size(rect.Width, rect.Height - 23);
                    }
                    else
                    {
                        rect.Location = new Point(rect.Location.X, rect.Location.Y);
                        rect.Size = new Size(rect.Width, rect.Height);
                    }
                }
                else
                {
                    if (pWnd.MDIFormsTab)
                    {
                        rect.Location = new Point(rect.Location.X, rect.Location.Y);
                        rect.Size = new Size(rect.Width, rect.Height);
                        _tabsRectangle = new Rectangle(rect.Location, new Size(rect.Width, 23));
                        rect.Location = new Point(rect.Location.X, rect.Location.Y + 23);
                        rect.Size = new Size(rect.Width, rect.Height - 23);
                    }
                    else
                    {
                        rect.Location = new Point(rect.Location.X, rect.Location.Y);
                        rect.Size = new Size(rect.Width, rect.Height);
                    }
                }

                _dropDownFileListButtonRect = new Rectangle(new Point(rect.Location.X + rect.Width - 15, rect.Location.Y - 19), new Size(15, 15)); 
            }

            protected virtual void OnPaint(PaintEventArgs e)
            {
                MetroVisualManager vm = VisualManager();
                e.Graphics.FillRectangle(new SolidBrush(vm.WindowForeColor), MdiClient.DisplayRectangle);
            }

            protected virtual void OnHandleAssigned(EventArgs e)
            {
                // Raise the HandleAssigned event.
                if (HandleAssigned != null)
                    HandleAssigned(this, e);
            }

            protected virtual void OnUpdateMDITabsState(IntPtr handle)
            {
                MetroWindow wnd = MetroWindow.FromHandle(handle) as MetroWindow;

                if (wnd == null)
                    throw new ArgumentException("Child form must be a MetroWindow derived class");

                for (int i = 0; i < _mdiTabs.Count; i++)
                {
                    MDITab tab = _mdiTabs[i];

                    if (tab.AssociateForm == wnd)
                        tab.State = MDITab.EMDITabState.SELECTED;
                    else
                    {
                          tab.State = MDITab.EMDITabState.NORMAL;
                    }
                }
            }

            protected virtual void OnUpdateMDITabsState(Point point)
            {
                bool bInvalidate = false;
                Rectangle rc = _tabsRectangle;
                rc = ParentForm.RectangleToScreen(rc);

                if (!rc.Contains(point))
                {
                    ResetMDITabsState();
                    return;
                }

                for (int i = 0; i < _mdiTabs.Count; i++)
                {
                    MDITab tab = _mdiTabs[i];

                    if (tab.State != MDITab.EMDITabState.SELECTED)
                    {
                        rc = tab.TabRect;
                        rc = ParentForm.RectangleToScreen(rc);
                        
                        if (rc.Contains(point))
                        {
                            if (tab.State != MDITab.EMDITabState.HOVER)
                            {
                                tab.State = MDITab.EMDITabState.HOVER;
                                bInvalidate = true;
                            }
                        }
                        else
                        {
                            if (tab.State != MDITab.EMDITabState.NORMAL)
                            {
                                tab.State = MDITab.EMDITabState.NORMAL;
                                bInvalidate = true;
                            }
                        }
                    }

                    rc = tab.CloseTabRect;
                    rc = ParentForm.RectangleToScreen(rc);

                    if (rc.Contains(point))
                    {
                        if (tab.CloseButtonState == MDITab.EMDITabCloseButtonState.NORMAL)
                        {
                            tab.CloseButtonState = MDITab.EMDITabCloseButtonState.HOVER;
                            bInvalidate = true;
                        }
                    }
                    else if (tab.CloseButtonState != MDITab.EMDITabCloseButtonState.NORMAL)
                    {
                        tab.CloseButtonState = MDITab.EMDITabCloseButtonState.NORMAL;
                        bInvalidate = true;
                    }
                }

                if (bInvalidate)
                    UpdateColors();
            }

            protected virtual void OnUpdateMDITabsSelection(Point point)
            {
                bool bInvalidate = false;
                bool bFoundCloseButtonPress = false;
                Rectangle rc = _tabsRectangle;
                rc = ParentForm.RectangleToScreen(rc);

                if (!rc.Contains(point))
                {
                    ResetMDITabsState();
                    return;
                }

                for (int i = 0; i < _mdiTabs.Count; i++)
                {
                    MDITab tab = _mdiTabs[i];
                    
                    rc = tab.CloseTabRect;
                    rc = ParentForm.RectangleToScreen(rc);

                    if (rc.Contains(point))
                    {
                        if (tab.CloseButtonState != MDITab.EMDITabCloseButtonState.PRESSED)
                        {
                            tab.CloseButtonState = MDITab.EMDITabCloseButtonState.PRESSED;
                            bFoundCloseButtonPress = true;
                            bInvalidate = true;
                        }
                    }
                    else if (tab.CloseButtonState != MDITab.EMDITabCloseButtonState.NORMAL)
                    {
                        tab.CloseButtonState = MDITab.EMDITabCloseButtonState.NORMAL;
                        bInvalidate = true;
                    }
                }

                if (!bFoundCloseButtonPress)
                {
                    MetroWindow formToActivate = null;
                    for (int i = 0; i < _mdiTabs.Count; i++)
                    {
                        MDITab tab = _mdiTabs[i];

                        if (tab.State != MDITab.EMDITabState.SELECTED)
                        {
                            rc = tab.TabRect;
                            rc = ParentForm.RectangleToScreen(rc);

                            if (rc.Contains(rc))
                            {
                                formToActivate = tab.AssociateForm;
                                tab.State = MDITab.EMDITabState.SELECTED;
                                bInvalidate = true;
                            }
                            else
                            {
                                tab.State = MDITab.EMDITabState.NORMAL;
                                bInvalidate = true;
                            }
                        }
                    }

                    if (formToActivate != null)
                        formToActivate.Activate();
                }

                if (bInvalidate)
                    UpdateColors();
            }

            protected virtual void OnUpdateMDITabsCloses(Point point)
            {
                bool bInvalidate = false;
                Rectangle rc = _tabsRectangle;
                rc = ParentForm.RectangleToScreen(rc);

                if (!rc.Contains(point))
                {
                    ResetMDITabsState();
                    return;
                }

                MetroWindow formToClose = null;

                for (int i = 0; i < _mdiTabs.Count; i++)
                {
                    MDITab tab = _mdiTabs[i];
                    
                    rc = tab.CloseTabRect;
                    rc = ParentForm.RectangleToScreen(rc);

                    if (rc.Contains(point))
                    {
                        formToClose = tab.AssociateForm;
                        bInvalidate = true;
                    }
                }

                if (formToClose != null)
                    formToClose.Close();

                if (bInvalidate)
                    UpdateColors();
            }

            protected virtual void ResetMDITabsState()
            {
                bool bInvalidate = false;
                foreach (MDITab tab in _mdiTabs)
                {
                    if (tab.State != MDITab.EMDITabState.SELECTED && tab.State != MDITab.EMDITabState.NORMAL)
                    {
                        tab.State = MDITab.EMDITabState.NORMAL;
                        bInvalidate = true;
                    }

                    if (tab.CloseButtonState != MDITab.EMDITabCloseButtonState.NORMAL)
                    {
                        tab.CloseButtonState = MDITab.EMDITabCloseButtonState.NORMAL;
                        bInvalidate = true;
                    }
                }

                if (!_bDropDownFileListVisible && _dropDownFileListButtonState != EDropDownFileListButtonState.NORMAL)
                {
                    _dropDownFileListButtonState = EDropDownFileListButtonState.NORMAL;
                    bInvalidate = true;
                }

                if (bInvalidate)
                    UpdateColors();
            }

            #endregion // Protected Methods

            #region Private Methods

            private void InitializeMdiClient()
            {
                if (mdiClient != null)
                    mdiClient.HandleDestroyed -= new EventHandler(MdiClientHandleDestroyed);

                if (parentForm == null)
                    return;

                for (int i = 0; i < parentForm.Controls.Count; i++)
                {
                    mdiClient = parentForm.Controls[i] as MdiClient;
                    if (mdiClient != null)
                    {
                        ReleaseHandle();
                        AssignHandle(mdiClient.Handle);

                        OnHandleAssigned(EventArgs.Empty);

                        _mdiTabs = new List<MDITab>();

                        mdiClient.HandleDestroyed += new EventHandler(MdiClientHandleDestroyed);
                        mdiClient.ControlAdded += mdiClient_ControlAdded;
                        mdiClient.ControlRemoved += mdiClient_ControlRemoved;

                        _dropDownFileList.Visible = false;
                        ParentForm.Controls.Add(_dropDownFileList);

                        try
                        {
                            if (_hook == IntPtr.Zero)
                            {
                                MouseHook = MouseHookProc;
                                Process curProc = Process.GetCurrentProcess();
                                ProcessModule procModule = curProc.MainModule;

                                _hook = WinApi.SetWindowsHookEx((int)WinApi.HOOKS.WH_MOUSE, MouseHook, IntPtr.Zero, (uint)AppDomain.GetCurrentThreadId());
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }

            void mdiClient_SizeChanged(object sender, EventArgs e)
            {
            }

            void mdiClient_ControlRemoved(object sender, ControlEventArgs e)
            {
                MetroWindow mw = e.Control as MetroWindow;

                if (mw == null)
                    throw new ArgumentException("Si possono aggiungere solo finestre di tipo MetroWindow");

                int removedTabWidth = 0;
                int indexToRemove = -1;
                Rectangle tmpRc;

                for (int i = 0; i < _mdiTabs.Count; i++)
                {
                    if (_mdiTabs[i].AssociateForm == e.Control)
                    {
                        removedTabWidth = _mdiTabs[i].TabRect.Width;
                        indexToRemove = i;
                    }
                    else if (indexToRemove != -1)
                    {
                        tmpRc = _mdiTabs[i].TabRect;
                        tmpRc.Location = new Point(tmpRc.Location.X - removedTabWidth, tmpRc.Location.Y);
                        _mdiTabs[i].TabRect = tmpRc;
                    }
                }

                if (indexToRemove != -1)
                    _mdiTabs.RemoveAt(indexToRemove);
            }

            void mdiClient_MouseMove(object sender, MouseEventArgs e)
            {
                OnUpdateMDITabsState(e.Location);
            }

            private void UpdateStyles()
            {
                SetWindowPos(mdiClient.Handle, IntPtr.Zero, 0, 0, 0, 0,
                             SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER |
                             SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
            }

            private void UpdateColors()
            {
                SetWindowPos(mdiClient.Handle, IntPtr.Zero, 0, 0, 0, 0,
                             SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER |
                             SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
            }


            private void MdiClientHandleDestroyed(object sender, EventArgs e)
            {
                if (mdiClient != null)
                {
                    mdiClient.HandleDestroyed -= new EventHandler(MdiClientHandleDestroyed);
                    mdiClient = null;
                }

                ReleaseHandle();
            }


            private void ParentFormHandleCreated(object sender, EventArgs e)
            {
                parentForm.HandleCreated -= new EventHandler(ParentFormHandleCreated);
                InitializeMdiClient();
                RefreshProperties();
            }


            private void RefreshProperties()
            {
                BorderStyle = borderStyle;
                AutoScroll = autoScroll;
            }

            #endregion // Private Methods

            void mdiClient_ControlAdded(object sender, ControlEventArgs e)
            {
                MetroWindow mw = e.Control as MetroWindow;

                if (mw == null)
                    throw new ArgumentException("Si possono aggiungere solo finestre di tipo MetroWindow");

                MDITab tab = new MDITab();
                tab.AssociateForm = mw;
                tab.State = MDITab.EMDITabState.NORMAL;
                tab.CloseButtonState = MDITab.EMDITabCloseButtonState.NORMAL;

                Rectangle lastTabRect = new Rectangle();
                Rectangle newTabRect = new Rectangle();

                if (_mdiTabs.Count > 0)
                    lastTabRect = _mdiTabs[_mdiTabs.Count - 1].TabRect;

                Graphics g = CreateGraphics();

                if (g == null)
                    throw new NullReferenceException();

                MetroWindow p = ParentForm as MetroWindow;

                if (p == null)
                    throw new Exception("Il form padre deve essere una finestra di tipo MetroWindow");

                MetroVisualManager vm = p.VisualManager();

                int width = (int)(g.MeasureString(mw.Text, vm.MDITabFont).Width + 30);

                newTabRect.Location = new Point(lastTabRect.Location.X + lastTabRect.Width, 0);
                newTabRect.Size = new Size(width, 21);

                tab.TabRect = newTabRect;

                _mdiTabs.Add(tab);
                _dropDownFileList.AddItem(mw.Text);
            }

            private Graphics CreateGraphics()
            {
                IntPtr hDC = WinApi.GetWindowDC(this.Handle);

                if (hDC == IntPtr.Zero)
                    return null;

                return Graphics.FromHdc(hDC);
            }

            protected void OnUpdateMDIDropDownFileListButtonState(Point point)
            {
                bool bInvalidate = false;
                Rectangle rc = _tabsRectangle;
                rc = ParentForm.RectangleToScreen(rc);

                if (!rc.Contains(point))
                {
                    ResetMDITabsState();
                    return;
                }

                rc = _dropDownFileListButtonRect;
                rc = ParentForm.RectangleToScreen(rc);

                if (rc.Contains(point))
                {
                    if (_dropDownFileListButtonState == EDropDownFileListButtonState.NORMAL)
                    {
                        _dropDownFileListButtonState = EDropDownFileListButtonState.HOVER;
                        bInvalidate = true;
                    }
                }
                else
                {
                    if (_dropDownFileListButtonState == EDropDownFileListButtonState.HOVER)
                    {
                        _dropDownFileListButtonState = EDropDownFileListButtonState.NORMAL;
                        bInvalidate = true;
                    }
                }

                if (bInvalidate)
                    UpdateColors();
            }

            protected bool OnUpdateMDIDropDownFileListButtonSelection(Point point)
            {
                if(_mdiTabs.Count == 0)
                    return false;

                bool bInvalidate = false;
                bool bRetVal = false;
                Rectangle rc = _tabsRectangle;
                rc = ParentForm.RectangleToScreen(rc);

                if (!rc.Contains(point))
                    return false;

                rc = _dropDownFileListButtonRect;
                rc = ParentForm.RectangleToScreen(rc);

                if (rc.Contains(point))
                {
                    _bDropDownFileListVisible = true;

                    if (_dropDownFileListButtonState != EDropDownFileListButtonState.OPENED)
                    {
                        _dropDownFileListButtonState = EDropDownFileListButtonState.OPENED;
                        bInvalidate = true;
                    }

                    rc = _dropDownFileListButtonRect;
                    Rectangle wndRect = rc;

                    Point location = new Point((wndRect.Location.X + wndRect.Width) - _dropDownFileList.Width, wndRect.Location.Y + 15);
                    
                    _dropDownFileList.Location = location;
                    _dropDownFileList.BringToFront();
                    _dropDownFileList.Visible = true;

                    bRetVal = true;
                }
                else
                {
                    if (_dropDownFileListButtonState == EDropDownFileListButtonState.OPENED)
                    {
                        _dropDownFileListButtonState = EDropDownFileListButtonState.NORMAL;
                        bInvalidate = true;
                    }

                    _bDropDownFileListVisible = false;
                    _dropDownFileList.Visible = false; 
                }

                if (bInvalidate)
                    UpdateColors();

                return bRetVal;
            }

            protected void OnUpdateMDIDropDownFileListButtonClick(Point point)
            {
            }

            private IntPtr MouseHookProc(int nCode, uint wParam, IntPtr lParam)
            {
                if (nCode >= 0)
                {
                    WinApi.MSLLHOOKSTRUCT hookStruct = (WinApi.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(WinApi.MSLLHOOKSTRUCT));
                    Point point = new Point(hookStruct.pt.x, hookStruct.pt.y);

                    if (WinApi.Messages.WM_NCMOUSEMOVE == (WinApi.Messages)wParam ||
                        WinApi.Messages.WM_MOUSEMOVE == (WinApi.Messages)wParam)
                    {
                        OnUpdateMDITabsState(point);
                        OnUpdateMDIDropDownFileListButtonState(point);
                    }
                    else if (WinApi.Messages.WM_NCLBUTTONDOWN == (WinApi.Messages)wParam ||
                             WinApi.Messages.WM_LBUTTONDOWN == (WinApi.Messages)wParam)
                    {
                        if(!OnUpdateMDIDropDownFileListButtonSelection(point))
                            OnUpdateMDITabsSelection(point);
                    }
                    else if (WinApi.Messages.WM_NCLBUTTONUP == (WinApi.Messages)wParam ||
                             WinApi.Messages.WM_LBUTTONUP == (WinApi.Messages)wParam)
                    {
                        if(!_bDropDownFileListVisible)
                            OnUpdateMDITabsCloses(point);
                    }
                }

                return WinApi.CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            #region Win32

            private const int WM_PAINT = 0x000F;
            private const int WM_ERASEBKGND = 0x0014;
            private const int WM_NCPAINT = 0x0085;
            private const int WM_THEMECHANGED = 0x031A;
            private const int WM_NCCALCSIZE = 0x0083;
            private const int WM_SIZE = 0x0005;
            private const int WM_PRINTCLIENT = 0x0318;

            private const uint SWP_NOSIZE = 0x0001;
            private const uint SWP_NOMOVE = 0x0002;
            private const uint SWP_NOZORDER = 0x0004;
            private const uint SWP_NOREDRAW = 0x0008;
            private const uint SWP_NOACTIVATE = 0x0010;
            private const uint SWP_FRAMECHANGED = 0x0020;
            private const uint SWP_SHOWWINDOW = 0x0040;
            private const uint SWP_HIDEWINDOW = 0x0080;
            private const uint SWP_NOCOPYBITS = 0x0100;
            private const uint SWP_NOOWNERZORDER = 0x0200;
            private const uint SWP_NOSENDCHANGING = 0x0400;

            private const int WS_BORDER = 0x00800000;
            private const int WS_EX_CLIENTEDGE = 0x00000200;
            private const int WS_DISABLED = 0x08000000;

            private const int GWL_STYLE = -16;
            private const int GWL_EXSTYLE = -20;

            private const int SB_HORZ = 0;
            private const int SB_VERT = 1;
            private const int SB_CTL = 2;
            private const int SB_BOTH = 3;


            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;

                public RECT(Rectangle rect)
                {
                    this.left = rect.Left;
                    this.top = rect.Top;
                    this.right = rect.Right;
                    this.bottom = rect.Bottom;
                }

                public RECT(int left, int top, int right, int bottom)
                {
                    this.left = left;
                    this.top = top;
                    this.right = right;
                    this.bottom = bottom;
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            private struct PAINTSTRUCT
            {
                public IntPtr hdc;
                public int fErase;
                public RECT rcPaint;
                public int fRestore;
                public int fIncUpdate;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
                public byte[] rgbReserved;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct NCCALCSIZE_PARAMS
            {
                public RECT rgrc0, rgrc1, rgrc2;
                public IntPtr lppos;
            }


            [DllImport("user32.dll")]
            private static extern int ShowScrollBar(IntPtr hWnd, int wBar, int bShow);

            [DllImport("user32.dll")]
            private static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);

            [DllImport("user32.dll")]
            private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern int GetWindowLong(IntPtr hWnd, int Index);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern int SetWindowLong(IntPtr hWnd, int Index, int Value);

            [DllImport("user32.dll", ExactSpelling = true)]
            private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            #endregion // Win32
        }
    }

    public class MDITab
    {
        public enum EMDITabState { SELECTED, HOVER, NORMAL };
        public enum EMDITabCloseButtonState { PRESSED, HOVER, NORMAL };

        private EMDITabState _state = EMDITabState.NORMAL;
        public EMDITabState State
        {
            get { return _state; }
            set { _state = value; }
        }

        private EMDITabCloseButtonState _closeButtonState = EMDITabCloseButtonState.NORMAL;
        public EMDITabCloseButtonState CloseButtonState
        {
            get { return _closeButtonState; }
            set { _closeButtonState = value; }
        }

        private Rectangle _closeTabRect;
        public Rectangle CloseTabRect { get { return _closeTabRect; } }

        private Rectangle _tabRect;
        public Rectangle TabRect 
        {
            get { return _tabRect; }
            set
            {
                _tabRect = value;

                _closeTabRect.Location = new Point(_tabRect.Location.X + _tabRect.Width - (_tabRect.Height - 4) - 2, 2);
                _closeTabRect.Size = new Size(_tabRect.Height - 4, _tabRect.Height - 4);
            }
        }

        private MetroWindow _associateForm;
        public MetroWindow AssociateForm 
        {
            get { return _associateForm; }
            set
            {
                _associateForm = value;
            }
        }
    }
}
