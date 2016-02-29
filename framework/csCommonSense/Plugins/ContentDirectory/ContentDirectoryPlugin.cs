using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using csGeoLayers.Content.Panoramio;
using csGeoLayers.Content.RainRadar;
using csGeoLayers.GeoRSS;
using csShared;
using csShared.FloatingElements;
using csShared.Interfaces;

namespace csGeoLayers.Plugins.ContentDirectory
{
    public interface IContentDirectory
    {
        IPlugin Plugin { get; set; }
    }

    [Export(typeof (IPlugin))]
    public class LayerDirectoryPlugin : PropertyChangedBase, IPlugin
    {
        private bool hideFromSettings;
        private bool isRunning;
        private MenuItem menuItem;
        private IPluginScreen screen;
        private ISettingsScreen settings;
        public FloatingElement Element { get; set; }

        #region IPlugin Members

        public bool CanStop
        {
            get { return true; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 5; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/mapcontent.png"; }
        }

        public AppStateSettings AppState { get; set; }


        public string Name
        {
            get { return "ContentDirectory"; }
        }

        public void Init()
        {
            //AppState.ViewDef.Content.Add(new FlightTrackerContent {Name = "Flight Tracker Netherlands"});
            AppState.ViewDef.Content.Add(new PanoramioContent {Name = "Panoramio"});
             AppState.ViewDef.Content.Add(new GeoRSSContent
                                             {
                                                 Name = "Earthquakes Past Hour (M1+)",
                                                 Location =
                                                     new Uri(
                                                     "http://earthquake.usgs.gov/earthquakes/catalogs/eqs1hour-M1.xml"),
                                                 Folder = "Disaster/Earthquakes",
                                                 IconUri =
                                                     new Uri(
                                                     @"http://maps.google.com/mapfiles/kml/shapes/earthquake.png")
                                             });
            AppState.ViewDef.Content.Add(new GeoRSSContent
                                             {
                                                 Name = "Earthquakes Past Day (M1+)",
                                                 Location =
                                                     new Uri(
                                                     "http://earthquake.usgs.gov/earthquakes/catalogs/eqs1day-M1.xml"),
                                                 Folder = "Disaster/Earthquakes",
                                                 IconUri =
                                                     new Uri(
                                                     @"http://maps.google.com/mapfiles/kml/shapes/earthquake.png")
                                             });
            AppState.ViewDef.Content.Add(new GeoRSSContent
                                             {
                                                 Name = "Earthquakes 7 Days (M5+)",
                                                 Location =
                                                     new Uri(
                                                     "http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M5.xml"),
                                                 Folder = "Disaster/Earthquakes",
                                                 IconUri =
                                                     new Uri(
                                                     @"http://maps.google.com/mapfiles/kml/shapes/earthquake.png")
                                             });
            AppState.ViewDef.Content.Add(new GeoRSSContent
                                             {
                                                 Name = "Reuters News",
                                                 Location =
                                                     new Uri(
                                                     "http://ws.geonames.org/rssToGeoRSS?feedUrl=http://feeds.reuters.com/reuters/worldNews"),
                                                 Folder = "News",
                                                 IconUri =
                                                     new Uri(@"http://www.borealbirds.org/images/icon-reuters-logo.gif"),
                                                 IconSize = 32
                                             });

            AppState.ViewDef.Content.Add(new WmsContent()
            {
                Name="BAG Data",
                Location = "http://geodata.nationaalgeoregister.nl/bagviewer/wms?",
                Layers = new[] { "pand", "ligplaats", "standplaats", "verblijfsobject"}

            });

            AppState.ViewDef.Content.Add(new WmsContent()
            {
                Name="Vaarwegen",
                Location = "http://www.vaarweginformatie.nl/wfswms/services?",
                Layers = new[] { "Bridge", "Fairway" }
            });
            
            AppState.ViewDef.Content.Add(new KmlContent
                                             {
                                                 Name = "Global Disaster Alert and Coordination system",
                                                 Location = new Uri("http://www.gdacs.org/xml/gdacs.kml"),
                                                 Folder = "Disaster"
                                             });

           

            AppState.ViewDef.Content.Add(new ArcgisOnlineContentPlugin {Name = "ArcGis"});

           
            AppState.ViewDef.Content.Add(new FlexibleRainRadarContent
                                             {
                                                 Name = "RainRadar EU",
                                                 Config =
                                                     new Dictionary<string, string>
                                                         {
                                                             {"interval", "15"},
                                                             {"tlLat", "-14.9515"},
                                                             {"tlLon", "41.4389"},
                                                             {"brLat", "20.4106"},
                                                             {"brLon", "59.9934"},
                                                             {
                                                                 "baseUrl",
                                                                 "http://134.221.210.43:8000/BuienRadarService/RainImage/eu/warped/"
                                                             }
                                                         }
                                             });
            
            
            
            #region old


            //AppState.ViewDef.Content.Add(new AirportContent() { Name = "Airports Worldwide" });
            //AppState.ViewDef.Content.Add(new GeoRSSContent() { Name = "C2000 masten", Location = new Uri("file://" + Directory.GetCurrentDirectory() + @"\Content\Data\c2000.rss"), Folder = "Disaster/Earthquakes", IconUri = new Uri(@"http://maps.google.com/mapfiles/kml/shapes/earthquake.png") });
          

            //AppState.ViewDef.Content.Add(new KmlContent()
            //                                 {
            //                                     Name="Tno",
            //                                     Location = new Uri("http://nationaalgeoregister.nl/geonetwork/srv/nl/google.kml?uuid=f646dfb9-5bf6-eab9-042b-cab6ff2dc275&layers=M11M0561"),
            //                                     Folder = "Ondergrond"
            //                                 });
            //AppState.ViewDef.Content.Add(new WmsContent()
            //                                 {
            //                                     //http://geoservices.cbs.nl/arcgis/services/BestandBodemGebruik2008/MapServer/WMSServer
            //                                     Name = "Bodemgebruik 2008",
            //                                     Location =
            //                                         "http://mesonet.agron.iastate.edu/cgi-bin/wms/nexrad/n0r.cgi",
            //                                     Folder = "Bodemgebruik",

            //                                 });

            //AppState.ViewDef.Content.Add(new NO2Content());

            //AppState.ViewDef.Content.Add(new FlexibleRainRadarContent()
            //{
            //    Name = "RainRadar NL",
            //    Config = new Dictionary<string, string> { { "interval", "5" }, { "tlLat", "0" }, { "tlLon", "55.974" }, { "brLat", "10.856" }, { "brLon", "48.895" }, { "baseUrl", "http://134.221.210.43:8000/BuienRadarService/RainImage/nl/warped/" } }
            //});

            //AppState.ViewDef.Content.Add(new TwitterContent()
            //                                 {
            //                                     Name = "Twitter"
            //                                 });
            #endregion
        }


        public void Start()
        {
            IsRunning = true;
            menuItem = new MenuItem();
            menuItem.Clicked += MenuItemClicked;
            menuItem.Name = "Add Content";
            AppState.MainMenuItems.Add(menuItem);
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Stop()
        {
            menuItem.Clicked -= MenuItemClicked;
            AppState.MainMenuItems.Remove(menuItem);
            IsRunning = false;
        }

        #endregion

        private void MenuItemClicked(object sender, EventArgs e)
        {
            object viewModel = IoC.GetInstance(typeof (IContentDirectory), null);
            if (viewModel != null)
            {
                Element = FloatingHelpers.CreateFloatingElement("Content Plugin", new Point(400, 400),
                                                                new Size(550, 475), viewModel);
                AppState.FloatingItems.AddFloatingElement(Element);
            }
        }
    }
}