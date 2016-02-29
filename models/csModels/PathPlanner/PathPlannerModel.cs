using System;
using DataServer;

namespace csModels.PathPlanner
{
    public class PathPlannerModel : IModel
    {
        public string Type { get { return "PathPlanner"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var ppp = new PathPlannerPoi { Poi = poi, Model = this };
            var key = Id + ".VisitedLocations"; // NOTE the key with the same name in PathPlannerViewModel
            if (poi.Labels.ContainsKey(key) && !string.IsNullOrEmpty(poi.Labels[key]))
            {
                ppp.VisitedLocations = new VisitedLocations();
                var visitedLocations = poi.Labels[key].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var split in visitedLocations)
                {
                    var visitedLocation = new VisitedLocation();
                    visitedLocation.FromString(split);
                    ppp.VisitedLocations.Add(visitedLocation);
                }
            }
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
