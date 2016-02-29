using System;
using System.Diagnostics;
using System.Web.UI.WebControls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ProtoBuf;

namespace csEvents
{
    public enum EventState
    {
        red,
        orange,
        green,
        grey
    }

    public enum EventClickBehaviour
    {
        MapZoom,
        TimeZoom
    }

    public interface IEvent
    {
        Guid        Id             { get; set; }
        string      Name           { get; set; }
        TimeSpan    TimeRange      { get; set; }
        EventState  State          { get; set; }
        DateTime    Date           { get; set; }
        string      Icon           { get; set; }
        bool        ShowOnTimeline { get; set; }
        bool        ShowInList     { get; set; }
        string      Description    { get; set; }
        string      Source         { get; set; }
        bool        Handled        { get; set; }
        string      Category       { get; set; }
        ImageSource Image          { get; set; }
        EventList   Parent         { get; set; }
        double      Latitude       { get; set; }
        double      Longitude      { get; set; }
        Color       Color          { get; set; }
        bool        Visible        { get; set; }
        Object     Content         { get; set; }
        /// <summary>
        /// If true, should not be filtered (and therefore, always be shown).
        /// </summary>
        bool        IgnoreFilter   { get; set; }

        bool InsideEnvelope(Envelope env);
        event EventHandler<EventList.EventClickedArgs> Clicked;
        void TriggerClicked(object sender, string command);
    }

    [ProtoContract]
    [Serializable]
    [DebuggerDisplay("[{Date}] {Name} at ({Longitude}, {Latitude}).")]    
    public class EventBase : PropertyChangedBase, ICloneable, IEvent
    {
        public EventBase()
        {
            Id = Guid.NewGuid();
        }

        public event EventHandler<EventList.EventClickedArgs> Clicked;

        public void TriggerClicked(object sender, string command)
        {
            var handler = Clicked;
            if (handler != null) handler(sender, new EventList.EventClickedArgs { command = command, e = this, sender = sender });
            var eventList = Parent;
            if (eventList != null) eventList.TriggerClicked(sender, this, command);
        }

        private EventList parent;
        public EventList Parent
        {
            get { return parent; }
            set { parent = value; }
        }
        
        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; NotifyOfPropertyChange(() => Description); }
        }

        private string referenceid;
        public string ReferenceId
        {
            get { return referenceid; }
            set { referenceid = value; NotifyOfPropertyChange(() => ReferenceId); }
        }

        private object content;

        public object Content
        {
            get { return content; }
            set { content = value; NotifyOfPropertyChange(()=>Content); }
        }
        

        private bool visible;

        public bool Visible
        {
            get { return visible; }
            set { visible = value; NotifyOfPropertyChange(()=> Visible); }
        }

        /// <summary>
        /// If true, should not be filtered (and therefore, always be shown).
        /// </summary>
        public bool IgnoreFilter
        {
            get { return ignoreFilter; }
            set {
                if (value.Equals(ignoreFilter)) return;
                ignoreFilter = value;
                NotifyOfPropertyChange(() => IgnoreFilter);
            }
        }

        private string source;
        public string Source
        {
            get { return source; }
            set { source = value; NotifyOfPropertyChange(() => Source); }
        }

        private bool showOnTimeline;
        public bool ShowOnTimeline
        {
            get { return showOnTimeline; }
            set { showOnTimeline = value; NotifyOfPropertyChange(() => ShowOnTimeline); }
        }

        private double latitude;
        public double Latitude
        {
            get { return latitude; }
            set { latitude = value; NotifyOfPropertyChange(() => Latitude); }
        }

        private double longitude;
        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; NotifyOfPropertyChange(() => Longitude); }
        }

        private bool handled;
        public bool Handled
        {
            get { return handled; }
            set { handled = value; NotifyOfPropertyChange(() => Handled); }
        }

        private bool showInList;
        public bool ShowInList
        {
            get { return showInList; }
            set { showInList = value; NotifyOfPropertyChange(() => ShowInList); }
        }
         private bool alwaysShow;
         public bool AlwaysShow
        {
            get { return alwaysShow; }
            set { alwaysShow = value; NotifyOfPropertyChange(() => AlwaysShow); }
        }

        private string category;
        public string Category
        {
            get { return category; }
            set { category = value; NotifyOfPropertyChange(() => Category); }
        }

        private string icon = "pack://application:,,,/csCommon;component/Resources/Icons/Message.png";
        public string Icon
        {
            get { return icon; }
            set { icon = value; NotifyOfPropertyChange(() => Icon); }
        }

        private ImageSource image;

        public ImageSource Image
        {
            get { return image; }
            set { image = value; NotifyOfPropertyChange(()=>Image); }
        }
        
        private Guid id;
	    public Guid Id
	    {
		    get { return id;}
    		set { id = value;}
	    }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(() => Name); }
        }

        private TimeSpan timeRange;
        public TimeSpan TimeRange
        {
            get { return timeRange; }
            set { timeRange = value; NotifyOfPropertyChange(() => TimeRange); }
        }

        private EventState state;
        public EventState State
        {
            get { return state; }
            set { state = value; NotifyOfPropertyChange(() => State); }
        }

        private DateTime date;
        public DateTime Date
        {
            get { return date; }
            set { date = value; NotifyOfPropertyChange(() => Date); }
        }

        public bool InsideEnvelope(Envelope env)
        {
            if (AlwaysShow) return true;
            var w = new WebMercator();
            var mCoord = w.FromGeographic(new MapPoint(Longitude, Latitude)) as MapPoint;
            return mCoord != null && mCoord.X > env.XMin && mCoord.X < env.XMax && mCoord.Y > env.YMin && mCoord.Y < env.YMax;
        }

        public object Clone()
        {
            return new EventBase();
        }

        private Color color = Colors.Gray;
        private bool ignoreFilter;

        public Color Color
        {
            get { return color; }
            set { color = value; NotifyOfPropertyChange(()=>Color); }
        }
    }
}
