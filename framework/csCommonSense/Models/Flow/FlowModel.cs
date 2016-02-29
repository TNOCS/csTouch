using System.Linq;
using csShared;
using DataServer;

namespace csModels.Flow
{
    public class FlowModel : IModel
    {
        public string Type { get { return "Flow"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }
        
        public DataServerBase DataServer { get; set; }
        
        public Model Model { get; set; }
        
        public object Layer { get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var ncp = new FlowPoi { Poi = poi, Model = this };
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
            Service.Tapped += Service_Tapped;
            //var layer = Layer as dsBaseLayer;
            //if (layer == null || layer.ChildLayers == null) return;
            //NetworkLayer = layer.ChildLayers.FirstOrDefault(c => c.ID == Id) as GraphicsLayer;
            //if (NetworkLayer != null) return;
            //NetworkLayer = new GraphicsLayer { ID = Id };
            //NetworkLayer.Initialize();
            //layer.ChildLayers.Insert(0, NetworkLayer);
        }

        AppStateSettings AppState {get { return AppStateSettings.Instance; }}

        void Service_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Content.PoiTypeId == "Terminal")
            {
                var db = AppState.Dashboards.FirstOrDefault(k => k.Title == "Terminal");
                if (db == null) return;
                AppState.Dashboards.ActiveDashboard = db;
            }
        }

        public void Stop()
        {
        }
    }
}
