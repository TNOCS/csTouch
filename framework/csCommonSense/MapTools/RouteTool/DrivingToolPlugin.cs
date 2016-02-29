using System;
using System.ComponentModel.Composition;
using csShared.Interfaces;

namespace csCommon.MapPlugins.MapTools.RouteTool
{
    [Export(typeof(IMapToolPlugin))]
    public class DrivingToolPlugin : IMapToolPlugin
    {

        public bool IsOnline { get { return true; } }

        public Type Control
        {
            get { return typeof(ucRouteTool); }
        }

        public string Name
        {
            get { return "DrivingRouteTool"; }
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
