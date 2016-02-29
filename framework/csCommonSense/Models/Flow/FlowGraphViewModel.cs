using System;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using csCommon.Utils.Collections;
using csEvents.Sensors;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using DataServer;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Zandmotor.Controls.Plot;

namespace csModels.Flow
{
  

    [Export(typeof(IScreen))]
    public class FlowGraphViewModel : Screen,  IDashboardItemViewModel
    {
        public override string DisplayName { get; set; }

        public DataServerBase DataServer { get; set; }

        public DashboardItem Item { get; set; }

        private IScreen configScreen;

        public IScreen ConfigScreen
        {
            get { return configScreen; }
            set { configScreen = value; NotifyOfPropertyChange(() => ConfigScreen); }
        }

        public FlowGraphConfigViewModel ChartConfig {get { return (FlowGraphConfigViewModel) ConfigScreen; }}
        
        private FlowCollection nextStops;

        public FlowCollection NextStops
        {
            get { return nextStops; }
            set { nextStops = value; NotifyOfPropertyChange(()=>NextStops); }
        }

        

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public void SelectFlow(FlowStop fs)
        {
            AppState.TriggerNotification("Flow selected");
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
           // if (Poi == null) return;
            ConfigScreen = new FlowGraphConfigViewModel();
            ChartConfig.ConfigChanged += (e, f) =>
            {
                UpdateGraph();
            };
            UpdateGraph();
            AppState.TimelineManager.FocusTimeThrottled += (e, f) => UpdateGraph();
            
            // AppState.TimelineManager.TimeContentChanged += (e, f) => UpdateGraph();

        }

        private PlotModel model = new PlotModel();

        public PlotModel Model
        {
            get { return model; }
            set { model = value; NotifyOfPropertyChange("Model"); }
        }

        private bool chartVisible;

        public bool ChartVisible
        {
            get { return chartVisible; }
            set { chartVisible = value; NotifyOfPropertyChange(()=>ChartVisible); }
        }

        private bool focusValueVisible;

        public bool FocusValueVisible
        {
            get { return focusValueVisible; }
            set { focusValueVisible = value; NotifyOfPropertyChange(() => FocusValueVisible); }
        }
        

        private void UpdateGraph()
        {
            Execute.OnUIThread(() =>
            {
                data = CreateDataSet();
                switch (ChartConfig.VizType)
                {
                    case "Bar Chart":
                        Model = InitModel();
                        ChartVisible = true;
                        FocusValueVisible = false;
                        break;
                    case "Focus Value":
                        ChartVisible = false;
                        FocusValueVisible = true;
                        break;
                }
                
            });
         
        }

        private DataSet data;

        public PlotModel InitModel()
        {
            
            var model = new PlotModel();
            model.PlotAreaBorderThickness = 0;
            model.Background = null;
            model.Title = "Planning";
            var dateAxis = new DateTimeAxis();
            dateAxis.Minimum = DateTimeAxis.ToDouble(AppState.TimelineManager.Start);
            dateAxis.Maximum = DateTimeAxis.ToDouble(AppState.TimelineManager.End);
            dateAxis.IsAxisVisible = true;
            dateAxis.Position = AxisPosition.Bottom;
            model.Axes.Add(dateAxis);
            var linearAxis2 = new LinearAxis();
            linearAxis2.MajorStep = 1;
            linearAxis2.IsAxisVisible = true;
            var maxquarter = 5;
            linearAxis2.Maximum = maxquarter;
            linearAxis2.Maximum = 5;
            linearAxis2.Minimum = 0;
            linearAxis2.MinorStep = 1;
            model.Axes.Add(linearAxis2);

            // REVIEW TODO Implement this? Or remove it.   
//            switch (ChartConfig.VizType)
//            {
//            }

            CreateBarSeries(linearAxis2,  model);

            return model;
        }

        private DataSet CreateDataSet()
        {
            var data = new DataSet() { Data = new ConcurrentObservableSortedDictionary<DateTime, double>() };
            //var d = new Dictionary<double,int>();
            foreach (var fs in Poi.InFlow())
            {
                var date = fs.Eta;

                if (data.Data.ContainsKey(date))
                {
                    data.Data[date] += 1;
                }
                else
                {
                    data.Data[date] = 1;
                }
            }

            data.GroupingOperation = GroupingOperations.count;
            return data;
        }

        private void CreateBarSeries(LinearAxis linearAxis2, PlotModel model)
        {            
            var barSeries = new RectangleBarSeries();
            barSeries.StrokeThickness = 0;
           
            var dif = GetDiff(data.Grouping);
            foreach (var p in data.GetGroupedDataSet()[0])
            {
                var r = new RectangleBarItem(p.Key, 0, p.Key + dif, p.Value, color: OxyColors.Blue);
                barSeries.Items.Add(r);
            }
            model.Series.Add(barSeries);
        }


        public double GetDiff(GroupingOptions go)
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
            set { poI = value; NotifyOfPropertyChange(() => Poi); }
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

        public string Config { get; set; }
    }
}