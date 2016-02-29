using DataServer;

namespace csModels.Router
{
    /// <summary>
    /// The RouterModel is responsible for drawing complex routes on the map.
    /// A route can consist of a combination of freehand lines, polylines and Google driving or walking routes.
    /// 
    /// You can add waypoints to segment the route in parts. For each segment, you can specify the 
    /// speed. You can also delete a segment, in which case the user is required to draw an alternative
    /// route between the two segments (except when we are dealing with the first or last segment). The 
    /// default is a straight line. In addition, you can specify the start and finish time of each 
    /// segment. For each waypoint, you can specify the arrival and departure time, which must match the finish 
    /// and start time of the corresponding segments, naturally, and, optionally, its altitude.
    /// </summary>
    public class RouterModel : IModel
    {
        public string Type { get { return "Router"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        /// <summary>
        /// Called for every PoI, so here is the place to process the parameters and pass them to the PoI.
        /// </summary>
        /// <param name="poi"></param>
        /// <returns></returns>
        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var pep = new RouterPoi { Poi = poi, Model = this, };
            poi.ModelInstances[Id] = pep;
            return pep;
        }

        public void RemovePoiInstance(PoI poi)
        {
            if (!poi.ModelInstances.ContainsKey(Id)) return;
            poi.ModelInstances[Id].Stop();
            poi.ModelInstances.Remove(Id);
        }

        /// <summary>
        /// Started only once in a service
        /// </summary>
        public void Start()
        {
        }

        public void Stop()
        {
        }

    }
}
