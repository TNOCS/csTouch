using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using DataServer;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using csEvents.Sensors;
using csShared;
using System.ComponentModel.Composition;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using System.Windows.Input;

namespace Zandmotor.Controls.Plot
{

    public class XAxisConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                try
                {
                    var format = parameter as string;
                    var d = (double) values[0];
                    var ax = values[1] as Axis;
                    if ((ax as DateTimeAxis) != null)
                        return DateTimeAxis.ToDateTime(d).ToString(format);
                    return d.ToString(CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    // FIXME TODO Deal with exception!
                }
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public interface IPlot
    {
    }

    [Export(typeof(IPlot))]
    public class PlotViewModel : Screen, IPlot
    {
        

        public List<Color> AvailableColors;

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }            
        }

        private bool fixTimeline = true;

        public bool FixTimeline
        {
            get { return fixTimeline; }
            set { fixTimeline = value; NotifyOfPropertyChange(()=>FixTimeline); }
        }
        

        public PlotView _view;
        private csEvents.Sensors.MathFunctions mf = new csEvents.Sensors.MathFunctions();
         
        private BindableCollection<DataSet> _dataSets = new BindableCollection<DataSet>();

        public BindableCollection<DataSet> DataSets
        {
            get { return _dataSets; }
            set { _dataSets = value; NotifyOfPropertyChange(()=>DataSets); }
        }

        public double DesiredHeight;

        public PlotViewModel()
        {
            AvailableColors = new List<Color>()
            {
                Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Gray, Colors.Purple, Colors.Chartreuse,
                Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Gray, Colors.Purple, Colors.Chartreuse,
                Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Gray, Colors.Purple, Colors.Chartreuse,
                Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Gray, Colors.Purple, Colors.Chartreuse,
                Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Gray, Colors.Purple, Colors.Chartreuse,
                Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Gray, Colors.Purple, Colors.Chartreuse
            };
            model = new PlotModel(Title);
        }

