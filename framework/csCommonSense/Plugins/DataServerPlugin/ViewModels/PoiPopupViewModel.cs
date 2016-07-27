#region

using Caliburn.Micro;
using csDataServerPlugin.ViewModels;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#endregion

namespace csDataServerPlugin
{
    public interface IPoiPopup
    {
    }


    /// <summary>
    ///     Interaction logic for twowaysliding.xaml
    /// </summary>
    [Export(typeof(IPoiPopup))]
    public class PoiPopupViewModel : Screen
    {
        private BindableCollection<Event> eventTypes;
        private bool isNewPoi;
        private dsLayer layer;

        private ObservableCollection<MetaLabel> metaLabels;
        private BaseContent poI;
        private PoiService service;
        //private string streetViewImage;

        private ObservableCollection<CallOutAction> actions = new ObservableCollection<CallOutAction>();

        public ObservableCollection<CallOutAction> Actions
        {
            get { return actions; }
            set
            {
                actions = value;
                NotifyOfPropertyChange(() => Actions);
            }
        }

        public ObservableCollection<MetaLabel> MetaLabels
        {
            get { return metaLabels; }
            set
            {
                metaLabels = value;
                NotifyOfPropertyChange(() => MetaLabels);
            }
        }

        public dsLayer Layer
        {
            get { return layer; }
            set
            {
                layer = value;
                NotifyOfPropertyChange(() => Layer);
            }
        }

        //public string StreetViewImage
        //{
        //    get { return streetViewImage; }
        //    set
        //    {
        //        streetViewImage = value;
        //        NotifyOfPropertyChange(() => StreetViewImage);
        //    }
        //}

        public DataServerPlugin Plugin { get; set; }

        public BaseContent PoI
        {
            get { return poI; }
            set
            {
                poI = value;
                NotifyOfPropertyChange(() => PoI);
            }
        }

        public BindableCollection<Event> EventTypes
        {
            get { return eventTypes; }
            set
            {
                eventTypes = value;
                NotifyOfPropertyChange(() => EventTypes);
            }
        }

        public List<BaseContent> PoiEvents
        {
            get { return Layer.Service.Events.Where(k => ((Event)k).PoiId == PoI.Id).ToList(); }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
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

        /// <summary>
        ///     Notifies this poi has not been saved, a save button will be added with the option to add it to the layer
        /// </summary>
        public bool IsNewPoi
        {
            get { return isNewPoi; }
            set
            {
                isNewPoi = value;
                NotifyOfPropertyChange(() => IsNewPoi);
            }
        }

        /// <summary>
        ///     reference to existing floating element container
        /// </summary>
        public FloatingElement FeElement { get; set; }

        public void SavePoi()
        {
            Layer.Service.PoIs.Add(PoI);
            Layer.Service.UpdateContentList();
            if (FeElement != null)
            {
                AppState.FloatingItems.RemoveFloatingElement(FeElement);
            }
        }

        private static MenuPopupViewModel GetMenu(FrameworkElement fe)
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
            return menu;
        }

        public void SelectFocus(MetaLabel ml, FrameworkElement b)
        {
            ml.Data = AppStateSettings.Instance.TimelineManager.FocusTime.ToString();
            //ml.PoI.Labels[ml.Meta.Label] = ml.Data;
        }

        public void SelectOption(MetaLabel ml, FrameworkElement b)
        {
            var m = GetMenu(b);
            foreach (var o in ml.Meta.Options)
            {
                var mi = m.AddMenuItem(o);
                mi.Click += (e, s) =>
                {
                    ml.Data = o;
                    ml.PoI.Labels[ml.Meta.Label] = o;
                };
            }

            AppState.Popups.Add(m);
        }

