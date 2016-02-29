using Caliburn.Micro;
using csShared;
using DataServer;
using PoiServer.PoI;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;

namespace csDataServerPlugin
{
    public class StyleViewModel : Screen
    {
        public class HighlightEventArgs : EventArgs
        {

        }

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private PoiService service;

        public PoiService Service
        {
            get { return service; }
            set { service = value; }
        }

        private GradientBrush gradient;

        public GradientBrush Gradient
        {
            get { return gradient; }
            set { gradient = value; NotifyOfPropertyChange(() => Gradient); }
        }

        private bool expanded;

        public bool Expanded
        {
            get { return expanded; }
            set
            {
                expanded = value; NotifyOfPropertyChange(() => Expanded);
            }
        }

        public event EventHandler<HighlightEventArgs> HighlightChanged;

        private bool showThresholdDoubleValue;

        public bool ShowThresholdDoubleValue
        {
            get { return showThresholdDoubleValue; }
            set { showThresholdDoubleValue = value; NotifyOfPropertyChange(() => ShowThresholdDoubleValue); }
        }


        private Highlight highlighter;

        public Highlight Highlighter
        {
            get { return highlighter; }
            set { highlighter = value; NotifyOfPropertyChange(() => Highlighter); }
        }

        private bool showSymbolSize;

        public bool ShowSymbolSize
        {
            get { return showSymbolSize; }
            set { showSymbolSize = value; NotifyOfPropertyChange(() => ShowSymbolSize); }
        }

        private bool showThreshholdType;

        public bool ShowThreshholdType
        {
            get { return showThreshholdType; }
            set { showThreshholdType = value; NotifyOfPropertyChange(() => ShowThreshholdType); }
        }

        private bool showColorRange;

        public bool ShowColorRange
        {
            get { return showColorRange; }
            set { showColorRange = value; NotifyOfPropertyChange(() => ShowColorRange); }
        }

        private bool showSelectionCriteria;

        public bool ShowSelectionCriteria
        {
            get { return showSelectionCriteria; }
            set { showSelectionCriteria = value; NotifyOfPropertyChange(() => ShowSelectionCriteria); }
        }


        public new void Refresh() // FIXME TODO "new" keyword missing?
        {
            ThreadPool.QueueUserWorkItem(delegate
                                             {

                                                 Service.TriggerRefreshAllPois();
                                             });

        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            if (highlighter == null || service == null) return;
            ShowThresholdDoubleValue = (Highlighter.HighlighterType == HighlighterTypes.FilterThreshold &&
                                        Highlighter.ValueType == ValueTypes.Number);
            ShowSymbolSize = Highlighter.VisualType == VisualTypes.SymbolSize;
            ShowThreshholdType = Highlighter.HighlighterType == HighlighterTypes.FilterThreshold;
            ShowColorRange = (Highlighter.HighlighterType == HighlighterTypes.Highlight) &&
                              (Highlighter.VisualType == VisualTypes.StrokeColor ||
                              Highlighter.VisualType == VisualTypes.FillColor);

            ShowSelectionCriteria = false;

            if (Highlighter.Palette != null) Gradient = Highlighter.Palette.GetBrush();

            var eventAsObservable = Observable.FromEventPattern<HighlightEventArgs>(ev => HighlightChanged += ev, ev => HighlightChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(200)).Subscribe(_ => UpdateFocusTime());

            Highlighter.PropertyChanged += HighlighterPropertyChanged;
        }

        public void UpdateFocusTime()
        {
            Refresh();
        }

        void HighlighterPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (HighlightChanged != null) HighlightChanged(this, null);

        }




    }

    public class StyleViewModelConverter : IMultiValueConverter
    {


        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new StyleViewModel() { Highlighter = (Highlight)values[0], Service = (PoiService)values[1] };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}