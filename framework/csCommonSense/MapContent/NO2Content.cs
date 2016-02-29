using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using csShared;
using csShared.Geo.Content;

namespace csGeoLayers.GeoRSS
{
    public class NO2Content : PropertyChangedBase, IContent
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

        public void Add()
        {            
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Environment");
            var w = new WebMercator();
            ElementLayer wi = new ElementLayer() { ID = "NO2" };
            var i = new Image
            {
                Source = new BitmapImage(new Uri("http://cool2.mooo.com/urbanfloodwebsite/images/NO2proj2.png")),
                IsHitTestVisible = false,
                Stretch = Stretch.Fill
            };
            //<LatLonBox id="khLatLonBox218"><north>90</north><south>-90</south><east>180</east><west>-180</west></LatLonBox>

            //102100
            var mpa = new MapPoint(-14.9515, 41.4389);
            var mpb = new MapPoint(20.4106, 59.9934);
            mpa = w.FromGeographic(mpa) as MapPoint;
            mpb = w.FromGeographic(mpb) as MapPoint;

            mpa = w.FromGeographic(new MapPoint(-180, -85.0511, new SpatialReference(4326))) as MapPoint;
            mpb = w.FromGeographic(new MapPoint(180, 85.0511, new SpatialReference(4326))) as MapPoint;
            var envelope = new Envelope(mpa, mpb);
            ElementLayer.SetEnvelope(i, envelope);

            wi.Children.Add(i);
            wi.Initialize();
            wi.Visible = true;
            gl.ChildLayers.Add(wi);


        }

        public void Remove()
        {

        }

        private string _name = "NO2 World";

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