        protected override void OnViewLoaded(object view)
        {
            //base.OnViewLoaded(view);
            //Service = Layer.Service;
            //MetaLabels = PoI.GetMetaLabels();
            //Layer.Service.Events.CollectionChanged += (e, f) => NotifyOfPropertyChange(() => PoiEvents);
            //PoI.PropertyChanged += PoI_PropertyChanged;
            //PoI.LabelChanged += (e, s) => UpdateLabels();
            //UpdateLabels();
            //UpdateEvents();
            //if (PoI.AllMedia.Count <= 0) return;
            //foreach (var m in PoI.AllMedia)
            //{
            //    if (m.Content == null) m.Content = PoI;
            //    if (m.Content.Service == null) m.Content.Service = Service;
            //}
            //StreetViewImage = @"c:\temp\streetview.jpg";

            base.OnViewLoaded(view);
            if (PoI == null) return;
            UpdateCalloutActions();
            UpdateCallOut();
            PoI.PropertyChanged += Poi_PropertyChanged;
            PoI.LabelChanged += Poi_LabelChanged;
            if (PoI.UpdateSensorData) AppState.TimelineManager.TimeContentChanged += TimelineManager_FocusTimeUpdated;
        }

        private void Poi_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach (var m in Sections.OfType<MetaLabelsViewModel>().SelectMany(mlvm => mlvm.Content).Where(m => string.Equals(m.Meta.Label, e.PropertyName)))
            {
                m.Refresh();
            }
        }

        private readonly object _lock = new object();

        public void StartEditing()
        {
            Actions.Remove(Actions.FirstOrDefault(action => string.Equals(action.Title, "Edit", StringComparison.InvariantCultureIgnoreCase)));
            //if (!Pinned) Pin();
            //OnEditing();
        }

        public void TapAction(CallOutAction action, EventArgs e)
        {
            if (action != null) action.TriggerClicked(e);
        }

        public void Drag(object sender, CallOutAction context, EventArgs e)
        {
            if (context.IsDraggable)
            {
                context.TriggerDragStart(sender, e);
            }
        }

