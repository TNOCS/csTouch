using System;
using System.ComponentModel.Composition;
using csShared.Interfaces;

namespace csCommon.MapPlugins.MapTools.RangeTool
{
    [Export(typeof(IMapToolPlugin))]
    public class RangeMapToolPlugin : IMapToolPlugin
    {
        public Type Control
        {
            get { return typeof(ucRangeMapTool); }
        }

        public bool IsOnline { get { return false; }}

        public string Name
        {
            get { return "RangeMapTool"; }
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
