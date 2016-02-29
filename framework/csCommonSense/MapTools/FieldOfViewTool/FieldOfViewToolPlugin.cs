using System;
using System.ComponentModel.Composition;
using csShared.Interfaces;

namespace csGeoLayers.MapTools.FieldOfViewTool
{

    

    [Export(typeof(IMapToolPlugin))]
    public class FieldOfViewToolPlugin : IMapToolPlugin
    {
        public Type Control
        {
            get { return typeof(ucFieldOfViewTool); }
        }

        public bool IsOnline { get { return false; } }

        public string Name
        {
            get { return "FieldOfViewTool"; }
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