        public PlotModel model;
        private DateTimeAxis dateAxis;
        private LinearAxis firstAxis;
        private BackgroundWorker _bw = new BackgroundWorker();
        private string title = "Plot";

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }

        private bool sparkline;

        public bool SparkLine
        {
            get { return sparkline; }
            set { sparkline = value; NotifyOfPropertyChange(()=>SparkLine);}
        }
        
        
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            _view = (PlotView) view;
            _view.SizeChanged += _view_SizeChanged;
            if (TrackerTemplate!=null)
                _view.Plot.DefaultTrackerTemplate = TrackerTemplate;
           
            _bw.DoWork += _bw_DoWork;
             //model = new PlotModel("Test");
            AppState.TimelineManager.TimeChanged += TimelineManager_TimeChanged;
            model.Title = Title;
            //model.IsLegendVisible = true;
            //model.LegendBackground = OxyColor.FromArgb(200, 255, 255, 255);
            //model.LegendBorder = OxyColors.Black;
            //model.LegendPlacement = LegendPlacement.Inside;
            //model.LegendPosition = LegendPosition.TopRight;

            dateAxis = new DateTimeAxis();
            dateAxis.Title = "Time Series";
            dateAxis.Minimum = DateTimeAxis.ToDouble(AppState.TimelineManager.Start);
            dateAxis.Maximum = DateTimeAxis.ToDouble(AppState.TimelineManager.End);
            dateAxis.ExtraGridlines = new double[1];            
            dateAxis.MajorGridlineThickness = 1;
            dateAxis.MajorGridlineStyle = LineStyle.Solid;
            dateAxis.MinorGridlineStyle = LineStyle.Solid;
            
            dateAxis.ExtraGridlines[0] = DateTimeAxis.ToDouble(AppState.TimelineManager.FocusTime);
            dateAxis.ExtraGridlineThickness = 2;
            dateAxis.TimeZone = TimeZoneInfo.Utc;

            if (TimeZone != null)
                dateAxis.TimeZone = TimeZone;

            firstAxis = new LinearAxis(AxisPosition.Left,"");
            firstAxis.Key = "Left";
            firstAxis.Title = "Values";
            firstAxis.MajorGridlineStyle = LineStyle.Solid;
            firstAxis.MinorGridlineStyle = LineStyle.Solid;

            model.Axes.Add(firstAxis);

            //var linearAxis2 = new LinearAxis();
            //linearAxis2.Position = AxisPosition.Right;
            //linearAxis2.Key = "Right";
            //model.Axes.Add(linearAxis2);

            model.Axes.Add(dateAxis);
            _view.Plot.Model = model;
            _view.Plot.PreviewTouchDown += Plot_PreviewTouchDown;
            _view.Plot.ActualModel.Updating += ActualModel_Updating;

            dateAxis.AxisChanged += dateAxis_AxisChanged;

            IObservable<System.Reactive.EventPattern<AxisChangedEventArgs>> eventAsObservable = Observable.FromEventPattern
                <AxisChangedEventArgs>(ev => dateAxis.AxisChanged += ev, ev => dateAxis.AxisChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(k =>
            {
                var min = DateTimeAxis.ToDateTime(dateAxis.ActualMinimum);
                var max = DateTimeAxis.ToDateTime(dateAxis.ActualMaximum);
                if (AppState.TimelineManager.Start != min || AppState.TimelineManager.End!=max)
                {
                    //Execute.OnUIThread(() =>
                    //{
                    //    AppState.TimelineManager.Start = min;
                    //    AppState.TimelineManager.End = max;
                    //    AppState.TimelineManager.ForceTimeChanged();
                    //});
                }
            }
                );

            SurfaceDragDrop.AddDropHandler(_view, Drop);

            DataSets.CollectionChanged += DataSetsCollectionChanged;
            if (DataSets.Count() == 1 && !string.IsNullOrEmpty(DataSets[0].Title)) model.Title = DataSets[0].Title;
            foreach (DataSet ds in DataSets )
            {
                //Set the title of the axis to the unit of the dataset being shown
                if (ds.Unit != null) firstAxis.Title = ds.Unit; 
                AddDataSet(ds);
            }
            //_bw.RunWorkerAsync();
            FitDataSets();
        }

        void dateAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            
        }

        void ActualModel_Updating(object sender, EventArgs e)
        {
            
        }

        private void FitDataSets()
        {
            if (DataSets.Count ==1)
                FitDataSet(DataSets.First());
        }

        private ControlTemplate TrackerTemplate;
        public void SetTrackerTemplate(ControlTemplate t)
        {
            TrackerTemplate = t;
        }

        private TimeZoneInfo TimeZone;
        public void SetTimezone(TimeZoneInfo timezone)
        {
            TimeZone = timezone;
        }


        void _bw_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (DataSet ds in DataSets)
            {
                if (ds.ReducedData == null)
                {
                    if (ds.DData != null)
                        ds.ReducedData = mf.CalculateReduction(ds.DData, 0.95);
                    else
                    {

                        var r = (from n in ds.Data select new KeyValuePair<double, double>(DateTimeAxis.ToDouble(n.Key), n.Value)).ToSortedDictionary(k => k.Key, k => k.Value);
                        ds.ReducedData = mf.CalculateReduction(r,0.95);
                    }
                }
                if (ds.AggregatedData == null)
                {
                    
                }
            }
        }

        void _view_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_view.ActualHeight == 0 || _view.ActualWidth == 0)
            {
                if (DesiredHeight > 0)
                {
                    _view.Height = DesiredHeight;
                }
                return;
            }
            if ((_view.ActualHeight < 100 || (_view.ActualWidth < 100)) && !SparkLine)
                _view.Plot.Visibility = Visibility.Collapsed;
            else
            {
                _view.Plot.Visibility = Visibility.Visible;
                foreach (var v in DataSets)
                {
                    if (v.FunctionOperation == FunctionOperations.auto)
                        UpdateDataSet(v);
                }
            }

            
        }

        void Plot_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            
        }

        public void Drop(object sender, SurfaceDragDropEventArgs e)
        {
            if (e.Cursor.Data is DataSet)
            {
                DataSet ds = (DataSet)e.Cursor.Data;
                if(DataSets.Any(k => k.Sensor.Id == ds.Sensor.Id))
                {
                    SelectedDataSet = DataSets.First(k => k.Sensor.Id == ds.Sensor.Id);
                }
                else
                {
                    var types = DataSets.Select(k => k.Unit).Distinct();
                    //TODO: Only two different data types can be added to a graph. If user adds third datatype, then alert him. 
                    if (types.Count() == 2 && !types.Contains(ds.Unit)) 
                    {
                        MessageBox.Show("Units  mismatch. Cannot add this datatype to this plot.");
                        return;
                    }
                    else
                    {
                        if (!types.Contains(ds.Unit))
                            ds.RightAxis = true;
                        DataSets.Add(ds);

                    }
                    
                }
                
            }
        }

       


        void DataSetsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (DataSet n in e.NewItems) AddDataSet(n);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (DataSet n in e.OldItems) RemoveDataSet(n);
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var ds in DataSets) RemoveDataSet(ds);
            }
        }

        public void SmoothChanged()
        {
            UpdateDataSet(SelectedDataSet);
        }

        private List<SortedDictionary<double, double>> GetDataSet(DataSet ds)
        {

            var res = new SortedDictionary<double, List<double>>();

            if (ds.Data!=null && Enumerable.Any(ds.Data))
            foreach (var p in ds.Data)
            {
                double day = DateTimeAxis.ToDouble(p.Key);
                double groupDay = day;

                switch (ds.Grouping)
                {            
                    case GroupingOptions.none:
                        groupDay = DateTimeAxis.ToDouble(p.Key);
                        break;
                    case GroupingOptions.minutes:
                        groupDay = DateTimeAxis.ToDouble(p.Key.Date.AddHours(p.Key.Hour).AddMinutes(p.Key.Minute));
                        break;
                    case GroupingOptions.quarterly:
                        var min = 0;
                        if (p.Key.Minute < 30) min = 15;
                        if (p.Key.Minute < 45) min = 30;
                        if (p.Key.Minute < 60) min = 45;
                        groupDay = DateTimeAxis.ToDouble(p.Key.Date.AddHours(p.Key.Hour).AddMinutes(min));
                        break;
                    case GroupingOptions.hourly:
                        groupDay = DateTimeAxis.ToDouble(p.Key.Date.AddHours(p.Key.Hour));
                        break;
                    case GroupingOptions.daily:
                        groupDay = DateTimeAxis.ToDouble(p.Key.Date);
                        break;
                    case GroupingOptions.weekly:
                        groupDay = DateTimeAxis.ToDouble(p.Key.GetDateTimeForDayOfWeek(DayOfWeek.Monday));
                        break;
                    case GroupingOptions.monthly:                        
                        groupDay = DateTimeAxis.ToDouble(p.Key.GetFirstDayOfTheMonth());
                        break;
                    case GroupingOptions.yearly:
                        groupDay = DateTimeAxis.ToDouble(p.Key.GetFirstDayOfTheYear());
                        break;
                }
                
                if (!res.ContainsKey(groupDay)) res[groupDay] = new List<double>();
                res[groupDay].Add(p.Value);
            }
            else if (ds.DData != null)
            {
                return new List<SortedDictionary<double, double>>() { ds.DData };
            }
            SortedDictionary<double, double> r = new SortedDictionary<double, double>();
            switch (ds.GroupingOperation)
            {
                case GroupingOperations.count:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Count)).ToSortedDictionary(k => k.Key, k => k.Value);                    
                    break;
                case GroupingOperations.maximum:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Max())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.minimum:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Min())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.average:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Average())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.total:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Sum())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;                
            }
            SortedDictionary<double, double> f = new SortedDictionary<double, double>();
            switch (ds.FunctionOperation)
            {
                case FunctionOperations.none:
                    f = r;
                    break;
                case FunctionOperations.reduction:
                    {
                        var m = new MathFunctions();
                        var temp = (from n in r select new Point(n.Key, n.Value)).ToList();
                        var euclistx = new List<double>();
                        var euclisty = new List<double>();
                        for (int i = 0; i < temp.Count - 1; i++)
                        {
                            euclistx.Add(Math.Sqrt((temp[i].X - temp[i + 1].X)*(temp[i].X - temp[i + 1].X)));
                            euclisty.Add(Math.Sqrt((temp[i].Y - temp[i + 1].Y)*(temp[i].Y - temp[i + 1].Y)));
                        }
                        var thresholdx = Convert.ToDouble(euclistx.Average());
                        var thresholdy = Convert.ToDouble(euclisty.Average());
                        var dp = m.DouglasPeuckerReduction(temp, 10*thresholdx);
                        f = (from n in dp select new KeyValuePair<double, double>(n.X, n.Y)).ToSortedDictionary(k => k.Key,
                                                                                                          k => k.Value);
                    }
                    break;
                case FunctionOperations.auto:
                    {
                        if (ds.ReducedData == null)
                            ds.ReducedData = mf.CalculateReduction(r, 0.95); ;

                        f = ds.ReducedData;


                        if (ds.AggregatedData == null)
                        {
                            ds.AggregatedData = CalculateAggregate(f);
                        }
                        var actualsize = Math.Max(_view.Plot.ActualWidth - 60, 20);
                        var v = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom);
                        var points = f.Where(k => k.Key > v.Minimum && k.Key < v.Maximum).Count();

                        if (points > actualsize / 2)
                        {
                            return ds.AggregatedData;
                        }
                    }
                    break;
                case FunctionOperations.periodic:
                    {
                        var first = r.First();
                        var test = DateTimeAxis.ToDouble(new DateTime());
                        var period = DateTimeAxis.ToDouble(new DateTime() + ds.Periodic)-test;
                        var idx = 1.0;
                        var resultlist = new List<SortedDictionary<double, double>>();
                        foreach (var val in r)
                        {
                           
                            if (val.Key - first.Key > period*idx)
                            {
                                resultlist.Add(f.ToSortedDictionary(k=>k.Key,k=>k.Value));
                                while (val.Key - first.Key > period*idx)
                                    idx++;
                                f.Clear();
                            }
                            f.Add(first.Key + ((val.Key - first.Key) - period*(idx-1)),val.Value);
                        }
                        resultlist.Add(f.ToSortedDictionary(k => k.Key, k => k.Value));
                        return resultlist;
                    }
                // FIXME TODO: Unreachable code
                    // break;
            }
            return new List<SortedDictionary<double, double>>() { f };

        }

        // private double lastfactor=1.5; // Never used.


        private SortedDictionary<double, double> CalculateReduction(SortedDictionary<double, double> input)
        {

            return mf.CalculateReduction(input, 0.95);

                        //var m = new MathFunctions();
                        //var temp = (from n in input select new Point(n.Key, n.Value)).ToList();
                        //var result = temp.ToList();
                        //var points = temp.Count;
                        //double factor  = 1.5;
                        //double calcfactor = Math.Max(1.5,lastfactor/(factor*factor));
                        //var euclistx = new List<double>();
                        //for (int i = 0; i < temp.Count - 1; i++)
                        //{
                        //    euclistx.Add(Math.Sqrt((temp[i].X - temp[i + 1].X) * (temp[i].X - temp[i + 1].X)));
                        //}
                        //var thresholdx = Convert.ToDouble(euclistx.Average());

                        //var xpercentage = 1.0;
                        //while (xpercentage >0.95)
                        //{
                        //    result = m.DouglasPeuckerReduction(temp, calcfactor*thresholdx);
                        //    calcfactor= calcfactor* factor;
                        //    lastfactor = calcfactor;
                        //    var diff = m.CalculateDifference(temp, result);
                        //    var difftotal = m.CalculateDifference(temp, new List<Point>() {temp.First(), temp.Last()});
                        //    xpercentage = 1- (diff/difftotal);
                        //}
                        //return (from n in result select new KeyValuePair<double, double>(n.X, n.Y)).ToSortedDictionary(k => k.Key, k => k.Value);
        }

        private List<SortedDictionary<double, double>> CalculateAggregate(SortedDictionary<double, double> input)
        {
                var ypercentage = 0.0;
                var xpoints = input.Count;
                var minlist = new SortedDictionary<double, double>();
                var avglist = new SortedDictionary<double, double>();
                var maxlist = new SortedDictionary<double, double>();
                while (ypercentage < 0.95)
                {
                    minlist.Clear();
                    maxlist.Clear();
                    avglist.Clear();
                    var hist = new SortedDictionary<int, List<double>>();
                    var min = input.Min(k => k.Key);
                    var max = input.Max(k => k.Key);
                    xpoints = xpoints/2;
                    var buckets = ((max - min)/(xpoints));
                    foreach (var va in input)
                    {
                        var bucket = 0;
                        if (buckets > 0)
                        {
                            bucket = (int) ((va.Key - min)/buckets);
                            if (bucket == xpoints)
                                bucket--;
                        }
                        if (!hist.ContainsKey(bucket))
                        {
                            hist[bucket] = new List<double>();
                        }
                        hist[bucket].Add(va.Value);
                    }
                    var totaldiff = input.Max(k => k.Value) - input.Min(k => k.Value);
                    var diff = 0.0;
                    foreach (var val in hist)
                    {
                        minlist[min + (val.Key*buckets)] = val.Value.Min();
                        avglist[min + (val.Key*buckets)] = val.Value.Average();
                        maxlist[min + (val.Key*buckets)] = val.Value.Max();
                        diff += (val.Value.Max() - val.Value.Min())/hist.Count;
                    }
                    ypercentage = diff/totaldiff;
                }
                return new List<SortedDictionary<double, double>>() { minlist, avglist, maxlist }; ;
            }

        public BindableCollection<string> AvailableGrouping
        {
            get { return new BindableCollection<string>() {"None", "Daily", "Weekly"}; }
        }

        private bool _editDataSet = false;
        public bool EditDataSet
        {
            get { return _editDataSet ; }
            set { _editDataSet = value; NotifyOfPropertyChange(()=>EditDataSet); }
        }

        private bool _drawDataSet = false;
        public bool DrawDataSet
        {
            get { return _drawDataSet; }
            set { _drawDataSet = value; NotifyOfPropertyChange(() => DrawDataSet); }
        }


        private DataSet _selectedDataSet;

        public DataSet SelectedDataSet
        {
            get { return _selectedDataSet; }
            set { _selectedDataSet = value; 
                NotifyOfPropertyChange(()=>SelectedDataSet);                
                NotifyOfPropertyChange(()=>BorderBrush);
            }
        }

        public Brush BorderBrush
        {
            get
            {
                if (SelectedDataSet!=null) return new SolidColorBrush(new Color() { A = 200, R = SelectedDataSet.Color.R, G = SelectedDataSet.Color.G, B = SelectedDataSet.Color.B });
                return Brushes.Black;
            }
        }
        
        private void UpdateDataSet(DataSet ds)
        {

            foreach (var rls in model.Series.OfType<LineSeries>().ToList())
            {
                //Todo rls.Tag is null sometimes and throwing null pointer exception fix it
                if (!DataSets.Any(k=>k.Id.ToString() == rls.Tag.ToString()))
                {
                    model.Series.Remove(rls);
                }
            }
            if (ds.SparkLine || SparkLine)
            {
                SparkLine = true;
                model.IsLegendVisible = false;
                model.TitlePadding = 0;
                model.TitleFontSize = 0;
                model.SubtitleFontSize = 0;
                model.Title = "";
                model.Subtitle = "";
                model.Padding = new OxyThickness(0);
                model.PlotMargins = new OxyThickness(0);
                model.PlotAreaBorderThickness = 0;
                model.Axes.Clear();
                model.Axes.Add(new InvisibleAxis() { Position = AxisPosition.Left, IsAxisVisible = false });
                if (ds.DData != null)
                    model.Axes.Add(new InvisibleAxis() { Position = AxisPosition.Bottom, IsAxisVisible = false });
                else
                    model.Axes.Add(new InvisibleDateAxis() { Position = AxisPosition.Bottom, IsAxisVisible = false });

                if (ds.SparkLineAmount > 2)
                {
                    if (ds.DData != null)
                        ds.ReducedData = mf.CalculateReduction(ds.DData, ds.SparkLineAmount);
                    else
                    {

                        var r = (from n in ds.Data select new KeyValuePair<double, double>(DateTimeAxis.ToDouble(n.Key), n.Value)).ToSortedDictionary(k => k.Key, k => k.Value);
                        ds.ReducedData = mf.CalculateReduction(r, ds.SparkLineAmount);
                    }
                    ds.FunctionOperation = FunctionOperations.auto;
                }
                else if (ds.SparkLinePercentage > 0)
                {
                    if (ds.DData != null)
                        ds.ReducedData = mf.CalculateReduction(ds.DData, ds.SparkLinePercentage);
                    else
                    {
                        var r = (from n in ds.Data select new KeyValuePair<double, double>(DateTimeAxis.ToDouble(n.Key), n.Value)).ToSortedDictionary(k => k.Key, k => k.Value);
                        ds.ReducedData = mf.CalculateReduction(r, ds.SparkLinePercentage);
                    }
                    ds.FunctionOperation = FunctionOperations.auto;
                }
                foreach (var ax in model.Axes)
                {
                    ax.IsPanEnabled = false;
                    ax.IsZoomEnabled = false;
                }
                //foreach (var ax in model.Axes)
                //    ax.IsAxisVisible = false;
            }
            else SparkLine = false;

            if (model.Series.Count(k => ((Guid)k.Tag) == ds.Id) > 1)
                foreach (var r in model.Series.Where(k => ((Guid)k.Tag) == ds.Id).ToList())
                {
                    model.Series.Remove(r);
                }
            
            var ls = model.Series.FirstOrDefault(k => k.Tag.ToString() == ds.Id.ToString()) as LineSeries;
            if (ls==null)
                ls = new LineSeries(){Tag = ds.Id};
            ls.Smooth = ds.Smooth;
            ls.YAxisKey = (ds.RightAxis) ? "Right" : "Left";
            if (ls.YAxisKey == "Right")
            {
                var ra = model.Axes.FirstOrDefault(k => k.Key == "Right");
                if (ra == null)
                {
                    var linearAxis2 = new LinearAxis();
                    linearAxis2.Position = AxisPosition.Right;
                    linearAxis2.Key = "Right";
                    linearAxis2.Title = ds.Unit != null ? ds.Unit : "Value";
                    model.Axes.Add(linearAxis2);
                }

            }
            if (ls != null)
            {
                var res = GetDataSet(ds);
                

                if (res.Count == 1)
                {
                    ls.Points.Clear();
                    foreach (var a in res.First())
                    {
                        ls.Points.Add(new DataPoint(a.Key, a.Value));
                    }
                    var ap = model.Series.OfType<AreaSeries>().FirstOrDefault(k => k.Tag.ToString() == ds.Id.ToString());
                    if (ap != null)
                    {
                        model.Series.Remove(ap);
                    }
                }
                else if (ds.FunctionOperation == FunctionOperations.auto)
                {
                    var ap = model.Series.OfType<AreaSeries>().FirstOrDefault(k => k.Tag.ToString() == ds.Id.ToString());
                    if (ap == null)
                    {
                        ap = new AreaSeries() { Tag = ds.Id };
                        model.Series.Add(ap);
                    }
                    ap.Fill = OxyColor.FromAColor((byte)96,OxyColors.LightBlue);
                    ap.Color = OxyColors.Black;
                    ap.Points.Clear();
                    ap.Points2.Clear();
                    foreach (var a in res.Last())
                    {
                        ap.Points.Add(new DataPoint(a.Key, a.Value));
                    }
                    foreach (var a in res.First())
                    {
                        ap.Points2.Add(new DataPoint(a.Key, a.Value));
                    }
                    foreach (var a in res[1])
                    {
                        ls.Points.Add(new DataPoint(a.Key, a.Value));
                    }
                }
                else
                {
                    foreach (var r in model.Series.Where(k => ((Guid)k.Tag) == ds.Id).ToList())
                    {
                        model.Series.Remove(r);
                    }
                    var l = new List<SortedDictionary<DateTime, double>>();
                    int colidx = 0;
                    foreach (var lineseries in res)
                    {
                        var lss = new LineSeries();
                        var ll = new SortedDictionary<DateTime, double>();

                        ds.Color = AvailableColors[colidx];
                        colidx++;

                        lss.YAxisKey = (ds.RightAxis) ? "Right" : "Left";
                        lss.Smooth = ds.Smooth;
                        lss.Color = OxyColor.FromArgb(ds.Color.A, ds.Color.R, ds.Color.G, ds.Color.B);
                        lss.MarkerFill = OxyColor.FromArgb(255, 78, 154, 6);
                        lss.Tag = ds.Id;
                        foreach (var a in lineseries)
                        {
                            lss.Points.Add(new DataPoint(a.Key, a.Value));
                            ll.Add(DateTimeAxis.ToDateTime(a.Key), a.Value);
                        }
                        model.Series.Add(lss);
                        l.Add(ll);
                    }
                }



                model.RefreshPlot(true);
            }
        }

        public class InvisibleAxis : Axis
        {
            public override bool IsXyAxis()
            {
                return true;
            }

            public override OxySize Measure(IRenderContext rc) { return new OxySize(0, 0); }
        }

        public class InvisibleDateAxis : DateTimeAxis
        {
            public override bool IsXyAxis()
            {
                return true;
            }

            public override OxySize Measure(IRenderContext rc) { return new OxySize(0, 0); }
        }

        private void AddDataSet(DataSet ds)
        {


            var ls = new LineSeries();
            
            ds.Color = Colors.Red;
            foreach (Color c in AvailableColors)
            {
                if (!DataSets.Any(k=>k.Color == c))
                {
                    ds.Color = c;
                    break;
                }
                
            }
            ls.YAxisKey = (ds.RightAxis) ? "Right" : "Left";
            ls.Smooth = ds.Smooth;
            ls.Color = OxyColor.FromArgb(ds.Color.A, ds.Color.R,ds.Color.G,ds.Color.B);
            ls.MarkerFill = OxyColor.FromArgb(255, 78, 154, 6);
            ls.Tag = ds.Id;
            ls.Title = ds.Sensor.Description;
            
            // week days


            if (ds.DData != null)
            {
                var da = model.Axes.OfType<DateTimeAxis>().ToList();
                foreach (var d in da)
                    model.Axes.Remove(d);
                if (model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom) == null)
                {
                    firstAxis = new LinearAxis(AxisPosition.Bottom, "");
                    firstAxis.Key = "Bottom";
                    firstAxis.Minimum = ds.DData.Keys.First();
                    firstAxis.Minimum = ds.DData.Keys.Last();
                    firstAxis.MajorGridlineStyle = LineStyle.Solid;
                    firstAxis.MinorGridlineStyle = LineStyle.Dot;
                    firstAxis.MinimumPadding = 0.05;
                    firstAxis.MaximumPadding = 0.1;
                    model.Axes.Add(firstAxis);
                }
            
                foreach (var a in ds.DData)
                {
                    ls.Points.Add(new DataPoint(a.Key, a.Value));
                }
            }
            else
            {
                var res = GetDataSet(ds);
                if (res.Count == 1)
                {
                    foreach (var a in res.First())
                    {
                        ls.Points.Add(new DataPoint(a.Key, a.Value));
                    }
                }
                else
                {
                    var l = new List<SortedDictionary<DateTime, double>>();
                    int colidx=0;
                    foreach (var lineseries in res)
                    {
                        var lss = new LineSeries();
                        var ll = new SortedDictionary<DateTime, double>();
                        ds.Color = AvailableColors[colidx];
                        colidx++;

                        lss.YAxisKey = (ds.RightAxis) ? "Right" : "Left";
                        lss.Smooth = ds.Smooth;
                        lss.Color = OxyColor.FromArgb(ds.Color.A, ds.Color.R, ds.Color.G, ds.Color.B);
                        lss.MarkerFill = OxyColor.FromArgb(255, 78, 154, 6);
                        lss.Tag = ds.Id;
                        foreach (var a in lineseries)
                        {
                            lss.Points.Add(new DataPoint(a.Key, a.Value));
                            ll.Add(DateTimeAxis.ToDateTime(a.Key),a.Value);
                        }
                        model.Series.Add(lss);
                        l.Add(ll);
                    }
                }
            }
            model.Series.Add(ls);
            UpdateDataSet(ds);
            //model.Title = ds.Sensor.Description != null ? ds.Sensor.Description : Title ;
            model.Update();
            ds.Updated -= ds_Updated;
            ds.Updated += ds_Updated;
        
            if (ds.Data!=null)
                ds.Data.CollectionChanged += (e,f) => UpdateDataSet(ds);
            _view.Plot.InvalidatePlot(true);
        }

        void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _view.Plot.InvalidatePlot(true);
            model.Update();
        }

        void ds_Updated(object sender, EventArgs e)
        {
            _view.Plot.InvalidatePlot(true);
                    }

        private void RemoveDataSet(DataSet dataSet)
        {
            var tbr = model.Series.Where(k => k.Tag is Guid && (Guid) k.Tag == dataSet.Id).ToList();
            foreach (var a in tbr)
            {
                model.Series.Remove(a);
            }
            model.Update();
            _view.Plot.InvalidatePlot(true);
        }

        public void DataSetEditDone()
        {
            EditDataSet = false;
            
        }

        private MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel();
            menu.RelativeElement = fe;
            menu.RelativePosition = new Point(0, 0);
            menu.TimeOut = new TimeSpan(0, 0, 0, 5);
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            menu.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            //menu.DisplayProperty = "ServiceName";
            menu.AutoClose = true;
            return menu;
        }

        public void OpenSettingsMenu(SurfaceButton b)
        {
            var m = GetMenu(b);
            //var fit = m.AddMenuItem("Fit All");
            //fit.Click += (e, f) =>
            //{

            //};

            var timeline = m.AddMenuItem("Use this timespan");
            timeline.Click += (e, f) =>
            {
                AppState.TimelineManager.Start = DateTimeAxis.ToDateTime(dateAxis.ActualMinimum);
                AppState.TimelineManager.End = DateTimeAxis.ToDateTime(dateAxis.ActualMaximum);
                AppState.TimelineManager.ForceTimeChanged();
            };
            var fitall = m.AddMenuItem("Auto fit all");
            fitall.Click += (e, f) =>
            {
                FitAllDataSets();
            };
            AppState.Popups.Add(m);
        }

        public void OpenGroupingOperationMenu(DataSet ds, SurfaceButton b)
        {

            var menu = GetMenu(b);
            foreach (var a in Enum.GetValues(typeof(GroupingOperations)))
            {
                var mi = menu.AddMenuItem(a.ToString());
                mi.Click += (e, s) =>
                {
                    SelectedDataSet.GroupingOperation = (GroupingOperations)a;
                    UpdateDataSet(SelectedDataSet);
                    menu.Close();
                };
            }
            AppState.Popups.Add(menu);
        }


        public void OpenFunctionOperationMenu(DataSet ds, SurfaceButton b)
        {
            var menu = new MenuPopupViewModel();
            menu.RelativeElement = b;
            menu.RelativePosition = new Point(0, 0);
            menu.TimeOut = new TimeSpan(0, 0, 0, 5);
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            menu.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            //menu.DisplayProperty = "ServiceName";
            menu.AutoClose = true;

            foreach (var a in Enum.GetValues(typeof(FunctionOperations)))
            {
                var mi = menu.AddMenuItem(a.ToString());
                mi.Click += (e, s) =>
                {
                    SelectedDataSet.FunctionOperation = (FunctionOperations)a;
                    UpdateDataSet(SelectedDataSet);
                    menu.Close();
                };
            }
            AppState.Popups.Add(menu);
        }

        public void OpenGroupingMenu(DataSet ds,SurfaceButton b)
        {
            var menu = new MenuPopupViewModel();
            menu.RelativeElement = b;
            menu.RelativePosition = new Point(0, 0);
            menu.TimeOut = new TimeSpan(0, 0, 0, 5);
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            menu.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            //menu.DisplayProperty = "ServiceName";
            menu.AutoClose = true;

            foreach (var a in Enum.GetValues(typeof(GroupingOptions)))
            {
                var mi = menu.AddMenuItem(a.ToString());
                mi.Click += (e, s) =>
                    {
                        SelectedDataSet.Grouping = (GroupingOptions)a;
                        UpdateDataSet(SelectedDataSet);
                        menu.Close();
                };                
            }
            AppState.Popups.Add(menu);
        }

        

        public void OpenDataSetMenu(DataSet ds, SurfaceButton b)
        {
            var menu = new MenuPopupViewModel();
            menu.RelativeElement = b;
            menu.RelativePosition = new Point(0, 0);
            menu.TimeOut = new TimeSpan(0, 0, 0, 5);
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            menu.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            //menu.DisplayProperty = "ServiceName";
            menu.AutoClose = true;
            var miRemove = menu.AddMenuItem("Remove");
            miRemove.Click += (e, s) =>
                {
                    DataSets.Remove(ds);
                    menu.Close();
                };

            var miFit = menu.AddMenuItem("Fit");
            miFit.Click += (e, s) =>
            {
                FitDataSet(ds);
                menu.Close();
            };

            var miDuplicate = menu.AddMenuItem("Duplicate");
            miDuplicate.Click += (e, s) =>
                {
                    DataSet d = ds.Clone() as DataSet;
                    DataSets.Add(d);
                    menu.Close();
                };

            var miEdit = menu.AddMenuItem("Edit");
            miEdit.Click += (e, s) =>
                {
                    SelectedDataSet = ds;
                    menu.Close();
                    EditDataSet = true;
                };


            var miDraw = menu.AddMenuItem("Draw");
            miDraw.Click += (e, s) =>
            {
                SelectedDataSet = ds;
                menu.Close();
                if (DrawDataSet == false)
                {
                    DrawDataSet = true;
                    // Subscribe to the mouse down event on the line series
                    _view.PreviewMouseLeftButtonDown += _view_PreviewMouseLeftButtonDown;
                    _view.PreviewMouseMove += _view_PreviewMouseMove;
                    _view.PreviewMouseLeftButtonUp += _view_PreviewMouseLeftButtonUp;
                    

                }
                else
                {
                    DrawDataSet = false;
                }
            };
            AppState.Popups.Add(menu);
        }

        void _view_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _view.CaptureMouse();
            var p = e.GetPosition(_view.Plot);

            s1 = new LineSeries("LineSeries" + (model.Series.Count + 1))
            {
                // Color = OxyColors.SkyBlue,
                MarkerType = MarkerType.None,
                StrokeThickness = 2
            };

            s1.Points.Add(Axis.InverseTransform(new ScreenPoint(p.X, p.Y), dateAxis, firstAxis));
            model.Series.Add(s1);
            model.RefreshPlot(false);
            e.Handled = true;
        }

        void _view_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (s1 != null)
            {
                var p = e.GetPosition(_view.Plot);

                s1.Points.Add(Axis.InverseTransform(new ScreenPoint(p.X, p.Y), dateAxis, firstAxis));
                model.RefreshPlot(false);
                e.Handled = true;
            }
        }

        void _view_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _view.ReleaseMouseCapture();
            if (s1 != null)
            {
                s1 = null;
            }
            _view.PreviewMouseLeftButtonDown -= _view_PreviewMouseLeftButtonDown;
            _view.PreviewMouseMove -= _view_PreviewMouseMove;
            _view.PreviewMouseLeftButtonUp -= _view_PreviewMouseLeftButtonUp;
            e.Handled = true;
            DrawDataSet = false;
        }

        private LineSeries s1 = null;


        public void FitAllDataSets()
        {
            var minx = double.MaxValue;
            var maxx = double.MinValue;
            var miny = double.MaxValue;
            var maxy = double.MinValue;
            // Do normal data on left axis first!
            foreach (var ds in DataSets.Where(k => !k.RightAxis && k.Data != null && k.DData == null))
            {
                var mix = DateTimeAxis.ToDouble(ds.Data.OrderBy(k => k.Key).FirstOrDefault().Key);
                var max = DateTimeAxis.ToDouble(ds.Data.OrderByDescending(k => k.Key).FirstOrDefault().Key);
                var miy = ds.Data.OrderBy(k => k.Value).FirstOrDefault().Value;
                var may = ds.Data.OrderByDescending(k => k.Value).FirstOrDefault().Value;
                if (mix < minx) minx = mix;
                if (max > maxx) maxx = max;
                if (miy < miny) miny = miy;
                if (may > maxy) maxy = may;
            }
            var difx = maxx - minx;
            var dify = maxy - miny;
            dateAxis.Zoom(minx - difx / 10.0, maxx + difx / 10.0);
            firstAxis.Zoom(miny - dify / 10.0, maxy + dify / 10.0);

            // Do non-time data on left axis first!
            foreach (var ds in DataSets.Where(k => !k.RightAxis && k.DData != null && k.Data == null))
            {
                var mix = ds.DData.OrderBy(k => k.Key).FirstOrDefault().Key;
                var max = ds.DData.OrderByDescending(k => k.Key).FirstOrDefault().Key;
                var miy = ds.DData.OrderBy(k => k.Value).FirstOrDefault().Value;
                var may = ds.DData.OrderByDescending(k => k.Value).FirstOrDefault().Value;
                if (mix < minx) minx = mix;
                if (max > maxx) maxx = max;
                if (miy < miny) miny = miy;
                if (may > maxy) maxy = may;
            }
            difx = maxx - minx;
            dify = maxy - miny;
            var baxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom);
            if (baxis != null)
                baxis.Zoom(minx - (difx) / 10.0, maxx + (difx) / 10.0);
            firstAxis.Zoom(miny - dify / 10.0, maxy + dify / 10.0);


            miny = double.MaxValue;
            maxy = double.MinValue;
            // Do normal data on right axis first!
            foreach (var ds in DataSets.Where(k => k.RightAxis && k.Data != null && k.DData == null))
            {
                var mix = DateTimeAxis.ToDouble(ds.Data.OrderBy(k => k.Key).FirstOrDefault().Key);
                var max = DateTimeAxis.ToDouble(ds.Data.OrderByDescending(k => k.Key).FirstOrDefault().Key);
                var miy = ds.Data.OrderBy(k => k.Value).FirstOrDefault().Value;
                var may = ds.Data.OrderByDescending(k => k.Value).FirstOrDefault().Value;
                if (mix < minx) minx = mix;
                if (max > maxx) maxx = max;
                if (miy < miny) miny = miy;
                if (may > maxy) maxy = may;
            }
            difx = maxx - minx;
            dify = maxy - miny;
            dateAxis.Zoom(minx - difx / 10.0, maxx + difx / 10.0);
            var raxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Right);
            if (raxis != null)
                raxis.Zoom(miny - dify / 10.0, maxy + dify / 10.0);

            // Do non-time data on right axis first!
            foreach (var ds in DataSets.Where(k => k.RightAxis && k.DData != null && k.Data == null))
            {
                var mix = ds.DData.OrderBy(k => k.Key).FirstOrDefault().Key;
                var max = ds.DData.OrderByDescending(k => k.Key).FirstOrDefault().Key;
                var miy = ds.DData.OrderBy(k => k.Value).FirstOrDefault().Value;
                var may = ds.DData.OrderByDescending(k => k.Value).FirstOrDefault().Value;
                if (mix < minx) minx = mix;
                if (max > maxx) maxx = max;
                if (miy < miny) miny = miy;
                if (may > maxy) maxy = may;
            }
            difx = maxx - minx;
            dify = maxy - miny;
            baxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom);
            if (baxis != null)
                baxis.Zoom(minx - (difx) / 10.0, maxx + (difx) / 10.0);
            if (raxis != null)
                raxis.Zoom(miny - dify / 10.0, maxy + dify / 10.0);



            //{
            //    if (ds.Data == null)
            //    {
            //        if (ds.DData != null)
            //        {
            //            var baxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom);
            //            if (baxis != null)
            //                baxis.Zoom(ds.DData.OrderBy(k => k.Key).FirstOrDefault().Key,
            //                           ds.DData.OrderByDescending(k => k.Key).FirstOrDefault().Key);
            //            var raxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Right);
            //            if (raxis != null)
            //                raxis.Zoom(ds.DData.OrderBy(k => k.Value).FirstOrDefault().Value,
            //                           ds.DData.OrderByDescending(k => k.Value).FirstOrDefault().Value);
            //        }
            //    }
            //    else
            //    {
            //        dateAxis.Zoom(DateTimeAxis.ToDouble(ds.Data.OrderBy(k => k.Key).FirstOrDefault().Key),
            //                      DateTimeAxis.ToDouble(
            //                          ds.Data.OrderByDescending(k => k.Key).FirstOrDefault().Key));
            //        var raxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Right);
            //        if (raxis != null)
            //            raxis.Zoom(ds.Data.OrderBy(k => k.Value).FirstOrDefault().Value,
            //                       ds.Data.OrderByDescending(k => k.Value).FirstOrDefault().Value);
            //    }
            //}
            model.Update();
            _view.Plot.InvalidatePlot(true);
        }



        public void FitDataSet(DataSet ds)
        {
            if (!ds.RightAxis)
            {
                if (ds.Data == null)
                {
                    if (ds.DData != null)
                    {
                        var minx = ds.DData.OrderBy(k => k.Key).FirstOrDefault().Key;
                        var maxx = ds.DData.OrderByDescending(k => k.Key).FirstOrDefault().Key;
                        var difx = maxx - minx;
                        var miny = ds.DData.OrderBy(k => k.Value).FirstOrDefault().Value;
                        var maxy = ds.DData.OrderByDescending(k => k.Value).FirstOrDefault().Value;
                        var dify = maxy - miny;
                        var baxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom);
                        if (baxis != null)
                            baxis.Zoom(minx - (difx) / 10.0 ,maxx + (difx) / 10.0);
                        var laxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Left);
                        if (laxis != null)
                            laxis.Zoom(miny - dify/10.0,maxy + dify / 10.0);
                    }
                }
                else
                {
                    var minx = DateTimeAxis.ToDouble(ds.Data.OrderBy(k => k.Key).FirstOrDefault().Key);
                    var maxx = DateTimeAxis.ToDouble(ds.Data.OrderByDescending(k => k.Key).FirstOrDefault().Key);
                    var difx = maxx - minx;
                    var miny = ds.Data.OrderBy(k => k.Value).FirstOrDefault().Value;
                    var maxy = ds.Data.OrderByDescending(k => k.Value).FirstOrDefault().Value;
                    var dify = maxy - miny;
                    dateAxis.Zoom(minx - difx / 10.0,maxx + difx / 10.0);
                    firstAxis.Zoom(miny - dify / 10.0,maxy + dify / 10.0);
                }
            }
            else
            {
                if (ds.Data == null)
                {
                    if (ds.DData != null)
                    {
                        var baxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Bottom);
                        if (baxis != null)
                            baxis.Zoom(ds.DData.OrderBy(k => k.Key).FirstOrDefault().Key,
                                       ds.DData.OrderByDescending(k => k.Key).FirstOrDefault().Key);
                        var raxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Right);
                        if (raxis != null)
                            raxis.Zoom(ds.DData.OrderBy(k => k.Value).FirstOrDefault().Value,
                                       ds.DData.OrderByDescending(k => k.Value).FirstOrDefault().Value);
                    }
                }
                else
                {
                    dateAxis.Zoom(DateTimeAxis.ToDouble(ds.Data.OrderBy(k => k.Key).FirstOrDefault().Key),
                                  DateTimeAxis.ToDouble(
                                      ds.Data.OrderByDescending(k => k.Key).FirstOrDefault().Key));
                    var raxis = model.Axes.FirstOrDefault(k => k.Position == AxisPosition.Right);
                    if (raxis != null)
                        raxis.Zoom(ds.Data.OrderBy(k => k.Value).FirstOrDefault().Value,
                                   ds.Data.OrderByDescending(k => k.Value).FirstOrDefault().Value);
                }
            }
            model.Update();
            _view.Plot.InvalidatePlot(true);
        }

        void TimelineManager_TimeChanged(object sender, System.EventArgs e)
        {
            
            //if (FixTimeline)
            {
                dateAxis.Minimum = DateTimeAxis.ToDouble(AppState.TimelineManager.Start);
                dateAxis.Maximum = DateTimeAxis.ToDouble(AppState.TimelineManager.End);
                dateAxis.ExtraGridlines[0] = DateTimeAxis.ToDouble(AppState.TimelineManager.FocusTime);
                model.Update();

                _view.Plot.InvalidatePlot(true);
                dateAxis.Reset();
            }
        }
    }

    public static class DataSetExtensions
    {
        public static List<SortedDictionary<double, double>> GetGroupedDataSet(this DataSet ds)
        {

            var res = new SortedDictionary<double, List<double>>();

            if (ds.Data != null && Enumerable.Any(ds.Data))
                foreach (var p in ds.Data)
                {
                    double day = DateTimeAxis.ToDouble(p.Key);
                    double groupDay = day;

                    switch (ds.Grouping)
                    {
                        case GroupingOptions.none:
                            groupDay = DateTimeAxis.ToDouble(p.Key);
                            break;
                        case GroupingOptions.minutes:
                            groupDay = DateTimeAxis.ToDouble(p.Key.Date.AddHours(p.Key.Hour).AddMinutes(p.Key.Minute));
                            break;
                        case GroupingOptions.quarterly:
                            var min = 0;
                            if (p.Key.Minute < 30) min = 15;
                            if (p.Key.Minute < 45) min = 30;
                            if (p.Key.Minute < 60) min = 45;
                            groupDay = DateTimeAxis.ToDouble(p.Key.Date.AddHours(p.Key.Hour).AddMinutes(min));
                            break;
                        case GroupingOptions.hourly:
                            groupDay = DateTimeAxis.ToDouble(p.Key.Date.AddHours(p.Key.Hour));
                            break;
                        case GroupingOptions.daily:
                            groupDay = DateTimeAxis.ToDouble(p.Key.Date);
                            break;
                        case GroupingOptions.weekly:
                            groupDay = DateTimeAxis.ToDouble(p.Key.GetDateTimeForDayOfWeek(DayOfWeek.Monday));
                            break;
                        case GroupingOptions.monthly:
                            groupDay = DateTimeAxis.ToDouble(p.Key.GetFirstDayOfTheMonth());
                            break;
                        case GroupingOptions.yearly:
                            groupDay = DateTimeAxis.ToDouble(p.Key.GetFirstDayOfTheYear());
                            break;
                    }

                    if (!res.ContainsKey(groupDay)) res[groupDay] = new List<double>();
                    res[groupDay].Add(p.Value);
                }
            else if (ds.DData != null)
            {
                return new List<SortedDictionary<double, double>>() { ds.DData };
            }
            SortedDictionary<double, double> r = new SortedDictionary<double, double>();
            switch (ds.GroupingOperation)
            {
                case GroupingOperations.count:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Count)).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.maximum:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Max())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.minimum:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Min())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.average:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Average())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
                case GroupingOperations.total:
                    r = (from n in res select new KeyValuePair<double, double>(n.Key, n.Value.Sum())).ToSortedDictionary(k => k.Key, k => k.Value);
                    break;
            }
            
            return new List<SortedDictionary<double, double>>() { r };

        }

    }
}
