using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using BaseWPFHelpers;
using Caliburn.Micro;
using csCommon.csMapCustomControls.PivotControl;
using csCommon.Plugins.SearchPlugin.APIs;
using csShared;
using csShared.Geo;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Projection;
using Microsoft.Surface.Presentation.Controls;

namespace csCommon.MapPlugins.Search
{
    [Export(typeof (IFindLocation))]
    public class SearchViewModel : Screen, IFindLocation
    {
        private string keyword = string.Empty;
        private BindableCollection<GeoCodeSearchResult> result = new BindableCollection<GeoCodeSearchResult>();
        private string selectedLocation;
        private BindableCollection<PivotItem> sections;

        public SearchViewModel()
        {
            // Default caption
            Caption = "Find";
        }        

        public BindableCollection<PivotItem> Sections
        {
            get { return sections; }
            set
            {
                sections = value;
                NotifyOfPropertyChange(() => Sections);
            }
        }

        public string SelectedLocation
        {
            get { return selectedLocation; }
            set
            {
                selectedLocation = value;
                NotifyOfPropertyChange(() => SelectedLocation);
            }
        }

        private static AppStateSettings AppState  { get { return AppStateSettings.Instance; } }

        public string Keyword
        {
            get { return keyword; }
            set {
                if (string.Equals(keyword, value, StringComparison.InvariantCultureIgnoreCase)) return;
                var trimmedKeyword = keyword.Trim();
                keyword = value;
                NotifyOfPropertyChange(() => Keyword);
                if (string.Equals(keyword.Trim(), trimmedKeyword, StringComparison.InvariantCultureIgnoreCase)) return;
                OnTextChanged();
            }
        }

        private void OnTextChanged() {
            var handler = TextChanged;
            if (handler != null) handler(this, null);
        }

        public string Caption { get; set; }

        public BindableCollection<GeoCodeSearchResult> Result
        {
            get { return result; }
            set
            {
                result = value;
                NotifyOfPropertyChange(() => Result);
            }
        }

        public SearchPlugin Plugin { get; set; } 

        public ISearchApi ActiveSearch { get; set; }

        private SearchView sv;
        
        public event EventHandler<SearchEventArgs> TextChanged;
        
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            sv = (SearchView)view;
            Init();
        }

        private void Init()
        {
            AppState.SearchApis.Add(new GoogleSearch   { Plugin = Plugin });
            AppState.SearchApis.Add(new WikiSearch     { Plugin = Plugin });
            AppState.SearchApis.Add(new PlacesSearch   { Plugin = Plugin });
            AppState.SearchApis.Add(new GeoNamesSearch { Plugin = Plugin });
            AppState.SearchApis.Add(new BagSearch      { Plugin = Plugin });
            
            sv.Sections.SelectionChanged += Sections_SelectionChanged;
            UpdateSections();

            var svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(sv, typeof(ScatterViewItem));
            var fe = (FloatingElement)svi.DataContext;

            fe.DockedChanged += (e, f) =>
            {
                if (fe.Large)
                {
                    sv.Keyword.Focus();
                    if (Plugin.SearchService!=null && Plugin.SearchService.Layer is Layer)((Layer)Plugin.SearchService.Layer).Visible = true;
                }
                else
                {
                    ClearService();
                    sv.Keyword.Text = "";
                    if (Plugin.SearchService != null && Plugin.SearchService.Layer is Layer) ((Layer)Plugin.SearchService.Layer).Visible = false;
                }
            };

            var eventAsObservable = Observable.FromEventPattern<SearchEventArgs>(ev => TextChanged += ev, ev => TextChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(k => DoSearch());
            DoSearch();

            AppState.IsOnlineChanged -= AppState_IsOnlineChanged;
            AppState.IsOnlineChanged += AppState_IsOnlineChanged;
        }

        void AppState_IsOnlineChanged(object sender, EventArgs e)
        {
            UpdateSections();
        }

        void Sections_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var pivotItem = sv.Sections.SelectedItem as PivotItem;
            if (pivotItem == null) return;
            var pi = pivotItem;
            var model = pi.Content as SearchResultViewModel;
            if (model == null) return;
            var rvm = model;
            if (ActiveSearch == rvm.SearchApi) return;
            ActiveSearch = rvm.SearchApi;
            DoSearch();
        }

        public void UpdateSections()
        {
            var si = string.Empty;
            if (sv.Sections.SelectedItem != null) si = ((PivotItem)sv.Sections.SelectedItem).Header.ToString();
            sv.Sections.Items.Clear();
            foreach (var p in AppState.SearchApis)
            {
                if (AppState.IsOnline || !p.IsOnline)
                {
                    sv.Sections.Items.Add(new PivotItem()
                    {
                        Header = p.Title,
                        Content = new SearchResultViewModel(p)
                    });
                }
            }
            
            if (string.IsNullOrEmpty(si))
            {
                sv.Sections.SelectedIndex = 0;
            }
            else if (sv.Sections.Items.Count>0)
            {
                var tbs = sv.Sections.Items[0] as PivotItem;
                foreach (PivotItem i in sv.Sections.Items)
                {
                    if (i.Header.ToString() == si) tbs = i;

                }
                sv.Sections.SelectedItem = tbs;
            }
        }

        private BindableCollection<GeoCodeSearchResult> activeResults;

        public BindableCollection<GeoCodeSearchResult> ActiveResults
        {
            get { return activeResults; }
            set { activeResults = value; NotifyOfPropertyChange(()=>ActiveResults); }
        }

        private void DoSearch() {
            Execute.OnUIThread(() => {
                ClearService();

                if (ActiveSearch == null || string.IsNullOrEmpty(Keyword)) return;

                ActiveSearch.Key       = Keyword;
                ActiveSearch.Extent    = AppState.ViewDef.MapControl.Extent;
                ActiveSearch.StartTime = AppState.TimelineManager.Start;
                ActiveSearch.EndTime   = AppState.TimelineManager.End;
                                         Plugin.SearchService.SaveName = ActiveSearch.Title + " " + Keyword;
                ActiveSearch.DoSearch(Plugin);
            });
        }

        private void ClearService()
        {
            lock (Plugin.ServiceLock)
            {
                while (Plugin.SearchService.PoITypes.Any()) Plugin.SearchService.PoITypes.RemoveAt(0);
                while (Plugin.SearchService.PoIs.Any()) Plugin.SearchService.PoIs.RemoveAt(0);
            }
        }   

        public void Find(PoI item)
        {
            //var c = new Point(item.Location.Longitude, item.Location.Latitude);
            //double dis = Distance(item.Box.North, item.Box.East, item.Box.North, itemwm.Box.West, 'K');
            //var wm = new WebMercator();
            
           // var e = (Envelope) wm.FromGeographic(new Envelope(item.Box.West, item.Box.North, item.Box.East, item.Box.South));
            //var e = new Envelope(item.Box.West, item.Box.North, item.Box.East, item.Box.South);
            //e.SpatialReference = new SpatialReference(4326);

            AppState.ViewDef.NextEffect = true;
            //AppState.ViewDef.MapControl.Extent = e;
            SelectedLocation = item.Name;
            item.Service.TriggerPing(item);
            
            AppState.ViewDef.ZoomTo(new KmlPoint(item.Position.Longitude, item.Position.Latitude), 1000);
        }

        public void SetKeywordFocus(TouchEventArgs e)
        {
            var textBox = (TextBox) e.Source;
            textBox.Focus();
        }

        public abstract class SearchEventArgs : EventArgs
        {
        }
    }
}