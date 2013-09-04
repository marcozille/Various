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
        #region Priprietà private

        #region Metro Styles

        private MetroShadowForm         _metroShadowForm;
        private MetroVisualManager      _visualManager; 
        private MetroWindowTextPos      _posizioneTitolo;
        private MetroWindowBorderStyle  _stileBordo;

        #endregion

        #region Utilità

        private Timer   _refreshTimer;
        private int     _iStoreHeight;
        private bool    _bIsMaximizing;
        private int     _altezzaBarraTitolo;
        private bool    _isActive;
        private int     _larghezzaPulsanti;
        private bool    _closeBox;

        #endregion

        #region MDI
        
        private MdiClientController _mdiClientController;
        
        #endregion
        
        #endregion

        #region Proprietà pubbliche

        #region Metro Styles

        private bool _mostraBordo;
        public bool MostraBordo { get { return _mostraBordo; } set { _mostraBordo = value; UpdateDisplay(); } }

        public virtual MetroVisualManager VisualManager(bool bSet = false, MetroVisualManager vm = null)
        {
            if (bSet)
            {
                _visualManager = vm;
                return null;
            }
            else
            {
                if (_visualManager == null)
                {
                    IMetroControl parentControl = Owner as IMetroControl;

                    if (parentControl != null)
                        return parentControl.VisualManager();

                    IMetroWindow parentWindow = Owner as IMetroWindow;

                    if (parentWindow != null)
                        return parentWindow.VisualManager();

                    throw new ArgumentException("Impissibile recuperare il MetroVisualManager");
                }
                else
                {
                    return _visualManager;
                }
            }
        }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public CombinazioneColori CombinazioneColori
        {
            get { return VisualManager().CombinazioneColori; }
            set
            {
                VisualManager().CombinazioneColori = value;
                if (OnCombinazioneColoriChanged != null)
                    OnCombinazioneColoriChanged(this, new EventArgs());
                UpdateDisplay();
            }
        }

        private bool _mostraBarraTitolo;
        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public bool MostraBarraTitolo
        {
            get { return _mostraBarraTitolo; }
            set
            {
                _mostraBarraTitolo = value;
                UpdateDisplay();
            }
        }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public StileMetro StileMetro
        {
            get { return VisualManager().StileMetro; }
            set
            {
                VisualManager().StileMetro = value;
                if (OnStileMetroChanged != null)
                    OnStileMetroChanged(this, new EventArgs());
                UpdateDisplay();
            }
        }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public MetroWindowTextPos PosizioneTitolo
        {
            get { return _posizioneTitolo; }
            set
            {
                _posizioneTitolo = value;
                UpdateDisplay();
            }
        }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public MetroWindowBorderStyle StileBordo
        {
            get { return _stileBordo; }
            set
            {
                _stileBordo = value;
                UpdateDisplay();
            }
        }

        #endregion

        #region Utilità

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public int AltezzaBarraTitolo
        {
            get { return _altezzaBarraTitolo; }
            set
            {
                if (value < 20) throw new InvalidOperationException("La dimensione minima della barra del titolo è di 20px");
                _altezzaBarraTitolo = value;
                UpdateDisplay();
            }
        }

        [Category(MetroDefaults.CategoriaProprietà.Comportamento)]
        public bool IsActive { get { return _isActive; } }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public int LarghezzaPulsanti
        {
            get { return _larghezzaPulsanti; }
            set
            {
                if (value < 20)
                    throw new InvalidOperationException("La larghezza minima dei pulsanti è di 20px");
                _larghezzaPulsanti = value;
                UpdateDisplay();
            }
        }

        [Category("Behavior")]
        public bool CloseBox
        {
            get { return _closeBox; }
            set { _closeBox = value; UpdateMetroWindowButtons(); }
        }

        public bool ShowShadowForm { get; set; }

        protected Rectangle WindowRectangle { get { return new Rectangle(0, 0, Width, Height); } }

        #endregion

        #region MDI

        public MdiClientController MDIClientController { get { return _mdiClientController; } }

        #endregion

        #region Override

        new public bool IsMdiContainer
        {
            get { return base.IsMdiContainer; }
            set
            {
                base.IsMdiContainer = value;
                _mdiClientController.RenewMdiClient();
                _mdiClientController.BorderStyle = BorderStyle.None;
            }
        }
        
        new public Form MdiParent
        {
            get { return base.MdiParent; }
            set
            {
                base.MdiParent = value;

                if (value == null)
                {
                    ShowShadowForm = true;
                    WindowState = FormWindowState.Normal;
                    StileBordo = MetroWindowBorderStyle.Ridimensionabile;
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
                }
                else
                {
                    Visible = false;
                    ShowShadowForm = false;
                    StileBordo = MetroWindowBorderStyle.Fisso;
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    CloseBox = false;
                    MaximizeBox = false;
                    MinimizeBox = false;
                    Visible = true;
                }
            }
        }

        [Category("Behavior")]
        new public bool MinimizeBox
        {
            get { return base.MinimizeBox; }
            set { base.MinimizeBox = value; UpdateMetroWindowButtons(); }
        }

        [Category("Behavior")]
        new public bool MaximizeBox
        {
            get { return base.MaximizeBox; }
            set { base.MaximizeBox = value; UpdateMetroWindowButtons(); }
        }

        #endregion

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public Image Icona { get; set; }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public bool MostraIcona { get; set; }

        [Category(MetroDefaults.CategoriaProprietà.Apparenza)]
        public bool MDIFormsTab { get; set; }

        #endregion
    }
}
