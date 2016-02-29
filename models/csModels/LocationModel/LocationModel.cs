using System;
using Caliburn.Micro;
using DataServer;

namespace csModels.LocationModel
{
    public class LocationModel : IModel
    {
        public string Type { get { return "Location"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }
        
        public DataServerBase DataServer { get; set; }
        
        public Model Model { get; set; }
        
        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var ncp = new LocationPoi { Poi = poi, Model = this };
           
            poi.ModelInstances[Id] = ncp;
            return ncp;
        }

        public void RemovePoiInstance(PoI poi)
        {
            // TODO Remove all network connections that were created by this POI.
            if (!poi.ModelInstances.ContainsKey(Id)) return;
            poi.ModelInstances[Id].Stop();
            poi.ModelInstances.Remove(Id);
        }

        public void Start()
        {
            //var layer = Layer as dsBaseLayer;
            //if (layer == null || layer.ChildLayers == null) return;
            //NetworkLayer = layer.ChildLayers.FirstOrDefault(c => c.ID == Id) as GraphicsLayer;
            //if (NetworkLayer != null) return;
            //NetworkLayer = new GraphicsLayer { ID = Id };
            //NetworkLayer.Initialize();
            //layer.ChildLayers.Insert(0, NetworkLayer);
        }

        public void Stop()
        {
        }
    }
}
