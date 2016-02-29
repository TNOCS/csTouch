using System;
using System.ComponentModel.Composition;
using csShared.Interfaces;

namespace csCommon.MapPlugins.MapTools.RouteTool
{
    [Export(typeof(IMapToolPlugin))]
    public class RouteToolPlugin : IMapToolPlugin
    {
        public Type Control
        {
            get { return typeof(ucWalkingTool); }
        }

        public bool IsOnline { get { return true; } }

        public string Name
        {
            get { return "WalkingRouteTool"; }
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
