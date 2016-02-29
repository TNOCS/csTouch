using System;
using System.ComponentModel.Composition;
using csShared.Interfaces;

namespace csGeoLayers.MapTools.CameraTool
{

    

    [Export(typeof(IMapToolPlugin))]
    public class CameraToolPlugin : IMapToolPlugin
    {
        public Type Control
        {
            get { return typeof(ucCameraTool); }
        }

        public bool IsOnline { get { return false; }}

        public string Name
        {
            get { return "CameraTool"; }
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
