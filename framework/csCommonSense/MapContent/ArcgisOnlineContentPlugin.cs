using System;
using System.Windows;
using Caliburn.Micro;
using csGeoLayers.Plugins.Layers;
using csShared;
using csShared.FloatingElements;
using csShared.Geo.Content;

namespace csGeoLayers.GeoRSS
{
    public class ArcgisOnlineContentPlugin : PropertyChangedBase, IContent
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
            set { isRunning = value; NotifyOfPropertyChange(()=>IsRunning); }
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

        public void Add()
        {
            IsRunning = false;

            var fe = FloatingHelpers.CreateFloatingElement("New Layer", new Point(300, 300), new Size(300, 300),
                                                           new NewLayerViewModel());
            AppState.FloatingItems.AddFloatingElement(fe);

        }

        public void Remove()
        {
           
            
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
