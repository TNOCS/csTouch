using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Geo.Esri;
using csShared.Interfaces;


namespace csCommon.MapPlugins.MapSelection
{
    [Export(typeof (IMapSelection))]
    public class MapSelectionViewModel : Screen, IMapSelection
    {
      
       

        public MapSelectionViewModel()
        {
        }

        public MapSelectionViewModel(DispatcherTimer updateMapTimer)
        {
            
            Maps = AppState.ViewDef.BaseLayerProviders;

            // Default caption
            Caption = "Maps";
        }

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }


        public MapViewDef ViewDef
        {
            get
            {
                return AppState.ViewDef;
            }
        }

        public ObservableCollection<ITileImageProvider> Maps { get; set; }
       
        public FloatingElement Element { get; set; }
        public string Caption { get; set; }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            foreach (var b in AppState.ViewDef.BaseLayerProviders)
            {
                AppState.Commands.AddCommand(b.Title, new []{ "Show " + b.Title}, MapSelected);
            }
        }

        public static void MapSelected(string id)
        {
             AppState.ViewDef.ChangeMapType(id);
        }

        public void SelectMapType(object mt)
        {
            
            AppState.ViewDef.ChangeMapType(((ITileImageProvider) mt).Title);
        }

        public void Remove(ITileImageProvider imageProvider)
        {
            AppState.ViewDef.RemoveMapType(imageProvider.Title);
        }

        public void Add(ITileImageProvider imageProvider)
        {
            AppState.ViewDef.AddMapType(imageProvider.Title);
        }

        public void SelectMapType(object mt, MouseEventArgs e)
        {
            AppState.ViewDef.ChangeMapType(((ITileImageProvider) mt).Title);
            //AppState.ViewDef.SelectedBaseLayer = (ITileImageProvider) mt;
        }
    }

    public class TileLayerExists : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return
                AppStateSettings.Instance.ViewDef.BaseLayers.ChildLayers.Any(
                    k => ((WebTileLayer) k).TileProvider.Title == ((ITileImageProvider) value).Title)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}