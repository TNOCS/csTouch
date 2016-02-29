using System.Collections.Generic;
using System.Windows;
using ESRI.ArcGIS.Client.Geometry;
using csShared;
using csShared.FloatingElements;
using csShared.Geo;

namespace csGeoLayers.ShapeFiles
{
    public class ShapeGraphicsLayer : SettingsGraphicsLayer
    {
        public string ShapeFilePath { get; set; }
        //private List<Shapefile.Shape> _shapes;
        private string _filePath;
        private SpatialReference spRef = new SpatialReference(4326);
       
        private FloatingElement _element;

        public ShapeGraphicsLayer(string filePath)
        {
            _filePath = filePath;
        }

        public override void StartSettings()
        {
            var viewModel = new ShapeLayerColorPickerViewModel(this, _filePath);
            
            _element = FloatingHelpers.CreateFloatingElement("Color Picker", new Point(400, 400), new Size(400, 400), viewModel);
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(_element);
        }
    }
}
