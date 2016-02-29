using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;
using System.ComponentModel.Composition;

namespace csModels.ExplosionModel
{
    [Export(typeof(IModel))]
    public class ExplosionModel : IModel
    {
        private GraphicsLayer explosionLayer;

        public string Type
        {
            get { return "ExplosionModel"; }
        }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var vop = new ExplosionPoi { Poi = poi, Model = this, ExplosionLayer = explosionLayer };
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
            if (explosionLayer != null) return;
            explosionLayer = new GraphicsLayer { ID = Id };
            explosionLayer.Initialize();
            ((dsBaseLayer)Layer).ChildLayers.Insert(0, explosionLayer);
        }

        public void Stop() { }
    }
}
