using Caliburn.Micro;
using csCommon.Plugins.TimeTab.ViewModels;
using csEvents;
using csShared;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Timeline;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Unit = csShared.Utils.Unit;

namespace csTimeTabPlugin
{
    public class TimeItem : PropertyChangedBase
    {
        private string rowId;
        private DateTime startTime;

        private string title;

        private bool visible;

        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                startTime = value;
                NotifyOfPropertyChange(() => StartTime);
            }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                NotifyOfPropertyChange();
            }
        }

        public string RowId
        {
            get { return rowId; }
            set
            {
                rowId = value;
                NotifyOfPropertyChange(() => RowId);
            }
        }
    }

    public class TimeTabViewModel : Screen
    {
        private BindableCollection<TimeItem> items = new BindableCollection<TimeItem>();

        private BindableCollection<TimeItemViewModel> timeItems = new BindableCollection<TimeItemViewModel>();
        private TimeTabView view;

        public TimeTabPlugin Plugin { get; set; }

        public BindableCollection<TimeItemViewModel> TimeItems
        {
            get { return timeItems; }
            set
            {
                timeItems = value;
                NotifyOfPropertyChange(() => TimeItems);
            }
        }

        public BindableCollection<TimeItem> Items
        {
            get { return items; }
            set
            {
                items = value;
                NotifyOfPropertyChange(() => Items);
            }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public TimelineManager TimelineManager
        {
            get { return AppState.TimelineManager; }
        }

        #region IModule Members

        #endregion

        public void RowSettings()
        {
            var rcvm = new RowsConfigViewModel { Plugin = Plugin, TimeTab = this };
            var p = view.TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
            p.X += 200;
            p.Y -= 150;
            var fe = FloatingHelpers.CreateFloatingElement("Timeline Settings", p, new Size(375, 300), rcvm);
            //fe.Style = Application.Current.FindResource("SimpleContainer") as Style;
            AppState.FloatingItems.AddFloatingElement(fe);
        }

        public void ShowLog()
        {
            var vm = new EventMessagesViewModel { Plugin = Plugin, TimeTab = this };
            var p = view.TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
            p.X += 250;
            p.Y +=  50;
            var fe = FloatingHelpers.CreateFloatingElement("Event messages log (filtered)", p, new Size(400, 500), vm);
            AppState.FloatingItems.AddFloatingElement(fe);
        }

        public void Switch()
        {
            AppState.DockedFloatingElementsVisible = !AppState.DockedFloatingElementsVisible;
        }

        private readonly DispatcherTimer dt = new DispatcherTimer();
        private bool forceRefresh;

        protected override void OnViewLoaded(object v)
        {
            base.OnViewLoaded(v);
            view = (TimeTabView)v;
            AppState.ViewDef.MapControl.ExtentChanged += MapControl_ExtentChanged;
            AppState.EventLists.CollectionChanged += EventLists_CollectionChanged;
            dt.Interval = new TimeSpan(0, 0, 0, 0, 200);
            dt.Tick += dt_Tick;
            dt.Start();

            //AppState.EventLists.FilteredList.CollectionChanged += TimeEvents_CollectionChanged;

            //int throttle = 300; //seconds

            //var filtersChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
            //                       eh => AppState.EventLists.FilteredList.CollectionChanged += eh,
            //                       eh => AppState.EventLists.FilteredList.CollectionChanged -= eh);

            //var filtersRemoved =
            //    from change in filtersChanged
            //    where change.EventArgs.Action == NotifyCollectionChangedAction.Remove
            //    from filter in change.EventArgs.OldItems.Cast<IEvent>()
            //    select filter;

            //var filtersAdded =
            //    from change in filtersChanged
            //    where change.EventArgs.Action == NotifyCollectionChangedAction.Add
            //    from filter in change.EventArgs.NewItems.Cast<IEvent>()
            //    select filter;

            //var filterPropertyChanges =
            //    from filter in filtersAdded
            //    from propertyChanged in Observable.FromEventPattern<PropertyChangedEventArgs>(filter, "PropertyChanged")
            //        .TakeUntil(filtersRemoved.Where(removed => removed == filter))
            //    select System.Reactive.Unit.Default;

            //var _rxFilters =
            //    new[]
            //    {
            //        filtersAdded.Select(_ => System.Reactive.Unit.Default), 
            //        filtersRemoved.Select(_ => System.Reactive.Unit.Default), 
            //        filterPropertyChanges,
            //    }
            //    .Merge()
            //    .Throttle(TimeSpan.FromMilliseconds(throttle))
            //    //.ObserveOnDispatcher() //System.Reactive.Windows.Threading asm
            //    .Subscribe(ApplyFilter);


            //AppState.EventLists.FilteredList
            //Items.CollectionChanged += (e, s) => UpdateVisibility();

            //AppState.TimelineManager.TimeContentChanged += (e, s) => UpdateVisibility();
            //vi.Items.ItemsSource = TimeItems;


            //var el = new EventList();
            //AppState.EventLists.AddEventList(el);


            //for (int i = 0; i < 100; i++)
            //{

            //    var d = new DateTime(2013, 10, 4).AddHours(i);

            //    var e = new EventBase()
            //    {

            //        Category = "test",
            //        Date = d,

            //        Image = new BitmapImage(new Uri("http://nl.waga4.com/img/home/small_web.png")),

            //        Name = d.ToShortDateString() + " " + d.ToShortTimeString(),

            //        ShowOnTimeline = true,

            //        AlwaysShow = true,
            //        Parent = el
            //    };

            //    el.Add(e);
            //}
            //for (int i = 0; i < 100; i++)
            //{

            //    var d = new DateTime(2013, 10, 4,0,30,0).AddHours(i);

            //    var e = new EventBase()
            //    {

            //        Category = "test2",
            //        Date = d,

            //        Image = new BitmapImage(new Uri("http://nl.waga4.com/img/home/small_web.png")),

            //        Name = d.ToShortDateString() + " " + d.ToShortTimeString(),

            //        ShowOnTimeline = true,

            //        AlwaysShow = true,
            //        Parent = el
            //    };

            //    el.Add(e);
            //}
        }

        void dt_Tick(object sender, EventArgs e)
        {
            if (!forceRefresh && (DateTime.Now - renewTimeTabs).TotalMilliseconds < 500) return;
            renewTimeTabs = DateTime.Now;
            forceRefresh = false;
            UpdateVisibility();
        }

        void EventLists_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null) return;
            foreach (var enew in e.NewItems)
            {
                var ne = enew as EventList;
                if (ne == null) continue;
                ne.CollectionChanged += ne_CollectionChanged;
            }
            forceRefresh = true;
            //throw new NotImplementedException();
        }

        void ne_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            forceRefresh = true;
        }

        void MapControl_ExtentChanged(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {
            forceRefresh = true;
        }

        public void ApplyFilter(Unit e)
        {
            forceRefresh = true;
        }

        private void TimeEvents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //return;
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                forceRefresh = true;
            }
            else
            {
                if (e.NewItems != null)
                    foreach (var n in e.NewItems)
                        AddEvent(n as IEvent);
                if (e.OldItems == null) return;
                foreach (var d in e.OldItems) RemoveEvent(d as IEvent);
            }
        }

        public void Next()
        {
            foreach (var i in Items.Where(k => k.StartTime > TimelineManager.FocusTime))
            {
                var r = TimelineManager.GetRow(i.RowId);
                if (r != null && r.Visible) TimelineManager.SetFocusTime(i.StartTime);
            }
        }

        public void Previous()
        {
        }

        public void ResetTimeline()
        {
            RemoveAllEvents();
            forceRefresh = true;
        }

        private DateTime renewTimeTabs;

        private void UpdateVisibility()
        {
            var elist = AppState.EventLists.GetAllFilteredEvents();
            foreach (var e in elist.Where(k => TimeItems.All(t => t.Item.Id != k.Id)).ToList())
                AddEvent(e);
            var remlist = TimeItems.Where(k => elist.All(t => t.Id != k.Item.Id)).ToList();
            remlist.Reverse();
            foreach (var e in remlist.ToList())
                RemoveEvent(e.Item);


            //foreach (
            //    IEvent ti in
            //        AppState.EventLists.FilteredList.Where(
            //            k => !k.Visible && k.Date >= TimelineManager.Start && k.Date < TimelineManager.End)
            //            .ToList())
            //{
            //    AddEvent(ti);
            //}

            //foreach (
            //    TimeItemViewModel ti in
            //        TimeItems.Where(
            //            k =>
            //                k.Item.Visible && (k.Item.Date < TimelineManager.Start || k.Item.Date > TimelineManager.End))
            //            .ToList())
            //{
            //    RemoveEvent(ti.Item);
            //}
        }

        private void RemoveEvent(IEvent ti)
        {
            ti.Visible = false;
            TimeItems.RemoveRange(TimeItems.Where(k => k.Item == ti).ToList());
        }

        private void RemoveAllEvents()
        {
            foreach (var ti in TimeItems) ti.Item.Visible = false;
            TimeItems.Clear();
        }

        private void AddEvent(IEvent ti)
        {
            var r = TimelineManager.GetRow(ti.Category);
            if (r == null || !r.Visible) return;
            ti.Visible = true;
            TimeItems.Add(new TimeItemViewModel { Item = ti, Row = r });
        }
    }

    public class TimeItemPositionConverter : IMultiValueConverter
    {
        static readonly TimelineManager Tlm = AppStateSettings.Instance.TimelineManager;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var ti = (TimeItemViewModel)values[0];
            var r = (Tlm.End - Tlm.Start).TotalSeconds;
            var w = Application.Current.MainWindow.ActualWidth;
            var p = w / r * (ti.Item.Date - Tlm.Start).TotalSeconds;
            return p;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}