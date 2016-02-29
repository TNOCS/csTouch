using System;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.MapPlugins.Search
{
    public interface ISearchApi
    {
        string Title { get; set; }
        string Key { get; set; }
        DateTime StartTime { get; set; }
        DateTime EndTime { get; set; }
        Envelope Extent { get; set; }
        SearchPlugin Plugin { get; set; }
        bool IsOnline { get;  }

        void DoSearch(SearchPlugin plugin);
    }
}