using System.ComponentModel.Composition;
using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;

namespace csModels.ZoneModel
{
    [Export(typeof(IModel))]
    public class ZonesModel : IModel
    {
        public string Type
        {
            get { return "Zones"; }
        }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public object Layer { get; set; }

        public string Id { get; set; }

        public Model Model { get; set; }

        public GraphicsLayer ZonesLayer;

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var vop = new ZonesPoi { Poi = poi, Model = this, ZoneLayer = ZonesLayer };
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
            if (ZonesLayer != null) return;
            ZonesLayer = new GraphicsLayer { ID = Id };
            ZonesLayer.Initialize();
            ((dsBaseLayer)Layer).ChildLayers.Insert(0, ZonesLayer);
        }

        public void Stop()
        {
            if (ZonesLayer == null) return;

            //if (((dsBaseLayer)Layer).ChildLayers.Contains(ImageLayer))
            //    ((dsBaseLayer)Layer).ChildLayers.Remove(ImageLayer);
        }
    }
}