        private void UpdateCalloutActions()
        {
            var effStyle = PoI.NEffectiveStyle;
            if (effStyle.CanEdit.Value)
            {
                var editCallout = new CallOutAction
                {
                    IconBrush = new SolidColorBrush(effStyle.CallOutForeground.Value),
                    Title = "Edit",
                    Path =
                        "M0,44.439791L18.98951,54.569246 0.47998798,62.66881z M17.428029,12.359973L36.955557,23.568769 21.957478,49.686174 20.847757,46.346189 15.11851,45.756407 14.138656,42.166935 8.5292659,41.966761 6.9493899,38.037481 2.4399572,38.477377z M26.812517,0.0009765625C27.350616,-0.012230873,27.875986,0.10826397,28.348372,0.3782568L42.175028,8.3180408C43.85462,9.2780154,44.234529,11.777948,43.02482,13.89789L41.375219,16.767812 21.460039,5.3381228 23.10964,2.4582005C23.979116,0.941679,25.437378,0.034730911,26.812517,0.0009765625z"
                };
                editCallout.Clicked += (e, f) => StartEditing();
                Actions.Add(editCallout);
            }

            // Lock the PoI
            var canMoveCallout = new CallOutAction
            {
                IconBrush = new SolidColorBrush(effStyle.CallOutForeground.Value),
                Title = "Lock",
                Path = effStyle.CanMove.Value
                    ? "F1M648.2778,1043.3809L648.2778,1047.4329 645.6158,1047.4329 645.6158,1043.3839C644.5488,1042.9079 643.7998,1041.9019 643.7998,1040.7169 643.7998,1039.0759 645.2068,1037.7479 646.9428,1037.7479 648.6858,1037.7479 650.0938,1039.0759 650.0938,1040.7169 650.0938,1041.8989 649.3458,1042.9079 648.2778,1043.3809 M654.3988,1031.2069C654.3988,1031.2009,654.3998,1031.1959,654.4008,1031.1899L651.2988,1031.1529 641.3268,1031.1529 640.3338,1028.5859C639.1988,1025.6569 640.6488,1022.3669 643.5788,1021.2339 646.5088,1020.0989 649.7988,1021.5549 650.9328,1024.4789L652.0168,1027.2809C652.2178,1027.8009,652.3348,1028.3359,652.3778,1028.8669L654.4728,1028.8909C654.3998,1028.2769,654.2548,1027.6609,654.0208,1027.0559L652.5638,1023.2979C651.0408,1019.3659 646.6198,1017.4139 642.6888,1018.9379 638.7598,1020.4589 636.8068,1024.8789 638.3278,1028.8109L639.3428,1031.4309C637.3868,1032.1059,635.9778,1033.9609,635.9778,1036.1469L635.9778,1046.2999C635.9778,1049.0579,638.2148,1051.2949,640.9708,1051.2949L653.7078,1051.2949C656.4668,1051.2949,658.7008,1049.0579,658.7008,1046.2999L658.7008,1036.1469C658.7008,1033.6249,656.8298,1031.5439,654.3988,1031.2069"
                    : "F1M339.3071,1185.249L339.3071,1188.437 337.2111,1188.437 337.2111,1185.251C336.3721,1184.876 335.7831,1184.085 335.7831,1183.151 335.7831,1181.861 336.8901,1180.815 338.2561,1180.815 339.6281,1180.815 340.7371,1181.861 340.7371,1183.151 340.7371,1184.082 340.1471,1184.876 339.3071,1185.249 M331.6851,1168.456C331.6851,1165.017 334.4711,1162.228 337.9101,1162.228 341.3491,1162.228 344.1411,1165.017 344.1411,1168.456L344.1411,1171.745C344.1411,1172.16,344.0991,1172.565,344.0211,1172.959L331.8051,1172.959C331.7281,1172.565,331.6851,1172.16,331.6851,1171.745z M346.2351,1173.133C346.2611,1172.861,346.2761,1172.586,346.2761,1172.308L346.2761,1167.893C346.2761,1163.274 342.5291,1159.528 337.9101,1159.528 333.2921,1159.528 329.5511,1163.274 329.5511,1167.893L329.5511,1172.308C329.5511,1172.586 329.5661,1172.861 329.5921,1173.132 327.2211,1173.733 325.4651,1175.875 325.4651,1178.432L325.4651,1189.558C325.4651,1192.578,327.9121,1195.028,330.9361,1195.028L344.8901,1195.028C347.9091,1195.028,350.3601,1192.578,350.3601,1189.558L350.3601,1178.432C350.3601,1175.876,348.6031,1173.733,346.2351,1173.133"
            };
            canMoveCallout.Clicked += (e, f) =>
            {
                if (PoI.Style == null)
                {
                    PoI.Style = new PoIStyle { CanMove = !effStyle.CanMove };
                }
                else PoI.Style.CanMove = !effStyle.CanMove;
                if (PoI.Style.CanMove == true && (effStyle.DrawingMode == DrawingModes.Polygon || effStyle.DrawingMode == DrawingModes.Polyline))
                    (PoI.Data["graphic"] as Graphic).MakeDraggable();

                PoI.TriggerLabelChanged("", "", "");
                PoI.UpdateEffectiveStyle();
            };
            Actions.Add(canMoveCallout);

            if (effStyle.CanRotate.Value &&
                (PoI.NEffectiveStyle.DrawingMode.Value == DrawingModes.Point ||
                 PoI.NEffectiveStyle.DrawingMode.Value == DrawingModes.Image))
            {
                var rotateCallOutAction = new CallOutAction
                {
                    IconBrush = new SolidColorBrush(PoI.NEffectiveStyle.CallOutForeground.Value),
                    Title = "Rotate",
                    Path = "F1M225.713,1773.49L232.795,1776.66 231.995,1768.94 231.192,1761.23 226.002,1764.99C221.113,1758.99 213.677,1755.15 205.337,1755.15 190.61,1755.15 178.672,1767.1 178.672,1781.82 178.672,1796.55 190.61,1808.49 205.337,1808.49 211.902,1808.49 217.903,1806.11 222.543,1802.17 222.573,1802.11 222.593,1802.06 222.627,1801.99 224.257,1798.82 220.791,1798.99 220.781,1798.99 216.686,1802.68 211.271,1804.93 205.337,1804.93 192.595,1804.93 182.228,1794.56 182.228,1781.82 182.228,1769.08 192.595,1758.71 205.337,1758.71 212.481,1758.71 218.867,1761.98 223.106,1767.09L218.631,1770.33 225.713,1773.49z"
                };
                rotateCallOutAction.Clicked += (e, f) => PoI.StartRotation();
                Actions.Add(rotateCallOutAction);
            }
            if (effStyle.CanDelete.Value)
            {
                var deleteCalloutAction = new CallOutAction
                {
                    IconBrush = new SolidColorBrush(PoI.NEffectiveStyle.CallOutForeground.Value),
                    Title = "Delete",
                    Path =
                        "M33.977998,27.684L33.977998,58.102997 41.373998,58.102997 41.373998,27.684z M14.841999,27.684L14.841999,58.102997 22.237998,58.102997 22.237998,27.684z M4.0319996,22.433001L52.183,22.433001 52.183,63.999001 4.0319996,63.999001z M15.974,0L40.195001,0 40.195001,7.7260003 56.167001,7.7260003 56.167001,16.000999 0,16.000999 0,7.7260003 15.974,7.7260003z"
                };
                deleteCalloutAction.Clicked += (e, f) =>
                {
                    var nea = new NotificationEventArgs
                    {
                        Text       = "Are you sure?",
                        Header     = "Delete " + PoI.Name,
                        Duration   = new TimeSpan(0, 0, 30),
                        Background = Brushes.Red,
                        Image      = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/Delete.png")),
                        Foreground = Brushes.White,
                        Options    = new List<string> { "Yes", "No" }
                    };
                    nea.OptionClicked += (s, n) =>
                    {
                        if (n.Option != "Yes") return;
                        AppState.TriggerNotification(PoI.Name + " was deleted", pathData: MenuHelpers.DeleteIcon);
                        Service.RemovePoi(PoI);
                        TryClose();
                    };
                    AppState.TriggerNotification(nea);
                };
                Actions.Add(deleteCalloutAction);
            }
            foreach (var action in PoI.CalloutActions)
            {
                Actions.Add(action);
            }
        }


