using System.ComponentModel.Composition;
using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;

namespace csModels.TrackHistory
{
    [Export(typeof(IModel))]
    public class TrackHistoryModel : IModel
    {
        private GraphicsLayer tracksLayer;

        public string Type { get { return "TrackHistory"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var thp = new TrackHistoryPoi { Poi = poi, Model = this, GraphicsLayer = tracksLayer };
            poi.ModelInstances[Id] = thp;
            return thp;
        }

        public void RemovePoiInstance(PoI poi)
        {
            if (!poi.ModelInstances.ContainsKey(Id)) return;
            poi.ModelInstances[Id].Stop();
            poi.ModelInstances.Remove(Id);
        }

        public void Start()
        {
            if (tracksLayer != null) return;
            tracksLayer                 = new GraphicsLayer {
                ID                      = Id,
                RenderingMode           = GraphicsLayerRenderingMode.Static,
                RendererTakesPrecedence = false
            };
            tracksLayer.Initialize();
            ((dsBaseLayer)Layer).ChildLayers.Insert(0, tracksLayer);
        }

        public void Stop()
        {
        }
    }
}
