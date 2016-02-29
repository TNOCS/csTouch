using System;
using System.ComponentModel.Composition;
using csGeoLayers.MapTools.MeasureTool;
using csShared.Interfaces;

namespace csCommon.MapPlugins.MapTools.MeasureTool
{
    [Export(typeof(IMapToolPlugin))]
    public class MeasureMapToolPlugin : IMapToolPlugin
    {
        public Type Control
        {
            get { return typeof(ucMeasureMapTool); }
        }

        public bool IsOnline { get { return false; } }

        public string Name
        {
            get { return "MeasureMapTool"; }
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
