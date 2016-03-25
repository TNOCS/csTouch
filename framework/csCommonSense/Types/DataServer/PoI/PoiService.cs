using Caliburn.Micro;
using csCommon.Utils.Collections;
using csDataServerPlugin;
using csEvents.Sensors;
using csShared;
using csShared.Geo;
using csShared.Utils;
using DataServer.SqlProcessing;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using IMB3;
using ProtoBuf;
using SharpMap.Geometries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using csCommon.Types.DataServer.PoI;
using csGeoLayers;
using csShared.Controls.Popups.MenuPopup;
//using csWebDotNetLib;
//using IO.Swagger.Model;
using Newtonsoft.Json.Linq;
//using Layer = IO.Swagger.Model.Layer;


namespace DataServer
{
    #region Selection

    public enum SelectionMode
    {
        None,
        Single,
        Multiple
    }

    #endregion Selection


    [Serializable]
    [ProtoContract]
    [DebuggerDisplay("Name: {Name}, Live: {Live}, IsTemplate: {IsTemplate}, #PoIs: {PoIs.Count}, #types: {PoITypes.Count}")]
    public class PoiService : Service
    {
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;

        [NonSerialized]
        public TEventEntry AudioStream;
        //public AudioRecorder Recorder;

        public event EventHandler<TappedEventArgs> Tapped;
        public event EventHandler<PoI> PoiLongTapped;

        public PoiService()
        {
            Id = Guid.NewGuid();

            #region Selection

            SelectedItems = new BindableCollection<PoI>();

            #endregion Selection
        }

        #region Selection

        [NonSerialized]
        private const int MessageSeparator = 71;


        public virtual List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            return new List<System.Windows.Controls.MenuItem>();
        }

        public void Start()
        {
            var layer = Layer as IStartStopLayer;
            if (layer != null) layer.Start();
        }

        public void ToProto()
        {
            using (var sw = new StreamWriter(ProtoFileName))
            {
                dsb.Model.SerializeWithLengthPrefix(sw.BaseStream, this, typeof(PoiService), PrefixStyle.Base128, MessageSeparator);
                sw.Close();
            }
        }

        public void InitSearchEngine()
        {
            SearchEngine = new DefaultSearch { Service = this };
            SearchEngine.Init();
        }

        public PoiService FromProto()
        {
            using (var ms = new MemoryStream())
            {

                var buffer = File.ReadAllBytes(ProtoFileName);
                ms.Write(buffer, 0, buffer.Length);
                ms.Position = 0;
                var s = Serializer.DeserializeWithLengthPrefix<PoiService>(ms, PrefixStyle.Base128, MessageSeparator);
                PoITypes = s.PoITypes;
                foreach (var p in s.PoITypes)
                {
                    p.Service = this;
                    //PoITypes.Add(p);
                }
                PoIs = s.poIs;
                foreach (var p in s.PoIs)
                {
                    p.Service = this;
                    //PoIs.Add(p);
                }

                return s;
                //message = (Message)model.Deserialize(ms, null, typeof(Message));
            }
        }

        [NonSerialized]
        private PoI selectedItem;

        public PoI SelectedItem
        {
            get { return selectedItem; }
            set
            {
                switch (Settings.SelectionMode)
                {
                    case SelectionMode.None:
                        return;

                    case SelectionMode.Single:
                        if (selectedItem == value && selectedItem != null)
                        {
                            selectedItem.IsSelected = !selectedItem.IsSelected;
                            if (!selectedItem.IsSelected)
                            {
                                SelectedItems.Remove(selectedItem);
                            }
                            break;
                        }

                        if (selectedItem != null)
                        {
                            selectedItem.IsSelected = false;
                            SelectedItems.Remove(selectedItem);
                        }
                        if (value != null)
                        {
                            value.IsSelected = true;
                            SelectedItems.Add(value);
                        }
                        break;

                    case SelectionMode.Multiple:
                        if (SelectedItems.Contains(value))
                        {
                            value.IsSelected = false;
                            SelectedItems.Remove(value);
                        }
                        else
                        {
                            value.IsSelected = true;
                            SelectedItems.Add(value);
                        }
                        break;
                }
                selectedItem = value;
                if (selectedItem != null)
                {
                    selectedItem.UpdateAnalysisStyle();
                    selectedItem.TriggerUpdated();
                }
                if (selectedItem == value) return;
                NotifyOfPropertyChange(() => SelectedItem);
            }
        }

        public ObservableCollection<PoI> SelectedItems { get; private set; }

        #endregion Selection

        /// <summary>
        /// Helper function to create a default PoiService in the default (PoiLayers) folder.
        /// </summary>
        /// <param name="dataServer"></param>
        /// <param name="serviceName">Name of the service (may contain spaces)</param>
        /// <param name="id">Unique GUID, e.g. Guid.NewGuid()</param>
        /// <param name="openTab">Open the Tab upon creation</param>
        /// <param name="autoStart">Should the service automatically start</param>
        /// <param name="isStatic">Do we create a static service</param>
        /// <returns>A new PoiService</returns>
        public static PoiService CreateService(DataServerBase dataServer, string serviceName, Guid id, bool openTab = false, bool autoStart = false, bool isStatic = true)
        {
            var res = new PoiService
            {
                IsLocal       = true,
                Name          = serviceName,
                Id            = id,
                IsFileBased   = false,
                StaticService = isStatic,
                AutoStart     = autoStart,
                HasSensorData = true,
                Mode          = Mode.client
            };

            res.Init(Mode.client, dataServer);
            res.Folder = Directory.GetCurrentDirectory() + @"\PoiLayers\" + serviceName;
            res.InitPoiService();
            //res.SettingsList = new ContentList
            //{
            //    ContentType = typeof(ServiceSettings)
            //};
            res.SettingsList.Add(new ServiceSettings());
            res.AllContent.Add(res.SettingsList);
            res.Settings.OpenTab = openTab;
            res.Settings.Icon    = "layer.png";

            dataServer.AddService(res, Mode.client);

            return res;
        }


        [NonSerialized]
        private bool live = true;

        public bool Live
        {
            get { return live; }
            set
            {
                live = value;
                NotifyOfPropertyChange(() => Live);
            }
        }

        public ISearchEngine SearchEngine { get; set; }

        [NonSerialized]
        private PoI me;

        public PoI Me
        {
            get { return me; }
            set
            {
                me = value;
                NotifyOfPropertyChange(() => Me);
            }
        }

        public void Clean() { while (PoIs.Any()) PoIs.RemoveAt(0); }

        [NonSerialized]
        private string searchFilter;

        public string SearchFilter
        {
            get { return searchFilter; }
            set
            {
                searchFilter = value;
                NotifyOfPropertyChange(() => SearchFilter);
            }
        }

        [ProtoMember(5)]
        public ContentList PoITypes;

        public SqlQueries SqlQueries { get; set; }

        [NonSerialized]
        private bool contentLoaded;

        public bool ContentLoaded
        {
            get { return contentLoaded; }
            set
            {
                contentLoaded = value;
                NotifyOfPropertyChange(() => ContentLoaded);
            }
        }

        private ContentList poIs;

        [ProtoMember(1)]
        public ContentList PoIs
        {
            get
            {
                return poIs ?? (poIs = new ContentList());
            }
            set
            {
                poIs = value;
                NotifyOfPropertyChange(() => PoIs);
            }
        }

        private ContentList events;

        [ProtoMember(2)]
        public ContentList Events
        {
            get { return events; }
            set
            {
                events = value;
                NotifyOfPropertyChange(() => Events);
            }
        }

        private ContentList eventTypes;

        public ContentList EventTypes
        {
            get { return eventTypes; }
            set
            {
                eventTypes = value;
                NotifyOfPropertyChange(() => EventTypes);
            }
        }



        private bool isTimelineEnabled;

        public bool IsTimelineEnabled
        {
            get { return isTimelineEnabled; }
            set
            {
                isTimelineEnabled = value;
                NotifyOfPropertyChange(() => IsTimelineEnabled);
            }
        }

