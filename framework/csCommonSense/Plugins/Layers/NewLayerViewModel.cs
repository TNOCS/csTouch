using System;
using System.Net;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon;
using csShared;
using csShared.Geo;
using System.ComponentModel.Composition;

namespace csGeoLayers.Plugins.Layers
{
    public interface INewLayerSelection
    {
    }

    [Export(typeof(INewLayerSelection))]
    public class NewLayerViewModel : Screen, INewLayerSelection
    {

        
        public Brush AccentBrush
        {
            get { return state.AccentBrush; }
            set { }
        }

        private string _path;

        public string Path
        {
            get { return _path; }
            set { _path = value; NotifyOfPropertyChange(()=>Path); }
        }
        

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }            
        }

        //private LayersView _view;

        public FloatingElement Element { get; set; }


        
        public NewLayerViewModel()
        {
        
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

        }

        private string _onlineSearchKeyword;

        public string OnlineSearchKeyword
        {
            get { return _onlineSearchKeyword; }
            set { _onlineSearchKeyword = value; NotifyOfPropertyChange(()=>OnlineSearchKeyword); }
        }

        public void OnlineSearch()
        {
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += wc_DownloadStringCompleted;
            Result.Clear();
            wc.DownloadStringAsync(new Uri("http://www.arcgis.com/sharing/search?q=" + OnlineSearchKeyword + "%20AND%20(type:%22Map%20Service%22%20OR%20type:%22Image%20Service%22%20OR%20type:%22Feature%20Service%22%20OR%20type:%22WMS%22%20OR%20type:%22KML%22)&f=json&start=1&num=12"));
        }

        private BindableCollection<StoredLayer> _result = new BindableCollection<StoredLayer>();

        public BindableCollection<StoredLayer> Result
        {
            get { return _result; }
            set { _result = value; NotifyOfPropertyChange(()=>Result); }
        }
        

        void wc_DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null) return;
            Newtonsoft.Json.Linq.JObject jObject = Newtonsoft.Json.Linq.JObject.Parse(e.Result);
            foreach (var r in jObject["results"])
            {
                if ((string)r["type"] == "WMS")
                {
                    StoredLayer sl = new StoredLayer();
                    sl.Type = "wms";
                    sl.Path = this.Path;
                    sl.Id = (string) r["url"];
                    sl.Title = (string) r["title"];

                    //wl.Layers = new string[] { "inspireadressen" };
                    //AppState.ViewDef.Layers.ChildLayers.Add(wl);
                    //wl.Initialize();
                    Result.Add(sl);


                }
                
                if ((string)r["type"]=="Feature Service")
                {
                    StoredLayer sl = new StoredLayer();
                    sl.Type = "Feature Service";
                    sl.Path = this.Path;
                    sl.Id = (string)r["url"];
                    sl.Title = (string)r["title"];

                    //wl.Layers = new string[] { "inspireadressen" };
                    //AppState.ViewDef.Layers.ChildLayers.Add(wl);
                    //wl.Initialize();
                    Result.Add(sl);
                }

                if ((string)r["type"] == "Map Service")
                {
                    StoredLayer sl = new StoredLayer();
                    sl.Type = "Map Service";
                    sl.Path = this.Path;
                    sl.Id = (string)r["url"];
                    sl.Title = (string)r["title"];

                    //wl.Layers = new string[] { "inspireadressen" };
                    //AppState.ViewDef.Layers.ChildLayers.Add(wl);
                    //wl.Initialize();
                    //Result.Add(sl);
                }

                //if ((string)r["type"] == "Feature Service")
                //{
                //    FeatureLayer fl = new FeatureLayer() { };
                //    fl.FeatureSymbol = new SimpleMarkerSymbol() { Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle, Color = Brushes.Green, Size = 30 };
                //    fl.Url = (string)r["item"];
                //    fl.Initialized += (s, es) =>
                //    {

                //    };
                //    AppState.ViewDef.Layers.ChildLayers.Add(fl);
                //    fl.Initialize();
                //}

                //if ((string)r["type"] == "Map Service")
                //{

                //    ArcGISDynamicMapServiceLayer ml = new ArcGISDynamicMapServiceLayer();
                //    ml.Url = (string)r["item"];
                //    ml.ID = (string)r["title"];
                //    AppState.ViewDef.Layers.ChildLayers.Add(ml);
                //    ml.Initialize();


                //}
            }

        }

        public AppStateSettings state { get { return AppStateSettings.Instance; }}

       
 

}
}
