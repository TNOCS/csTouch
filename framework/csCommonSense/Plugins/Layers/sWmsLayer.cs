using ESRI.ArcGIS.Client.Toolkit.DataSources;

namespace csCommon
{
    public class sWmsLayer : sLayer
    {
        private WmsLayer.LayerInfo layerInfo;

        public WmsLayer.LayerInfo LayerInfo
        {
            get { return layerInfo; }
            set { layerInfo = value; NotifyOfPropertyChange(() => LayerInfo); }
        }

        public sWmsLayer BaseLayer { get; set; }

        
        

        private bool visible;

        public new bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                visible = value; NotifyOfPropertyChange(() => Visible);
            }
        }


        public new sLayer Parent { get; set; } // FIXME TODO "new" keyword missing?
    }
}