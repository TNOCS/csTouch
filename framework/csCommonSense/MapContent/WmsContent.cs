using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using csShared;
using csShared.Geo.Content;

namespace csGeoLayers.GeoRSS
{
    public class WmsContent : PropertyChangedBase, IContent
    {

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        public string[] Layers { get; set; }

        private WmsLayer kml;
        private GroupLayer gl;

        public bool IsOnline
        {
            get { return true; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public void Configure()
        {

        }

        private string _location;

        public string Location
        {
            get { return _location; }
            set { _location = value; NotifyOfPropertyChange(()=>Location); }
        }
        

        public void Init()
        {
            
        }

        public void Add()
        {
            //WmtsLayer kml = new WmtsLayer()
            //                     {
            //                         ID = Name,
            //                         Url =
            //                             "http://134.221.210.43:8008/geoserver/gwc/service/wmts?REQUEST=getcapabilities"
            //                     };
             kml = new WmsLayer() { ID = Name, Url = Location };            
            gl = AppState.ViewDef.FindOrCreateGroupLayer(this.Folder);
            //kml.Version = "1.1.0";
            //kml.ServiceMode = WmtsLayer.WmtsServiceMode.KVP;
            kml.Layers = Layers;    
            if (gl != null) gl.ChildLayers.Add(kml);
            kml.Initialize();
            IsRunning = true;

        }

        public void Remove()
        {
            gl.ChildLayers.Remove(kml);
            IsRunning = false;
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyOfPropertyChange(() => Name); }
        }


        private string _imageUrl;

        public string ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; NotifyOfPropertyChange(() => ImageUrl); }
        }


        public string Folder { get; set; }

    }
}
