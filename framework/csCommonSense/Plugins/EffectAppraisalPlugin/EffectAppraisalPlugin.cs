using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using csCommon.Plugins.EffectAppraisalPlugin.ViewModels;
using csShared;
using csShared.Interfaces;
using DataServer;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.Specialized;

// TODO When the plugin is started, hide the icon.
// Subscribe to new layers, and subscribe to the POI selection event.
// TODO When the POI is selected, check whether there are labels containing criteria to show. If so, show the plugin.
// Add criteria labels to POI dsd. Label format: <Effect.NameOfThreat.NameOfEffect>5</Effect.NameOfThreat.NameOfEffect>
// When the plugin is clicked, center the map around the currently active POI.
// TODO When the plugin is clicked, show a semi-transparent overlay with a radial menu.
// TODO When the radial menu is changed, update the labels in the POI.
// TODO Use the timeline to set the start and end time of a measure (when it is active).
// TODO Compute the evaluation score
// TODO Create a dashboard with the evaluation result.
using DocumentFormat.OpenXml.Wordprocessing;

namespace csCommon.Plugins.EffectAppraisalPlugin
{
    [Export(typeof(IPlugin))]
    public class EffectAppraisalPlugin : PropertyChangedBase, IPlugin
    {
        /// <summary>
        /// Key to identify a label that is used to indicate that we are dealing with a usable poi type.
        /// EAM = Effect Appraisal Model.
        /// </summary>
        public const string LabelKey = "EAM.";

        private PoI selectedPoi;
        private bool isEditorVisible;
        private CircularMenuItem circularMenuItem;
        // private MenuItem config; // Never used.
        private bool hideFromSettings;
        private bool isRunning;
        private IPluginScreen screen;
        private ISettingsScreen settings;
        public FloatingElement Element { get; set; }

        public EffectAppraisalPlugin()
        {
            avm = AppStateSettings.Instance.Container.GetExportedValue<IEffectAppraisal>();
            if (avm == null) throw new SystemException("Couldn't create EffectAppraisalViewModel");
        }

