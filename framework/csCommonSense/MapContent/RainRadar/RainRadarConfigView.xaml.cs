using System.Collections.ObjectModel;
using System.Windows;
using ESRI.ArcGIS.Client;
using csShared;

namespace csGeoLayers.Content.RainRadar
{
    public partial class RainRadarConfigView
    {
        private AppStateSettings state
        {
            get { return AppStateSettings.Instance; }
        }

        //private GroupLayer _selectedGroup;

        public ObservableCollection<GroupLayer> PreviousLayers
        {
            get { return (ObservableCollection<GroupLayer>)GetValue(_previousLayersProperty); }
            set { SetValue(_previousLayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for _previousLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty _previousLayersProperty =
            DependencyProperty.Register("_previousLayers", typeof(ObservableCollection<GroupLayer>), typeof(RainRadarConfigView), new UIPropertyMetadata(null));


        public RainRadarConfigView()
        {
            InitializeComponent();            
            this.Loaded += LayersView_Loaded;
        }

            

        void LayersView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {            
           
            
            
        }

       
        
    }
}
