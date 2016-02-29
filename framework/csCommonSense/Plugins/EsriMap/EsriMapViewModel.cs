using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using IMB3;
using csShared;
using csShared.Interfaces;

namespace csCommon.MapPlugins.EsriMap
{
    [Export(typeof(IPluginScreen))]
    public class EsriMapViewModel : Screen, IPluginScreen
    {
        private EsriMapView map;

        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        private TEventEntry mapState;

        private Point geoPoint;

        /// <summary>
        /// Geo Point for ping animation
        /// </summary>
        public Point GeoPoint
        {
            get { return geoPoint; }
            set { geoPoint = value; NotifyOfPropertyChange(()=>GeoPoint); }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            map = (EsriMapView)view;
                       
            if (AppState.Imb != null && AppState.Imb.Imb!=null && AppState.Imb.IsConnected)
            {
                InitImbEvents();                
            }
            else
            {
                if (AppState.Imb != null) AppState.Imb.Connected += Imb_Connected;
            }

            //remove esri logo
            map.emMain.IsLogoVisible = false;

            // make sure we can zoom in
            map.emMain.MinimumResolution = 0.05;
            SetScaleLineOffset();
            AppState.ViewDef.GeoPointerAdded += ViewDef_GeoPointerAdded;
            AppState.PropertyChanged += AppStateOnPropertyChanged;
            AppState.TriggerMapStarted();
        }

        private void AppStateOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.Equals(e.PropertyName, "TimelineManager")) return;
            AppState.PropertyChanged -= AppStateOnPropertyChanged;
            AppState.TimelineManager.PropertyChanged += (s, args) =>
            {
                if (!string.Equals(args.PropertyName, "Visible")) return;
                SetScaleLineOffset();
            };
        }

        private void SetScaleLineOffset()
        {
            ScaleLineOffset = AppState.TimelineManager != null && AppState.TimelineManager.Visible
                ? new Thickness(10, 10, 10, 10 + AppState.Config.GetDouble("Timeline.Height", 10))
                : new Thickness(10);
        }

        private bool geoPointerVisible;
        private Thickness scaleLineOffset;

        public bool GeoPointerVisible
        {
            get { return geoPointerVisible; }
            set { geoPointerVisible = value; NotifyOfPropertyChange(()=>GeoPointerVisible); }
        }
        
        void ViewDef_GeoPointerAdded(object sender, csShared.Geo.GeoPointerArgs e)
        {
            GeoPoint = map.emMain.MapToScreen(e.Position, true);
            GeoPointerVisible = true;
            var tm = new DispatcherTimer();
            tm.Tick += delegate
            {
                GeoPointerVisible = false;
                tm.Stop();
            };
            tm.Interval = e.Duration;
            tm.Start();
        }

        public void InitImbEvents()
        {
            mapState = AppState.Imb.Imb.Subscribe(AppState.Imb.Id + ".mapstate");

        }

        void Imb_Connected(object sender, System.EventArgs e)
        {
            InitImbEvents();
        }
        
        public string Name
        {
            get { return "EsriMap"; }
        }

        public Thickness ScaleLineOffset
        {
            get { return scaleLineOffset; }
            set
            {
                if (Equals(value, scaleLineOffset)) return;
                scaleLineOffset = value;
                NotifyOfPropertyChange(() => ScaleLineOffset);
            }
        }
    }
}
