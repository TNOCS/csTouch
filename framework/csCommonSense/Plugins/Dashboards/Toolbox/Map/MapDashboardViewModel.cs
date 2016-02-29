using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BaseWPFHelpers;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using csCommon.Utils.Collections;
using csEvents.Sensors;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using DataServer;
using Microsoft.Surface.Presentation.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Zandmotor.Controls.Plot;

namespace csCommon.Plugins.Dashboards.Toolbox.Map
{
    [Export(typeof (Screen))]
    public class MapDashboardViewModel : Screen, IDashboardItemViewModel
    {
        public override string DisplayName { get; set; }

        public DataServerBase DataServer { get; set; }

        private IScreen configScreen;

        public IScreen ConfigScreen
        {
            get { return configScreen; }
            set
            {
                configScreen = value;
                NotifyOfPropertyChange(() => ConfigScreen);
            }
        }

        private DashboardItem item;

        public DashboardItem Item
        {
            get { return item; }
            set { item = value; NotifyOfPropertyChange(()=>Item); }
        }
        

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        private ScatterViewItem svi;
        private MapDashboardView mdv;
      
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            mdv = (MapDashboardView)view;
            svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(mdv, typeof(ScatterViewItem));
            
            //if (_fe.Style == null) _fe.Style = this.FindResource("DefaultContainerStyle") as Style;
            
            svi.ContainerManipulationDelta += (e, f) =>
            {
                UpdateMap();
            };
            UpdateMap();

        }

        private void UpdateMap()
        {
            
           
            var w = Application.Current.MainWindow;
            var p1 = mdv.TranslatePoint(new Point(0, 0), w);
            var p2 = mdv.TranslatePoint(new Point(mdv.ActualWidth, mdv.ActualHeight), w);
            AppState.ViewDef.MapControl.Margin = new Thickness(p1.X, p1.Y, w.ActualWidth - p2.X, w.ActualHeight - p2.Y);
            //svi.IsHitTestVisible = false;
        }

        private string config;

        public string Config
        {
            get
            {
                return config;
                
            }
            set { config = value; NotifyOfPropertyChange(()=>Config); }
        }
    }

}
