using DataServer;
using ESRI.ArcGIS.Client;
using System.ComponentModel.Composition;

namespace FieldOfViewModel
{
    /// <summary>
    /// Field of View model: computes the field of view based on a height map.
    /// 
    /// Model.InputParameters are: 
    ///     Color:          A value in hex (#AARRGGBB)
    ///     StrokeWidth:    The stroke width (only applies to polygons)
    ///     Precision:      The precision of the algorithm (only applies to polygons)
    ///     OperatingMode:  Currently, only Image or Polygon are supported.
    /// Labels
    ///     Model.Id + ".StartAngle":   start angle (default is 0, looking north)
    ///     Model.Id + ".Distance":     distance in meters (default = 1000)
    ///     Model.Id + ".ViewAngle":    view angle in degrees (default = 45)
    ///     Model.Id + ".Height":       height of sensor in meters (default = 2)
    ///     Model.Id + ".Enabled":      turn it on or off (default = true).
    ///     Model.Id + ".Domain":       to specify the mask. Options are surface (water mask) or ground (land mask). Default applies no mask.
    /// </summary>
    [Export(typeof(IModel))]
    public class FoVModel : IModel
    {
        public string Type
        {
            get { return "FoV"; }
        }

        public PoiService Service { get; set; }

        public DataServerBase DataServer { get; set; }

        public object Layer { get; set; }

        private string id;

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        public Model Model { get; set; }

        public ElementLayer ImageLayer { get; set; }

        public GraphicsLayer GraphicsLayer { private get; set; }

        public IModelPoiInstance GetPoiInstance(PoI poi)
        {
            var vop = new FoVPoi { Poi = poi, Model = this, ImageLayer = ImageLayer, GraphicsLayer = GraphicsLayer };
            poi.ModelInstances[id] = vop;
            return vop;
        }

        public void RemovePoiInstance(PoI poi)
        {
            if (!poi.ModelInstances.ContainsKey(id)) return;
            poi.ModelInstances[id].Stop();
            poi.ModelInstances.Remove(id);
        }

        public void Start()
        {
        }

        public void Stop()
        {
            //if (ImageLayer == null) return;
            //if (((dsBaseLayer)Layer).ChildLayers.Contains(ImageLayer))
            //    ((dsBaseLayer)Layer).ChildLayers.Remove(ImageLayer);
        }

    }
}