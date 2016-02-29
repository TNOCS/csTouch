using System;
using csDataServerPlugin;
using csModels.PathPlanner;
using DataServer;
using ESRI.ArcGIS.Client;

namespace csModels.TrackModel
{
    public class TrackModel : IModel
    {
        public string Type { get { return "Track"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        GraphicsLayer TrackLayer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var ppp = new TrackPoi { Poi = poi, Model = this, TrackLayer = TrackLayer};
            
            poi.ModelInstances[Id] = ppp;
            return ppp;
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
            if (TrackLayer != null) return;
            TrackLayer = new GraphicsLayer { ID = Id };
            TrackLayer.Initialize();
            ((dsBaseLayer)Layer).ChildLayers.Insert(0, TrackLayer);
        }

        public void Stop()
        {
        }
    }
}
