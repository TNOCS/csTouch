using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Interfaces;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.MapPlugins.EsriMap
{
    [Export(typeof (ISettingsScreen))]
    public class EsriMapSettingsViewModel : Screen, ISettingsScreen
    {
        private DispatcherTimer cacheTimer;
        private Envelope cacheE;
        private int cacheLevel = 2;

        private int cacheX;
        private int cacheY;
        private string completed;
        private int count;

        private Map map;

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public string Completed
        {
            get { return completed; }
            set
            {
                completed = value;
                NotifyOfPropertyChange(() => Completed);
            }
        }

        public string Name
        {
            get { return "EsriMap"; }
        }

        public void Save()
        {
        }

        private MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement   = fe,
                RelativePosition  = new Point(35, -5),
                TimeOut           = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty   = string.Empty,
                AutoClose         = true
            };

            menu.AddMenuItem("<none>");
            foreach (var wtl in AppState.ViewDef.BaseLayerProviders)
            {
                menu.AddMenuItem(wtl.Title);
            }

            return menu;
        }

        public void SetShortcut1(FrameworkElement obj)
        {
            var m = GetMenu(obj);
            m.Selected += (e, s) =>
            {
                AppState.MapShortcut1 =  (s.Object.ToString() == "<none>") ? "" : s.Object.ToString();                
            };
            AppState.Popups.Add(m);
        }

        public void SetShortcut2(FrameworkElement obj)
        {
            var m = GetMenu(obj);
            m.Selected += (e, s) =>
            {
                AppState.MapShortcut2 = (s.Object.ToString() == "<none>") ? "" : s.Object.ToString();
            };
            AppState.Popups.Add(m);
        }

        public void ClearCache()
        {
            AppState.ViewDef.ClearMapCache();
        }

        public void Offline()
        {
            AppState.TriggerNotification("Creating cache");
            map = AppState.ViewDef.MapControl;
            map.IsHitTestVisible = false;

            cacheE = map.Extent.Clone();
            cacheX = 0;
            cacheY = 0;
            cacheLevel = 2;
            count = 0;

            cacheTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 0, 4)};
            cacheTimer.Tick += CacheTimer_Tick;
            cacheTimer.Start();
        }

        private void CacheTimer_Tick(object sender, EventArgs es)
        {
            count += 1;
            Completed = ((count/((2.0*2) + (4*4) + (8*8)))*100).ToString("###") + "% completed";
            var difx = (cacheE.XMax - cacheE.XMin)/cacheLevel;
            var dify = (cacheE.YMax - cacheE.YMin)/cacheLevel;
            map.Extent = new Envelope(cacheE.XMin + (difx*cacheX),
                cacheE.YMin + (dify*cacheY),
                cacheE.XMin + (difx*(cacheX + 1)),
                cacheE.YMin + (dify*(cacheY + 1)));
            cacheX += 1;
            if (cacheX != cacheLevel) return;
            cacheY += 1;
            cacheX = 0;
            if (cacheY != cacheLevel) return;
            cacheLevel = cacheLevel*2;
            cacheX = 0;
            cacheY = 0;

            if (cacheLevel != 16) return;
            cacheTimer.Stop();
            Completed = "";
            map.IsHitTestVisible = true;
            AppState.TriggerNotification("Creating cache finished");
        }

    }
}