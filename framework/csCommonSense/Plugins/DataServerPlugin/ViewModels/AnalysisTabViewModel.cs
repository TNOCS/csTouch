#region

using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using csImb;
using csShared;
using csShared.Controls.Popups.InputPopup;
using csShared.Controls.Popups.MenuPopup;
using DataServer;
using Microsoft.Surface.Presentation.Controls;
using PoiServer.PoI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

#endregion

namespace csDataServerPlugin
{
    public class AnalysisTabViewModel : Screen
    {
        private SolidColorBrush accentBrush;
        private List<string> availableCategories;
        private ObservableCollection<ImbClientStatus> clients = new ObservableCollection<ImbClientStatus>();
        private BindableCollection<Highlight> highlights = new BindableCollection<Highlight>();
        private bool newHIghlightArea;
        private Highlight newHighlight = new Highlight();
        private string newMessage;
        private PoIStyle newStyle;
        private BindableCollection<BaseContent> poiTypesWithStyles;
        private string selectedCategory;

        private BindableCollection<ImbClientStatus> selectedClients = new BindableCollection<ImbClientStatus>();
        private PoiService service;

        public Visibility ShowVisualType
        {
            get
            {
                return (NewHighlight.HighlighterType == HighlighterTypes.Highlight)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        public ObservableCollection<ImbClientStatus> Clients
        {
            get { return clients; }
            set
            {
                clients = value;
                NotifyOfPropertyChange(() => Clients);
            }
        }

        public BindableCollection<ImbClientStatus> SelectedClients
        {
            get { return selectedClients; }
            set
            {
                selectedClients = value;
                NotifyOfPropertyChange(() => SelectedClients);
            }
        }

        public BindableCollection<BaseContent> PoiTypesWithStyles
        {
            get { return poiTypesWithStyles; }
            set
            {
                poiTypesWithStyles = value;
                NotifyOfPropertyChange(() => PoiTypesWithStyles);
            }
        }

        public BindableCollection<Highlight> Highlights
        {
            get { return highlights; }
            set { highlights = value; }
        }


        public PoiService Service
        {
            get { return service; }
            set
            {
                service = value;
                NotifyOfPropertyChange(() => Service);
            }
        }

        public string SelectedCategory
        {
            get { return selectedCategory; }
            set
            {
                //if (string.Equals(selectedCategory, value)) return;
                selectedCategory = value;

                Highlights.Clear();
                var hl = (from pt in PoiTypesWithStyles
                          from h in pt.Style.Analysis.Highlights
                          where h.Category == selectedCategory
                          select h);
                Highlights.AddRange(hl);

                foreach (var ps in PoiTypesWithStyles)
                {
                    foreach (var h in ps.NEffectiveStyle.Analysis.Highlights)
                    {
                        h.IsEnabled = h.Category == selectedCategory;
                    }
                }
                //Refresh();

                NotifyOfPropertyChange(() => SelectedCategory);
                NotifyOfPropertyChange(() => CanNextCategory);
                NotifyOfPropertyChange(() => CanPreviousCategory);
            }
        }

        public List<string> AvailableCategories
        {
            get { return availableCategories; }
            set
            {
                availableCategories = value;
                NotifyOfPropertyChange(() => AvailableCategories);
            }
        }

        public bool NewHighlightArea
        {
            get { return newHIghlightArea; }
            set
            {
                newHIghlightArea = value;
                NotifyOfPropertyChange(() => NewHighlightArea);
            }
        }

        public SortedObservableCollection<BaseContent> TimeLine
        {
            get
            {
                if (Service != null) return Service.TimeLine;
                return null;
            }
        }

        public DataServerBase Dsb { get; set; }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public PoIStyle NewStyle
        {
            get { return newStyle; }
            set
            {
                newStyle = value;
                NotifyOfPropertyChange(() => NewStyle);
            }
        }


        public string NewMessage
        {
            get { return newMessage; }
            set
            {
                newMessage = value;
                NotifyOfPropertyChange(() => NewMessage);
            }
        }

        public bool CanPreviousCategory
        {
            get { return AvailableCategories.Count > 1; }
        }

        public bool CanNextCategory
        {
            get { return AvailableCategories.Count > 1; }
        }

        public SolidColorBrush AccentBrush
        {
            get { return accentBrush; }
            set
            {
                accentBrush = value;
                NotifyOfPropertyChange(() => AccentBrush);
            }
        }

        public Highlight NewHighlight
        {
            get { return newHighlight; }
            set
            {
                newHighlight = value;
                NotifyOfPropertyChange(() => NewHighlight);
            }
        }

        public void Zoom(BaseContent p)
        {
            if (p.Position == null) return;
            AppState.ViewDef.ZoomAndPoint(new Point(p.Position.Longitude, p.Position.Latitude));
        }

        public void PreviousCategory()
        {
            if (AvailableCategories.Count > 1)
            {
                var c = AvailableCategories.IndexOf(SelectedCategory);
                c -= 1;
                if (c < 0) c = AvailableCategories.Count - 1;
                SelectedCategory = AvailableCategories[c];
            }
        }

        public void NextCategory()
        {
            if (AvailableCategories.Count <= 1) return;
            var c = AvailableCategories.IndexOf(SelectedCategory);
            c += 1;
            if (c >= AvailableCategories.Count) c = 0;
            SelectedCategory = AvailableCategories[c];
        }

        public bool CanChangeSource
        {
            get
            {
                return !string.IsNullOrEmpty(NewHighlight.PoiType);
            }
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            var c = ((SolidColorBrush)AppStateSettings.Instance.AccentBrush).Color;
            c.A = 100;
            AccentBrush = new SolidColorBrush(c);
            AppState.ScriptCommand += AppStateScriptCommand;
            UpdateCategories();
        }

        private void UpdateEnabled()
        {
            NotifyOfPropertyChange(() => CanChangeSource);
        }

        private void AppStateScriptCommand(object sender, string command)
        {
            if (command == "HighlightersUpdated")
            {
                UpdateCategories();
            }
        }

        private void UpdateCategories()
        {
            var oldcat = SelectedCategory;
            PoiTypesWithStyles = new BindableCollection<BaseContent>();
            PoiTypesWithStyles.AddRange(
                Service.PoITypes.Where(
                    k =>
                        k.Style != null && k.Style.Analysis != null && k.Style.Analysis.Highlights != null &&
                        k.Style.Analysis.Highlights.Any()).ToList());

            var b = (from pt in PoiTypesWithStyles
                     from h in pt.Style.Analysis.Highlights
                     group h by h.Category
                         into g
                         select new { Category = g.Key });

            AvailableCategories = b.Select(k => k.Category).ToList();
            if (!AvailableCategories.Any()) AvailableCategories.Add("Analysis");

            if (!AvailableCategories.Any()) return;
            SelectedCategory = AvailableCategories.Contains(oldcat) ? oldcat : AvailableCategories.First();
        }

        private static MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        public void ChangeSource(FrameworkElement el)
        {
            var menu = GetMenu(el);
            foreach (SelectionTypes p in Enum.GetValues(typeof(SelectionTypes)))
            {
                var mi = menu.AddMenuItem(p.ToString());
                var p1 = p;
                mi.Click += (o, e) =>
                {
                    NewHighlight.SelectionType = p1;
                    UpdateTitle();
                };
            }
            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void ChangeCriteria(FrameworkElement el)
        {
            if (string.IsNullOrEmpty(NewHighlight.PoiType)) return;
            var pt = Service.PoITypes.FirstOrDefault(k => k.ContentId == NewHighlight.PoiType);
            if (pt == null || pt.MetaInfo==null) return;
            var menu = GetMenu(el);
            foreach (var metaInfo in pt.MetaInfo)
            {
                var mi = menu.AddMenuItem(metaInfo.Title);
                var info = metaInfo;
                mi.Click += (o, e) =>
                {
                    NewHighlight.SelectionCriteria = info.Label;
                    NewHighlight.UpdateMultipleCriteria(pt);
                    NewHighlight.StringFormat = info.StringFormat;
                    NewHighlight.SelectionType = info.Type == MetaTypes.sensor
                        ? SelectionTypes.Sensor
                        : SelectionTypes.Label;
                    UpdateTitle();
                    CanAddAdditionalProperties = true;
                };
            }
            AppStateSettings.Instance.Popups.Add(menu);
        }

        /// <summary>
        /// Add additional criteria to the highlighter.
        /// Note that the first highlighter property determines the Label or Sensor nature, 
        /// and string format.
        /// </summary>
        /// <param name="el"></param>
        public void ExtendCriteria(FrameworkElement el)
        {
            if (string.IsNullOrEmpty(NewHighlight.PoiType)) return;
            var pt = Service.PoITypes.FirstOrDefault(k => k.ContentId == NewHighlight.PoiType);
            if (pt == null) return;
            var menu = GetMenu(el);
            foreach (var metaInfo in pt.MetaInfo)
            {
                var mi = menu.AddMenuItem(metaInfo.Title);
                var info = metaInfo;
                mi.Click += (o, e) =>
                {
                    var criteria = NewHighlight.SelectionCriteria.Split(new[] { ' ', '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (criteria.Contains(info.Label))
                    {
                        criteria.Remove(info.Label);
                        NewHighlight.SelectionCriteria = criteria.Count > 0
                            ? string.Join("\r\n", criteria.Select(x => x)).TrimEnd(new[] { '\r', '\n' })
                            : string.Empty;
                        //NewHighlight.UpdateMultipleCriteria(pt);
                        CanAddAdditionalProperties = !string.IsNullOrEmpty(NewHighlight.SelectionCriteria);
                    }
                    else
                    {
                        NewHighlight.SelectionCriteria += "\r\n" + info.Label;
                        //NewHighlight.UpdateMultipleCriteria(pt);
                    }
                };
            }
            AppStateSettings.Instance.Popups.Add(menu);
        }

        private bool canAddAdditionalProperties;
        /// <summary>
        /// Indicates whether a highlighter uses multiple PoI properties instead of just one.
        /// </summary>
        public bool CanAddAdditionalProperties
        {
            get { return canAddAdditionalProperties; }
            set
            {
                if (canAddAdditionalProperties == value) return;
                canAddAdditionalProperties = value;
                NotifyOfPropertyChange(() => CanAddAdditionalProperties);
            }
        }

        public void ChangeVisualType(FrameworkElement el)
        {
            var menu = GetMenu(el);
            foreach (VisualTypes p in Enum.GetValues(typeof(VisualTypes)))
            {
                var mi = menu.AddMenuItem(p.ToString());
                var p1 = p;
                mi.Click += (o, e) =>
                {
                    NewHighlight.VisualType = p1;
                    NotifyOfPropertyChange(() => ShowVisualType);
                    UpdateTitle();
                };
            }
            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void ChangeHighlighterType(FrameworkElement el)
        {
            var menu = GetMenu(el);
            foreach (HighlighterTypes p in Enum.GetValues(typeof(HighlighterTypes)))
            {
                var mi = menu.AddMenuItem(p.ToString());
                var p1 = p;
                mi.Click += (o, e) =>
                {
                    NewHighlight.HighlighterType = p1;
                    NotifyOfPropertyChange(() => ShowVisualType);
                    UpdateTitle();
                };
            }
            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void SelectPreset(FrameworkElement el)
        {
            var menu = GetMenu(el);

            foreach (var pt in Service.PoITypes.Where(k => k.Style != null && k.MetaInfo != null))
            {
                var ps = pt.Style;
                foreach (var ml in pt.MetaInfo)
                {
                    var h1 = new Highlight
                    {
                        Title = ml.Title + " (" + pt.Name + ")",
                        IsActive = false,
                        HighlighterType = HighlighterTypes.Highlight,
                        Palette = new Palette
                        {
                            Type = PaletteType.Gradient,
                            Stops = new List<PaletteStop> {
                                    new PaletteStop {
                                        Color = new Color {A = 255, R = 237, G = 248, B = 251},
                                        StopValue = 0
                                    },
                                    new PaletteStop {
                                        Color = new Color {A = 255, R = 0, G = 109, B = 44},
                                        StopValue = 1.0
                                    }
                                }
                        },
                        ValueType         = ValueTypes.Number,
                        MinValue          = ml.MinValue,
                        MaxValue          = ml.MaxValue,
                        StringFormat      = ml.StringFormat,
                        SelectionCriteria = ml.Label,
                        SelectionType     = SelectionTypes.Label,
                        VisualType        = VisualTypes.FillColor
                    };
                    if (ml.Type == MetaTypes.sensor) h1.SelectionType = SelectionTypes.Sensor;
                    if (ml.Type == MetaTypes.options || ml.Type == MetaTypes.boolean || ml.Type == MetaTypes.text) h1.ValueType = ValueTypes.String;

                    var mi1 = menu.AddMenuItem(h1.Title);
                    mi1.Click += (o, e) =>
                    {
                        NewStyle = ps;
                        NewHighlight = h1;
                        NewHighlight.Category = SelectedCategory;
                    };
                    var h2 = new Highlight
                    {
                        Title             = ml.Label + " Filter" + " (" + pt.Name + ")",
                        IsActive          = false,
                        HighlighterType   = HighlighterTypes.FilterThreshold,
                        ThresholdType     = ThresholdTypes.GreaterOrEqual,
                        ValueType         = ValueTypes.Number,
                        MinValue          = ml.MinValue,
                        MaxValue          = ml.MaxValue,
                        SelectionCriteria = ml.Label,
                        SelectionType     = SelectionTypes.Label,
                        StringFormat      = ml.StringFormat,
                        ThresHoldValue    = (ml.MinValue + ml.MinValue) / 2
                    };
                    if (ml.Type == MetaTypes.options)
                    {
                        h2.ValueType     = ValueTypes.String;
                        h2.ThresholdType = ThresholdTypes.Equal;
                    }
                    if (ml.Type == MetaTypes.sensor) h2.SelectionType = SelectionTypes.Sensor;
                    var mi2 = menu.AddMenuItem(h2.Title);
                    mi2.Click += (o, e) =>
                    {
                        NewStyle              = ps;
                        NewHighlight          = h2;
                        NewHighlight.Category = SelectedCategory;
                    };
                }
            }

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void ChangeNewStyle(FrameworkElement el)
        {
            var menu = GetMenu(el);

            foreach (var p in Service.PoITypes.Where(k => k.Style != null))
            {
                var mi = menu.AddMenuItem(p.ContentId);
                var p1 = p;
                mi.Click += (o, e) =>
                {
                    NewStyle             = p1.Style;
                    NewHighlight.PoiType = p1.ContentId;
                    NewStyle.Name        = p1.ContentId;
                    UpdateEnabled();

                    var pt = Service.PoITypes.FirstOrDefault(k => k.ContentId == p1.ContentId);
                    if (pt == null || pt.MetaInfo==null) return;
                    var i = pt.MetaInfo.FirstOrDefault();
                    if (i != null) NewHighlight.SelectionCriteria = i.Label;
                    UpdateTitle();
                };
            }

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void CategorySettings(FrameworkElement el)
        {
            var menu = GetMenu(el);
            var addCategory = menu.AddMenuItem("Add Category");
            addCategory.Click += (o, e) =>
            {
                var text = new InputPopupViewModel
                {
                    RelativeElement   = el,
                    RelativePosition  = new Point(35, -5),
                    TimeOut           = new TimeSpan(0, 0, 0, 15),
                    VerticalAlignment = VerticalAlignment.Top,
                    AutoClose         = true,
                    Title             = "Category Name"
                };
                text.Saved += (s, f) =>
                {
                    if (AvailableCategories.Contains(f.Result))
                    {
                        AppState.TriggerNotification("Category already exists");
                    }
                    else
                    {
                        NewHighlightArea          = true;
                        SelectedCategory          = f.Result;
                        NotifyOfPropertyChange(() => CanNextCategory);
                        NotifyOfPropertyChange(() => CanPreviousCategory);
                        SelectPreset(el);
                    }
                };
                AppState.Popups.Add(text);
            };

            var removeCategory = menu.AddMenuItem("Remove Category");
            removeCategory.Click += (o, e) =>
            {
                if (AvailableCategories.Count > 1)
                {
                    foreach (var p in poiTypesWithStyles)
                    {
                        if (p.Style == null || p.Style.Analysis == null) continue;
                        var tbd = p.Style.Analysis.Highlights.Where(k => k.Category == SelectedCategory).ToList();
                        if (!tbd.Any()) continue;
                        foreach (var d in tbd) p.Style.Analysis.Highlights.Remove(d);
                    }
                    UpdateCategories();
                    NotifyOfPropertyChange(() => CanNextCategory);
                    NotifyOfPropertyChange(() => CanPreviousCategory);
                }
                else
                {
                    AppState.TriggerNotification("Can not delete last category");
                }
            };

            var renameCategory = menu.AddMenuItem("Rename Category");
            renameCategory.Click += (o, e) =>
            {
                var text = new InputPopupViewModel
                {
                    RelativeElement = el,
                    RelativePosition = new Point(35, -5),
                    TimeOut = new TimeSpan(0, 0, 0, 15),
                    VerticalAlignment = VerticalAlignment.Top,
                    AutoClose = true,
                    Title = "New Name",
                    DefaultValue = SelectedCategory
                };
                text.Saved += (s, f) =>
                {
                    foreach (var r in from p in poiTypesWithStyles
                                      where p.Style != null && p.Style.Analysis != null
                                      from r in p.Style.Analysis.Highlights
                                      where r.Category == SelectedCategory
                                      select r)
                    {
                        r.Category = f.Result;
                    }
                    SelectedCategory = f.Result;
                    UpdateCategories();
                };
                AppState.Popups.Add(text);
            };

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void UpdateTitle()
        {
            if (NewHighlight != null) NewHighlight.UpdateTitle();
        }

        public void AddDashboardItem(FrameworkElement el)
        {
            var db = AppState.Dashboards.GetOrCreateActiveDashboard();
            var s = new ServiceDashboardItem()
            {
                Title = this.Service.Name,
                GridX = 20,
                GridY = 20,
                GridHeight = 12,
                GridWidth = 12,
                Dashboard = db,
                Service = Service,
                ServiceId = Service.Id.ToString()
            };
            s.ViewModel = new ServiceDashboardItemViewModel() {Item = s};
            db.DashboardItems.Add(s);

        }

        public void AddFilter(FrameworkElement el)
        {
            NewHighlightArea = true;
            NewHighlight = new Highlight();
            var pl = Service.PoITypes.FirstOrDefault(k => k.Style != null);
            if (pl == null) return;
            NewStyle = pl.Style;
            NewHighlight.Style = pl.Style;
            NewHighlight.PoiType = pl.ContentId;
            NewHighlight.VisualType = VisualTypes.FillColor;
            NewHighlight.ThresholdType = ThresholdTypes.Greater;
            NewHighlight.Category = SelectedCategory;
            if (pl.MetaInfo == null) return;
            var i = pl.MetaInfo.FirstOrDefault();
            if (i != null)
            {
                NewHighlight.SelectionCriteria = i.Label;
                NewHighlight.StringFormat = i.StringFormat;
                NewHighlight.SelectionType = (i.Type == MetaTypes.sensor)
                    ? SelectionTypes.Sensor
                    : SelectionTypes.Label;
            }
            NewStyle.Name = pl.ContentId;
            NewHighlight.Palette = new Palette
            {
                Type = PaletteType.Gradient,
                Stops = new List<PaletteStop> {
                        new PaletteStop {
                            Color = new Color {A = 255, R = 237, G = 248, B = 251},
                            StopValue = 0
                        },
                        new PaletteStop {
                            Color = new Color {A = 255, R = 0, G = 109, B = 44},
                            StopValue = 1.0
                        }
                    }
            };
            NewHighlight.ValueType = ValueTypes.Number;
            NotifyOfPropertyChange(() => ShowVisualType);
        }

        public void StartWizard(FrameworkElement el)
        {
            SelectPreset(el);
        }

        public void SaveHighlighter()
        {
            var pt = Service.PoITypes.FirstOrDefault(k => k.ContentId == NewHighlight.PoiType);
            if (pt == null) return;
            NewHighlight.UpdateMultipleCriteria(pt);
            if (NewStyle != null && NewHighlight != null) {
                NewHighlight.Style = NewStyle;
                NewHighlight.PropertyChanged += NewHighlight_PropertyChanged;
                NewHighlight.IsActive = true;
                if (!newHighlight.UsesMultipleCriteria && newHighlight.SelectionType == SelectionTypes.Label && !string.IsNullOrEmpty(newHighlight.SelectionCriteria))
                {
                    var l = pt.MetaInfo.FirstOrDefault(k => k.Label == newHighlight.SelectionCriteria);
                    if (l.Type == MetaTypes.text)
                    {
                        newHighlight.ValueType = ValueTypes.String;
                    }
                }
                if (NewStyle.Analysis == null) {
                    NewStyle.Analysis = new AnalysisMetaInfo { Highlights = new List<Highlight>() };
                }

                NewStyle.Analysis.Highlights.Add(NewHighlight);
            }
            if (NewHighlight != null) {
                NewHighlight.CalculateMax(Service);
                NewHighlight.CalculateMin(Service);
                NewHighlight.CalculateCategories(Service);
            }
            NewHighlightArea = false;
            UpdateCategories();
            Service.TriggerRefreshAllPois();
            //Service.TriggerContentChanged(null);
        }

        void NewHighlight_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive")
            {
                Service.TriggerContentChanged(null);
            }
        }

        public void CancelHighlighter()
        {
            NewHighlightArea = false;
        }

        public new void Refresh()
        {
            //foreach (var p in Service.PoIs)
            //{
            //    p.UpdateAnalysisStyle();
            //    p.TriggerUpdated();
            //}
        }

        public void SendTo(FrameworkElement sender)
        {
            Clients.Clear();
            foreach (var a in AppState.Imb.Clients.Values.Where(k => k.Client)) Clients.Add(a);
        }

        public void SelectionChanged(SurfaceListBox s)
        {
            SelectedClients.Clear();
            foreach (ImbClientStatus c in s.SelectedItems) SelectedClients.Add(c);
        }

        public void SendMessage()
        {
            if (string.IsNullOrEmpty(NewMessage)) return;
            var e = Service.CreateEvent("Message");
            e.Labels["Text"] = NewMessage;
            if (SelectedClients.Count > 0)
            {
                e.Labels["To"] = string.Join<string>(",", SelectedClients.Select(k => k.Name));
            }
            e.UserId = AppState.Imb.Status.Name;
            e.Name = NewMessage;
            Service.Events.Add(e);
            NewMessage = "";
        }
    }
}