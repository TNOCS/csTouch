using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.Utils.Collections;

namespace csEvents.Sensors
{
    public enum DataType
    {
        Double,
        Bool,
        String
    }

    public class DataSet : PropertyChangedBase
    {
        private Color color = Colors.Red;
        private string description;
        private string focusStringValue;
        private double focusValue;
        private FunctionOperations functionOperation = FunctionOperations.none;
        private GroupingOptions grouping = GroupingOptions.none;
        private GroupingOperations groupingOperation = GroupingOperations.average;
        private Guid id = Guid.NewGuid();
        private TimeSpan periodic = new TimeSpan(0, 1, 0, 0, 0);
        private bool rightAxis;
        private Sensor sensor;
        private bool smooth;
        private bool sparkline;
        private int sparklineAmount;
        private double sparklinepercentage;
        private string title;
        private DataType type = DataType.Double;
        private string unit;
        
        private string category = "General";

        public event EventHandler Updated;

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                NotifyOfPropertyChange(() => Description);
            }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public string Category {
            get { return category; }
            set {
                if (value == category) return;
                category = value;
                NotifyOfPropertyChange(() => Category);
            }
        }

        public DataType Type
        {
            get { return type; }
            set
            {
                type = value;
                NotifyOfPropertyChange(() => Type);
            }
        }

        public GroupingOptions Grouping
        {
            get { return grouping; }
            set
            {
                grouping = value;
                NotifyOfPropertyChange(() => Grouping);
            }
        }

        public GroupingOperations GroupingOperation
        {
            get { return groupingOperation; }
            set
            {
                groupingOperation = value;
                NotifyOfPropertyChange(() => GroupingOperation);
            }
        }

        public FunctionOperations FunctionOperation
        {
            get { return functionOperation; }
            set
            {
                functionOperation = value;
                NotifyOfPropertyChange(() => FunctionOperation);
            }
        }

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public double FocusValue
        {
            get { return focusValue; }
            set
            {
                focusValue = value;
                NotifyOfPropertyChange(() => FocusValue);
            }
        }

        public Brush ColorBrush
        {
            get { return new SolidColorBrush(Color); }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                color = value;
                NotifyOfPropertyChange(() => Color);
                NotifyOfPropertyChange(() => ColorBrush);
            }
        }

        public bool SparkLine
        {
            get { return sparkline; }
            set
            {
                sparkline = value;
                NotifyOfPropertyChange(() => SparkLine);
            }
        }

        public double SparkLinePercentage
        {
            get { return sparklinepercentage; }
            set
            {
                sparklinepercentage = value;
                NotifyOfPropertyChange(() => SparkLinePercentage);
            }
        }

        public int SparkLineAmount
        {
            get { return sparklineAmount; }
            set
            {
                sparklineAmount = value;
                NotifyOfPropertyChange(() => SparkLineAmount);
            }
        }

        public TimeSpan Periodic
        {
            get { return periodic; }
            set
            {
                periodic = value;
                NotifyOfPropertyChange(() => Periodic);
            }
        }

        public string Unit
        {
            get { return unit; }
            set
            {
                unit = value;
                NotifyOfPropertyChange(() => Unit);
            }
        }

        public bool RightAxis
        {
            get { return rightAxis; }
            set
            {
                rightAxis = value;
                NotifyOfPropertyChange(() => RightAxis);
            }
        }

        public bool Smooth
        {
            get { return smooth; }
            set
            {
                smooth = value;
                NotifyOfPropertyChange(() => Smooth);
            }
        }

        public Sensor Sensor
        {
            get { return sensor; }
            set
            {
                sensor = value;
                NotifyOfPropertyChange(() => Sensor);
            }
        }

        //public DataType SensorDataType { get; set; }
        //public SortedDictionary<DateTime, bool> BooleanData { get; set; }
        //public SortedDictionary<DateTime, float> FloatData { get; set; }

        public ConcurrentObservableSortedDictionary<DateTime, string> StringData { get; set; }
        public ConcurrentObservableSortedDictionary<DateTime, double> Data { get; set; }

        public SortedDictionary<double, double> DData { get; set; }
        public SortedDictionary<double, double> ReducedData { get; set; }
        public List<SortedDictionary<double, double>> AggregatedData { get; set; }

        public void TriggerUpdated()
        {
            var handler = Updated;
            if (handler != null) handler(this, null);
        }

        public DateTime? FirstDateTime
        {
            get
            {
                if (Data==null || !Data.Any()) return null;
                return Data.Keys.OrderBy(k => k).FirstOrDefault();
            }
        }

        public DateTime? LastDateTime
        {
            get
            {
                if (Data == null || !Data.Any()) return null;
                return Data.Keys.OrderByDescending(k => k).FirstOrDefault();
            }
        }

        public bool SetFocusDate(DateTime focusTime)
        {
            switch (Type)
            {
                case DataType.Double:
                    if (Data == null || Data.Count == 0) return false;
                    
                    var vt = Data.Where(k => k.Key >= focusTime).OrderBy(k=>k.Key).FirstOrDefault();

                    if (vt.Key.Year == 1 && vt.Key.Hour == 0)
                    {
                        // No value was found, so the focus time exceeds the data set, so use the last value
                        vt = Data.Last();
                    }

                    if (vt.Value == focusValue) return false;
                    FocusValue = vt.Value;
                    return true;
                case DataType.String:
                    if (StringData == null || StringData.Count == 0) return false;

                    KeyValuePair<DateTime, string> vts = StringData.FirstOrDefault(k => k.Key >= focusTime);

                    if (vts.Key.Year == 1 && vts.Key.Hour == 0)
                    {
                        // No value was found, so the focus time exceeds the data set, so use the last value
                        vts = StringData.Last();
                    }

                    if (vts.Value == focusStringValue) return false;
                    focusStringValue = vts.Value;
                    break;
            }
            return false;

            //var values = Data.Where(k => k.Key >= focusTime).Take(2).ToList();
            //if (values.Count() < 2)
            //{
            //    var vt = Data.Last();
            //    if (vt.Value == focusValue) return false;
            //    focusValue = vt.Value;
            //    return true;
            //}

            //// Interpolate result linearly
            //var startTime = values[0].Key;
            //var endTime = values[1].Key;
            //var duration = (endTime - startTime).Ticks;
            //var offset = (focusTime - startTime).Ticks;
            //var startValue = values[0].Value;
            //var endValue = values[1].Value;
            //focusValue = startValue + ((endValue - startValue)*offset)/duration;
            //return true;

            //var vt = Data.FirstOrDefault(k => k.Key >= date);

            //if (vt.Value != focusValue)
            //{
            //    focusValue = vt.Value;
            //    return true;
            //}
            //return false;
            ////{
            ////    double v = vt.FirstOrDefault().Value;
            ////    if (v != focusValue) focusValue = v;
            ////}
        }

        /// <summary>
        ///     Shallow clone.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var ds = new DataSet
            {
                Sensor = Sensor,
                Smooth = Smooth,
                Grouping = Grouping,
                GroupingOperation = GroupingOperation,
                Data = Data,
                //BooleanData = BooleanData,
                //FloatData = FloatData,
                //StringData = StringData,
                DData = DData,
                RightAxis = RightAxis,
                Color = Color
            };
            return ds;
        }

        private string dataSetId;
        public string DataSetId
        {
            get
            {
                if (string.IsNullOrEmpty(dataSetId)) dataSetId = Category + "-" + Title;
                return dataSetId;
            }
            set { dataSetId = value; }
        }
    }
}