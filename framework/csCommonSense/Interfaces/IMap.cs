using csShared.Geo;
using csShared.Interfaces;

namespace csCommon
{
    public interface IMap
    {
        ITimelineManager Timeline { get; set;  }
    }
}
