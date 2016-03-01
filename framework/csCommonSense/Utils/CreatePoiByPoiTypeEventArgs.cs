using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Utils
{
    using csCommon.Plugins.Events;

    using DataServer;

    public class CreatePoiByPoiTypeEventArgs : EventArgs
    {
        public CreatePoiByPoiTypeEventArgs(PoI pPoiType, Position pPosition)
        {
            LatLonPosition = pPosition;
            PoiType = pPoiType;
        }

        public Position LatLonPosition { get; private set; }
        public PoI PoiType { get; private set; }
    }
}
