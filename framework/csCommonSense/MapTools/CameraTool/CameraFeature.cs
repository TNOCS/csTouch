using csCommon.MapPlugins.MapTools.Georeference;
using csShared.Geo;

namespace csCommon.MapPlugins.MapTools.CameraTool
{
    public class CameraFeature : BaseFeature
    {

        private KmlPoint _destination;

        public KmlPoint Destination
        {
            get { return _destination; }
            set { _destination = value; NotifyOfPropertyChange(()=>Destination); }
        }
        

        public CameraFeature()
       {
           MapControlType = typeof (ucCameraTool);
          
       }
 
    }
}