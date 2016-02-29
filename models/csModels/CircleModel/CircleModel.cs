using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;
using System.ComponentModel.Composition;

namespace csModels.CircleModel
{
    [Export(typeof(IModel))]
    public class CircleModel : IModel
    {
        private GraphicsLayer circleLayer;

        public string Type
        {
            get { return "CircleModel"; }
        }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var vop = new CirclePoi { Poi = poi, Model = this, CircleLayer = circleLayer };
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
            if (circleLayer != null) return;
            circleLayer = new GraphicsLayer { ID = Id, IsHitTestVisible = false };
            circleLayer.Initialize();
            ((dsBaseLayer)Layer).ChildLayers.Insert(0, circleLayer);
        }

        public void Stop() { }
    }
}
