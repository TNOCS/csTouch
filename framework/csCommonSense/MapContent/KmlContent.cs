using Caliburn.Micro;
using csShared;
using csShared.Geo.Content;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using System;

namespace csGeoLayers.GeoRSS
{
    public class KmlContent : PropertyChangedBase, IContent
    {

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

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

        private Uri location;

        public Uri Location
        {
            get { return location; }
            set { location = value; NotifyOfPropertyChange(() => Location); }
        }

        public void Init()
        {

        }

        private KmlLayer kml;
        private GroupLayer gl;

        public void Add()
        {
            IsRunning = true;
            kml = new KmlLayer { ID = Name, Url = Location };
            gl = AppState.ViewDef.FindOrCreateGroupLayer(Folder);
            if (gl != null) gl.ChildLayers.Add(kml);
            kml.Initialize();
        }

        public void Remove()
        {
            gl.ChildLayers.Remove(kml);
            IsRunning = false;
        }

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(() => Name); }
        }


        private string imageUrl;

        public string ImageUrl
        {
            get { return imageUrl; }
            set { imageUrl = value; NotifyOfPropertyChange(() => ImageUrl); }
        }

        public string Folder { get; set; }

    }
}
