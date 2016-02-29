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

namespace nl.tno.cs.presenter
{
    /// <summary>
    /// Interaction logic for MetroExplorerViewer.xaml
    /// </summary>
    public partial class MetroExplorerView : UserControl
    {
        public MetroExplorerView()
        {
            InitializeComponent();
            this.PreviewMouseWheel += new MouseWheelEventHandler(MetroExplorerView_MouseWheel);
        }

        void MetroExplorerView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            
        }
    }
}
