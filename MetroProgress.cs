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
    public partial class MetroProgress : MetroWindowControlBase
    {
        private Timer _timerPartenza;
        private Timer _timerAttesaFine;

        private List<MetroProgressElement> _elements;

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

        public MetroProgress()
        {
            InitializeElements();
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);

            _timerPartenza = new Timer();
            _timerPartenza.Interval = 100;
            _timerPartenza.Tick += _timerPartenza_Tick;
            _timerAttesaFine = new Timer();
            _timerAttesaFine.Interval = 200;
            _timerAttesaFine.Tick += _timerAttesaFine_Tick;
        }

        private void InitializeElements()
        {
            _elements = new List<MetroProgressElement>();

            MetroProgressElement elem_1, elem_2, elem_3, elem_4, elem_5;
            elem_1 = new MetroProgressElement(MetroProgressElement.ID_ELEMENTO.UNO);
            elem_2 = new MetroProgressElement(MetroProgressElement.ID_ELEMENTO.DUE);
            elem_3 = new MetroProgressElement(MetroProgressElement.ID_ELEMENTO.TRE);
            elem_4 = new MetroProgressElement(MetroProgressElement.ID_ELEMENTO.QUATTRO);
            elem_5 = new MetroProgressElement(MetroProgressElement.ID_ELEMENTO.CINQUE);

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
            foreach(MetroProgressElement elem in _elements)
                elem.Stop();
        }

        void OnElemEndReached(object sender, EventArgs e)
        {
            if(((MetroProgressElement)sender).Id == MetroProgressElement.ID_ELEMENTO.CINQUE)
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

            foreach(MetroProgressElement elem in _elements)
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

        protected override void OnPaint(PaintEventArgs e)
        {
            foreach (MetroProgressElement elem in _elements)
                elem.Draw(VisualManager(), e.Graphics);
        }
    }

    class MetroProgressElement
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

        private float _spostamento;
        private float _velocitaSpostamentoCostante;
        private float _velocitaRallentamentoAccellerazione;
        private float _massimaVelocita;

        private float _inizioZonaCostante;
        private float _inizioZonaAccellerazione;

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

        public MetroProgressElement(ID_ELEMENTO id = ID_ELEMENTO.UNO)
        {
            Posizione = new Point();
            Id = id;
            OnUpdateDraw = null;

            _timerSpostamento = new Timer();
            _timerSpostamento.Interval = 5;
            _timerSpostamento.Tick += _timerSpostamento_Tick;
        }

        public void Start()
        {
            if (!_timerSpostamento.Enabled)
                _timerSpostamento.Enabled = true;
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
            if (Posizione.X >= 0 && Posizione.X < _inizioZonaCostante)
                _spostamento -= _velocitaRallentamentoAccellerazione;
            else if (Posizione.X >= _inizioZonaCostante && Posizione.X < _inizioZonaAccellerazione)
                _spostamento = _velocitaSpostamentoCostante;
            else if (Posizione.X >= _inizioZonaAccellerazione && Posizione.X < Width)
                _spostamento += _velocitaRallentamentoAccellerazione;

            if (_spostamento <= 1)
                _spostamento = 1;
            if (_spostamento >= _massimaVelocita)
                _spostamento = (int)_massimaVelocita;

            int tmp = (int)_spostamento;
            Posizione = new Point(Posizione.X + tmp, Posizione.Y);

            if (Posizione.X >= Width)
            {
                Posizione = new Point(0, Posizione.Y);
                Stop();

                if(OnEndReached != null)
                    OnEndReached(this, new EventArgs());
            }

            if (OnUpdateDraw != null)
                OnUpdateDraw(this, new EventArgs());
        }

        public void Draw(MetroVisualManager vm, Graphics g)
        {
            if (Posizione.X == 0)
                return;

            Color clr = vm.MetroLinearProgressColor;
            g.FillRectangle(new SolidBrush(clr), new Rectangle(new Point(Posizione.X - 2, Posizione.Y - 2), new Size(3, 3)));
        }

        void CalcolaZone()
        {
            float dimensionaZonaCostante = (float)(Width * 0.2);
            float tmp = (float)((float)Width - (float)dimensionaZonaCostante) / 2;

            _inizioZonaCostante = tmp;
            _inizioZonaAccellerazione = _inizioZonaCostante + dimensionaZonaCostante;
        }

        void CalcolaVelocita()
        {
            float dimensionaZonaCostante = (float)(Width * 0.2);
            _velocitaSpostamentoCostante = dimensionaZonaCostante / 100;

            float tmp = (float)((float)Width - (float)dimensionaZonaCostante) / 2;
            _velocitaRallentamentoAccellerazione = ((tmp / 30) - _velocitaSpostamentoCostante) / 50;
            _spostamento = tmp / 30;
            _massimaVelocita = tmp / 30;
        }
    }
}
