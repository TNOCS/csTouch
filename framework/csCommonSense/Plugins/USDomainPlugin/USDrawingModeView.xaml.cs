using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using csSurface;
using SessionsEvents;

namespace csUSDomainPlugin
{
    /// <summary>
    /// Interaction logic for USDrawingModeView.xaml
    /// </summary>
    public partial class USDrawingModeView : UserControl
    {
        public USDrawingModeView()
        {
            InitializeComponent();
        }

        private USDomainPlugin fPlugin;
        public USDomainPlugin Plugin
        {
            get { return fPlugin; }
            set
            {
                fPlugin = value;
                if (fPlugin != null && fPlugin.CurrentDomain != null)
                {
                    //Title = fPlugin.CurrentDomain.domain;
                    //Palette = fPlugin.CurrentDomain.palette;
                }
            }
        }

        private double fOrientation = 0.0;
        public double Orientation
        {
            get { return fOrientation; }
            set 
            {
                if (fOrientation != value)
                {
                    mainGrid.RenderTransform.Value.Rotate(value);
                    fOrientation = value;
                }
            }
        }
    }
}
