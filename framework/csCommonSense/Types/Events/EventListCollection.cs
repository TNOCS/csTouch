using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Data;
using ESRI.ArcGIS.Client.Geometry;
using OxyPlot;

namespace csEvents
{
    public class EventListCollection : ObservableCollection<EventList>
    {
        private readonly object listlock = new object();
        private DateTime startTime;
        private DateTime endTime;
        public event EventHandler<NewEventArgs> NewEvent;
        public event EventHandler<NewEventArgs> ResetEvent;
        public event EventHandler<NewEventArgs> RemoveEvent;
        private Envelope envelope;

        public event EventHandler FilteredListUpdated; // _FIXME: FilteredListUpdated may never be invoked?

        public EventListCollection()
        {
            BindingOperations.EnableCollectionSynchronization(this, listlock);
        }

        public bool TimeFilter { get; set; }

        public bool MapFilter { get; set; }

        //private EventList filteredList = new EventList();

        //public EventList FilteredList
        //{
        //    get { return filteredList;}
        //    set { filteredList = value;}
        //}
	
        
        public void SetTime(DateTime startTime, DateTime endTime) // REVIEW TODO fix: Async removed
        {            
            this.startTime = startTime;
            this.endTime = endTime;
            hasChanged = true;
            GetAllFilteredEvents();
            //var a = FilteredList.Where(k => k.Date < startTime || k.Date < endTime).ToList();
            //if (a.Any()) foreach (var c in a) FilteredList.Remove(c);

            //CheckMissingItems();

        }

        //private void CheckMissingItems()
        //{
            
        //        foreach (var l in Items.ToList())
        //        {
        //            var a = l; //.Where(k => k.Date >= startTime && k.Date <= endTime);
        //            var r = FilteredList.Where(k => !a.Contains(k) && l.Contains(k)).ToList();
                    
        //            var b = a.Where(k => !FilteredList.Contains(k) && l.Contains(k)).ToList();
        //            Execute.OnUIThread(() =>
        //                {
        //                    if (r.Any()) //FilteredList.RemoveRange(r);
        //                        foreach (var c in r)
        //                            FilteredList.Remove(c);
        //                    if (b.Any()) //FilteredList.AddRange(b);
        //                        foreach (var c in b)
        //                            FilteredList.Add(c);
        //                });
                    
        //        }
               
            
        //}

        public void SetMapExtent(Envelope anEnvelope)
        {
            if (anEnvelope == null) return;
            envelope = anEnvelope;
            GetAllFilteredEvents();
            //    var a = FilteredList.Where(k => !k.InsideEnvelope(envelope)).ToList();
            //    if (a.Any())
            //        foreach (var @event in a)
            //        {
            //            FilteredList.Remove(@event);
            //        }


            //    CheckMissingItems();
        }

        /// <summary>
        /// Filter the list of events unless we ignore the filter (IgnoreFilter == true).
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IEnumerable<IEvent> Filter(IEnumerable<IEvent> list)
        {
            if (envelope == null) return list.ToList();
            // Check whether we should ignore the filter, and if not, check
            // if the start time is between start/end of the timeline, 
            // or if the end time is between start/end of the timeline, 
            // or if the start time is before the start of the timeline and the end time is after the end of the timeline
            // and the event takes place inside the enveloppe
            return list.ToList().Where(k => k.IgnoreFilter ||
                (( (startTime <= k.Date && k.Date <= endTime) 
                || (startTime <= k.Date.Add(k.TimeRange) && k.Date.Add(k.TimeRange) <= endTime) 
                || (startTime > k.Date && k.Date.Add(k.TimeRange) < endTime)) 
                && (k.Latitude.IsZero() || k.InsideEnvelope(envelope))));
        }


        public DateTime oldStartTime;
        public DateTime oldEndtime;
        public DateTime RefreshTime= DateTime.Now;
        public Envelope oldEnvelope;
        public TimeSpan IntervalTime = new TimeSpan(0,0,0,0,100);
        public List<IEvent> oldFilteredList= new List<IEvent>();
        private bool hasChanged;

        /// <summary>
        /// Gets the list of all events in the timeline's current time range.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Note that we also need to check whether no events have been added or removed, which is why we check for CollectionChanged events.
        /// </remarks>
        public List<IEvent> GetAllFilteredEvents()
        {
            if (!hasChanged && startTime == oldStartTime && endTime == oldEndtime && envelope == oldEnvelope)
                return oldFilteredList;
            hasChanged = false;
            
            //if ((DateTime.Now - RefreshTime) < IntervalTime)
            //    return oldFilteredList;
            
            oldStartTime = startTime;
            oldEndtime   = endTime;
            oldEnvelope  = envelope;
            RefreshTime  = DateTime.Now;

            var templist = new List<IEvent>();
            foreach (var el in this.ToList())
            {
                templist.AddRange(Filter(el));
            }
            oldFilteredList = templist;
            return templist;
            // TODO: Check if anything changed?
            // FIXME TODO: Unreachable code
            //if (templist.Intersect(oldFilteredList).Count() != templist.Count())
//                if (FilteredListUpdated!=null)
//                    FilteredListUpdated.Invoke(this, null);
//            oldFilteredList = templist;
//            return templist;
        }

        public IEnumerable<IEvent> GetEvents(IList list) {
            return list.Cast<IEvent>();
        }

        public void AddEventList(EventList eventList)
        {
            Add(eventList);

            eventList.CollectionChanged += (e, f) =>
            {
                hasChanged = true;
                if (NewEvent != null)
                {
                    switch (f.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            var fl = Filter(GetEvents(f.NewItems));
                            //FilteredList.AddRange(fl);
                            foreach (var eb in fl)
                            {
                                //FilteredList.Add(eb);
                                OnNewEvent(eb);
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (IEvent re in f.OldItems)
                            {
                                //if (FilteredList.Contains(re)) FilteredList.Remove(re);
                                if (RemoveEvent != null) RemoveEvent(this, new NewEventArgs {e = re});
                            }
                            break;
                    }
                }
                else if (ResetEvent != null)
                {
                    switch (f.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            ResetEvent(this, new NewEventArgs());
                            break;
                    }
                }
                else if (RemoveEvent != null)
                {
                    switch (f.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            var fl = Filter(GetEvents(f.NewItems));

                            //FilteredList.RemoveRange(fl);
                            foreach (IEvent eb in fl)
                            {
                                //FilteredList.Remove(eb);
                                RemoveEvent(this, new NewEventArgs() {e = eb});
                            }
                            break;
                    }
                }
            };
        }

        private void OnNewEvent(IEvent iEvent)
        {
            var handler = NewEvent;
            if (handler != null) handler(this, new NewEventArgs(iEvent));
        }
    }
}