        private void TimelineManager_FocusTimeUpdated(object sender, csShared.Interfaces.TimeEventArgs e)
        {
            //UpdateCallOut();
            lock (_lock)
                UpdateSensorMetaLabels();
        }

        private void Poi_LabelChanged(object sender, System.EventArgs e)
        {
            var mll = PoI.GetMetaLabels();
            var ml = mll.FirstOrDefault(k => k.Meta.Label == ((LabelChangedEventArgs)e).Label);
            if (ml != null && !ml.Meta.EditActive)
                UpdateCallOut();
        }

        public void UpdateSensorMetaLabels()
        {
            Execute.OnUIThread(() =>
            {
                if (PoI == null) return;

                var mll = PoI.GetMetaLabels();
                if (mll == null) return;

                foreach (var ml in mll.Where(ml => ml.Meta.VisibleInCallOut).Where(ml => ml.Sensor != null))
                {
                    ml.Sensor.SetFocusDate(AppState.TimelineManager.FocusTime);
                }
            });
        }

        private void UpdateEvents()
        {
            EventTypes = new BindableCollection<Event>();
            foreach (Event e in Layer.Service.EventTypes) EventTypes.Add(e);

            //PoiEvents = new BindableCollection<Event>();
            //foreach(Event e in Layer.Service.Events.Where(k=>((Event)k).PoiId == PoI.Id)) PoiEvents.Add(e);
        }

