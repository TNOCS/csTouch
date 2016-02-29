using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using csCommon.Plugins.Dashboards;
using csCommon.Utils.Collections;
using csEvents.Sensors;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using DataServer;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Zandmotor.Controls.Plot;

namespace csCommon.Plugins.DashboardPlugin
{
    [Export(typeof (Screen))]
    public class DashboardItemViewModel : Screen, IDashboardItemViewModel
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
        
        public ItemConfigViewModel ChartConfig { get { return (ItemConfigViewModel)ConfigScreen; } }

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public void FindDataset()
        {
            if (Data != null) return;
            foreach (var sc in AppState.SensorSets)
            {
                var ds = sc.FirstOrDefault(k => k.DataSetId == Item.DataSetId);
                if (ds == null) continue;
                Item.Data = ds;
                Data = ds;
                Item.Data.Updated += Data_Updated;
                return;
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            FindDataset();
            // if (Poi == null) return;
            ConfigScreen = new ItemConfigViewModel {Item = Item};
            ChartConfig.ConfigChanged += (e, f) => UpdateGraph();
            if (Item.Data!=null) Item.Data.Updated += Data_Updated;
            UpdateGraph();
            AppState.TimelineManager.FocusTimeThrottled += (e, f) => UpdateGraph();
            if (item.Data!=null)
                item.Data.Updated += (e, f) => UpdateGraph();
            // AppState.TimelineManager.TimeContentChanged += (e, f) => UpdateGraph();
        }

        void Data_Updated(object sender, EventArgs e)
        {
            UpdateGraph();
        }

        private PlotModel model = new PlotModel();

        public PlotModel Model
        {
            get { return model; }
            set
            {
                model = value;
                NotifyOfPropertyChange();
            }
        }

        private bool chartVisible;

        public bool ChartVisible
        {
            get { return chartVisible; }
            set
            {
                chartVisible = value;
                NotifyOfPropertyChange(() => ChartVisible);
            }
        }

        private bool focusValueVisible;

        public bool FocusValueVisible
        {
            get { return focusValueVisible; }
            set
            {
                focusValueVisible = value;
                NotifyOfPropertyChange(() => FocusValueVisible);
            }
        }

        private string focusValue;

        public string FocusValue
        {
            get { return focusValue; }
            set { focusValue = value; NotifyOfPropertyChange(()=>FocusValue); }
        }

        private string vizType;

        public string VizType
        {
            get { return vizType; }
            set { vizType = value; NotifyOfPropertyChange(()=>VizType); }
        }

        private void UpdateGraph()
        {
            Execute.OnUIThread(() =>
            {
                if (item == null) return; 
                VizType = item.Config;

                if (Data != null)
                {
                    //Data.SetFocusDate(AppState.TimelineManager.FocusTime);
                    FocusValue = Data.FocusValue.ToString(CultureInfo.InvariantCulture);
                }
                if (VizType == "Bar Chart") Model = InitModel();
            });
        }

        private DataSet data;

        public DataSet Data
        {
            get { return data; }
            set { data = value; NotifyOfPropertyChange(()=>Data); }
        }

        private PlotModel InitModel()
        {
            if (Data == null) return new PlotModel();
            Data.Grouping = GroupingOptions.hourly;
            var plotModel = new PlotModel {PlotAreaBorderThickness = 0, Background = null, PlotMargins = new OxyThickness(0, 0, 0, 0), Title = Data.Title};
            var dateAxis = new DateTimeAxis {Minimum = DateTimeAxis.ToDouble(AppState.TimelineManager.Start), Maximum = DateTimeAxis.ToDouble(AppState.TimelineManager.End), IsAxisVisible = true, Position = AxisPosition.Bottom};
            plotModel.Axes.Add(dateAxis);
            var linearAxis2 = new LinearAxis {IsAxisVisible = true};

            var maxvalue = (Data.Data.Values.Any()) ? Data.Data.Values.Max() :  10.0;
            linearAxis2.Maximum = maxvalue;
            //linearAxis2.Maximum = 5;
            linearAxis2.Minimum = 0;
            linearAxis2.MinorStep = 1;
            plotModel.Axes.Add(linearAxis2);

            CreateBarSeries(linearAxis2, plotModel);

            return plotModel;
        }

        private DataSet CreateDataSet()
        {
            var dataSet = new DataSet
            {
                Data = new ConcurrentObservableSortedDictionary<DateTime, double>(), GroupingOperation = GroupingOperations.count
            };
            //var d = new Dictionary<double,int>();
            //foreach (var fs in Poi.InFlow())
            //{
            //    var date = fs.Eta;

            //    if (data.Data.ContainsKey(date))
            //    {
            //        data.Data[date] += 1;
            //    }
            //    else
            //    {
            //        data.Data[date] = 1;
            //    }
            //}

            return dataSet;
        }

        private void CreateBarSeries(LinearAxis linearAxis2, PlotModel plotModel)
        {
            var barSeries = new RectangleBarSeries {StrokeThickness = 0};

            var dif = GetDiff(Data.Grouping);
            foreach (var p in Data.GetGroupedDataSet()[0])
            {
                var r = new RectangleBarItem(p.Key, 0, p.Key + dif, p.Value, color: OxyColors.Blue);
                barSeries.Items.Add(r);
            }
            plotModel.Series.Add(barSeries);
        }


        private static double GetDiff(GroupingOptions go)
        {
            switch (go)
            {
                case GroupingOptions.daily:
                    return DateTimeAxis.ToDouble(DateTime.Now) - DateTimeAxis.ToDouble(DateTime.Now.AddDays(1));
                case GroupingOptions.hourly:
                    return DateTimeAxis.ToDouble(DateTime.Now) - DateTimeAxis.ToDouble(DateTime.Now.AddHours(1));
                case GroupingOptions.quarterly:
                    return DateTimeAxis.ToDouble(DateTime.Now) - DateTimeAxis.ToDouble(DateTime.Now.AddMinutes(15));
            }
            return DateTimeAxis.ToDouble(DateTime.Now) - DateTimeAxis.ToDouble(DateTime.Now.AddMinutes(15));
        }

        private PoI poI;

        public PoI Poi
        {
            get { return poI; }
            set
            {
                poI = value;
                NotifyOfPropertyChange(() => Poi);
            }
        }

        //private string selectedColor = "Black";

        private bool canEdit;

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        private string config;

        public string Config
        {
            get { return config; }
            set { config = value; NotifyOfPropertyChange(()=>Config); }
        }
    }

    public class Selector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;
            var frameworkElement = container as FrameworkElement;
            if (frameworkElement == null) return null;
            var template = frameworkElement.FindResource(item) as DataTemplate;
            return template;
        }
    }
}
