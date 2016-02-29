using System;
using System.Globalization;
using System.Net;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using csShared;
using csShared.Geo.Content;

namespace csGeoLayers.Wikipedia
{

    public class WikiSearchResult : PropertyChangedBase
    {
        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private string _summary;

        public string Summary
        {
            get { return _summary; }
            set { _summary = value; }
        }

        private MapPoint _position;

        public MapPoint Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private string _imageUrl;

        public string ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; }
        }

        private string _wikiUrl;

        public string WikiUrl
        {
            get { return _wikiUrl; }
            set { _wikiUrl = value; }
        }
        
        
        
        
        
    }

    public class WikiSearchContent : PropertyChangedBase, IContent
    {

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

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }


        private string _keyword = "Groningen";

        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; NotifyOfPropertyChange(()=>Keyword); }
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

        private BindableCollection<WikiSearchResult> _results = new BindableCollection<WikiSearchResult>();

        public BindableCollection<WikiSearchResult> Results
        {
            get { return _results; }
            set { _results = value; }
        }
        
        private WebMercator _wm = new WebMercator();

        public void Add()
        {            
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Wikipedia");

            GraphicsLayer lWiki = new GraphicsLayer() {ID = "Wiki Search: " + Keyword};
            gl.ChildLayers.Add(lWiki);
            lWiki.Initialize();
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += (e, s) =>
                                              {
                                                  if (s.Error == null)
                                                  {
                                                      Results.Clear();
                                                      XDocument xdoc = XDocument.Parse(s.Result);
                                                      foreach (var a in xdoc.Root.Elements("entry"))
                                                      {
                                                          WikiSearchResult wsr = new WikiSearchResult()
                                                                                     {
                                                                                         Title =
                                                                                             a.Element("title").Value,
                                                                                         Summary =
                                                                                             a.Element("summary").Value,
                                                                                         WikiUrl =
                                                                                             a.Element("wikipediaUrl").
                                                                                             Value,
                                                                                         ImageUrl =
                                                                                             a.Element("thumbnailImg").
                                                                                             Value
                                                                                     };
                                                          double lat = Convert.ToDouble(a.Element("lat").Value,
                                                                                        CultureInfo.InvariantCulture);
                                                          double lon = Convert.ToDouble(a.Element("lng").Value,
                                                                                        CultureInfo.InvariantCulture);

                                                          wsr.Position =
                                                              (MapPoint) _wm.FromGeographic(new MapPoint(lon, lat));

                                                          Results.Add(wsr);
                                                          if (wsr.ImageUrl!="")
                                                          {
                                                              Graphic g = new Graphic();
                                                              
                                                              g.Symbol = new PictureMarkerSymbol()
                                                                             {
                                                                                 Source =
                                                                                     new BitmapImage(
                                                                                     new Uri(wsr.ImageUrl)),
                                                                                 Width = 30,
                                                                                 OffsetX = 15,
                                                                                 OffsetY = 15
                                                                             };
                                                              g.Geometry = wsr.Position;
                                                              lWiki.Graphics.Add(g);
                                                          }
                                                      }
                                                  }
                                              };
            wc.DownloadStringAsync(new Uri(@"http://api.geonames.org/wikipediaSearch?q=" + Keyword + @"&maxRows=100&username=damylen"));

        }

        public void Remove()
        {

        }

        private string _name = "Wikipedia Search";

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