        private BindableCollection<BaseContent> visibleContent = new BindableCollection<BaseContent>();

        public BindableCollection<BaseContent> VisibleContent
        {
            get { return visibleContent; }
            set
            {
                visibleContent = value;
                NotifyOfPropertyChange(() => VisibleContent);
            }
        }

        private BindableCollection<BaseContent> searchContent = new BindableCollection<BaseContent>();

        public BindableCollection<BaseContent> SearchContent
        {
            get { return searchContent; }
            set
            {
                searchContent = value;
                NotifyOfPropertyChange(() => SearchContent);
            }
        }

        private bool searchToLong;

        public bool SearchToLong
        {
            get { return searchToLong; }
            set
            {
                searchToLong = value;
                NotifyOfPropertyChange(() => SearchToLong);
            }
        }

        [NonSerialized]
        private readonly BindableCollection<BaseContent> contentInExtent = new BindableCollection<BaseContent>();

        public BindableCollection<BaseContent> ContentInExtent
        {
            get { return contentInExtent; }
        }

        [NonSerialized]
        private SortedObservableCollection<BaseContent> timeLine = new SortedObservableCollection<BaseContent>();

        public SortedObservableCollection<BaseContent> TimeLine
        {
            get { return timeLine; }
            set
            {
                timeLine = value;
                NotifyOfPropertyChange(() => TimeLine);
            }
        }

        public void AddTimelineItem(BaseContent b)
        {
            if (!TimeLine.Contains(b)) TimeLine.Add(b);

            //TimeLine.DescendingSort((x, y) => x.Date.CompareTo(y.Date));
        }

        public event EventHandler VisibilityChanged;

