using System;
using Caliburn.Micro;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.MapPlugins.Search
{
    public class BaseSearch : PropertyChangedBase, ISearchApi
    {
        public string Key { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Envelope Extent { get; set; }

        public SearchPlugin Plugin { get; set; }
        public virtual string Title { get; set; }

        public virtual bool IsOnline { get; set; }

        public virtual void DoSearch(SearchPlugin plugin)
        {
            Plugin = plugin;
        }

    }
}