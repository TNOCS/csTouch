using Caliburn.Micro;
using csDataServerPlugin.ViewModels;
using csShared;
using csShared.Utils;
using DataServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;

namespace csDataServerPlugin
{
    public interface IContentList
    {
    }

    [Export(typeof(IContentList))]
    public class ContentListViewModel : Screen
    {
        private BindableCollection<PoI> allPois = new BindableCollection<PoI>();
        private AnalysisTabViewModel analysisTab;
        private int selectedTab = 1;
        private PoiService service;
        private ServiceTimelineViewModel serviceTimeline;
        private bool showFilter;
        private bool showTasks;
        private bool showTimeline;
        private StylesTabViewModel stylesTab;
        public DataServerBase Dsb { get; set; }

        private BackupTabViewModel backupTab;

        public BackupTabViewModel BackupTab
        {
            get { return backupTab; }
            set { backupTab = value; NotifyOfPropertyChange(() => BackupTab); }
        }

        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public PoiService Service
        {
            get { return service; }
            set
            {
                service = value;
                NotifyOfPropertyChange(() => Service);
            }
        }

        public string FilterText
        {
            get { return Service.SearchFilter; }
            set
            {
                Service.SearchFilter = value;
                NotifyOfPropertyChange(() => FilterText);
                if (SearchChanged != null) SearchChanged(this, null);
            }
        }

        public ServiceTimelineViewModel ServiceTimeline
        {
            get { return serviceTimeline; }
            set
            {
                serviceTimeline = value;
                NotifyOfPropertyChange(() => ServiceTimeline);
            }
        }

        public AnalysisTabViewModel AnalysisTab
        {
            get { return analysisTab; }
            set
            {
                analysisTab = value;
                NotifyOfPropertyChange(() => AnalysisTab);
            }
        }

        public StylesTabViewModel StylesTab
        {
            get { return stylesTab; }
            set
            {
                stylesTab = value;
                NotifyOfPropertyChange(() => StylesTab);
            }
        }

        public BindableCollection<PoI> AllPois
        {
            get { return allPois; }
            set
            {
                allPois = value;
                NotifyOfPropertyChange(() => AllPois);
            }
        }

        public bool ShowTimeline
        {
            get { return showTimeline; }
            set
            {
                showTimeline = value;
                NotifyOfPropertyChange(() => ShowTimeline);
            }
        }

        public bool ShowTasks
        {
            get { return showTasks; }
            set
            {
                showTasks = value;
                NotifyOfPropertyChange(() => ShowTasks);
            }
        }

        public bool ShowFilter
        {
            get { return showFilter; }
            set
            {
                showFilter = value;
                NotifyOfPropertyChange(() => ShowFilter);
            }
        }

        public int SelectedTab
        {
            get { return selectedTab; }
            set
            {
                selectedTab = value;
                NotifyOfPropertyChange(() => selectedTab);
            }
        }

        public event EventHandler<SearchEventArgs> SearchChanged;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            //Service.PoIs.CollectionChanged += PoIs_CollectionChanged;
            ServiceTimeline = new ServiceTimelineViewModel { Service = Service };
            AnalysisTab = new AnalysisTabViewModel { Service = Service };
            StylesTab = new StylesTabViewModel { Service = Service };
            BackupTab = new BackupTabViewModel { Service = Service };

            Service.Initialized += Service_Initialized;

            UpdateTabs();

            var eventAsObservable = Observable.FromEventPattern<SearchEventArgs>(ev => SearchChanged += ev,
                ev => SearchChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(1000)).Subscribe(_ => UpdateSearch());
        }

        public async void UpdateSearch()
        {
            await Service.DoSearch();
        }

        private void UpdateTabs()
        {
            if (Service == null || Service.Settings == null) return;
            ShowTimeline = Service.Settings.ShowTimeline;
            ShowFilter = Service.Settings.ShowAnalysis;
            // Service.PoITypes.Any(k => k.Style != null && k.Style.Analysis != null && k.Style.Analysis.Highlights != null && k.Style.Analysis.Highlights.Any());
            ShowFilter = true; // Service.Settings.ShowAnalysis; // Service.PoITypes.Any(k => k.Style != null && k.Style.Analysis != null && k.Style.Analysis.Highlights != null && k.Style.Analysis.Highlights.Any());

            //SelectedTab = 1; //(ShowFilter) ? 5 : 1;
        }

        public void UpdateFilterMap()
        {
            //Service.TriggerRefreshAllPois();
        }

        private void Service_Initialized(object sender, EventArgs e)
        {
            UpdateTabs();
        }

        //private bool CustomerFilter(object item)
        //{
        //    var p = item as BaseContent;
        //    return !p.IsDeleted;
        //}

        public void Edit(PoI p)
        {
            try
            {
                if (p.Position != null)
                {
                    AppState.ViewDef.PanAndPoint(new Point(p.Position.Longitude, p.Position.Latitude));
                }
                else if (p.Data.ContainsKey("graphic"))
                {
                    var pg = (PoiGraphic)p.Data["graphic"];
                    if (pg == null || pg.Geometry == null) return;
                    AppState.ViewDef.MapControl.Extent = pg.Geometry.Extent;
                }
                p.Select();
            }
            catch (Exception ex)
            {
                Logger.Log("ContentList", "Error editing poi", ex.Message, Logger.Level.Error);
            }
        }

        public void Zoom(PoI p)
        {
            try
            {
                if (p.Position != null)
                {
                    AppState.ViewDef.ZoomAndPoint(new Point(p.Position.Longitude, p.Position.Latitude));
                }
                else if (p.Data.ContainsKey("graphic"))
                {
                    var pg = (PoiGraphic)p.Data["graphic"];
                    if (pg == null || pg.Geometry == null) return;
                    AppState.ViewDef.MapControl.Extent = pg.Geometry.Extent;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("ContentList", "Error zooming in onto poi", ex.Message, Logger.Level.Error);
            }
        }

        public void RemovePoi(PoI dp)
        {
            Service.RemovePoi(dp);
        }

        public void UpdatePois()
        {
            AllPois.Clear();
            foreach (var p in from PoiService s in Dsb.Services from p in s.PoIs.Cast<PoI>() select p) AllPois.Add(p);
        }

        public class SearchEventArgs : EventArgs
        {
        }
    }

    public class PoiCallOutViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new PoiCallOutViewModel { Poi = (BaseContent)value };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}