        private bool isVisible = true;

        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value) return;
                isVisible = value;
                NotifyOfPropertyChange(() => IsVisible);
                if (VisibilityChanged != null) VisibilityChanged(this, null);
            }
        }


        public BindableCollection<Track> LocalTracks { get; set; }

        private ContentList tasks;

        [ProtoMember(3)]
        public ContentList Tasks
        {
            get { return tasks; }
            set
            {
                tasks = value;
                NotifyOfPropertyChange(() => Tasks);
            }
        }

        public string Roles
        {
            get
            {
                if (!Settings.Labels.ContainsKey("Roles"))
                {
                    Settings.Labels["Roles"] = "";
                }
                return Settings.Labels["Roles"];
            }
            set { Settings.Labels["Roles"] = value; }
        }

        public void Ping(BaseContent bc)
        {
            if (!client.IsConnected) return;
            var dcm = new ContentMessage { Id = bc.Id, Action = ContentMessageActions.Ping, Sender = client.Id };
            var msc = new MemoryStream();
            Model.SerializeWithLengthPrefix(msc, bc, typeof(BaseContent), PrefixStyle.Base128, 0);

            dcm.Content = msc.ToArray();
            var content = dcm.ConvertToStream().ToArray();
            serviceChannel.SignalBuffer(0, content);
            msc.Dispose();
        }

        public void InitPoiService()
        {
            PoITypes = new ContentList { ContentType = typeof(PoI), Id = "poitypes", Service = this };
            AllContent.Add(PoITypes);

            SqlQueries = new SqlQueries { ContentType = typeof(SqlQuery), Id = "sqlqueries", Service = this };
            AllContent.Add(SqlQueries);

            PoIs = new ContentList { ContentType = typeof(PoI), Id = "pois", Service = this, IsRessetable = true };
            AllContent.Add(PoIs);

            EventTypes = new ContentList { ContentType = typeof(Event), Id = "eventtypes", Service = this };
            AllContent.Add(EventTypes);

            Events = new ContentList { ContentType = typeof(Event), Id = "events", Service = this, IsRessetable = true };
            AllContent.Add(Events);

            Tasks = new ContentList { ContentType = typeof(Task), Id = "tasks", Service = this, IsRessetable = true };
            AllContent.Add(Tasks);

            LocalTracks = new BindableCollection<Track>();
            if (!string.IsNullOrEmpty(Folder))
            {
                if (!FileStore.FolderExists(Folder)) FileStore.CreateFolder(Folder);
                if (!FileStore.FolderExists(MediaFolder)) FileStore.CreateFolder(MediaFolder);
                if (!FileStore.FolderExists(BackupFolder)) FileStore.CreateFolder(BackupFolder);
            }

            Initialized                += PoiService_Initialized;
            PoITypes.CollectionChanged += PoITypesCollectionChanged;
            Events.CollectionChanged   += EventsCollectionChanged;
            PoIs.CollectionChanged     += PoIsCollectionChanged;
        }

        public string SensorFileName { get { return Path.Combine(Folder ?? string.Empty, string.Format("{0}.{1}.s", Id, Name)); } }

        public string BinarySensorFileName { get { return string.Format("{0}b", SensorFileName); } }

        private bool hasSensorData;

        public bool HasSensorData
        {
            get { return hasSensorData; }
            set { hasSensorData = value; NotifyOfPropertyChange(() => HasSensorData); }
        }

        #region sensordata

        /// <summary>
        /// Reads sensor data from the default filename and loads it into the poi's. In case a binary version exists,
        /// the text version is neglected.
        /// </summary>
        public void ReadSensorFile()
        {
#if !WINDOWS_PHONE
            if (store.HasFile(BinarySensorFileName))
            {
                ReadBinarySensorData();
                return;
            }
            if (!store.HasFile(SensorFileName)) return;
            foreach (var l in store.ReadAllLines(SensorFileName))
            {
                var s = l.Split(',');
                if (s.Count() != 4) continue;
                HasSensorData = true;
                var id = Guid.Parse(s[1]);
                var p = PoIs.FirstOrDefault(k => k.Id == id);
                if (p == null) continue;
                if (p.Sensors == null) p.Sensors = new SensorSet();
                var key = s[2];
                if (!p.Sensors.ContainsKey(key))
                {
                    p.Sensors[key] = new DataSet { Data = new ConcurrentObservableSortedDictionary<DateTime, double>() };
                }
                // TODO Are we dealing with doubles, floats, bools or strings?
                var date = long.Parse(s[0]).FromEpoch();
                var value = double.Parse(s[3], CultureInfo.InvariantCulture);
                p.Sensors[key].Data[date] = value;
            }
#endif
        }

        [NonSerialized]
        private bool isLoadingSensorData;

        public bool IsLoadingSensorData
        {
            get { return isLoadingSensorData; }
            set { isLoadingSensorData = value; NotifyOfPropertyChange(() => IsLoadingSensorData); }
        }

        /// <summary>
        /// Read the specifed sensor file and apply it to your PoIs.
        /// </summary>
        /// <param name="file"></param>
        public void ReadSensorFile(string file)
        {
#if !WINDOWS_PHONE
            if (!File.Exists(file) || !string.Equals(Path.GetExtension(file), ".s")) return;
            var allLines = File.ReadAllLines(file);
            ReadSensorData(allLines);
#endif
        }

        /// <summary>
        /// Read the specifed sensor stream and apply it to your PoIs.
        /// </summary>
        /// <param name="stream"></param>
        public void ReadSensorFile(Stream stream)
        {
#if !WINDOWS_PHONE
            var allLines = stream.ReadAllLines(Encoding.UTF8);
            ReadSensorData(allLines);
#endif
        }


        private void ReadSensorData(IEnumerable<string> allLines)
        {
#if !WINDOWS_PHONE
            var sensorData = false;
            foreach (var s in allLines.Select(l => l.Split(',')).Where(s => s.Count() == 4))
            {
                sensorData = true;
                try
                {
                    var id = Guid.Parse(s[1]);
                    var p = PoIs.FirstOrDefault(k => k.Id == id);
                    if (p == null) continue;
                    if (p.Sensors == null) p.Sensors = new SensorSet();
                    var key = s[2];
                    if (!p.Sensors.ContainsKey(key))
                    {
                        p.Sensors[key] = new DataSet { Data = new ConcurrentObservableSortedDictionary<DateTime, double>() };
                    }
                    // TODO Are we dealing with doubles, floats, bools or strings?
                    var date = long.Parse(s[0]).FromEpoch();
                    var value = double.Parse(s[3], CultureInfo.InvariantCulture);
                    p.Sensors[key].Data[date] = value;
                }
                catch (Exception e)
                {
                    AppState.TriggerNotification("Error reading sensor data: " + e.Message);
                }
            }
            HasSensorData = sensorData;
#endif
        }

        /// <summary>
        /// Reads binary sensor data from the default filename and loads it into the poi's.
        /// </summary>
        private void ReadBinarySensorData()
        {




#if !WINDOWS_PHONE
            if (IsLoadingSensorData) return;
            IsLoadingSensorData = true;
            var d = AppState.AddDownload("Loding sensordata", "");
            try
            {
                using (var fs = File.OpenRead(BinarySensorFileName))
                {
                    var sdc = Serializer.Deserialize<SensorDataCollection>(fs);
                    foreach (var sd in sdc.SensorDataItems)
                    {
                        var p = PoIs.FirstOrDefault(k => k.Id == sd.Id);
                        if (p == null) continue;
                        if (p.Sensors == null) p.Sensors = new SensorSet();
                        //p.Sensors[sd.SensorName] = new DataSet { Data = new SortedDictionary<DateTime, double>(sd.DataItems.ToDictionary(kv => kv.Key, kv => kv.Value)) };
                        if (!p.Sensors.ContainsKey(sd.SensorName))
                        {
                            if (sd.DataItemsDouble != null)
                            {
                                if (!hasSensorData) HasSensorData = true;
                                p.Sensors[sd.SensorName] = new DataSet
                                {
                                    Data = new ConcurrentObservableSortedDictionary<DateTime, double>(),
                                    Sensor = sd.Sensor
                                };
                            }
                            else if (sd.DataItemsDoubleDouble != null)
                                p.Sensors[sd.SensorName] = new DataSet
                                {
                                    DData = new SortedDictionary<double, double>(),
                                    Sensor = sd.Sensor
                                };
                        }
                        if (sd.DataItemsDouble != null)
                            foreach (var di in sd.DataItemsDouble)
                            {
                                p.Sensors[sd.SensorName].Data[di.Key] = di.Value;
                            }
                        if (sd.DataItemsDoubleDouble != null)
                            foreach (var di in sd.DataItemsDoubleDouble)
                            {
                                p.Sensors[sd.SensorName].DData[di.Key] = di.Value;
                            }
                    }
                }
            }
            catch (Exception exception)
            {

                Logger.Log("Sensor Data", "Error loading binary sensor data", exception.Message, Logger.Level.Error);
            }
            finally
            {
                IsLoadingSensorData = false;
                AppState.FinishDownload(d);
            }

#endif

        }

        /// <summary>
        /// Finds all sensor data in the poi files, and saves it into a text-based .s file with the default name
        /// </summary>
        public void SaveSensorData()
        {
            var sb = new StringBuilder();
            foreach (var p in PoIs.Where(p => p.Sensors != null))
            {
                foreach (var ds in p.Sensors)
                {
                    if (ds.Value.Data != null)
                        foreach (var v in ds.Value.Data)
                        {
                            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", v.Key.ToEpoch(), p.Id, ds.Key, v.Value));
                        }
                    if (ds.Value.DData != null)
                        foreach (var v in ds.Value.DData)
                        {
                            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", v.Key, p.Id, ds.Key, v.Value));
                        }
                }
            }
            FileStore.SaveString(Folder, SensorFileName, sb.ToString());
        }

        /// <summary>
        /// Finds all sensor data in the poi files, and saves it into a binary .sb file with the default name.
        /// In case the binary sensor data file already exists, the method returns immediately.
        /// </summary>
        public void SaveBinarySensorData()
        {
            //if (store.HasFile(BinarySensorFileName)) return;
            var sdc = new SensorDataCollection();
            foreach (BaseContent poi in PoIs)
            {
                if (poi.Sensors != null)
                    foreach (KeyValuePair<string, DataSet> ds in poi.Sensors)
                    {
                        if (ds.Value.Data != null)
                        {

                            var sensorData = new SensorData { Id = poi.Id, SensorName = ds.Key, DataItemsDouble = Enumerable.ToList(ds.Value.Data), Sensor = ds.Value.Sensor };
                            sdc.SensorDataItems.Add(sensorData);
                        }
                    }
            }
            foreach (var sensorData in
                from p in PoIs.Where(poi => poi.Sensors != null)
                from ds in p.Sensors
                where ds.Value.DData != null
                select new SensorData { Id = p.Id, SensorName = ds.Key, DataItemsDoubleDouble = ds.Value.DData.ToList(), Sensor = ds.Value.Sensor })
            {
                sdc.SensorDataItems.Add(sensorData);
            }
            using (var fs = File.Create(BinarySensorFileName))
            {
                Serializer.Serialize(fs, sdc);
            }
        }

        #endregion sensordata

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FilterLocation")
                UpdateContentList();
        }

        public Uri IconUri
        {
            get
            {
                if (!string.IsNullOrEmpty(Settings.Icon))
                {
                    var f = MediaFolder + Settings.Icon; //
                    if (File.Exists(f)) return new Uri("file://" + f);
                }
                var l = MediaFolder + "layer.png";
                if (File.Exists(l)) return new Uri("file://" + l);

                return new Uri("pack://application:,,,/csCommon;component/Resources/Icons/layers3.png");

            }
        }

        private async void PoiService_Initialized(object sender, EventArgs e)
        {
            //Execute.OnUIThread(() =>
            //{
            //    //IsTimelineEnabled = !StaticService;
            //    //if (IsTimelineEnabled)
            //    //{
            //    //    //ResetTimeline();
            //    //}
            //});

            if (SqlQueries.Any())
            {
                await SqlQueries.Execute(this, null, AppState.ViewDef.MapControl.Extent.ConvertToPoints());
                AppState.ViewDef.MapControl.ExtentChanged -= MapControlOnExtentChanged;
                AppState.ViewDef.MapControl.ExtentChanged += MapControlOnExtentChanged;
            }

            foreach (var p in AllContent.SelectMany(cl => cl).ToList())
            {
                p.Service = this;
                p.CheckIcon();
            }

            var eventAsObservable = Observable.FromEventPattern<ContentChangedEventArgs>(ev => ContentChanged += ev, ev => ContentChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromSeconds(10)).Subscribe(_ => CheckSave());
            if (Settings != null) Settings.PropertyChanged += Settings_PropertyChanged;
            InitSearchEngine();
            SetExtent(AppState.ViewDef.WorldExtent);

        }

        private async void MapControlOnExtentChanged(object sender, ExtentEventArgs extentEventArgs)
        {
            await SqlQueries.Execute(this, null, AppState.ViewDef.MapControl.Extent.ConvertToPoints());
        }

        public void ResetTimeline()
        {
            TimeLine.Clear();
            //var t = new BindableCollection<BaseContent>();
            foreach (var p in PoIs) AddTimelineItem(p);
            foreach (var te in Events)
                AddTimelineItem(te);

            //TimeLine.DescendingSort((x, y) => x.Date.CompareTo(y.Date));
            //TimelineView.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
        }

        //private void WordsView_Filter(object sender, FilterEventArgs e)
        //{
        //}

        public void StartTracking(PoI p)
        {
            Track t;
            if (p.Data.ContainsKey("track"))
            {
                t = p.Data["track"] as Track;
            }
            else
            {
                t = new Track { Service = this };
                p.Data["track"] = t;
                LocalTracks.Add(t);
            }
            if (t == null) return;
            t.Open(p, this);
            if (!t.IsRunning) t.Start(p);
        }

        public Event CreateEvent(string name)
        {
            var e = EventTypes.FirstOrDefault(k => k.ContentId == name);
            if (e == null)
            {
                e = EventTypes.FirstOrDefault(k => k.Name == name);
                if (e != null) e.ContentId = name;
            }
            var myEvent = e as Event;
            if (myEvent == null) return null;
            var ev = myEvent;
            var ne = new Event
            {
                PoiTypeId = ev.ContentId,
                PoiType = ev,
                Service = this,
                Name = ev.Name,
                Date = AppState.TimelineManager.CurrentTime,
                Labels = new Dictionary<string, string>()
            };
            if (client != null) ne.UserId = client.Status.Name;
            foreach (var lk in ev.Labels) ne.Labels[lk.Key] = lk.Value;
            return ne;
        }

        public void StopTracking(PoI p)
        {
            if (!p.Data.ContainsKey("track")) return;
            var t = p.Data["track"] as Track;
            if (t != null) t.Stop();
            p.Data.Remove("track");
            LocalTracks.Remove(t);
        }

        private void PoIsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PoI np in e.NewItems)
                    {
                        if (np.Service == null) np.Service = this;
                        foreach (var m in np.AllMedia)
                        {
                            if (m.Content == null) m.Content = np;
                        }
                        np.TimelineString = np.Name + " added";
                        if (!string.IsNullOrEmpty(np.PoiTypeId) && np.PoiType == null)
                        {
                            var pt = PoITypes.FirstOrDefault(k => (k).ContentId == np.PoiTypeId);
                            np.PoiType = pt;
                        }
                        if (Settings != null && Settings.SelectionMode != SelectionMode.None && np.Data.ContainsKey("Data.IsSelected") && string.Equals("1", np.Data["Data.IsSelected"]))
                        {
                            np.IsSelected = true;
                            SelectedItems.Add(np);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (PoI np in e.OldItems)
                    {
                        np.TriggerDeleted();
                        StopTracking(np);
                    }
                    break;
            }
        }

        /// <summary>
        /// Create a clone of the data service, including images.
        /// </summary>
        /// <param name="folder">Folder of reference data service</param>
        /// <param name="file">Reference data service</param>
        /// <param name="newFolder">Output folder</param>
        /// <param name="newName">New data service name</param>
        /// <param name="autoSave">If true, saves the data service (default)</param>
        /// <param name="forceStatic">If true, create a static service.</param>
        /// <returns>Clone of an existing data service</returns>
        public static PoiService GetCleanClone(string folder, string file, string newFolder, string newName,
            bool autoSave = true, bool forceStatic = false)
        {
            if (!FileStore.FolderExists(newFolder)) return null;
            var referenceFile = folder + "\\" + file;
            if (!FileStore.FileExists(referenceFile)) return null;
            var s = new PoiService { Folder = folder, IsFileBased = true, IsLocal = true };
            s.InitPoiService();
            if (forceStatic) s.StaticService = true;
            var xml = s.store.GetString(referenceFile); // await 
            s.SettingsList.Clear();
            s.FromXml(xml, folder);
            s.Reset();
            s.Id = Guid.NewGuid();
            s.Folder = newFolder;
            s.RelativeFolder = newFolder.Replace(AppState.Config.Get("Poi.LocalFolder", "PoiLayers"), string.Empty);
            s.Name = newName;
            //var fn = s.FileName;
            if (folder != newFolder)
                CopyDirectory(Path.Combine(folder, "_Media"), Path.Combine(newFolder, "_Media"), "*.png");
            if (autoSave) s.SaveXml();
            return s;
        }

        /// <summary>
        /// Helper method to create a new data service based on a template. Opens the tab immediately.
        /// </summary>
        /// <param name="folder">Folder where the new service must be created</param>
        /// <param name="templateFile">Full file name of the template</param>
        /// <param name="newServiceName">Optional name of the new service (default uses the template name).</param>
        /// <returns>The newly created data service (or null on failure).</returns>
        public static async Task<PoiService> CreateTemplateBasedService(string folder, string templateFile, string newServiceName = "")
        {
            var templateName = Path.GetFileNameWithoutExtension(templateFile);
            newServiceName = CreateUniqueServiceName(string.IsNullOrEmpty(newServiceName) ? templateName : newServiceName);

            var clonedFile = await GetCleanClone(Path.GetDirectoryName(templateFile), templateName + ".dsd", folder, newServiceName);
            if (string.IsNullOrEmpty(clonedFile)) return null;
            var clonedDataService = AppState.DataServer.AddLocalDataService(folder, Mode.client, clonedFile, autoStart: true);
            clonedDataService.Initialized += (e, f) => AppState.ActivateStartPanelTabItem(clonedDataService.Name);
            clonedDataService.AutoStart = true;
            //clonedDataService.Start();
            return clonedDataService;
        }

        /// <summary>
        /// Create a unique service name.
        /// </summary>
        /// <param name="newServiceName"></param>
        /// <returns></returns>
        private static string CreateUniqueServiceName(string newServiceName)
        {
            if (!AppState.DataServer.Services.Any(s => Equals(s.Name, newServiceName))) return newServiceName;
            var i = 1;
            var baseName = newServiceName;
            newServiceName = baseName + i;
            while (AppState.DataServer.Services.Any(s => Equals(s.Name, newServiceName))) newServiceName = baseName + ++i;
            return newServiceName;
        }

        /// <summary>
        /// Create a clone of the data service, including images.
        /// Important the Id of the service is not changed; this is done when adding service
        /// </summary>
        /// <param name="folder">Folder of reference data service</param>
        /// <param name="file">Reference data service</param>
        /// <param name="newFolder">Output folder</param>
        /// <param name="newName">New data service name</param>
        /// <returns>Clone of an existing data service</returns>
        public static async Task<string> GetCleanClone(string folder, string file, string newFolder, string newName)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                if (!FileStore.FolderExists(newFolder)) return null;
                var referenceFile = folder + "\\" + file;
                if (!FileStore.FileExists(referenceFile)) return null;

                var res = string.Format("{0}.{1}.ds", Guid.NewGuid(), newName);
                var destFileName = Path.Combine(newFolder, res);
                try
                {
                    File.Copy(referenceFile, destFileName);
                    if (Path.GetFullPath(folder) != Path.GetFullPath(newFolder))
                    {
                        CopyDirectory(Path.Combine(folder, "_Media"), Path.Combine(newFolder, "_Media"), "*.png");
                        //CopyDirectory(Path.Combine(folder, "_Media"), Path.Combine(newFolder, "_Media"), "*.jpg");
                    }
                }
                catch (SystemException e)
                {
                    AppState.TriggerNotification(e.Message);
                }
                return destFileName;
            });

            //var s = new PoiService { Folder = folder, IsFileBased = true, IsLocal = true };
            //s.InitPoiService();
            //if (forceStatic) s.StaticService = true;
            //var xml = await s.store.GetString(referenceFile);
            //s.SettingsList.Clear();
            //s.FromXml(xml, folder);
            //s.Reset();
            //s.Id = Guid.NewGuid();
            //s.Folder = newFolder;
            //s.Name = newName;
            //var fn = s.FileName;
            //if (folder != newFolder)
            //    CopyDirectory(Path.Combine(folder, "_Media"), Path.Combine(newFolder, "_Media"), "*.png");
            //if (autoSave) s.SaveXml();
            //return s;
        }

        private static void CopyDirectory(string sourceDir, string targetDir, string ext)
        {
            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                foreach (var file in Directory.GetFiles(sourceDir, ext))
                    File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file) ?? string.Empty), true);

                foreach (var directory in Directory.GetDirectories(sourceDir))
                    CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory) ?? string.Empty), ext);
            }
            catch (SystemException e)
            {
                // FIXME TODO Deal with exception.
                //   AppStateSettings.Instance.TriggerNotification("Error copying directory: " + targetDir);
            }
        }

        /// <summary>
        /// Match events with pois
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            foreach (Event ne in e.NewItems)
            {
                if (ne.Service == null) ne.Service = this;
                foreach (var m in ne.AllMedia)
                {
                    if (m.Content == null) m.Content = ne;
                }

                if (!string.IsNullOrEmpty(ne.PoiTypeId) && ne.PoiType == null)
                {
                    var et = EventTypes.FirstOrDefault(k => k.ContentId == ne.PoiTypeId);
                    if (et is Event) ne.PoiType = et;
                }
                if (ne.PoI == null && ne.PoiId != Guid.Empty && PoIs.Any(k => k.Id == ne.PoiId))
                {
                    ne.PoI = PoIs.FirstOrDefault(k => k.Id == ne.PoiId) as PoI;
                }
                ne.TimelineString = ne.Labels.ContainsKey("Timeline")
                    ? ne.Labels["Timeline"]
                    : ne.Name;

                //if (IsInitialized && IsTimelineEnabled)
                {
                    var include = true;
                    if (ne.Labels.ContainsKey("To") && ne.UserId != client.Status.Name)
                    {
                        var to = ne.Labels["To"];
                        var tos = to.Split(',');
                        if (!tos.Contains(client.Status.Name)) include = false;
                    }
                    if (include) AddTimelineItem(ne);
                }
            }
        }

        /// <summary>
        /// update poi's if poitypes was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PoITypesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            // check for incomming poitypes if all poi references are coorect
            foreach (PoI pt in e.NewItems)
            {
                if (string.IsNullOrEmpty(pt.ContentId) && !string.IsNullOrEmpty(pt.Name)) pt.ContentId = pt.Name;
                foreach (PoI p in PoIs.Where(k => (k.PoiTypeId == pt.ContentId)))
                {
                    var prevNotifying = p.IsNotifying;
                    p.IsNotifying = false;
                    p.PoiType = pt;
                    Update(p);
                    // REVIEW TODO Implement below, or remove.
                    //                    if (pt.Style != null)
                    //                    {
                    //                        pt.Style.StyleChanged += (s, f) =>
                    //                        {
                    //                            var b = 10;
                    //                        };
                    //                    }
                    p.IsNotifying = prevNotifying;
                }
                if (string.IsNullOrEmpty(pt.PoiTypeId) || pt.PoiType != null) continue;
                var npt = PoITypes.FirstOrDefault(k => (k).ContentId == pt.PoiTypeId);
                pt.PoiType = npt;
            }
        }


        public dsBaseLayer Layer { get; set; }

        public event EventHandler AllPoisRefreshed; // Ignore Resharper. This is absolutely used!

        public event EventHandler<PoiUpdatedEventArgs> PoiUpdated;

        internal void Update(BaseContent p)
        {
            if (PoITypes.Contains(p))
            {
                foreach (var poi in PoIs.Where(k => k.PoiType == p))
                {
                    poi.UpdateEffectiveStyle();
                    Update(poi);
                }
            }
            else if (PoIs.Contains(p))
            {
                // TODO Check if we need this. It seems that it is computed twice when turning on/off the highlighter.
                //p.UpdateAnalysisStyle();

                var handler = PoiUpdated;
                if (handler != null) handler(this, new PoiUpdatedEventArgs { Poi = p });
            }
        }

        public override void ListReset(byte[] buffer, string channel)
        {
            base.ListReset(buffer, channel);
            var firstOrDefault = AllContent.FirstOrDefault(k => k.Id == channel);
            if (firstOrDefault != null)
            {
                foreach (var bc in firstOrDefault)
                {
                    var p = bc as PoI;
                    if (p == null) continue;
                    if (p.Style != null && p.Style.Icon != null) store.QueueBytes(p.Style.Icon);
                }
            }
            store.GetQueue();
        }

        internal void RemoveMe()
        {
            if (Me != null && PoIs.Contains(Me))
            {
                RemovePoi(Me);
            }
        }

        public void AddMe(Guid id, string meStyle)
        {
            if (!IsInitialized || !IsSubscribed) return;

            Me = new PoI { Id = id, Name = client.Status.Name, UserId = client.Status.Name, Date = AppState.TimelineManager.CurrentTime };
            //Me.Style = PoITypes.FirstOrDefault(k => k.Name == MeStyle).NEffectiveStyle;
            var meStylePoIType = PoITypes.FirstOrDefault(k => k.Name == meStyle);
            if (meStylePoIType != null)
                Me = new PoI
                {
                    Id = id,
                    Name = client.Status.Name,
                    UserId = client.Status.Name,
                    Date = AppState.TimelineManager.CurrentTime,
                    Style = meStylePoIType.NEffectiveStyle as PoIStyle // TODO Should this not be clone?
                };

            //if (Me.Style == null) return;

            if (PoITypes.Any())
            {
                Me.PoiTypeId = meStyle;
                if (PoITypes.Any(k => k.ContentId == Me.PoiTypeId))
                {
                    Me.PoiType = PoITypes.FirstOrDefault(k => k.ContentId == Me.PoiTypeId) as PoI;
                    Me.Style = Me.NEffectiveStyle.CloneStyle();
                    if (Me.Style != null)
                    {
                        Me.Style.CanDelete = false;
                        Me.UpdateEffectiveStyle();
                        Debug.Assert(false, "this code was called, but doesnt make sence to set on NEffectiveStyle (overwritten):Me.NEffectiveStyle.CanEdit = Me.NEffectiveStyle.CanMove = Me.NEffectiveStyle.CanRotate = false;");
                    }

                    //Me.Models = new List<Model>()
                    //=======
                    //                    Me.Style = Me.EffectiveStyle.Clone() as PoIStyle;
                    //                    if (Me.Style != null) {
                    //                        Me.Style.CanTrack = true;
                    //                        Me.Style.CanDelete = Me.EffectiveStyle.CanEdit = Me.EffectiveStyle.CanMove = Me.EffectiveStyle.CanRotate = false;
                    //                    }


                }
                else
                {
                    Me.PoiType = PoITypes[0];
                    Me.PoiTypeId = me.PoiType.ContentId;
                }
            }
            Me.Labels["mobile"] = "1";
            var old = PoIs.FirstOrDefault(k => k.Id == Me.Id);
            if (old != null)
            {
                Me = (PoI)old;
            }
            else
            {
                PoIs.Add(Me);
            }
        }

        public void Stop()
        {
            Layer.Stop();
        }

        public void StartRecording()
        {
            var e = new Event { Date = AppState.TimelineManager.CurrentTime };
            var ppt = new Media { Type = MediaType.PTT };
            e.AllMedia.Add(ppt);
            Events.Add(e);

            //Recorder.StartRecording(ppt, e, this);
        }

        public void StopRecording()
        {
            // Recorder.StopRecording();
        }

        public void RemovePoi(BaseContent p)
        {
            if (PoIs.Contains(p))
                PoIs.Remove(p);
            if (ContentInExtent.Contains(p))
                ContentInExtent.Remove(p);
            UpdateContentList();
            //RemoveContent(p,PoIs);
        }

        public void RemoveEvent(Event e)
        {
            RemoveContent(e, Events);
        }

        /// <summary>
        /// Delete content by setting IsDeleted is true and update priority
        /// </summary>
        public void RemoveContent(BaseContent content, ContentList list, DateTime date = new DateTime())
        {
            if (!list.Contains(content)) return;
            content.Priority = 5;
            content.TriggerChanged();
        }

        public BoundingBox Extent { get; set; }

        [NonSerialized]
        private readonly object updateContentLock = new object();

        //private static readonly AppStateSettings appState = AppStateSettings.Instance;

        private bool _isUpdatingContentList = false;

        public void UpdateContentList()
        {
            if (!IsInitialized || _isUpdatingContentList) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                lock (updateContentLock)
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    _isUpdatingContentList = true;
                    try
                    {
                        //Console.WriteLine("Updating contentlist");
                        var start = AppState.TimelineManager.CurrentTime;
                        //if (PoIs.Count > 200 && !Settings.FilterLocation) Settings.FilterLocation = true;
                        // Settings.FilterLocation = true;
                        //VisibleContent.IsNotifying = false;
                        if (Extent == null || Settings == null) return;
                        if (contentInExtent == SearchContent)
                            SearchContent = null;
                        var remove = contentInExtent
                            .Where(p => !p.IsVisibleInExtent(Extent))
                            .ToArray();
                        var add = PoIs
                            .Where(p => !contentInExtent.Contains(p) && (p.IsVisibleInExtent(Extent)))
                            .ToArray();
                        Debug.WriteLine("Add: {0}, Remove: {1}", add.Length, remove.Length);
                        contentInExtent.RemoveRange(remove);
                        contentInExtent.AddRange(add);

                        var mapViewExtent = AppState.ViewDef.Extent;
                        var mapViewExtentEnvelope = new Envelope(mapViewExtent.MinX, mapViewExtent.MinY,
                            mapViewExtent.MaxX, mapViewExtent.MaxY);
                        /*
                        Execute.OnUIThread(() =>
                        {
                            try
                            {
                                var graphics = Layer.ChildLayers.OfType<GraphicsLayer>()
                                        .SelectMany(gl => gl.Graphics.OfType<PoiGraphic>())
                                        .Where(g => g.BaseGeometry != null && mapViewExtentEnvelope.Intersects(g.BaseGeometry.Extent))
                                        .ToArray();
                                foreach (var g in graphics)
                                {
                                    System.Threading.Tasks.Task.Run(
                                        () => Execute.OnUIThread(() => g.UpdateForCurrentMapExtent()));
                                }
                            }
                            catch (Exception e)
                            {
                                // TODO Warn the user about a corrupt layer!
                            }
                        });
                        */
                        foreach (var a in add) a.UpdateAnalysisStyle();

                        //var rv =
                        //    VisibleContent.Where(
                        //        k => !contentInExtent.Contains(k) || !k.IsVisible).
                        //                   ToList();
                        //var av = ContentInExtent.Where(p => !VisibleContent.Contains(p) && p.IsVisible).ToList();
                        //const int maxLength = 100;
                        //VisibleContent.IsNotifying = true;
                        DoSearch();
                        //VisibleContent.RemoveRange(rv);
                        //var available = maxLength - VisibleContent.Count();
                        //if (av.Count() > available) av = av.Take(available).ToList();
                        //VisibleContent.AddRange(av);
                        //NotifyOfPropertyChange(()=>VisibleContent);
                        //var finish = (DateTime.Now - start).TotalMilliseconds;
                    }
                    catch (Exception e)
                    {
                        Logger.Log("PoiService", "Error updating content list", e.Message, Logger.Level.Error);
                    }
                    finally
                    {
                        timer.Stop();
                        Debug.WriteLine("Content list updated: {0}s",timer.Elapsed.TotalSeconds);
                        _isUpdatingContentList = false;
                    }
                }
            });
        }

        public void SetSearchContent(BindableCollection<BaseContent> c)
        {
            SearchToLong = c.Count > 200;
            SearchContent.Clear();
            if (!SearchToLong) SearchContent.AddRange(c);
            if (Settings.FilterMap)
            {
                TriggerRefreshAllPois();
            }
            //SearchContent = (SearchToLong) ? null : c;
        }

        public async Task<bool> DoSearch()
        {
            try
            {
                if (!IsInitialized || Settings == null) return false;
                if (string.IsNullOrWhiteSpace(SearchFilter) || SearchEngine == null)
                {
                    SetSearchContent(Settings.FilterLocation ? ContentInExtent : PoIs);
                }
                else
                {
                    if (SearchEngine == null) return true;
                    var res = await SearchEngine.Search(SearchFilter);
                    var rest = new BindableCollection<BaseContent>();

                    foreach (var sc in res.Select(k => k.Content))
                    {
                        var hit = !Settings.FilterLocation || (Settings.FilterLocation && ContentInExtent.Contains(sc));
                        if (hit) rest.Add(sc);
                    }
                    SetSearchContent(rest);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetExtent(BoundingBox t)
        {
            if (Equals(t, Extent)) return;
            Extent = t;
            UpdateContentList();
        }

        public bool updating = false;

        public void TriggerRefreshAllPois()
        {
            //if (this.Layer is dsStaticLayer)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    //((dsStaticLayer) Layer).RefreshAllVisiblePois();
                    Layer.RefreshAllVisiblePois();
                });

            }
            //else
            //{
            //    var handler = AllPoisRefreshed;
            //    if (handler != null) handler(this, null);
            //}


        }

        internal void TriggerContentUpdated(BaseContent baseContent)
        {
            if (PoiUpdated != null) PoiUpdated(this, new PoiUpdatedEventArgs { Poi = baseContent });
        }

        public void TriggerTapped(TappedEventArgs tappedEventArgs)
        {
            var poi = tappedEventArgs.Content as PoI;
            if (poi != null) SelectedItem = poi;

            var handler = Tapped;
            if (handler != null) handler(this, tappedEventArgs);
            dsb.TriggerTapped(tappedEventArgs);
        }

        public class TimeRange
        {
            public DateTime Start { get; set; }

            public DateTime End { get; set; }
        }

        public TimeRange GetTimeRange()
        {
            var start = new DateTime(2014, 1, 1);
            var end = new DateTime(2012, 1, 1);
            foreach (var p in PoIs)
            {
                if (p.Sensors == null) continue;
                foreach (var s in p.Sensors)
                {

                    var d = s.Value.Data.Keys.OrderBy(k => k);
                    if (!d.Any()) continue;
                    if (d.First() < start) start = d.First();
                    if (d.Last() > end) end = d.Last();
                }
            }
            return new TimeRange { Start = start, End = end };
        }

        /// <summary>
        /// Notify that a filter has been reset (added/removed/changed)
        /// </summary>
        public void ResetFilters()
        {
            foreach (var p in PoIs)
            {
                p.FilterReset = true;
            }
        }

        [ProtoMember(6)]
        public bool AutoStart
        {
            get { return Settings.AutoStart; }
            set { Settings.AutoStart = value; NotifyOfPropertyChange(() => AutoStart); }
        }

        public PoI AddPoiType(string name, DrawingModes mode, Color? fill = null, Color? stroke = null, double strokeWidth = 1, string icon = "", double iconSize = 30)
        {
            var p = new PoI
            {
                ContentId = name,
                Service = this,
                Id = Guid.NewGuid(),
                Style = new PoIStyle
                {
                    IconWidth = iconSize,
                    IconHeight = iconSize,
                    Icon = icon,
                    DrawingMode = mode,
                    FillColor = fill,
                    CallOutFillColor = Colors.White,
                    CallOutForeground = Colors.Black,
                    StrokeColor = stroke,
                    StrokeWidth = strokeWidth,
                    CallOutTimeOut = 10
                },
                MetaInfo = new List<MetaInfo>()
            };

            if (!string.IsNullOrWhiteSpace(icon))
            {
                p.Style.Picture = new BitmapImage(p.Style.IconUri);
            }
            PoITypes.Add(p);
            return p;
        }

        //public static void SyncPoi(PoI p, Feature f, csWebApi api)
        //{
        //    if (!p.Data.ContainsKey("cs") || p.Data["cs"] == null)
        //    {
        //        p.Data["cs"] = f;
        //    }
        //    var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => p.PositionChanged += ev,
        //        ev => p.PositionChanged -= ev);
        //    posChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k =>
        //    {
        //        var coords = f.Geometry.Coordinates as JArray;
        //        if (coords != null && ((double)coords[0] != p.Position.Longitude || (double)coords[1] != p.Position.Latitude) )
        //        {
        //            ((JArray)f.Geometry.Coordinates)[0] = p.Position.Longitude;
        //            ((JArray)f.Geometry.Coordinates)[1] = p.Position.Latitude;
        //            //var c = ((JArray) f.Geometry.Coordinates).Select(x => (double) x).ToList();
        //            //c[0] = p.Position.Longitude;
        //            //c[1] = p.Position.Latitude;
        //            //f.Geometry.Coordinates = c;
        //            var t = api.features.UpdateFeatureAsync(f, p.Service.Id.ToString(), f.Id);
        //        }
        //    });
        //}

        //private Subscription sub;
        public new void MakeOnline()
        {
            if (client != null && client.Enabled && client.IsConnected)
            {
                base.MakeOnline();
            }
            //else if (csWebApi.Instance.IsConnected)
            //{
            //    var webApi = csWebApi.Instance;
            //    var layer = new IO.Swagger.Model.Layer()
            //    {
            //        Id = this.Id.ToString(),
            //        Description = "iTable shared layer",
            //        Dynamic = true,
            //        Title = this.Name,
            //        Type = "dynamicgeojson",
            //        TypeUrl = "/api/resources/" + this.Layer.Parent.ID
            //    };

            //    layer.Features = new List<Feature>();

            //    var r = new Resource() {Id = this.Layer.Parent.ID ,FeatureTypes = new Dictionary<string, FeatureType>()};
            //    var props = new Dictionary<string,MetaInfo>();
            //    this.PoITypes.ForEach(pt =>
            //    {
            //        if (r.FeatureTypes.ContainsKey(pt.PoiId))
            //        {
            //            return;
            //        }

            //        var myKeys = new List<string>();
            //        if (pt.MetaInfo != null)
            //        {
            //            pt.MetaInfo.ForEach(mi =>
            //            {
            //                if (props.ContainsKey(mi.Label))
            //                {
            //                    props[mi.Label] = mi;
            //                }
            //                else
            //                {
            //                    props.Add(mi.Label, mi);
            //                }

            //                myKeys.Add(mi.Label);
            //            });
            //        }

            //        var style = pt.NEffectiveStyle;
            //        var featureType = new FeatureType()
            //        {
            //            Id = pt.PoiId,
            //            Name = pt.Name,
            //            PropertyTypeKeys = string.Join(";", myKeys),
            //            ShowAllProperties = false,
            //            Style = new FeatureTypeStyle()
            //            {
            //                IconUri = !string.IsNullOrWhiteSpace(style.Icon)?"images/" + this.Layer.Parent.ID + "/" + style.Icon:null,
            //                FillColor = style.FillColor.ToString(),
            //                FillOpacity = style.FillOpacity,
            //                IconHeight = style.IconHeight,
            //                IconWidth = style.IconWidth,
            //                NameLabel = style.NameLabel,
            //                DrawingMode = style.DrawingMode.ToString(),
            //                Stroke = style.StrokeWidth,
            //                StrokeColor = style.StrokeColor.ToString(),
            //            }
            //        };
            //        r.FeatureTypes.Add(pt.PoiId, featureType);
            //    });

            //    if (props.Count > 0)
            //    {
            //        r.PropertyTypeData = new Dictionary<string, PropertyType>();
            //        foreach (var p in props)
            //        {
            //            var prop = new PropertyType()
            //            {
            //                Label = p.Key,
            //                Type = p.Value.Type.ToString(),
            //                Description = p.Value.Description,
            //                Title = p.Value.Title,
            //                CanEdit = p.Value.IsEditable,
            //                //DefaultValue = p.Value.DefaultValue,
            //                IsSearchable = p.Value.IsSearchable,
            //                MaxValue = p.Value.MaxValue,
            //                MinValue = p.Value.MinValue,
            //                Section = p.Value.Section=="Info"?null:p.Value.Section,
            //                StringFormat = p.Value.StringFormat,
            //                VisibleInCallout = p.Value.VisibleInCallOut
            //            };
            //            r.PropertyTypeData.Add(p.Key, prop);
            //        }
            //    }
            //    webApi.resources.AddResource(r);


            //    this.PoIs.OfType<PoI>().ForEach(p =>
            //    {
            //        var f = WebApiService.GetFeatureFromPoi(p);
            //        SyncPoi(p,f,webApi);
            //        layer.Features.Add(f);
            //    });

                

            //    webApi.layers.AddLayer(layer);
            //    var availablePoIs = new List<string>();
            //    this.PoIs.CollectionChanged += (es, tp) =>
            //    {

            //        if (!IsInitialized) return;

            //        if (tp.OldItems != null)
            //        {
            //            foreach (var p in tp.OldItems.OfType<PoI>().Where(p=>p.Data.ContainsKey("cs")))
            //            {
            //                availablePoIs.Remove(p.Id.ToString());
            //                var feature = p.Data["cs"] as Feature;
                            
            //                webApi.features.DeleteFeature(feature.Id, Id.ToString());
            //            }
            //        }

            //        if (tp.NewItems != null)
            //        {
            //            foreach (global::DataServer.PoI p in tp.NewItems)
            //            {
            //                var newp = (!availablePoIs.Contains(p.Id.ToString()));
            //                if (newp)
            //                {
            //                    if (IsInitialized)
            //                    {
            //                        var f = WebApiService.GetFeatureFromPoi(p);
            //                        SyncPoi(p, f, webApi);
            //                        webApi.features.AddFeature(this.Id.ToString(), f);

            //                    }
            //                    availablePoIs.Add(p.Id.ToString());
            //                }


            //            }
            //        }
            //        // foreach (va)
            //    };
            //    AppStateSettings.Instance.TriggerNotification("You started sharing " + Name,
            //        pathData: MenuHelpers.LayerIcon);
            //    AppStateSettings.Instance.ViewDef.UpdateLayers();
            //    sub = webApi.GetLayerSubscription(this.Id.ToString());
            //    sub.LayerCallback += (e, s) =>
            //    {
            //        switch (s.action)
            //        {
            //            case LayerUpdateAction.deleteFeature:

            //                var dp =
            //                    PoIs.FirstOrDefault(
            //                        k => k.Data.ContainsKey("cs") && ((Feature)k.Data["cs"]).Id == s.featureId);
            //                if (dp != null)
            //                {
            //                    RemovePoi(dp);
            //                    availablePoIs.Remove(dp.Id.ToString());
            //                }
            //                break;
            //            case LayerUpdateAction.updateFeature:
            //                var f = WebApiService.GetFeature((JObject)s.item);
            //                // find feature
            //                var p = PoIs.FirstOrDefault(k => k.Data.ContainsKey("cs") && ((Feature)k.Data["cs"]).Id == f.Id);
            //                if (p != null)
            //                {
            //                    // update poi
            //                    UpdateCsWebApiFeature(f, (global::DataServer.PoI)p, layer);
            //                    TriggerContentChanged(p);
            //                }
            //                else
            //                {
            //                    // add poi  
            //                    var g = Guid.NewGuid();
            //                    availablePoIs.Add(g.ToString());
            //                    var np = AddCsWebApiFeature(f, g, layer);

            //                }
            //                break;
            //        }
            //    };

            //    /*
            //    this.PoIs.OfType<PoI>().ForEach(poi=>
            //    {
            //        var f = WebApiService.GetFeatureFromPoi(poi);
            //        webApi.features.AddFeature(layer.Id, f);
            //    });*/
                
                
            //    /*
            //    foreach (var f in layer.Features)
            //    {
            //        var p = AddCsWebApiFeature(f, Guid.NewGuid(), layer);
            //        availablePoIs.Add(p.Id.ToString());
            //    }*/


            //    IsLoading = false;

            //    ContentLoaded = true;

            //    Execute.OnUIThread(() => Layer.IsLoading = false);
            //    IsLocal = true;
            //    Mode = Mode.server;
            //    IsShared = true;
            //    PoIs.FinishBatch();
            //}
        }

        //private global::DataServer.PoI AddCsWebApiFeature(Feature f, Guid id, Layer layer)
        //{
        //    var p = new global::DataServer.PoI { Service = this, Id = id, PoiTypeId = f.Type ?? (string)f.Properties["featureTypeId"] };
        //    UpdateCsWebApiFeature(f, p, layer);

        //    var webApi = csWebApi.Instance;
        //    PoIs.Add(p);
        //    p.Deleted += (o, s) => { if (IsInitialized) webApi.features.DeleteFeature(f.Id, Name); };

        //    p.LabelChanged += (sender, args) =>
        //    {
        //        f.Properties[args.Label] = args.NewValue;
        //        var t = webApi.features.UpdateFeatureAsync(f, layer.Id, f.Id);
        //    };

        //    var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => p.PositionChanged += ev,
        //        ev => p.PositionChanged -= ev);
        //    posChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k =>
        //    {
        //        if (f.Geometry.Coordinates is JArray)
        //        {
        //            var coordinates = (JArray) f.Geometry.Coordinates;
        //            var lng = (double)coordinates[0];
        //            var lat = (double)coordinates[1];
        //            if (lng != p.Position.Longitude || lat != p.Position.Latitude)
        //            {
        //                ((JArray)f.Geometry.Coordinates)[0] = p.Position.Longitude;
        //                ((JArray)f.Geometry.Coordinates)[1] = p.Position.Latitude;
        //                //var c = ((JArray) f.Geometry.Coordinates).Select(x => (double) x).ToList();
        //                //c[0] = p.Position.Longitude;
        //                //c[1] = p.Position.Latitude;
        //                //f.Geometry.Coordinates = c;
        //                var t = webApi.features.UpdateFeatureAsync(f, layer.Id, f.Id);
        //            }
        //        }
        //    });
        //    return p;
        //}

        //private void UpdateCsWebApiFeature(Feature f, global::DataServer.PoI p, Layer layer)
        //{
        //    p.Data["cs"] = f;

        //    if (f.Geometry.Type == "Point")
        //    {
        //        if (f.Geometry.Coordinates is object[])
        //        {
        //            var co = (object[])f.Geometry.Coordinates;
        //            f.Geometry.Coordinates = new JArray(co);
        //        }
        //        var c = ((JArray)f.Geometry.Coordinates).Select(x => (double)x).ToList();

        //        if (p.Position == null || p.Position.Longitude != c[0] || p.Position.Latitude != c[1])
        //        {
        //            p.Position = new Position(c[0], c[1]);
        //        }
        //    }
        //    if (layer.DefaultFeatureType != null)
        //    {
        //        p.PoiTypeId = layer.DefaultFeatureType;
        //    }
        //    var t = this.PoITypes.FirstOrDefault((pt) => p.PoiTypeId == pt.PoiId);

        //    //var type = AppState.f
        //    //p.PoiTypeId = 
        //    f.Properties.ForEach((v) =>
        //    {
        //        p.Labels[v.Key] = v.Value.ToString();
        //        var mt = t?.MetaInfo?.FirstOrDefault((mi) => mi.Id == v.Key);
        //        if (mt != null)
        //        {
        //            if (mt.Type == MetaTypes.datetime)
        //            {
        //                p.TimelineString = this.Layer.ID;
        //                DateTime dd;
        //                if (DateTime.TryParse(v.Value.ToString(), out dd))
        //                {
        //                    p.Date = dd;
        //                    this.Events.Add(p);
        //                }

        //            }
        //        }
        //    });
        //    p.ForceUpdate(true,false);
        //}

        public bool IsShared { get; set; }

        public void ShareInGroup()
        {
            if (AppState.Imb.ActiveGroup == null) return;
            if (!AppState.Imb.ActiveGroup.Layers.Contains(Id)) AppState.Imb.ActiveGroup.Layers.Add(Id);
            AppState.Imb.ActiveGroup.UpdateGroup();
            MakeOnline();
        }

        /// <summary>
        /// Remove all pois from service
        /// </summary>
        public void RemoveAllPois()
        {
            while (PoIs.Any()) RemovePoi(PoIs[0]);
        }

        public void RaisePoiLongTapped(PoI pPoI)
        {
            var handler = PoiLongTapped;
            if (handler != null) handler(this, pPoI);
        }
    }

    public class PoiUpdatedEventArgs : EventArgs
    {
        public BaseContent Poi { get; set; }
    }

    public static class CollectionSorter
    {
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var comparer = new Comparer<T>(comparison);

            var sorted = collection.OrderBy(x => x, comparer).ToList();

            for (var i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }

        public static void DescendingSort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var comparer = new ReverseComparer<T>(comparison);
            var sorted = collection.OrderBy(x => x, comparer).ToList();
            for (var i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }
    }

    internal class Comparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public Comparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            return comparison.Invoke(x, y);
        }

        #endregion IComparer<T> Members
    }

    public class TappedEventArgs : EventArgs
    {
        public BaseContent Content { get; set; }

        public PoiService Service { get; set; }

        public System.Windows.Point TapPoint { get; set; }
    }

    public class ContentChangedEventArgs : EventArgs
    {
        public BaseContent Content { get; set; }
    }

    internal class ReverseComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public ReverseComparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            return -comparison.Invoke(x, y);
        }

        #endregion IComparer<T> Members
    }

    public class SortedObservableCollection<T> : ObservableCollection<T>
        where T : IComparable
    {
        protected override void InsertItem(int index, T item)
        {
            for (var i = 0; i < Count; i++)
            {
                switch (Math.Sign(this[i].CompareTo(item)))
                {
                    case 0:
                    //throw new InvalidOperationException("Cannot insert duplicated items");
                    case 1:
                        base.InsertItem(i, item);
                        return;

                    case -1:
                        break;
                }
            }

            base.InsertItem(Count, item);
        }
    }


