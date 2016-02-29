using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using DataServer;
using PoiServer.PoI;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace csDataServerPlugin
{
    public class HighlighterViewModel : Screen
    {
        private Brush accentBrush;
        private bool expanded;
        private GradientBrush gradient;
        private Highlight highlighter;
        private PoiService service;
        private bool showColorRange;
        private bool showSelectionCriteria;
        private bool showSymbolSize;
        private bool showThreshholdType;
        private bool showThresholdDoubleValue;
        private bool showVisualType;
        private PoIStyle style;

        public Visibility ShowStringValue
        {
            get
            {
                return (highlighter.ValueType == ValueTypes.String)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        public Visibility ShowNumberValue
        {
            get
            {
                return (highlighter.ValueType == ValueTypes.Number)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
        
        

        public Brush AccentBrush
        {
            get { return accentBrush; }
            set
            {
                accentBrush = value;
                NotifyOfPropertyChange(() => AccentBrush);
            }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public bool Expanded
        {
            get { return expanded; }
            set
            {
                expanded = value;
                NotifyOfPropertyChange(() => Expanded);
            }
        }

        public GradientBrush Gradient
        {
            get { return gradient; }
            set
            {
                gradient = value;
                NotifyOfPropertyChange(() => Gradient);
            }
        }

        public Highlight Highlighter
        {
            get { return highlighter; }
            set
            {
                highlighter = value;
                NotifyOfPropertyChange(() => Highlighter);
            }
        }

        public PoiService Service
        {
            get { return service; }
            set { service = value; }
        }

        public bool ShowColorRange
        {
            get { return showColorRange; }
            set
            {
                showColorRange = value;
                NotifyOfPropertyChange(() => ShowColorRange);
            }
        }

        public bool ShowSelectionCriteria
        {
            get { return showSelectionCriteria; }
            set
            {
                showSelectionCriteria = value;
                NotifyOfPropertyChange(() => ShowSelectionCriteria);
            }
        }

        public bool ShowSymbolSize
        {
            get { return showSymbolSize; }
            set
            {
                showSymbolSize = value;
                NotifyOfPropertyChange(() => ShowSymbolSize);
            }
        }

        public bool ShowThreshholdType
        {
            get { return showThreshholdType; }
            set
            {
                showThreshholdType = value;
                NotifyOfPropertyChange(() => ShowThreshholdType);
            }
        }

        public bool ShowThresholdDoubleValue
        {
            get { return showThresholdDoubleValue; }
            set
            {
                showThresholdDoubleValue = value;
                NotifyOfPropertyChange(() => ShowThresholdDoubleValue);
            }
        }

        public bool ShowVisualType
        {
            get { return showVisualType; }
            set
            {
                showVisualType = value;
                NotifyOfPropertyChange(() => ShowVisualType);
            }
        }

        public PoIStyle Style
        {
            get { return style; }
            set
            {
                style = value;
                NotifyOfPropertyChange(() => Style);
            }
        }

        public event EventHandler<HighlightEventArgs> HighlightChanged;

        public void CalculateMax()
        {
            Highlighter.CalculateMax(Service);
        }

        public void CalculateMin()
        {
            Highlighter.CalculateMin(Service);
        }

        public void ChangeThresholdType(FrameworkElement button)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = button,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            var types = Enum.GetValues(typeof(ThresholdTypes));
            foreach (ThresholdTypes t in types)
            {
                var mi = menu.AddMenuItem(t.ToString());
                var t1 = t;
                mi.Click += (o, e) => { Highlighter.ThresholdType = t1; };
            }

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void ChangeVisualType(FrameworkElement button)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = button,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            var types = Enum.GetValues(typeof(VisualTypes));
            foreach (VisualTypes t in types)
            {
                var mi = menu.AddMenuItem(t.ToString());
                var t1 = t;
                mi.Click += (o, e) =>
                {
                    Highlighter.VisualType = t1;
                    Service.ResetFilters();
                };
            }

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void ChangeStringValue(FrameworkElement button)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = button,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            var options = new List<string>();
            foreach (var p in Service.PoIs.Where(k => ((PoI) k).PoiTypeId == Highlighter.PoiType))
            {
                if (p.Labels.ContainsKey(Highlighter.SelectionCriteria) &&
                    !options.Contains(p.Labels[Highlighter.SelectionCriteria]))
                {
                    options.Add(p.Labels[Highlighter.SelectionCriteria]);
                }
            }

            //var types = Enum.GetValues(typeof(VisualTypes));
            foreach (var t in options)
            {
                var mi = menu.AddMenuItem(t);
                var t1 = t;
                mi.Click += (o, e) =>
                {
                    Highlighter.StringValue = t1;
                    Refresh();
                };
            }

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public new void Refresh()
        {
            Service.TriggerRefreshAllPois();
            Service.TriggerContentChanged(null);
            Service.UpdateContentList();
        }

        public void Remove(FrameworkElement el)
        {
            if (Highlighter == null || Highlighter.Style == null) return;
            if (!Highlighter.Style.Analysis.Highlights.Contains(Highlighter)) return;
            Highlighter.CleanUp(service);
            Highlighter.IsActive = false;
            Highlighter.Style.Analysis.Highlights.Remove(Highlighter);
            AppState.TriggerScriptCommand(this, "HighlightersUpdated");
            Service.ResetFilters();
        }

        public void ToggleMCA()
        {
            if (Highlighter == null) return;
            Highlighter.InMcaMode = !Highlighter.InMcaMode;
        }

        public void SelectColor(FrameworkElement fe)
        {
            var menu = new PaletteSelectionViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(160, -150),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                AutoClose = true
            };
            menu.Saved += (e, f) => { Highlighter.Palette = f.Result; };

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void UpdateFocusTime()
        {
            Refresh();
        }

        public void HighLighterChanged()
        {
            Refresh();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var c = ((SolidColorBrush)AppStateSettings.Instance.AccentBrush).Color;
            c.A = 100;
            AccentBrush = new SolidColorBrush(c);

            UpdateUI();

            var eventAsObservable = Observable.FromEventPattern<HighlightEventArgs>(ev => HighlightChanged += ev, ev => HighlightChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(200)).Subscribe(_ => HighLighterChanged());

            UpdateUI();

            Highlighter.PropertyChanged += HighlighterPropertyChanged;
        }

        public void ChangeActive()
        {
            //Service.ResetFilters();
            if (Highlighter.MinValue == 0 && Highlighter.MaxValue == 0)
            {
                Highlighter.CalculateMin(Service);
                Highlighter.CalculateMin(Service);
            }
            // Will already be called from HighLighterChanged()
            //Refresh();
        }

        private void HighlighterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (HighlightChanged != null) HighlightChanged(this, null);
            UpdateUI();
            Service.TriggerContentChanged(null);
        }

        private void UpdateUI()
        {
            if (highlighter == null || service == null) return;
            ShowThresholdDoubleValue = (Highlighter.HighlighterType == HighlighterTypes.FilterThreshold &&
                                        Highlighter.ValueType == ValueTypes.Number);
            ShowSymbolSize = Highlighter.VisualType == VisualTypes.SymbolSize;
            ShowThreshholdType = Highlighter.HighlighterType == HighlighterTypes.FilterThreshold;
            ShowVisualType = Highlighter.HighlighterType == HighlighterTypes.Highlight;
            ShowColorRange = ShowVisualType &&
                             (Highlighter.VisualType == VisualTypes.StrokeColor ||
                              Highlighter.VisualType == VisualTypes.FillColor);

            ShowSelectionCriteria = false;


            if (Highlighter.Palette != null) Gradient = Highlighter.Palette.GetBrush();
        }

        public void MathFormulateUpdated()
        {
            service.ResetFilters();
        }

        public void UpdateMathFormula()
        {
            Highlighter.UpdateMathFormula();
        }

        public class HighlightEventArgs : EventArgs
        {
        }
    }
}