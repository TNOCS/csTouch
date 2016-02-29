using System.ComponentModel.Composition;
using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace csModels.GridModel
{
    [Export(typeof(IModel))]
    public class GridModel : IModel
    {
        public string Type
        {
            get { return "Grid"; }
        }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public object Layer { get; set; }

        public string Id { get; set; }

        public Model Model { get; set; }

        public GraphicsLayer GridLayer;

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var vop = new GridPoi {
                Poi = poi,
                Model = this,
                GridLayer = GridLayer
            };
            poi.ModelInstances[Id] = vop;
            return vop;
        }

        public void RemovePoiInstance(PoI poi)
        {
            if (!poi.ModelInstances.ContainsKey(Id)) return;
            poi.ModelInstances[Id].Stop();
            poi.ModelInstances.Remove(Id);
        }

        public void Start()
        {
            if (GridLayer != null) return;
            GridLayer = new GraphicsLayer { ID = Id, Opacity = 0.3 };
            GridLayer.MapTip = CreateMapTip();
            GridLayer.MaximumResolution = 12;
            GridLayer.Initialize();
            ((dsBaseLayer)Layer).ChildLayers.Insert(0, GridLayer);
        }

        private FrameworkElement CreateMapTip()
        {
            var tb = new TextBlock
            {
                Foreground = Brushes.Black,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.SetBinding(TextBlock.TextProperty, new Binding("[NAME]"));
            var mapTip = new Grid
            {
                Background = Brushes.LightYellow
            };
            mapTip.Children.Add(tb);
            return mapTip;
        }

        public void Stop()
        {
            //if (GridLayer == null) return;

            //if (GridLayer.Graphics.Count == 0 && ((dsBaseLayer)Layer).ChildLayers.Contains(GridLayer))
            //{
            //    ((dsBaseLayer)Layer).ChildLayers.Remove(GridLayer);
            //    GridLayer = null;
            //}
        }
    }
}