#if !WINDOWS_PHONE
    [Serializable]

    public class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IXmlSerializable, ISerializable
    {
        #region Constants
        //private const string DictionaryNodeName = "Dictionary";
        private const string ItemNodeName = "Item";
        private const string KeyNodeName = "Key";
        private const string ValueNodeName = "Value";
        #endregion
        #region Constructors
        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IDictionary<TKey, TVal> dictionary)
            : base(dictionary)
        {
        }

        public SerializableDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
        }

        public SerializableDictionary(int capacity)
            : base(capacity)
        {
        }

        public SerializableDictionary(IDictionary<TKey, TVal> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer)
        {
        }

        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer)
        {
        }

        #endregion
        #region ISerializable Members

        protected SerializableDictionary(SerializationInfo info, StreamingContext context)
        {
            var itemCount = info.GetInt32("ItemCount");
            for (var i = 0; i < itemCount; i++)
            {
                var kvp = (KeyValuePair<TKey, TVal>)info.GetValue(String.Format("Item{0}", i), typeof(KeyValuePair<TKey, TVal>));
                Add(kvp.Key, kvp.Value);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ItemCount", Count);
            var itemIdx = 0;
            foreach (var kvp in this)
            {
                info.AddValue(String.Format("Item{0}", itemIdx), kvp, typeof(KeyValuePair<TKey, TVal>));
                itemIdx++;
            }
        }

        #endregion
        #region IXmlSerializable Members

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            //writer.WriteStartElement(DictionaryNodeName);
            foreach (var kvp in this)
            {
                writer.WriteStartElement(ItemNodeName);
                writer.WriteStartElement(KeyNodeName);
                KeySerializer.Serialize(writer, kvp.Key);
                writer.WriteEndElement();
                writer.WriteStartElement(ValueNodeName);
                ValueSerializer.Serialize(writer, kvp.Value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            //writer.WriteEndElement();
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }

            // Move past container
            if (!reader.Read())
            {
                throw new XmlException("Error in Deserialization of Dictionary");
            }

            //reader.ReadStartElement(DictionaryNodeName);
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement(ItemNodeName);
                reader.ReadStartElement(KeyNodeName);
                var key = (TKey)KeySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement(ValueNodeName);
                var value = (TVal)ValueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadEndElement();
                Add(key, value);
                reader.MoveToContent();
            }
            //reader.ReadEndElement();

            reader.ReadEndElement(); // Read End Element to close Read of containing node
        }

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        #endregion
        #region Private Properties
        protected XmlSerializer ValueSerializer
        {
            get { return valueSerializer ?? (valueSerializer = new XmlSerializer(typeof(TVal))); }
        }

        private XmlSerializer KeySerializer
        {
            get { return keySerializer ?? (keySerializer = new XmlSerializer(typeof(TKey))); }
        }
        #endregion
        #region Private Members
        private XmlSerializer keySerializer;
        private XmlSerializer valueSerializer;
        #endregion
    }
#endif
}