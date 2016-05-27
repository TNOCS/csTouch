using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using DataServer;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace csDataServerPlugin.ViewModels
{
    public class PoiCallOutViewModel : Screen
    {
        private BindableCollection<IEditableScreen> sections;
        private BaseContent poi;
        public DataServerBase Dsb { get; set; }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public MapCallOutViewModel CallOut
        {
            get { return callOut; }
            set
            {
                if (Equals(callOut, value)) return;
                callOut = value;
                if (callOut != null)
                {
                    callOut.Editing += (sender, args) =>
                    {
                        callOut.AutoClose = false;
                        InEditMode = true;
                        if (Sections == null) return;
                        foreach (var section in Sections.OfType<MetaLabelsViewModel>().ToList())
                        {
                            setEditMode(section, true);
                        }
                    };
                }
                NotifyOfPropertyChange(() => CallOut);
            }
        }

        private void setEditMode(MetaLabelsViewModel section, bool editMode)
        {
            section.CanEdit = editMode;
            foreach (var metaLabel in section.Content)
            {
                if (!metaLabel.Meta.IsEditable) continue;
                metaLabel.Meta.InEditMode = editMode;
                var poiMl = metaLabel.PoI.GetMetaLabels().First(ml => ml.Meta.Label == metaLabel.Meta.Label);
                if (poiMl != null)
                {
                    poiMl.Meta.EditActive = editMode;
                }
            }
        }

        public BaseContent Poi
        {
            get { return poi; }
            set
            {
                poi = value;
                NotifyOfPropertyChange(() => Poi);
            }
        }

        public Brush Foreground
        {
            get
            {
                if (Poi != null && Poi.NEffectiveStyle.CallOutForeground.HasValue) return new SolidColorBrush(Poi.NEffectiveStyle.CallOutForeground.Value);
                return Brushes.Black;
            }
        }

        public IEditableScreen SelectedSection
        {
            get { return selectedSection; }
            set
            {
                if (selectedSection == value) return;
                selectedSection = value;
                if (callOut != null) callOut.ResetTimer();
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

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            if (Poi == null) return;
            UpdateCallOut();
            Poi.PropertyChanged += Poi_PropertyChanged;
            Poi.LabelChanged += Poi_LabelChanged;
            if (Poi.UpdateSensorData)
                AppState.TimelineManager.TimeContentChanged += TimelineManager_FocusTimeUpdated;

            
        }

        private void Poi_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach (var m in Sections.OfType<MetaLabelsViewModel>().SelectMany(mlvm => mlvm.Content).Where(m => string.Equals(m.Meta.Label, e.PropertyName)))
            {
                m.Refresh();
            }
        }

        private readonly object _lock = new object();


        private void TimelineManager_FocusTimeUpdated(object sender, csShared.Interfaces.TimeEventArgs e)
        {
            //UpdateCallOut();
            lock (_lock)
                UpdateSensorMetaLabels();
        }

        private void Poi_LabelChanged(object sender, System.EventArgs e)
        {
            var lcArgs = (LabelChangedEventArgs) e;
            if ((lcArgs.OldValue == lcArgs.NewValue) && !string.IsNullOrWhiteSpace(lcArgs.Label)) return;
            var mll = Poi.GetMetaLabels();
            var ml = mll.FirstOrDefault(k => k.Meta.Label == ((LabelChangedEventArgs)e).Label);
            if (string.IsNullOrWhiteSpace(lcArgs.Label) || (ml != null && !InEditMode && !ml.Meta.EditActive && Poi.NEffectiveStyle.TapMode != TapMode.CallOutEdit) )
                UpdateCallOut();
        }

        private bool isOpen = true;
        private MapCallOutViewModel callOut;
        private IEditableScreen selectedSection;

        public bool IsOpen
        {
            get { return isOpen; }
            set { isOpen = value; NotifyOfPropertyChange(() => IsOpen); }
        }


        public void Close()
        {
            Poi.LabelChanged -= Poi_LabelChanged;
            AppState.TimelineManager.FocusTimeUpdated -= TimelineManager_FocusTimeUpdated;
            AppState.TimelineManager.TimeContentChanged -= TimelineManager_FocusTimeUpdated;

            if (Sections == null) return;
            foreach (var section in Sections.OfType<MetaLabelsViewModel>().ToList())
            {
                setEditMode(section, false);
            }

            IsOpen = false;
        }

        public void UpdateSensorMetaLabels()
        {
            Execute.OnUIThread(() =>
            {
                if (Poi == null) return;

                var mll = Poi.GetMetaLabels();
                if (mll == null) return;

                foreach (var ml in mll)
                {
                    if (!ml.Meta.VisibleInCallOut) continue;
                    if (ml.Sensor != null)
                    {
                        ml.Sensor.SetFocusDate(AppState.TimelineManager.FocusTime);
                    }
                }

            });
        }

        public bool InEditMode { get; set; }

        public void UpdateCallOut()
        {
            Execute.OnUIThread(() =>
            {
                //Todo:Jeroen - Properly fix update changes (also remove labels/sections instead of add only)
                //If there is no poi, there is no info to collect! (Nothing to show here.. moving on)
                if (Poi == null) return;

                //Collect all info
                var defaultSection = new MetaLabelsViewModel
                {
                    DisplayName = "Info",
                    CallOut = CallOut,
                    Content = new BindableCollection<MetaLabel>()
                };
                var dict = new Dictionary<string, MetaLabelsViewModel> { { defaultSection.DisplayName, defaultSection } };

                //Get all labels from the poi
                var mll = Poi.GetMetaLabels();
                if (mll == null) return;
                foreach (var ml in mll)
                {
                    //If it is not visible, skip it!
                    if (!ml.Meta.VisibleInCallOut) continue;
                    //Get the section/category for the label
                    var sectionLabel = ml.Meta.Section;
                    if (string.IsNullOrEmpty(sectionLabel))
                        sectionLabel = "Info";

                    if (!dict.ContainsKey(sectionLabel))
                        dict[sectionLabel] = new MetaLabelsViewModel() { DisplayName = sectionLabel, CallOut = CallOut, Content = new BindableCollection<MetaLabel>() };
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
                    setEditMode(sect, InEditMode || Poi.NEffectiveStyle.TapMode == TapMode.CallOutEdit);
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

                var vms = Poi.ModelInstances.Values.Where(k => k.ViewModel != null).Select(k => k.ViewModel);
                foreach (var vm in vms)
                {
                    vm.CallOut = CallOut;
                    Sections.Add(vm);
                }

                foreach (var mf in Poi.GetMetaLabels().Where(k => k.Meta.Type == MetaTypes.mediafolder))
                {
                    var mvm = new MediaViewModel() { CallOut = CallOut, Label = mf, DisplayName = mf.Meta.Section };
                    Sections.Add(mvm);
                }
            });
        }
    }
}