using System;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using csShared;
using csShared.Geo.Content;

namespace csGeoLayers.FlightTracker
{
    public class FlightTrackerContent : PropertyChangedBase, IContent
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


        private Uri _location;

        public Uri Location
        {
            get { return _location; }
            set { _location = value; NotifyOfPropertyChange(()=>Location); }
        }
        

        public void Init()
        {
            
        }

        private FlightTrackerLayer ftl;
        private GroupLayer gl;
        public void Add()
        {
            
            IsRunning = true;
            Folder = "Transport";
            ftl = new FlightTrackerLayer();            
            gl = AppState.ViewDef.FindOrCreateGroupLayer(this.Folder);
            if (gl != null) gl.ChildLayers.Add(ftl);
            ftl.Initialize();

        }

        public void Remove()
        {
            ftl.Stop();
            gl.ChildLayers.Remove(ftl);
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
