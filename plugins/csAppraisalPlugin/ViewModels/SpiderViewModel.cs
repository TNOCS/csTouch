using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using BaseWPFHelpers;
using Caliburn.Micro;
using Microsoft.Surface.Presentation.Controls;

using csAppraisalPlugin.Classes;
using csAppraisalPlugin.Interfaces;
using csAppraisalPlugin.Views;
using csShared;

namespace csAppraisalPlugin.ViewModels
{
    [Export(typeof(ISpider)), PartCreationPolicy(CreationPolicy.Shared)]
    public class SpiderViewModel : Screen, ISpider
    {
        private readonly Random random = new Random(1234);
        private SpiderView view;

        private ObservableCollection<string> axes;
        private ObservableCollection<WpfCharts.ChartLine> lines;

        private FloatingElement fe;

        private int nextAvailable;
        private AppraisalPlugin plugin;
        private int previousAvailable;
        private bool spider;
        private ScatterViewItem svi;
        private bool showSpider = true;
        private bool showScore = true;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            DisplayName = "Spider";
        }

        public AppraisalPlugin Plugin
        {
            get { return plugin; }
            set
            {
                if (plugin != null) plugin.Functions.CollectionChanged -= FunctionsOnCollectionChanged;
                plugin = value;
                if (plugin == null) return;
                plugin.Functions.CollectionChanged += FunctionsOnCollectionChanged;
                foreach (var function in plugin.Functions)
                {
                    function.PropertyChanged -= FunctionOnPropertyChanged;
                    function.PropertyChanged += FunctionOnPropertyChanged;
                }
                NotifyOfPropertyChange(() => Appraisals);
                plugin.AppraisalsUpdated -= plugin_AppraisalsUpdated;
                plugin.AppraisalsUpdated += plugin_AppraisalsUpdated;
                //NotifyOfPropertyChange(() => Active);
                //plugin.ActiveChanged += (e, s) => NotifyOfPropertyChange(() => Active);
            }
        }

        void plugin_AppraisalsUpdated(object sender, EventArgs e)
        {
            UpdateSpider();
        }

