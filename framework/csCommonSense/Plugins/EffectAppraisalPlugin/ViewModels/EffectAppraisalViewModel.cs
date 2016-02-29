using System.Windows.Media;
using Caliburn.Micro;
using csCommon.Plugins.EffectAppraisalPlugin.Views;
using csShared;
using csShared.Interfaces;
using DataServer;
using System.ComponentModel.Composition;
using System.Windows;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using csCommon.Plugins.EffectAppraisalPlugin.Models;
using csShared.Controls.Popups.MenuPopup;
using System.Windows.Data;
using System.ComponentModel;

namespace csCommon.Plugins.EffectAppraisalPlugin.ViewModels
{
    public interface IEffectAppraisal : IPluginScreen, IScreen
    {
        EffectAppraisalPlugin Plugin { get; set; }
        string LabelKey { get; set; }
        PoI SelectedPoI { get; set; }
        BindableCollection<PoI> Measures { get; }
        BindableCollection<PoI> Threats { get; }
    }

    [Export(typeof(IEffectAppraisal)), PartCreationPolicy(CreationPolicy.Shared)]
    public class EffectAppraisalViewModel : Screen, IEffectAppraisal
    {
        private readonly BindableCollection<PoI> measures = new BindableCollection<PoI>();
        private readonly BindableCollection<PoI> threats  = new BindableCollection<PoI>();
        private readonly ConcurrentDictionary<PoI, PoI> measureToThreat = new ConcurrentDictionary<PoI, PoI>();
        private readonly PhaseState rootState = new PhaseState("root");
        private ICollectionView cvs;
        private EffectAppraisalView view;
        private PoI selectedPoI;
        private PhaseState selectedPhase;

        public BindableCollection<PoI> Measures { get { return measures; } }

        public BindableCollection<PoI> Threats { get { return threats; } }

        public Brush BackgroundBrush { get { return AppStateSettings.Instance.AccentBrush; }}

        public EffectAppraisalPlugin Plugin { get; set; }
        public string Name { get { return "EffectAppraisalViewModel"; } }

        // public BindableCollection<AppraisalEffect> Effects { get; set; }

        [ImportingConstructor]
        public EffectAppraisalViewModel()
        {
            //Effects = new BindableCollection<AppraisalEffect>();
            Threats.CollectionChanged  += CollectionChanged;
            Measures.CollectionChanged += CollectionChanged;
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        var poi = item as PoI;
                        if (poi == null) continue;
                        var eventAsObservable = Observable.FromEventPattern<PositionEventArgs>(poi, "PositionChanged");
                        eventAsObservable.Throttle(TimeSpan.FromMilliseconds(300)).Subscribe(k => Execute.OnUIThread(UpdateState));
                        //poi.PositionChanged += PoiPositionChanged;
                        poi.LabelChanged    += PoiLabelChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        var poi = item as PoI;
                        if (poi == null) continue;
                        //poi.PositionChanged -= PoiPositionChanged;
                        poi.LabelChanged    -= PoiLabelChanged;
                    }
                    break;
            }
            UpdateState();
        }

        private void PoiLabelChanged(object sender, LabelChangedEventArgs e)
        {
            if (string.Equals(e.Label, "EAM.Influence", StringComparison.InvariantCultureIgnoreCase) || 
                string.Equals(e.Label, "EAM.Effect.Combined.Level", StringComparison.InvariantCultureIgnoreCase)) return;
            if (e.Label.StartsWith("EAM.") || string.Equals(e.Label, "Circle.CircleRadius")) UpdateState();
        }

        private void PoiPositionChanged(object sender, PositionEventArgs e)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            rootState.Reset();
            foreach (var measure in measures)
                rootState.AddMeasure(measure);
            foreach (var threat in threats)
                rootState.AddThreat(threat);
            TrySetSelectedPhase();
            NotifyOfPropertyChange(() => Phases);
            NotifyOfPropertyChange(() => ThreatEffects);
            NotifyOfPropertyChange(() => MeasureEffects);
            NotifyOfPropertyChange(() => Resources);
        }

        private string selectedPhaseTitle;

        private void TrySetSelectedPhase()
        {
            if (!string.IsNullOrEmpty(selectedPhaseTitle))
            {
                foreach (var phase in Phases)
                {
                    if (!string.Equals(phase.Title, selectedPhaseTitle)) continue;
                    selectedPhase = phase;
                    selectedPhase.Activate();
                    return;
                }
            }
            if (Phases.Count > 0)
            {
                selectedPhase = Phases[0];
                selectedPhase.Activate();
            }
        }

        public PhaseState SelectedPhase
        {
            get
            {
                return selectedPhase;
            }
            set
            {
                if (selectedPhase == value) return;
                selectedPhase = value;
                if (selectedPhase != null) selectedPhaseTitle = selectedPhase.Title;
                UpdateState();
                NotifyOfPropertyChange(() => SelectedPhase);
            }
        }
        public BindableCollection<PhaseState> Phases { get { return rootState.PhaseStates; } }
        public ICollection<EffectState> ThreatEffects {
            get { return selectedPhase == null ? null : selectedPhase.ThreatStates; } }
        public ICollectionView MeasureEffects {
            get
            {
                if (selectedPhase == null) return null;
                cvs = CollectionViewSource.GetDefaultView(selectedPhase.MeasureStates);
                cvs.Filter = new Predicate<object>(x => ((EffectState)x).IsDisturbance);
                return cvs;
            }
        }
        public ICollection<ResourceState> Resources { get { return selectedPhase == null ? null : selectedPhase.ResourceStates; } }

        public string LabelKey { get; set; }

        public PoI SelectedPoI
        {
            get { return selectedPoI; }
            set
            {
                if (selectedPoI == value) return;
                selectedPoI = value;
                NotifyOfPropertyChange(() => SelectedPoI);
            }
        }

        protected override void OnViewLoaded(object loadedView)
        {
            base.OnViewLoaded(loadedView);
            view = loadedView as EffectAppraisalView;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (view != null) view.Visibility = Visibility.Visible;
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            view.Visibility = Visibility.Collapsed;
            //var cc = view.Parent as ContentControl;
            //if (cc == null) return;
            //var grid = cc.Parent as Grid;
            //if (grid != null) grid.Children.Remove(cc);
        }

        public void SelectOption(FrameworkElement b)
        {
            var m = GetMenu(b);
            foreach (var o in rootState.PhaseStates)
            {
                var mi = m.AddMenuItem(o.Title);
                mi.Click += (e, s) =>
                {
                    SelectedPhase = o;
                };
            }

            AppStateSettings.Instance.Popups.Add(m);
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

    }
}
