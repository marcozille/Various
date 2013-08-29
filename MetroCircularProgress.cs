using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MetroUI.Componenti;
using MetroUI.Interfacce;
using MetroUI.Controlli;
using MetroUI.Controlli.Finestre;
using MetroUI.Controlli.ControlliUtente;

namespace MetroUI.Controlli.ControlliUtente
{
    public partial class MetroCircularProgress : MetroWindowControlBase
    {
       private Timer _timerPartenza;
        private Timer _timerAttesaFine;

        private List<MetroCircularProgressElement> _elements;

        private bool _enabled;
        [Category("Behavior")]
        [Description("Attiva o disattiva il controllo")]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _timerPartenza.Enabled = value;

                if(value == false)
                    StopAll();
            }
        }

        public MetroCircularProgress()
        {
            InitializeElements();
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);

            _timerPartenza = new Timer();
            _timerPartenza.Interval = 200;
            _timerPartenza.Tick += _timerPartenza_Tick;
            _timerAttesaFine = new Timer();
            _timerAttesaFine.Interval = 200;
            _timerAttesaFine.Tick += _timerAttesaFine_Tick;
        }

        public override Color GetControlColor()
        {
            if (Parent is MetroWindow)
            {
                MetroWindow wnd = Parent as MetroWindow;

                if (wnd.IsMdiContainer)
                    return wnd.VisualManager().WindowForeColor;
                else
                    return wnd.VisualManager().WindowBackColor;
            }
            else if (Parent is IMetroControl)
            {
                MetroWindowControlBase ctrl = Parent as MetroWindowControlBase;

                return ctrl.GetControlColor();
            }
            else
            {
                return Parent.BackColor;
            }
        }

        private void InitializeElements()
        {
            _elements = new List<MetroCircularProgressElement>();

            MetroCircularProgressElement elem_1, elem_2, elem_3, elem_4, elem_5;
            elem_1 = new MetroCircularProgressElement(MetroCircularProgressElement.ID_ELEMENTO.UNO);
            elem_2 = new MetroCircularProgressElement(MetroCircularProgressElement.ID_ELEMENTO.DUE);
            elem_3 = new MetroCircularProgressElement(MetroCircularProgressElement.ID_ELEMENTO.TRE);
            elem_4 = new MetroCircularProgressElement(MetroCircularProgressElement.ID_ELEMENTO.QUATTRO);
            elem_5 = new MetroCircularProgressElement(MetroCircularProgressElement.ID_ELEMENTO.CINQUE);

            int posY = ClientSize.Height / 2;

            elem_1.Posizione = new Point(0, posY);
            elem_2.Posizione = new Point(0, posY);
            elem_3.Posizione = new Point(0, posY);
            elem_4.Posizione = new Point(0, posY);
            elem_5.Posizione = new Point(0, posY);

            elem_1.Width = Width;
            elem_2.Width = Width;
            elem_3.Width = Width;
            elem_4.Width = Width;
            elem_5.Width = Width;

            elem_1.OnUpdateDraw += OnUpdateElementDraw;
            elem_2.OnUpdateDraw += OnUpdateElementDraw;
            elem_3.OnUpdateDraw += OnUpdateElementDraw;
            elem_4.OnUpdateDraw += OnUpdateElementDraw;
            elem_5.OnUpdateDraw += OnUpdateElementDraw;

            elem_1.OnEndReached += OnElemEndReached;
            elem_2.OnEndReached += OnElemEndReached;
            elem_3.OnEndReached += OnElemEndReached;
            elem_4.OnEndReached += OnElemEndReached;
            elem_5.OnEndReached += OnElemEndReached;

            _elements.Add(elem_1);
            _elements.Add(elem_2);
            _elements.Add(elem_3);
            _elements.Add(elem_4);
            _elements.Add(elem_5);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            int posY = ClientSize.Height / 2;

            for (int i = 0; i < _elements.Count; i++)
            {
                _elements[i].Width = ClientSize.Width;
                _elements[i].Posizione = new Point(_elements[i].Posizione.X, posY);
            }
        }

        private void StopAll()
        {
            foreach(MetroCircularProgressElement elem in _elements)
                elem.Stop();
        }

        void OnElemEndReached(object sender, EventArgs e)
        {
            if(((MetroCircularProgressElement)sender).Id == MetroCircularProgressElement.ID_ELEMENTO.CINQUE)
                _timerAttesaFine.Start();
        }

        void OnUpdateElementDraw(object sender, EventArgs e)
        {
            Invalidate();
        }
        
        void _timerAttesaFine_Tick(object sender, EventArgs e)
        {
            _timerPartenza.Start();
            _timerAttesaFine.Stop();
        }

        void _timerPartenza_Tick(object sender, EventArgs e)
        {
            int tmp = 0;

            foreach(MetroCircularProgressElement elem in _elements)
            {
                tmp++;
                if(elem.Active)
                    continue;
                elem.Start();
                break;
            }

            if(tmp == _elements.Count)
                _timerPartenza.Enabled = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            foreach (MetroCircularProgressElement elem in _elements)
                elem.Draw(VisualManager(), e.Graphics);

            base.OnPaint(e);
        }
    }
    
    class MetroCircularProgressElement
    {
        private int _width;
        public int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                CalcolaZone();
                CalcolaVelocita();
            }
        }
        public Point Posizione { get; set; }

        private float _angoloCorrente;
        private float _spostamento;
        private float _velocitaSpostamentoCostante;
        private float _velocitaRallentamentoAccellerazione;
        private float _massimaVelocita;
        private float _minimoSpostamento;

        private float _inizioZonaCostante_primoGiro;
        private float _inizioZonaRallentamento_primoGiro;
        private float _inizioZonaAccellerazione_primoGiro;
        private float _inizioZonaCostante_secondoGiro;
        private float _inizioZonaRallentamento_secondoGiro;
        private float _inizioZonaAccellerazione_secondoGiro;

        public bool Active { get; set; }

        public delegate void OnUpdateDrawEventHandler(object sender, EventArgs e);
        public delegate void OnEndReachedEventHandler(object sender, EventArgs e);
        public event OnUpdateDrawEventHandler OnUpdateDraw;
        public event OnEndReachedEventHandler OnEndReached;

        private Timer _timerSpostamento;

        public enum ID_ELEMENTO : uint
        {
            UNO = 1,
            DUE = 2,
            TRE = 3,
            QUATTRO = 4,
            CINQUE = 5
        }

        public ID_ELEMENTO Id { get; set; }

        public MetroCircularProgressElement(ID_ELEMENTO id = ID_ELEMENTO.UNO)
        {
            Posizione = new Point();
            Id = id;
            OnUpdateDraw = null;

            _timerSpostamento = new Timer();
            _timerSpostamento.Interval = 10;
            _timerSpostamento.Tick += _timerSpostamento_Tick;
        }

        public void Start()
        {
            if (!_timerSpostamento.Enabled)
                _timerSpostamento.Enabled = true;
            _angoloCorrente = 90;
            Active = true;
            CalcolaZone();
            CalcolaVelocita();
        }
        public void Stop()
        {
            if (_timerSpostamento.Enabled)
                _timerSpostamento.Enabled = false;
            Active = false;
        }

        void _timerSpostamento_Tick(object sender, EventArgs e)
        {
            if(_angoloCorrente < 360)
            {
                if (_angoloCorrente >= 55 && _angoloCorrente < _inizioZonaCostante_primoGiro)
                    _spostamento -= _velocitaRallentamentoAccellerazione;
                else if (_angoloCorrente >= _inizioZonaCostante_primoGiro && _angoloCorrente < _inizioZonaAccellerazione_primoGiro)
                    _spostamento = _velocitaSpostamentoCostante;
                else if (_angoloCorrente >= _inizioZonaAccellerazione_primoGiro && _angoloCorrente < _inizioZonaCostante_secondoGiro)
                    _spostamento += _velocitaRallentamentoAccellerazione;
            }
            else
            {
                if (_angoloCorrente >= 360 && _angoloCorrente < _inizioZonaRallentamento_secondoGiro)
                    _spostamento += _velocitaRallentamentoAccellerazione;
                else if(_angoloCorrente >= _inizioZonaRallentamento_secondoGiro && _angoloCorrente < _inizioZonaCostante_secondoGiro)
                    _spostamento -= _velocitaRallentamentoAccellerazione;
                else if (_angoloCorrente >= _inizioZonaCostante_secondoGiro && _angoloCorrente < _inizioZonaAccellerazione_secondoGiro)
                    _spostamento = _velocitaSpostamentoCostante;
                else if (_angoloCorrente >= _inizioZonaAccellerazione_secondoGiro && _angoloCorrente < 810)
                    _spostamento += _velocitaRallentamentoAccellerazione;
            }

            if (_spostamento <= 1.5)
                _spostamento = 1.5f;
            if (_spostamento >= _massimaVelocita)
                _spostamento = (int)_massimaVelocita;

            _angoloCorrente += _spostamento;

            if (_angoloCorrente >= 810)
            {
                _angoloCorrente = 90;
                Stop();

                if (OnEndReached != null)
                    OnEndReached(this, new EventArgs());
            }

            if (OnUpdateDraw != null)
                OnUpdateDraw(this, new EventArgs());
        }

        public void Draw(MetroVisualManager vm, Graphics g)
        {
            if (_angoloCorrente <= 90)
                return;

            Point p = CalcolaPosizioneDaAngolo();
            Color clr = vm.MetroCircularProgressColor;

            if(Width < 70)
                g.FillEllipse(new SolidBrush(clr), new Rectangle(new Point(p.X - 2, p.Y - 2), new Size(3, 3)));
            else
                g.FillEllipse(new SolidBrush(clr), new Rectangle(new Point(p.X - 3, p.Y - 3), new Size(5, 5)));
        }

        private Point CalcolaPosizioneDaAngolo()
        {
            double angoloRad = (_angoloCorrente * Math.PI) / 180;

            float raggio = (Width - 10) / 2;
            float centro = (Width - 10) / 2;
            double x = raggio * Math.Cos(angoloRad) + centro + 5;
            double y = raggio * Math.Sin(angoloRad) + centro + 5;

            return new Point((int)x, (int)y);
        }

        void CalcolaZone()
        {
            _inizioZonaRallentamento_primoGiro = 90;
            _inizioZonaCostante_primoGiro = 190;
            _inizioZonaAccellerazione_primoGiro = 280;
            _inizioZonaCostante_secondoGiro = _inizioZonaCostante_primoGiro + 360;
            _inizioZonaRallentamento_secondoGiro = _inizioZonaRallentamento_primoGiro + 360;
            _inizioZonaAccellerazione_secondoGiro = _inizioZonaAccellerazione_primoGiro + 360;
        }

        void CalcolaVelocita()
        {
            float angoloZonaCostante = 90;
            _velocitaSpostamentoCostante = angoloZonaCostante / 80;

            _velocitaRallentamentoAccellerazione = ((135 / 25) - _velocitaSpostamentoCostante) / 80;
            _spostamento = 135 / 25;
            _massimaVelocita = 135 / 25;
            _minimoSpostamento = Width / 85;
        }
    }
}
