using System;
using Caliburn.Micro;
using DataServer;

namespace NetworkModel
{
    public class NetworkCreatorModel : IModel
    {
        public string Type { get { return "NetworkCreator"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }
        
        public DataServerBase DataServer { get; set; }
        
        public Model Model { get; set; }
        
        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var ncp = new NetworkCreatorPoi { Poi = poi, Model = this };
            var key = Id + ".Networks";
            if (poi.Labels.ContainsKey(key) && !string.IsNullOrEmpty(poi.Labels[key]))
            {
                ncp.Networks = new BindableCollection<Network>();
                var networks = poi.Labels[key].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var network in networks)
                {
                    ncp.Networks.Add(new Network { Title = network });
                }
            }
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