        public bool CanStop
        {
            get { return true; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/radial_sliders.png"; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }

        public string Name
        {
            get { return "EffectAppraisalPlugin"; }
        }

        public void Init()
        {
        }

        public void Start()
        {
            IsRunning = true;
            SubscribeToPoiSelections();
            circularMenuItem = new CircularMenuItem { Title = "Evaluate measure" };
            circularMenuItem.Selected += circularMenuItem_Selected;

            circularMenuItem.Id = Guid.NewGuid().ToString();
            circularMenuItem.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/radial_sliders.png";
            AppState.AddCircularMenu(circularMenuItem);

            DisableMenuItem();
        }

        private void SubscribeToPoiSelections()
        {
            AppState.DataServer.UnSubscribed += (sender, args) =>
            {
                var poiService = args.Service as PoiService;
                if (poiService == null) return;
                poiService.Tapped -= PoiServiceTapped;
                poiService.PoIs.CollectionChanged -= PoICollectionChanged;
                RemovePoIsFromViewModelCollection(poiService.Layer.ID);
                poiService.Tapped -= DisableMenuItem;
            };
            AppState.DataServer.Subscribed += (sender, args) =>
            {
                var poiService = args.Service as PoiService;
                if (poiService == null) return;
                if (poiService.PoITypes.Where(p => p.MetaInfo != null).SelectMany(poiType => poiType.MetaInfo).Any(mi => mi.Label != null && mi.Label.StartsWith(LabelKey)))
                {
                    AddPoIsToViewModelCollection(poiService.PoIs);
                    poiService.PoIs.CollectionChanged += PoICollectionChanged;
                    poiService.Tapped += PoiServiceTapped;
                } 
                else
                {
                    poiService.Tapped += DisableMenuItem;
                }
            };
        }

        private void PoICollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var cl = new ContentList();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.Cast<BaseContent>()) cl.Add(item);
                    AddPoIsToViewModelCollection(cl);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.Cast<BaseContent>()) cl.Add(item);
                    RemovePoIsFromViewModelCollection(cl);
                    break;
            }
        }

        private void AddPoIsToViewModelCollection(ContentList pois)
        {
            foreach (var poi in pois)
            {
                if (!(poi is PoI)) continue;
                switch (poi.PoiType.PoiTypeId.ToLower())
                {
                    case "threat":
                        if (!avm.Threats.Contains(poi)) avm.Threats.Add(poi as PoI);
                        break;
                    case "measure":
                        if (!avm.Measures.Contains(poi)) avm.Measures.Add(poi as PoI);
                        break;
                }
            }
        }

        private void RemovePoIsFromViewModelCollection(ContentList pois)
        {
            foreach (var poi in pois)
            {
                if (!(poi is PoI)) continue;
                if (string.Equals(poi.PoiType.PoiTypeId, "threat", StringComparison.InvariantCultureIgnoreCase)) avm.Threats.Remove(poi as PoI);
                else if (string.Equals(poi.PoiType.PoiTypeId, "measure", StringComparison.InvariantCultureIgnoreCase)) avm.Measures.Remove(poi as PoI);
            }
        }

        private void RemovePoIsFromViewModelCollection(string serviceName)
        {
            for (var i = avm.Threats.Count - 1; i >= 0; i--)
            {
                var poi = avm.Threats[i];
                if (string.Equals(poi.Service.Name, serviceName)) avm.Threats.RemoveAt(i);
            }
            for (var i = avm.Measures.Count - 1; i >= 0; i--)
            {
                var poi = avm.Measures[i];
                if (string.Equals(poi.Service.Name, serviceName)) avm.Measures.RemoveAt(i);
            }
        }

        /// <summary>
        /// When a PoI is selected in another layer, disable the menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisableMenuItem(object sender = null, TappedEventArgs e = null)
        {
            circularMenuItem.IsEnabled = false;
            circularMenuItem.Opacity = 0.5;
            circularMenuItem.Fill = Brushes.Gray;
            circularMenuItem.Background = Brushes.Red;
        }

        private void EnableMenuItem()
        {
            circularMenuItem.IsEnabled = true;
            circularMenuItem.Opacity = 1;
            circularMenuItem.BorderBrush = Brushes.Black;
        }

        private static bool CanPoiBeEvaluated(BaseContent poi)
        {
            return poi.Labels.Any(label => label.Key.StartsWith(LabelKey));
        }

        void PoiServiceTapped(object sender, TappedEventArgs e)
        {
            var poi = e.Content as PoI;
            if (poi == null) return;
            if (CanPoiBeEvaluated(poi))
            {
                EnableMenuItem();
                selectedPoi = poi;
                if (!isEditorVisible || selectedPoi == null) return;
                //ZoomAndPoint();
                avm.SelectedPoI = selectedPoi;
                //AppState.TriggerNotification("Tapped");
            }
            else
            {
                selectedPoi = null;
                DisableMenuItem();
                HideEditor();
            }
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Stop()
        {
            IsRunning = false;
            if (circularMenuItem == null) return;
            AppState.RemoveCircularMenu(circularMenuItem.Id);
        }

        private void circularMenuItem_Selected(object sender, MenuItemEventArgs e)
        {
            if (!circularMenuItem.IsEnabled || selectedPoi == null) return;

            if (isEditorVisible)
                HideEditor();
            else
                ShowEditor();

            ZoomAndPoint();
        }

        private void ZoomAndPoint() {
            Execute.OnUIThread(() => AppState.ViewDef.ZoomAndPoint(new Point(selectedPoi.Position.Longitude, selectedPoi.Position.Latitude)));
        }
        
        private IEffectAppraisal avm;

        private void ShowEditor()
        {
            if (isEditorVisible) return;
            AppState.LeftTabMenuVisible = false;
            AppState.BottomTabMenuVisible = false;
            AppState.BotomPanelVisible = false;

            avm = AppState.Container.GetExportedValue<IEffectAppraisal>();
            if (avm == null) throw new SystemException("Couldn't create EffectAppraisalViewModel");
            avm.LabelKey = LabelKey;
            avm.SelectedPoI = selectedPoi;
            avm.Plugin = this;
            isEditorVisible = true;
            // Since the Plugin is not part of the lifecycle, I need to activate the AppraisalViewModel myself.
            ScreenExtensions.TryActivate(avm);
            Screen = avm;
        }

        private void HideEditor() {
            if (!isEditorVisible) return;
            isEditorVisible = false;
            AppState.LeftTabMenuVisible = true;
            AppState.BottomTabMenuVisible = true;
            AppState.BotomPanelVisible = true;
            avm.Deactivate(true);
        }
    }
}