        public void AddEvent(Event eventType)
        {
            if (eventType == null) return;
            var e = new Event
            {
                Name = eventType.Name,
                PoiTypeId = eventType.ContentId,
                Date = DateTime.Now,
                Position = PoI.Position,
                PoiType = eventType,
                PoiId = PoI.Id,
                Icon = eventType.Icon
            };
            Layer.Service.Events.Add(e);
            NotifyOfPropertyChange(() => PoiEvents);
        }

        private void UpdateLabels()
        {
            var mtl = PoI.GetMetaLabels();
            if (mtl == null) return;
            foreach (var mtli in mtl)
            {
                var mi = MetaLabels.FirstOrDefault(k => k.Meta.Label == mtli.Meta.Label);
                if (mi != null) mi.Data = mtli.Data;
            }
        }

        private void PoI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateLabels();
        }

        private BindableCollection<IEditableScreen> sections;
        private IEditableScreen selectedSection;

        public IEditableScreen SelectedSection
        {
            get { return selectedSection; }
            set
            {
                if (selectedSection == value) return;
                selectedSection = value;
                NotifyOfPropertyChange(() => SelectedSection);
            }
        }

        public BindableCollection<IEditableScreen> Sections
        {
            get { return sections; }
            set
            {
                sections = value;
                NotifyOfPropertyChange(() => Sections);
            }
        }

        public void UpdateCallOut()
        {
            Execute.OnUIThread(() =>
            {
                //Todo:Jeroen - Properly fix update changes (also remove labels/sections instead of add only)
                //If there is no poi, there is no info to collect! (Nothing to show here.. moving on)
                if (PoI == null) return;

                //Collect all info
                var defaultSection = new MetaLabelsViewModel
                {
                    DisplayName = "Info",
                    //CallOut = CallOut,
                    Content = new BindableCollection<MetaLabel>()
                };
                var dict = new Dictionary<string, MetaLabelsViewModel> { { defaultSection.DisplayName, defaultSection } };

                //Get all labels from the poi
                var mll = PoI.GetMetaLabels();
                if (mll == null) return;
                foreach (var ml in mll)
                {
                    //If it is not visible, skip it!
                    if (!ml.Meta.VisibleInCallOut) continue;
                    //Get the section/category for the label
                    var sectionLabel = ml.Meta.Section;
                    
                    ml.Meta.InEditMode = isNewPoi;
                    if (string.IsNullOrEmpty(sectionLabel))
                        sectionLabel = "Info";

                    if (!dict.ContainsKey(sectionLabel))
                        dict[sectionLabel] = new MetaLabelsViewModel
                        {
                            DisplayName = sectionLabel,
                            //CallOut = CallOut, 
                            Content = new BindableCollection<MetaLabel>()
                        };
                    //Add label content
                    dict[sectionLabel].Content.Add(ml);
                }

                // Remove the default section if it is empty.
                if (defaultSection.Content.Count == 0) dict.Remove(defaultSection.DisplayName);

                //Propagate changes
                //Check if Sections exists
                if (Sections == null)
                    Sections = new BindableCollection<IEditableScreen>();

                //Propagate section changes
                var addSections = dict.Values.Where(k => Sections.All(g => g.DisplayName != k.DisplayName)).ToList();
                var remSections = Sections.Where(k => dict.Values.All(g => g.DisplayName != k.DisplayName)).ToList();
                foreach (var sect in remSections)
                    Sections.Remove(sect);
                foreach (var sect in addSections)
                    Sections.Add(sect);

                //propagate label changes
                foreach (var sect in Sections.OfType<MetaLabelsViewModel>().ToList())
                {
                    //foreach (var content in dict[sect.DisplayName].Content)
                    //{
                    //    var cont = sect.Content.FirstOrDefault(k => k.Meta.Title == content.Meta.Title);
                    //    if (cont != null)
                    //    {

                    //    }
                    //}
                    var addcontent = dict[sect.DisplayName].Content.Where(k => sect.Content.All(g => g.Meta.Title != k.Meta.Title)).ToList();
                    var remcontent = sect.Content.Where(k => dict[sect.DisplayName].Content.All(g => g.Meta.Title != k.Meta.Title)).ToList();
                    var updcontent = sect.Content.Where(k => dict[sect.DisplayName].Content.Any(g => g.Meta.Title == k.Meta.Title)).ToList();
                    foreach (var r in remcontent)
                        sect.Content.Remove(r);
                    foreach (var a in addcontent)
                        sect.Content.Add(a);
                    foreach (var u in updcontent)
                    {
                        var idx = sect.Content.IndexOf(u);
                        sect.Content.RemoveAt(idx);
                        sect.Content.Insert(idx, u);
                    }
                }

                //var mll = Poi.GetMetaLabels();
                //if (mll == null) return;
                //foreach (var ml in mll)
                //{
                //    if (!ml.Meta.VisibleInCallOut) continue;
                //    var sectionLabel = ml.Meta.Section;
                //    if (string.IsNullOrEmpty(sectionLabel))
                //        sectionLabel = "Info";

                //    var section = Sections.FirstOrDefault(k => k.DisplayName == sectionLabel);
                //    if (section == null)
                //    {
                //        section = new MetaLabelsViewModel()
                //        {
                //            DisplayName = sectionLabel,
                //            CallOut = CallOut,
                //            Content = new BindableCollection<MetaLabel> {}
                //        };
                //        Sections.Add(section);
                //    }

                //    var content = (section as MetaLabelsViewModel).Content.FirstOrDefault(k=>k.Meta.Title == ml.Meta.Title);
                //    if (content == null)
                //        (section as MetaLabelsViewModel).Content.Add(ml);
                //    else
                //    {
                //        var idx = (section as MetaLabelsViewModel).Content.IndexOf(content);
                //        (section as MetaLabelsViewModel).Content.RemoveAt(idx);
                //        (section as MetaLabelsViewModel).Content.Insert(idx,ml);
                //    }
                //}

                //                var dict = new Dictionary<string, MetaLabelsViewModel> { { defaultSection.DisplayName, defaultSection } };
                //foreach (var ml in mll)
                //{
                //    if (!ml.Meta.VisibleInCallOut) continue;
                //    //if (!ml.Meta.VisibleInCallOut || Poi.NEffectiveStyle.NameLabel == ml.Meta.Label) continue;
                //    var sectionLabel = ml.Meta.Section;
                //    if (string.IsNullOrEmpty(sectionLabel))
                //        defaultSection.Content.Add(ml);
                //    else
                //    {
                //        if (dict.ContainsKey(sectionLabel)) dict[sectionLabel].Content.Add(ml);
                //        else dict[sectionLabel] = new MetaLabelsViewModel
                //        {
                //            DisplayName = sectionLabel,
                //            CallOut = CallOut,
                //            Content = new BindableCollection<MetaLabel> { ml }
                //        };
                //    }
                //    Console.WriteLine("label:" + ml.Meta.Title +" - " + ml.Data);
                //}
                //foreach (var section in dict.Values.Where(section => section.Content != null && section.Content.Count != 0))
                //{
                //    Sections.Add(section);
                //}

                var vms = PoI.ModelInstances.Values.Where(k => k.ViewModel != null).Select(k => k.ViewModel);
                foreach (var vm in vms)
                {
                    //vm.CallOut = CallOut;
                    Sections.Add(vm);
                }

                foreach (var mf in PoI.GetMetaLabels().Where(k => k.Meta.Type == MetaTypes.mediafolder))
                {
                    var mvm = new MediaViewModel
                    {
                        //CallOut = CallOut, 
                        Label = mf,
                        DisplayName = mf.Meta.Section
                    };
                    Sections.Add(mvm);

                }
            });
        }

    }
}