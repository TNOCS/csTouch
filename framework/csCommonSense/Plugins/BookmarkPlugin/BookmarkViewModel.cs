using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using csShared;
using System.Windows.Input;
using System.Windows.Controls;
using csShared.Utils;

namespace csBookmarkPlugin
{
    [Export(typeof(IBookmark))]
    public class BookmarkViewModel:Screen
    {
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            Bookmarks = BookmarkList.Load(BFile);
            //PropertyChanged += BookmarkViewModel_PropertyChanged;

            //var dirpart = BFile.Remove(BFile.LastIndexOfAny(new char[] {'\\', '/'}));
            //var filepart = BFile.Remove(0,dirpart.Count()+1);
            //var folder = Directory.GetCurrentDirectory() + "\\" + dirpart;
            //var fw = new FileSystemWatcher(folder) {EnableRaisingEvents = true};
            //fw.Changed += fw_Changed;

            foreach (var d in AppState.DashboardStateStates.Where(k => k.IsPinned))
            {
                AddDashboard(d);
            }

        }

        public void Pin(DashboardState state)
        {
            if (state == null) return;
            if (!state.IsPinned)
            {
                AddDashboard(state);
            }
            else
            {
                UnPin(state);   
            }
        }


        private void AddDashboard(DashboardState d)
        {
            d.IsPinned = true;
            var p = new Pin() { BackgroundBrush = Brushes.Black, Id = d.Guid, Title = d.Title };
            p.DoUnpin = UnPinDashboard;
            p.Clicked += (f, e) => { AppState.DashboardStateStates.GoToDashboard(d); };
            AppState.Pins.Add(p);
            AppState.DashboardStateStates.Save();
        }

        public void UnPin(DashboardState d)
        {
            var pin = AppState.Pins.FirstOrDefault(k => k.Id == d.Guid);
            if (pin != null)
            {
                AppState.Pins.Remove(pin);
                d.IsPinned = false;
            }
            AppState.DashboardStateStates.Save();
        }

        public void UnPinDashboard(Pin p)
        {
            if (p.Tag is DashboardState)
            {
                UnPin((DashboardState)p.Tag);
            }
        }

        void fw_Changed(object sender, FileSystemEventArgs e)
        {
            Bookmarks = BookmarkList.Load(BFile);
        }

        void BookmarkViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //e. throw new NotImplementedException();
        }

        public AppStateSettings AppState { get { return AppStateSettings.Instance; }}

        private string bfile;

        public string BFile
        {
            get { return bfile; }
            set { bfile = value; NotifyOfPropertyChange(()=>BFile);}
        }

        private string _stbName;

        public string stbName
        {
            get { return _stbName; }
            set { _stbName = value; NotifyOfPropertyChange(()=>stbName);  }
        }

        private string dashboardName;

        public string DashboardName
        {
            get { return dashboardName; }
            set { 
                dashboardName = value; NotifyOfPropertyChange(()=>DashboardName); }
        }
        

        public DashboardStateCollection Dashboards
        {
            get { return AppState.DashboardStateStates; }
        }

        private BookmarkList _bookmarks;

        public BookmarkList Bookmarks
        {
            get { return _bookmarks; }
            set { _bookmarks = value; NotifyOfPropertyChange(()=>Bookmarks); }
        }

        public void RemoveDashboard(DashboardState d)
        {
            if (d.IsPinned) UnPin(d);
            AppState.DashboardStateStates.RemoveDashboard(d);
            NotifyOfPropertyChange(() => Dashboards);
        }

        public void GoToDashboard(DashboardState d)
        {
            AppState.DashboardStateStates.GoToDashboard(d);
        }

        public void AddDashboard()
        {
            AppState.DashboardStateStates.SaveDashboard(DashboardName);
            NotifyOfPropertyChange(()=>Dashboards);
        }
        
        public void GoTo(Bookmark bm)
        {
            try
            {
                var mvd = AppStateSettings.Instance.ViewDef;
                mvd.StartTransition();
                //mvd.ZoomTo(new KmlPoint(bm.Location.X, bm.Location.Y), bm.Resolution);
                //mvd.MapControl.Extent = bm.Extent;
                //mvd.MapControl.ZoomDuration = new TimeSpan(0,0,0,0,500);

                switch (bm.Extent.SpatialReference.WKID)
                {
                    case 4326:
                    {
                        var wm = new WebMercator();
                        var extent = (Envelope) wm.FromGeographic(bm.Extent);
                        mvd.MapControl.ZoomTo(extent);
                        break;
                    }
                    case 102100:
                        mvd.MapControl.ZoomTo(bm.Extent);
                        break;
                }
                Logger.Stat("BookmarkSelected");
            }
            catch
            {

            }
        }

        public void AddBmTextFocus(TouchEventArgs e)
        {
            var txtBox = (TextBox)e.Source;
            txtBox.Focus();
        }

        public void Delete(Bookmark del)
        {
            if (Bookmarks.Contains(del))
            {
                Bookmarks.Remove(del);
            }
            Bookmarks.Save(BFile);
        }

        public void AddPlan2()
        {
            AddPlan();
        }

        public void AddPlan()
        {
            if (Bookmarks == null) return;
            //var p = new Point()
            //{
            //    X=AppStateSettings.Instance.ViewDef.Center.Y,
            //    Y=AppStateSettings.Instance.ViewDef.Center.X
            //};

            Bookmarks.Add(new Bookmark { 
                Id = stbName,                    
                Extent =  AppStateSettings.Instance.ViewDef.MapControl.Extent,
            });
            stbName = "";
            Bookmarks.Save(BFile);
            Logger.Stat("BookmarkAdded");
        }
    }

   

        


        
    
}
