using csShared.Interfaces;
using System;
using System.ComponentModel.Composition;

namespace csCommon.MapTools.GeoCodingTool
{
    [Export(typeof(IMapToolPlugin))]
    public class GeoCodingMapToolPlugin : IMapToolPlugin
    {
        public bool IsOnline { get { return true; } }

        public Type Control
        {
            get { return typeof(ucGeoCodingTool); }
        }

        public string Name
        {
            get { return "GeoCodingMapTool"; }
        }

        public void Init()
        {

        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        public bool Enabled { get; set; }
    }
}
