using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Caliburn.Micro;
using WpfCharts;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.Interfaces;

namespace csAppraisalPlugin.ViewModels
{
    public delegate void CloseViewHandler(object sender, EventArgs e);

    [Export(typeof(ISpiderImageCombi)), PartCreationPolicy(CreationPolicy.Shared)]
    public class SpiderImageCombiViewModel : Screen, ISpiderImageCombi
    {
        private Appraisal selectedAppraisal;
        private BindableCollection<ChartLine> lines;
        private BindableCollection<string> axes;
        private double angle;

        public Appraisal SelectedAppraisal {
            get { return selectedAppraisal; }
            set {
                if (selectedAppraisal == value) return;
                selectedAppraisal = value;
                UpdateSpider();
                NotifyOfPropertyChange(() => SelectedAppraisal);
            }
        }

        private void UpdateSpider() {
            if (selectedAppraisal == null) return;
            Axes = new BindableCollection<string>();
            foreach (var criterion in selectedAppraisal.Criteria) {
                criterion.PropertyChanged -= CriterionOnPropertyChanged;
                criterion.PropertyChanged += CriterionOnPropertyChanged;
                Axes.Add(criterion.Title);
            }
            Lines = new BindableCollection<ChartLine> {
                new ChartLine {
                    LineColor = Colors.DarkGreen,
                    FillColor = Color.FromArgb(128, 0, 100, 0),
                    LineThickness = 4,
                    PointDataSource = selectedAppraisal.Criteria.AssignedValues,
                    Name = "Issue"
                }
            };
        }

        public void ValuesChanged(SpiderChartPanel.ValuesChangedEventArgs e)
        {
            selectedAppraisal.Criteria.SetAssignedValues(e.PointDataSource);
        }

        private void CriterionOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (!e.PropertyName.Equals("Title")) return;
            var criterion = sender as Criterion;
            if (criterion == null) return;
            var index = selectedAppraisal.Criteria.IndexOf(criterion);
            if (index >= 0) Axes[index] = criterion.Title;
        }

        public BindableCollection<string> Axes {
            get { return axes; }
            set {
                if (axes == value) return;
                axes = value;
                NotifyOfPropertyChange(() => Axes);
            }
        }

        public BindableCollection<ChartLine> Lines {
            get { return lines; }
            set {
                if (lines == value) return;
                lines = value;
                NotifyOfPropertyChange(() => Lines);
            }
        }

        public event CloseViewHandler CloseView;

        protected virtual void OnCloseView() {
            var handler = CloseView;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void GotoComparison() {
            OnCloseView();
        }

        public double Angle {
            get { return angle; }
            set {
                angle = value;
                NotifyOfPropertyChange(() => Angle);
            }
        }

        public void RotateImage() {
            if (angle >= 270) Angle = 0;
            else Angle += 90;
        }
    }
}
