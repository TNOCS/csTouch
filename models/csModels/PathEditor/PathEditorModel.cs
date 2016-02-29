using DataServer;

namespace csModels.PathEditor
{
    public class PathEditorModel : IModel
    {
        public string Type { get { return "PathEditor"; } }

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
            var pep = new PathEditorPoi { Poi = poi, Model = this,  };
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