        private void FunctionOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("IsSelected")) return;
            UpdateSpider();
        }

        private void FunctionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var function in from object newItem in e.NewItems select newItem as Function)
                        function.PropertyChanged += FunctionOnPropertyChanged;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var function in from object oldItem in e.OldItems select oldItem as Function)
                        function.PropertyChanged -= FunctionOnPropertyChanged;
                    break;
            }
            return;
            // EV TODO
            if (plugin.Functions.Count < 3) return;
            Axes.Clear();
            foreach (var function in plugin.Functions)
            {
                Axes.Add(function.Name);
                function.PropertyChanged += (o, args) =>
                                                {
                                                    if (!args.PropertyName.Equals("Name")) return; 
                                                    Axes.Clear();
                                                    foreach (var f in plugin.Functions) Axes.Add(f.Name);
                                                };
            }

            //Lines = new ObservableCollection<ChartLine> {
            //                                                new ChartLine {
            //                                                                  LineColor = Colors.Red,
            //                                                                  //FillColor = Color.FromArgb(128, 255, 0, 0),
            //                                                                  LineThickness = 2,
            //                                                                  PointDataSource = GenerateDataSet(),
            //                                                                  Name = "Chart 1"
            //                                                              },
            //                                                new ChartLine {
            //                                                                  LineColor = Colors.Blue,
            //                                                                  //FillColor = Color.FromArgb(128, 0, 0, 255),
            //                                                                  LineThickness = 2,
            //                                                                  PointDataSource = GenerateDataSet(),
            //                                                                  Name = "Chart 2"
            //                                                              }
            //                                            };
        }

        public List<double> GenerateDataSet()
        {
            var nmbrOfPoints = plugin.Functions.Count;
            var pts = new List<double>(nmbrOfPoints);
            for (var i = 0; i < nmbrOfPoints; i++)
            {
                pts.Add(random.NextDouble());
                //pts.Add(Appraisals[0].Criteria[i].AssignedValue);
            }
            return pts;
        }

        public ObservableCollection<string> Axes
        {
            get { return axes; }
            set
            {
                axes = value;
                NotifyOfPropertyChange(() => Axes);
            }
        }

        public ObservableCollection<WpfCharts.ChartLine> Lines
        {
            get { return lines; }
            set
            {
                lines = value;
                NotifyOfPropertyChange(() => Lines);
            }
        }

        public bool Large
        {
            get { return fe != null && fe.IsFullScreen; }
        }

        public ObservableCollection<Appraisal> Appraisals
        {
            get
            {
                return Plugin != null
                           ? Plugin.SelectedAppraisals
                           : null;
            }
        }

        public bool CanPrevious
        {
            get { return PreviousAvailable > 0; }
        }

        public bool CanNext
        {
            get { return NextAvailable > 0; }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public bool Spider
        {
            get { return spider; }
            set
            {
                spider = value;
                NotifyOfPropertyChange(() => Spider);
                //NotifyOfPropertyChange(() => SelectedSize);
            }
        }

        public Brush AccentBrush
        {
            get { return AppStateSettings.Instance.AccentBrush; }
        }

        //public double SelectedSize
        //{
        //    get
        //    {
        //        return (Spider)
        //                   ? ItemSize
        //                   : 300;
        //    }
        //}

        //public double ItemSize
        //{
        //    get { return itemSize; }
        //    set
        //    {
        //        itemSize = value;
        //        NotifyOfPropertyChange(() => ItemSize);
        //        NotifyOfPropertyChange(() => SelectedSize);
        //    }
        //}

        public int NextAvailable
        {
            get { return nextAvailable; }
            set
            {
                nextAvailable = value;
                NotifyOfPropertyChange(() => NextAvailable);
            }
        }

        public int PreviousAvailable
        {
            get { return previousAvailable; }
            set
            {
                previousAvailable = value;
                NotifyOfPropertyChange(() => PreviousAvailable);
            }
        }

        public List<double> GenerateRandomDataSet(int nmbrOfPoints)
        {
            var pts = new List<double>(nmbrOfPoints);
            for (var i = 0; i < nmbrOfPoints; i++)
            {
                pts.Add(random.NextDouble());
            }
            return pts;
        }

        public void Previous()
        {
            Plugin.SelectedAppraisal = Appraisals[Appraisals.IndexOf(Plugin.SelectedAppraisal) - 1];
        }

        public void Next()
        {
            Plugin.SelectedAppraisal = Appraisals[Appraisals.IndexOf(Plugin.SelectedAppraisal) + 1];
        }


        protected override void OnViewLoaded(object loadedView)
        {
            base.OnViewLoaded(loadedView);
            view = (SpiderView) loadedView;
            
            svi = Helpers.FindElementOfTypeUp(view, typeof (ScatterViewItem)) as ScatterViewItem;
            if (svi != null && svi.DataContext is FloatingElement) fe = (FloatingElement) svi.DataContext;
            if (fe != null)
            {
                AppState.FullScreenFloatingElementChanged += (e, s) => NotifyOfPropertyChange(() => Large);
                NotifyOfPropertyChange(() => Large);
            }
            //AppraisalTab = AppState.Container.GetExportedValue<IAppraisalTab>();
            //AppraisalTab.Plugin = Plugin;
            Plugin.SelectedAppraisals.CollectionChanged += SelectedAppraisalsCollectionChanged;
            Plugin.PropertyChanged += (e, s) =>
                                          {
                                              if (!s.PropertyName.Equals("SelectedAppraisal")) return;
                                              UpdateNextPrevious();
                                              UpdateSpider();
                                          };
            if (Plugin.SelectedAppraisals.Count > 0) Plugin.SelectedAppraisal = Plugin.SelectedAppraisals.First();

            UpdateSpider();
        }

        private void UpdateSpider()
        {
            Axes = new BindableCollection<string>();
            var selectedAppraisal = Plugin.SelectedAppraisal;
            if (selectedAppraisal == null) return;
            foreach (var criterion in selectedAppraisal.Criteria)
            {
                criterion.PropertyChanged -= CriterionOnPropertyChanged;
                criterion.PropertyChanged += CriterionOnPropertyChanged;
                Axes.Add(criterion.Title);
            }
            Lines = new BindableCollection<WpfCharts.ChartLine>();
            foreach (var a in Plugin.Appraisals.Where(k => k.IsCompare && k != selectedAppraisal))
            {
                Lines.Add( new WpfCharts.ChartLine {
                                                                            LineColor = a.Color,
                                                                            FillColor = Color.FromArgb(128, a.Color.R, a.Color.G, a.Color.B),
                                                                            LineThickness = 4,
                                                                            PointDataSource = a.Criteria.AssignedValues,
                                                                            Name = a.Title,
                                                                            CanEdit = false
                                                                        });
            }
            Lines.Add(

                new WpfCharts.ChartLine
                {
                    LineColor = selectedAppraisal.Color,
                    FillColor = Color.FromArgb(128, selectedAppraisal.Color.R, selectedAppraisal.Color.G, selectedAppraisal.Color.B),                                                                            
                    LineThickness = 4,
                    PointDataSource = selectedAppraisal.Criteria.AssignedValues,
                    Name = selectedAppraisal.Title,
                    CanEdit = true

                }
                );
        }

        private void CriterionOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("Title")) return;
            var criterion = sender as Criterion;
            if (criterion == null) return;
            var index = Plugin.SelectedAppraisal.Criteria.IndexOf(criterion);
            if (index>=0) Axes[index] = criterion.Title;
        }

        private void SelectedAppraisalsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Plugin.SelectedAppraisals.Count > 0 && Plugin.SelectedAppraisal == null)
            {
                Plugin.SelectedAppraisal = Plugin.SelectedAppraisals.First();
            }
            UpdateNextPrevious();
        }

        public void UpdateNextPrevious()
        {
            if (Plugin.SelectedAppraisal == null) return;
            var c = Appraisals.IndexOf(Plugin.SelectedAppraisal);
            NextAvailable = Appraisals.Count - c - 1;
            PreviousAvailable = c;
            NotifyOfPropertyChange(() => CanNext);
            NotifyOfPropertyChange(() => CanPrevious);
        }

        public void Selected(ActionExecutionContext o)
        {
            //var slb = o.Source as SurfaceListBox;
        }

        public void ValuesChanged(WpfCharts.SpiderChartPanel.ValuesChangedEventArgs e)
        {
            Plugin.SelectedAppraisal.Criteria.SetAssignedValues(e.PointDataSource);
        }

        public bool ShowSpider {
            get { return showSpider; }
            set {
                if (showSpider == value) return;
                showSpider = value;
                if (!showSpider) ShowScore = false;
                NotifyOfPropertyChange(() => ShowSpider);
            }
        }

        public bool ShowScore {
            get { return showScore; }
            set {
                if (showScore == value) return;
                showScore = value;
                NotifyOfPropertyChange(() => ShowScore);
            }
        }
    }

}