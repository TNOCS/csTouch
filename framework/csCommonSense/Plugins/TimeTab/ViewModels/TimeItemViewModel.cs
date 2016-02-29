using System.Windows;
using Caliburn.Micro;
using csEvents;
using csShared;
using csShared.Geo;
using csShared.Interfaces;

namespace csTimeTabPlugin
{
    public class TimeItemViewModel : Screen
    {
        private IEvent item;
        private double posX;
        private double posY;
        private TimeRow row;
        private TimeItemView view;
        public bool CustomItem { get; set; }

        public TimeTabPlugin Plugin { get; set; }

        public IEvent Item
        {
            get { return item; }
            set
            {
                item = value;
                NotifyOfPropertyChange(() => Item);
            }
        }

        public TimeRow Row
        {
            get { return row; }
            set
            {
                row = value;
                NotifyOfPropertyChange(() => Row);
            }
        }

        public double PosX
        {
            get { return posX; }
            set
            {
                posX = value;
                NotifyOfPropertyChange(() => PosX);
            }
        }

        public double PosY
        {
            get { return posY; }
            set
            {
                posY = value;
                NotifyOfPropertyChange(() => PosY);
            }
        }

        public double ItemOpacity
        {
            get { return CustomItem ? 0.75 : 1; }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public ITimelineManager TimelineManager
        {
            get { return AppState.TimelineManager; }
        }

       
        public void CalculatePos()
        {
            if (CustomItem) return;
            var ti = Item.Date;
            var tlm = AppStateSettings.Instance.TimelineManager;
            var s = tlm.Start;
            var e = tlm.End;
            var r = (e - s).TotalSeconds;
            var w = Application.Current.MainWindow.ActualWidth;

            PosX = Application.Current.MainWindow.TranslatePoint(new Point(tlm.GetScreenPos(ti), 0), view).X;
            ; //(w / r * (ti - s).TotalSeconds) - 13;

            PosY = (Row.ActualOrder)*57;
        }

        public void Switch()
        {
            AppState.DockedFloatingElementsVisible = !AppState.DockedFloatingElementsVisible;
        }

        protected override void OnViewLoaded(object v)
        {
            base.OnViewLoaded(v);
            view = (TimeItemView) v;
            if (!CustomItem)
            {
                AppState.TimelineManager.PropertyChanged += (e, s) => { if (s.PropertyName == "Start") CalculatePos(); };
                AppState.TimelineManager.TimeChanged += (e, s) => CalculatePos();
                CalculatePos();
            }

            //vi.Items.ItemsSource = TimeItems;
        }

        public void Button()
        {
            TimelineManager.SetFocusTime(Item.Date);
            Item.TriggerClicked(this, "");
            //TimelineManager.CenterTime(Item.Date);
        }
    }
}