using Caliburn.Micro;
using ESRI.ArcGIS.Client.Geometry;

namespace csBookmarkPlugin
{
    public class Bookmark : PropertyChangedBase
    {
        public string Id { get; set; }

        public Envelope Extent { get; set; }
    }
}