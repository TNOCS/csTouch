using System.Windows.Controls;

namespace csDataServerPlugin
{
    /// <summary>
    /// Interaction logic for PoiLayerSelectionView.xaml
    /// </summary>
    public partial class TabItemView : UserControl
    {
        public TabItemView()
        {
            InitializeComponent();
        }

        private void SurfaceButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //PoiLayerSelectionViewModel psvm = (PoiLayerSelectionViewModel)this.DataContext;
            //DataService ds = ((FrameworkElement)sender).DataContext as DataService;
            //psvm.Plugin.RegisterDataService(ds,true);
            
            
            // TODO: Add event handler implementation here.
        }

        private void sbRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //PoiLayerSelectionViewModel psvm = (PoiLayerSelectionViewModel)this.DataContext;
            //PoiLayer fe = ((FrameworkElement)sender).DataContext as PoiLayer;
            //psvm.Plugin.DeregisterDataService(fe.DataService);            
        }
    }
}
