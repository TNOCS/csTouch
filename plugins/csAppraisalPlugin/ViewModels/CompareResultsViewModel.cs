using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Caliburn.Micro;
using WpfCharts;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.Interfaces;

namespace csAppraisalPlugin.ViewModels
{
    public class GotoDetailedViewEventArgs : EventArgs {
        public GotoDetailedViewEventArgs(Appraisal appraisal) {
            Appraisal = appraisal;
        }

        public Appraisal Appraisal { get; set; }
    }

    public delegate void GotoDetailedViewEventHandler(object sender, GotoDetailedViewEventArgs e);

    [Export(typeof (ICompareResults)), PartCreationPolicy(CreationPolicy.Shared)]
    public class CompareResultsViewModel : Screen, ICompareResults
    {
        private readonly ObservableCollection<string>[] axes = new ObservableCollection<string>[4];
        private readonly ObservableCollection<ChartLine>[] lines = new ObservableCollection<ChartLine>[4];
        private readonly bool[] showImage = new bool[4];
        private AppraisalPlugin plugin;

        public ObservableCollection<string>[] Axes
        {
            get { return axes; }
        }

        public ObservableCollection<ChartLine>[] Lines
        {
            get { return lines; }
        }

        public bool[] ShowImage
        {
            get { return showImage; }
        }

        protected override void OnInitialize()
        {
            DisplayName = "Compare";
            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            RedrawAllSpiders();
        }

        private void FunctionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems)
                    {
                        var function = newItem as Function;
                        if (function == null) return;
                        function.PropertyChanged += FunctionOnPropertyChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems)
                    {
                        var function = oldItem as Function;
                        if (function == null) return;
                        function.PropertyChanged -= FunctionOnPropertyChanged;
                    }
                    break;
            }
        }

        private void FunctionOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("IsSelected")) return;
            RedrawAllSpiders();
        }

        private void SelectedAppraisalsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RedrawAllSpiders();
        }

        private void RedrawAllSpiders()
        {
            if (!IsActive) return;
            for (var i = 0; i < 4; i++)
            {
                ShowImage[i] = i < Plugin.SelectedAppraisals.Count;
                if (ShowImage[i]) DrawSpiderOnImage(i);
            }
            NotifyOfPropertyChange(() => ShowImage);
            NotifyOfPropertyChange(() => Lines);
            NotifyOfPropertyChange(() => Axes);
        }

        private void DrawSpiderOnImage(int index)
        {
            var appraisal = Plugin.SelectedAppraisals[index];
            if (appraisal == null) return;
            Axes[index] = new BindableCollection<string>();
            foreach (var criterion in appraisal.Criteria)
                Axes[index].Add(criterion.Title);
            Lines[index] = new BindableCollection<ChartLine> {
                                                                 new ChartLine {
                                                                                   LineColor = Colors.DarkGreen,
                                                                                   FillColor = Color.FromArgb(128, 0, 100, 0),
                                                                                   LineThickness = 4,
                                                                                   PointDataSource = appraisal.Criteria.AssignedValues,
                                                                                   Name = "Issue"
                                                                               }
                                                             };
        }

        public event GotoDetailedViewEventHandler GotoDetailedViewModel;
        private void OnGotoDetailedView(Appraisal appraisal)
        {
            var handler = GotoDetailedViewModel;
            if (handler != null) handler(this, new GotoDetailedViewEventArgs(appraisal));
        }
        
        public void GotoDetailedView(int index) {
            OnGotoDetailedView(Plugin.SelectedAppraisals[index]);
        }

        #region IAppraisal Members

        public AppraisalPlugin Plugin
        {
            get { return plugin; }
            set
            {
                plugin = value;
                if (plugin == null) return;
                Plugin.SelectedAppraisals.CollectionChanged += SelectedAppraisalsOnCollectionChanged;
                Plugin.Functions.CollectionChanged += FunctionsOnCollectionChanged;
                foreach (var function in plugin.Functions)
                    function.PropertyChanged += FunctionOnPropertyChanged;
            }
        }

        #endregion
    }
}