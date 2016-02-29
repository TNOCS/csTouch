using Caliburn.Micro;
using csShared;
using csShared.Geo.Content;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using System;
using System.Windows.Media.Imaging;

namespace csGeoLayers.GeoRSS
{
    public class GeoRSSContent : PropertyChangedBase, IContent
    {

        public bool IsOnline
        {
            get { return true; }
        }

        public static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public void Configure()
        {

        }

        private SimpleRenderer renderer;

        public SimpleRenderer Renderer
        {
            get { return renderer; }
            set { renderer = value; NotifyOfPropertyChange(() => Renderer); }
        }

        private Uri _iconUri;

        public Uri IconUri
        {
            get { return _iconUri; }
            set { _iconUri = value; }
        }



        private double _iconSize = 16;

        public double IconSize
        {
            get { return _iconSize; }
            set { _iconSize = value; NotifyOfPropertyChange(() => IconSize); }
        }






        private Uri _location;

        public Uri Location
        {
            get { return _location; }
            set { _location = value; NotifyOfPropertyChange(() => Location); }
        }


        public void Init()
        {

        }

        private GeoRssLayer geoRss;
        private GroupLayer gl;
        public void Add()
        {
            IsRunning = true;

            geoRss = new GeoRssLayer() { ID = Name, Source = Location };
            gl = AppState.ViewDef.FindOrCreateGroupLayer(this.Folder);
            if (gl != null) gl.ChildLayers.Add(geoRss);
            var sr = new SimpleRenderer
            {
                Symbol = new PictureMarkerSymbol()
                {
                    Source = new BitmapImage(IconUri),
                    Width = IconSize,
                    OffsetX = IconSize / 2,
                    OffsetY = IconSize / 2
                }
            };
            geoRss.Renderer = sr; // Renderer;
            geoRss.Initialize();
        }

        public void Remove()
        {
            gl.ChildLayers.Remove(geoRss);
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
