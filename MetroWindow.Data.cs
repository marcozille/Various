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
        public enum MetroWindowTextPos { Destra, Centro, Sinistra }
        public enum MetroWindowBorderStyle { Fisso, Ridimensionabile }

        public class MetroShadowStruct
        {
            public CombinazioneColori combinazioneColoriCorrente;
            public Bitmap top;
            public Bitmap left;
            public Bitmap bottom;
            public Bitmap right;
            public Bitmap topRight;
            public Bitmap topLeft;
            public Bitmap bottomRight;
            public Bitmap bottomLeft;
        }
    }
}
