using System.ComponentModel.Composition;
using csDataServerPlugin;
using DataServer;
using ESRI.ArcGIS.Client;

namespace csModels.SqlQueryModel
{
    [Export(typeof(IModel))]
    public class SqlQueryModel : IModel
    {
        public string Type { get { return "SqlQueryModel"; } }

        public string Id { get; set; }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public Model Model { get; set; }

        public object Layer { get; set; }

        private GraphicsLayer QueryLayer { get; set; }

        /// <summary>
        /// Called for every PoI, so here is the place to process the parameters and pass them to the PoI.
        /// </summary>
        /// <param name="poi"></param>
        /// <returns></returns>
        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var pep = new SqlQueryPoi { Poi = poi, Model = this, QueryLayer = QueryLayer};
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
            if (QueryLayer != null) return;
            QueryLayer = new GraphicsLayer { ID = Id };
            QueryLayer.Initialize();
            var dsBaseLayer = (dsBaseLayer)Layer;
            if (dsBaseLayer != null) dsBaseLayer.ChildLayers.Insert(0, QueryLayer);
        }

        public void Stop()
        {
        }
    }
}