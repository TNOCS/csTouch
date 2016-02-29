using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProtoBuf;

namespace DataServer
{
    [ProtoContract]
    public class Track : BaseContent
    {
        private readonly object saveLock = new object();
        private readonly List<string> saveQueue = new List<string>();
        private bool isRunning;
        private Guid poiId;
        private DateTime startTime;
        private string title;

        public Service Service { get; set; } // FIXME TODO "new" keyword missing?

        public override string XmlNodeId
        {
            get { return "Track"; }
        }


        [ProtoMember(1)]
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        [ProtoMember(2)]
        public new Guid PoiId // REVIEW TODO Added new.
        {
            get { return poiId; }
            set
            {
                poiId = value;
                NotifyOfPropertyChange(() => PoiId);
            }
        }

        [ProtoMember(3)]
        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                startTime = value;
                NotifyOfPropertyChange(() => StartTime);
            }
        }

        private DateTime endTime;

        [ProtoMember(4)]
        public DateTime EndTime
        {
            get { return endTime; }
            set { endTime = value; NotifyOfPropertyChange(()=>EndTime); }
        }
        
        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                if (isRunning == value) return;

                isRunning = value;
                if (value && Started != null) Started(this, null);
                if (!value && Stoped != null) Stoped(this, null);
                NotifyOfPropertyChange(() => IsRunning);
            }
        }


        public PoI Poi { get; set; }

        private ObservableCollection<Position> history = new ObservableCollection<Position>();

        public ObservableCollection<Position> History
        {
            get { return history; }
            set { history = value; NotifyOfPropertyChange(()=>History); }
        }       

        public string FileName
        {
            get {
                if (PoiId!=null) return Service.Folder + "\\tracks\\" + PoiId.ToString() + ".t";
                return Service.Folder + "\\tracks\\" + Id.ToString() + ".t";
            }
        }

        public event EventHandler Started;
        public event EventHandler Stoped;

        internal void Start(PoI p)
        {
            if (p == null || Service == null) return;
            var folder = Service.Folder + "\\tracks\\";
            if (!FileStore.FolderExists(folder)) FileStore.CreateFolder(folder);
            Poi = p;
            PoiId = p.Id;
            IsRunning = true;            
            //StartTime = DateTime.Now;
            p.PositionChanged += p_PositionChanged;
        }

        internal void Stop()
        {
            IsRunning = false;
            EndTime = DateTime.Now;
            Poi.PositionChanged -= p_PositionChanged;
        }

        private void p_PositionChanged(object sender, PositionEventArgs e)
        {
            if (e.Position.Date.Ticks==0)
                e.Position.Date = DateTime.Now;
            History.Add(e.Position);
            //return;
            // FIXME TODO: Unreachable code
//            lock (saveLock)
//            {
//                saveQueue.Add(e.Position.ToCsv());
//                if (saveQueue.Count > 10)
//                {
//                    try
//                    {
//                        var s = saveQueue.Aggregate(string.Empty, (current, p) => current + (p + Environment.NewLine));
//                        saveQueue.Clear();                        
//                        Service.store.AppendString("Tracks", FileName, s);                        
//                    }
//                    catch (Exception)
//                    {
//                        var c = e.Position;
//                    }
//                }
//            }
        }

        

        public void Open(PoI p, PoiService service) // REVIEW TODO fix: async removed
        {
            var f = p.Id + ".t";
            string folder = service.Folder + "\\tracks\\";
            if (!service.store.HasFile(folder, f)) return;
            var his = service.store.GetString(folder + "\\" + f); // REVIEW TODO fix: await removed
            foreach (var ln in his.Split('\n'))
            {      
                try
                {
                    if (!ln.StartsWith("#")) {
                        var pos = new Position(ln);
                        History.Add(pos);
                    }
                }
                catch (Exception e)
                {
                    // FIXME TODO Deal with exception!
                    //Logger.Log("DataService","Error opening track file",e.Message,Logger.Level.Error);
                }
            }
        }
    }
}