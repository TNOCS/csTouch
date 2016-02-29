using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CsvToDataService.Model;

namespace PoiConvertor.Views
{
    /// <summary>
    ///     Interaction logic for ProgressPopup.xaml
    /// </summary>
    public partial class BackgroundWorkerProgressPopup : Window
    {
        private const int TicksToSeconds = 10000000;
        private readonly BackgroundWorker _backgroundWorker;
        private readonly ProgressStruct _progressStruct;
        private long _now;

        public BackgroundWorkerProgressPopup(Window owner, BackgroundWorker backgroundWorker,
            ProgressStruct progressStruct)
        {
            Owner = owner;
            _backgroundWorker = backgroundWorker;
            _progressStruct = progressStruct;
            _progressStruct.PropertyChanged += ProgressStructOnPropertyChanged;

            InitializeComponent();
        }

        private void ProgressStructOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action<Label>(SetTimeRemaining), TimeRemainingLabel);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action<Label>(SetTimeElapsed), TimeElapsedLabel);
        }

        private void SetTimeRemaining(Label txt)
        {
            txt.Content = EstimatedTimeRemaining;
        }

        private void SetTimeElapsed(Label txt)
        {
            txt.Content = TimeElapsed;
        }

        public ProgressStruct ProgressStruct
        {
            get { return _progressStruct; }
            set
            {
                _progressStruct.NumTotal = value.NumTotal;
                _progressStruct.NumDone = value.NumDone;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
        }

        public void StartWorking()
        {
            _backgroundWorker.RunWorkerCompleted += delegate
            {
                Owner.IsEnabled = true;
                Owner = null;
                Hide();
                Close();
            };
            Owner.IsEnabled = false;
            Show();
            _now = DateTime.Now.Ticks;
            _backgroundWorker.RunWorkerAsync();
        }

        private long TimeElapsed
        {
            get
            {
                if (_now == 0) _now = DateTime.Now.Ticks;
                return (DateTime.Now.Ticks - _now) / TicksToSeconds;                 // seconds
            }
        }

        private long EstimatedTimeRemaining
        {
            get
            {
                long expected = (long)(((1.0 * _progressStruct.NumTotal - _progressStruct.NumDone) / _progressStruct.NumDone) * (TimeElapsed*TicksToSeconds));
                return expected/TicksToSeconds; // seconds.
            }
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            _backgroundWorker.CancelAsync();
        }
    }
}