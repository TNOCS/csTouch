using csShared.Interfaces;
using System;
using System.ComponentModel.Composition;

namespace csCommon.MapTools.BagGeoCodingTool
{
    [Export(typeof(IMapToolPlugin))]
    public class BagGeoCodingMapToolPlugin : IMapToolPlugin
    {
        public bool IsOnline { get { return false; } }

        public Type Control
        {
            get { return typeof(ucBagGeoCodingTool); }
        }

        public string Name
        {
            get { return "BagGeoCodingMapTool"; }
        }

        public void Init() { }

        public void Start() { }

        public void Stop() { }

        public bool Enabled { get; set; }
    }
}
