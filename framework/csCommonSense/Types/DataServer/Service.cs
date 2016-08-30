using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Types.DataServer.PoI;
using csCommon.Types.TextAnalysis;
using csCommon.Types.TextAnalysis.TextFinder;
using csDataServerPlugin;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using DocumentFormat.OpenXml.Math;
using IMB3;
using IMB3.ByteBuffers;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Linq;
using csCommon.Utils;
using csGeoLayers;
using csCommon.Logging;
using System.Text;

namespace DataServer
{
    [ProtoContract]
    public class ContentListCollection : BindableCollection<ContentList>, ITextSearchableCollection<ContentList>
    {
        public long IndexId
        {
            get
            {
                return Items.SelectMany(contentList => contentList).Aggregate<BaseContent, long>(1, (current, baseContent) => current * baseContent.IndexId);
            }
        }

        public new void Add(ContentList value)
        {
            if (this.All(k => k.Id != value.Id))
            {
                base.Add(value);
            }
        }
    }

    [ProtoContract]
    [Serializable]
    public class Service : PropertyChangedBase, IConvertibleXml, ITextSearchableCollection<BaseContent>, ITextSearchableCollection<ContentList>
    {
        #region Searching

        /// <summary>
        /// Interface property for generating a unique ID. Rather slow implementation.
        /// </summary>
        public long IndexId
        {
            get
            {
                return allContent.SelectMany(contentList => contentList).Aggregate<BaseContent, long>(1, (current, baseContent) => current * baseContent.IndexId);
            }
        }

        private bool _cachedFinderKeywordsOnly;
        private bool _cachedFinderPrefixesOnly;
        private ITextFinder<BaseContent> _cachedFinder;

        /// <summary>
        /// Return the content of this service that contains the query either in keywords or in the full text.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="keywordsOnly">Whether to consider only keywords, or all text including keywords.</param>
        /// <param name="prefixesOnly">Whether to search only for prefixes (e.g. 'it' matches 'itself' 
        ///                            but not 'legitimately'), or also for infixes.</param>
        /// <returns>A list of text finder results; i.e. the data and a score for each result returned.</returns>
        public IEnumerable<TextFinderResult<BaseContent>> Find(string query, bool keywordsOnly = true,
            bool prefixesOnly = true)
        {
            if (_cachedFinder == null || _cachedFinderKeywordsOnly != keywordsOnly || _cachedFinderPrefixesOnly != prefixesOnly)
            {
                _cachedFinder = new TrieTextFinder<BaseContent>();
                _cachedFinder.Initialize(this, keywordsOnly, prefixesOnly);
                _cachedFinderKeywordsOnly = keywordsOnly;
                _cachedFinderPrefixesOnly = prefixesOnly;
            }
            return _cachedFinder.Find(query);
        }

        #endregion Searching

        public string FolderIcon { get; set; }

        public delegate void DataReceivedHandler(string contentId, byte[] content, Service service);

        public delegate void PingReceivedHandler(Service s, BaseContent b);

        public delegate void ModelLoadedHandler(BaseContent baseContent, IModelPoiInstance model);

        public const string ServiceVariableName = "Service:";
        public const string StaticServiceVariableName = "StaticService:";
        //private static readonly object _lock = new object();
        private ContentListCollection allContent;

        [NonSerialized]
        public readonly Dictionary<string, List<DataReceivedHandler>> ContentPipeLine = new Dictionary<string, List<DataReceivedHandler>>();

        [NonSerialized]
        public DataServerBase dsb;

        private Guid id;

        private bool isFileBased = true;
        [NonSerialized]
        private bool isInitialized;
        [NonSerialized]
        private bool isLoading;
        private bool isLocal;
        [NonSerialized]
        private bool isSaving; // FIXME TODO isSaving is assigned but not used.
        [NonSerialized]
        private bool isSubscribed;
        [NonSerialized]
        private bool isTemplate;
        private Mode mode;
        [NonSerialized]
        private string name;
        //[NonSerialized]
        //private Timer saveTimer;
        [NonSerialized]
        public TEventEntry serviceChannel;
        private bool staticService;
        [NonSerialized]
        public readonly FileStore store;

        [NonSerialized]
        private List<int> subscribers = new List<int>();

        public Service()
        {
            AllContent = new ContentListCollection();
            store = new FileStore();
            store.Init(this);

            SettingsList = CreateContentList("settings", typeof(ServiceSettings));
            if (!SettingsList.Any()) SettingsList.Add(new ServiceSettings());

            _baseContentDebounce.EntriesAdded += (sender, contents) =>
            {
                var poiCl = AllContent.FirstOrDefault(cl => cl.ContentType == typeof(PoI));
                if (poiCl == null) return;

                poiCl.StartBatch();
                contents.ForEach(d =>
                {
                    var prevNotifying = d.IsNotifying;
                    d.IsNotifying = false;
                    d.ForceUpdate(true);
                    d.IsNotifying = prevNotifying;
                });
                poiCl.FinishBatch();
            };
        }

        /// <summary>
        /// ESRI relative group layer name
        /// </summary>
        public string RelativeFolder { get; set; }

        public bool IsInitialized
        {
            get { return isInitialized; }
            private set
            {
                if (value == isInitialized) return;
                isInitialized = value;
                NotifyOfPropertyChange(() => IsInitialized);
                if (value) OnInitialized();
            }
        }

        private void OnInitialized()
        {
            var handler = Initialized;
            if (handler != null) handler(this, null);
        }

        public bool StaticService
        {
            get { return staticService; }
            set
            {
                staticService = value;
                NotifyOfPropertyChange(() => StaticService);
            }
        }

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        public bool CanReset
        {
            get { return IsLocal; }
        }

        public csImb.csImb client
        {
            get { return dsb != null ? dsb.client : null; }
        }

        public Mode Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                NotifyOfPropertyChange(() => Mode);
            }
        }

        public bool IsFileBased
        {
            get { return isFileBased; }
            set
            {
                isFileBased = value;
                NotifyOfPropertyChange(() => IsFileBased);
            }
        }

        public ContentList SettingsList { get; set; }

        public ServiceSettings Settings
        {
            get
            {
                SettingsList = (ContentList)AllContent.FirstOrDefault(k => k.Id == "settings");

                if (SettingsList == null)
                {
                    SettingsList = new ContentList
                    {
                        Service = this,
                        ContentType = typeof(ServiceSettings),
                        Id = "settings",
                        IsRessetable = false

                    };
                    AllContent.Add(SettingsList);
                }

                if (!SettingsList.Any())
                {
                    SettingsList.Add(new ServiceSettings());
                }
                return SettingsList.FirstOrDefault(k => k is ServiceSettings) as ServiceSettings;
            }
        }

        public bool IsTemplate
        {
            get { return isTemplate; }
            set { isTemplate = value; }
        }

        public string FileName
        {
            get
            {
                var res = ((StaticService) ? "~" : "") + Id + "." + Name + (IsTemplate ? ".dsd" : ".ds");
                if (!string.IsNullOrEmpty(Folder)) res = Folder + @"\" + res;
                return res;
            }
        }

        public string GetBackupFileName()
        {
            return BackupFolder + @"\" + Id + "." + DateTime.Now.Ticks + ".ds.b";
        }

        public string ProtoFileName
        {
            get
            {
                var res = ((StaticService) ? "~" : "") + Id + "." + Name + ".dsp";
                if (!string.IsNullOrEmpty(Folder)) res = Folder + @"\" + res;
                return res;
            }
        }

        public string BinaryFileName
        {
            get
            {
                var res = ((StaticService) ? "~" : "") + Id + "." + Name + ".dsb";
                if (!string.IsNullOrEmpty(Folder)) res = Folder + @"\" + res;
                return res;
            }
        }

        /// <summary>
        /// Folder path on disk.
        /// </summary>
        public string Folder { get; set; }

        public string BackupFolder
        {
            get
            {
                return Folder + "\\_Backup\\";
            }
        }

        public string MediaFolder
        {
            get { return Folder + "\\_Media\\"; }
        }

        public bool IsLocal
        {
            get { return isLocal; }
            set
            {
                isLocal = value;
                NotifyOfPropertyChange(() => IsLocal);
            }
        }

        /// <summary>
        ///     unique serviceId
        /// </summary>
        [ProtoMember(1)]
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        ///     serviceId of the server
        /// </summary>
        [ProtoMember(2)]
        public int Server { get; set; }

        /// <summary>
        ///     Userfriendly Name
        /// </summary>
        [ProtoMember(3)]
        public string Name
        {
            get { return name; }
            set
            {
                // Added fix: for DS files that do not have a GUID, the name is set to the full path instead of just the file name.
                name = Path.GetFileNameWithoutExtension(value);
                NotifyOfPropertyChange(() => Name);
            }
        }



        private DispatcherTimer backupTimer;

        public void InitBackupInterval()
        {
            if (backupTimer != null && backupTimer.IsEnabled) backupTimer.Stop();
            backupTimer = new DispatcherTimer();
            if (Settings.BackupInterval != 0)
            {
                backupTimer.Interval = TimeSpan.FromMinutes(Settings.BackupInterval);
                backupTimer.Tick += backupTimer_Tick;
                backupTimer.Start();
            }

        }

        private void backupTimer_Tick(object sender, EventArgs e)
        {
            CreateBackup();
            //if (CreateBackup()) AppStateSettings.Instance.TriggerNotification("Automatic backup created for " + Name, pathData: MenuHelpers.BackupIcon);
        }

        /// <summary>
        /// Check if a backup is available. If not create one
        /// </summary>
        public void CheckOriginalBackup()
        {
            // only works for local filebased services
            if (!IsFileBased || (IsOnline && Mode == Mode.client)) return;

            InitBackupInterval();

            // check if original backup is available
            if (GetBackups().Any()) return;
            CreateBackup();
            //AppStateSettings.Instance.TriggerNotification("Initial backup created for " + Name, pathData: MenuHelpers.BackupIcon);
        }

        public bool CreateBackup()
        {
            if (!IsFileBased) return CreateBackupOfLiveService();
            if (string.IsNullOrEmpty(FileName) || !File.Exists(FileName)) return false;
            var backupFileName = GetBackupFileName();
            if (File.Exists(backupFileName)) return false;

            try
            {
                File.Copy(FileName, backupFileName);

                return true;
            }
            catch (Exception e)
            {
                Logger.Log("Service Backup", "Error saving backup", e.Message, Logger.Level.Error);
                return false;
            }
        }
        private bool CreateBackupOfLiveService()
        {
            if (string.IsNullOrEmpty(FileName)) return false;
            var backupFileName = GetBackupFileName();
            if (File.Exists(backupFileName)) return false;
            var xml = ToXml(true, false);
            File.WriteAllText(backupFileName, xml.ToString());
            return true;
        }

        public IEnumerable<Backup> GetBackups()
        {

            var r = new List<Backup>();
            if (!Directory.Exists(BackupFolder)) return r;
            foreach (var f in Directory.GetFiles(BackupFolder, "*.b"))
            {
                var file = new FileInfo(f);
                var ff = file.Name.Split('.');
                if (ff.Count() != 4) continue;
                var b = new Backup();
                Guid guid;
                if (!Guid.TryParse(ff[0], out guid)) continue;
                if (guid != Id) continue;
                var ticks = long.Parse(ff[1]);
                b.Date = new DateTime(ticks);
                b.FileName = f;
                r.Add(b);
            }
            return r.OrderByDescending(k => k.Date).ToList();
        }


        public ContentListCollection AllContent
        {
            get { return allContent; }
            set
            {
                allContent = value;
                NotifyOfPropertyChange(() => AllContent);
            }
        }

        public RuntimeTypeModel Model
        {
            get { return RuntimeTypeModel.Default; }
        }

        /// <summary>
        ///     Am I currently subscribed to it
        /// </summary>
        public bool IsSubscribed
        {
            get { return isSubscribed; }
            set
            {
                isSubscribed = value;
                NotifyOfPropertyChange(() => IsSubscribed);
            }
        }

        public List<int> Subscribers
        {
            get { return subscribers; }
            set { subscribers = value; }
        }

        public static bool Dirty { get; set; }

        public string OnlineStatus
        {
            get
            {
                if (IsLocal && Mode == Mode.client) return "Local Service";
                if (IsLocal && Mode == Mode.server) return "Hosted Local Service";
                if (IsLocal) return string.Empty;
                var displayName = Server.ToString(CultureInfo.InvariantCulture);
                if (client.Clients.ContainsKey(Server)) displayName = client.Clients[Server].DisplayName;
                return "Online service hosted by " + displayName;
            }
        }

        public bool IsOnline
        {
            get { return (!IsLocal || (IsLocal && Mode == Mode.server)); }
        }

        public event EventHandler<ContentChangedEventArgs> ContentChanged;
        public event EventHandler Unsubscribed;
        public event EventHandler Initialized;

        public void TriggerContentChanged(BaseContent content)
        {
            var handler = ContentChanged;
            if (handler != null) handler(this, new ContentChangedEventArgs { Content = content });
        }

        /// <summary>
        /// Check if service can be saved, and if so, save it.
        /// In case of a ShapeService, only the DSD is saved.
        /// </summary>
        public void CheckSave()
        {
            if (!IsFileBased)
            {
                var service = this as ShapeService;
                if (service != null) SaveXml(service.DsdFile, true);
                return;
            }
            try
            {
                isSaving = true;
                Dirty = false;
                if (IsSubscribed && IsInitialized) SaveXml();
            }
            catch (Exception e)
            {
                Logger.Log("Service", "Error saving service", e.Message, Logger.Level.Error);
            }
            finally
            {
                isSaving = false;
            }
        }

        public void SubscribeServiceChannel()
        {
            if (client == null || !client.IsConnected) return;
            serviceChannel = client.Imb.Subscribe("ServiceChannel." + Id);
            serviceChannel.OnBuffer += serviceChannel_OnBuffer;
            LogCs.LogMessage(String.Format("Service '{0}' ({1}) subscribed to IMB communication channel '{2}' ", Name, Id, serviceChannel.EventName));
        }

        public void UnSubscribeServiceChannel()
        {
            if (serviceChannel == null) return;
            serviceChannel.UnSubscribe();
            serviceChannel.UnPublish();
            serviceChannel.OnBuffer -= serviceChannel_OnBuffer;
            LogCs.LogMessage(String.Format("Service '{0}' ({1}) unsubscribed from IMB service communication channel", Name, Id));
        }

        [DebuggerStepThrough]
        public void RequestData(string myId, DataReceivedHandler handler)
        {
            if (handler == null || dsb == null) return;
            if (!ContentPipeLine.ContainsKey(myId))
            {
                try
                {
                    var h = new List<DataReceivedHandler> { handler };
                    ContentPipeLine.Add(myId, h);
                    dsb.SendPrivateMessage(Server, PrivateMessageActions.RequestData, Id, myId, Guid.Empty);
                }
                catch (Exception e)
                {
                    Logger.Log("Service", "Error requesting media item", e.Message, Logger.Level.Error);
                }
            }
            else
            {
                ContentPipeLine[myId].Add(handler);
            }
        }

        public void DataReceived(string myId, byte[] content)
        {
            if (!ContentPipeLine.ContainsKey(myId)) return;
            foreach (var h in ContentPipeLine[myId])
            {
                h.Invoke(myId, content, this);
            }
            ContentPipeLine.Remove(myId);
        }

        public void Init(Mode serviceMode, DataServerBase dataServerBase)
        {
            IsInitialized = false;
            mode = serviceMode;
            dsb = dataServerBase;

            if (mode == Mode.server)
            {
                UpdateServiceVariable();
            }

            //SettingsList = AllContent.FirstOrDefault(k => k.Id == "settings");
            //if (SettingsList == null)
            //{
            //    SettingsList = new ContentList {ContentType = typeof (BaseContent), Id = "settings", Service = this};
            //    AllContent.Add(SettingsList);
            //}

            //if (Settings == null && (mode == Mode.client && !IsLocal))
            //{
            //    var s = new ServiceSettings {Id = Guid.NewGuid()};
            //    s.CanEdit = true;
            //    SettingsList.Add(s);
            //}
        }


        private void UpdateServiceVariable()
        {
            if (client != null
#if !WINDOWS_PHONE
 && AppStateSettings.Instance.Imb.Enabled
#endif
)
            {
                if (Mode == Mode.server && !string.IsNullOrEmpty(Name))
                {
                    var serviceVar = StaticService ? StaticServiceVariableName : ServiceVariableName;
                    client.Imb.SetVariableValue(serviceVar + id,
                        client.Id + "|" + Name.Replace('|', ' '));
                }
                else
                {
                    client.Imb.SetVariableValue(ServiceVariableName + id, "");
                }
            }
        }

        public event PingReceivedHandler PingReceived;

        public event ModelLoadedHandler OnModelLoaded;

        public void MakeLocal()
        {
            UnSubscribeServiceChannel();
            foreach (var c in AllContent) c.UnRegisterAllCountent();
            Mode = Mode.client;
            UpdateServiceVariable();

            var ag = AppStateSettings.Instance.Imb.ActiveGroup;
            if (ag != null && ag.Layers.Contains(Id))
            {
                ag.Layers.Remove(Id);
                ag.UpdateGroup();
            }

            AppStateSettings.Instance.TriggerNotification("You stopped sharing " + Name, pathData: MenuHelpers.LayerIcon);
            AppStateSettings.Instance.ViewDef.UpdateLayers();

        }

        public virtual void MakeOnline()
        {
            if (client != null && client.Enabled)
            {
                SubscribeServiceChannel();

                Mode = Mode.server;
                UpdateServiceVariable();
                foreach (var cl in AllContent) cl.RegisterAllContent();
            }
            AppStateSettings.Instance.TriggerNotification("You started sharing " + Name, pathData: MenuHelpers.LayerIcon);
            AppStateSettings.Instance.ViewDef.UpdateLayers();
        }

        public void TriggerPing(BaseContent c)
        {
            if (PingReceived != null) PingReceived(this, c);
        }


        // Debounce every second or when 100 pois are received
        private DebouncedProcessor<BaseContent> _baseContentDebounce = new DebouncedProcessor<BaseContent>(1000,100);
        internal void RaiseModelLoaded(BaseContent baseContent, IModelPoiInstance model)
        {
            var handler = OnModelLoaded;
            if (handler != null) handler(baseContent, model);
        }
        private void serviceChannel_OnBuffer(TEventEntry aEvent, int aTick, int aBufferId, TByteBuffer aBuffer)
        {
            var cm = ContentMessage.ConvertByteArrayToMessage(aBuffer.Buffer);
            if (cm == null)
            {
                LogImbService.LogWarning(String.Format("Corrupted IMB message received in service {0} ({1}) ", Name, Id));
                return;
            }

            switch (cm.Action)
            {
                case ContentMessageActions.Ping:
                    if (PingReceived != null)
                    {
                        var pobj = new BaseContent {IsInTransit = true};
                        try
                        {
                            using (var ms = new MemoryStream())
                            {
                                ms.Write(cm.Content, 0, cm.Content.Length);
                                ms.Position = 0;

                                RuntimeTypeModel.Default.DeserializeWithLengthPrefix(ms, pobj, typeof(BaseContent), PrefixStyle.Base128, 0);
                                TriggerPing(pobj);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log("Ping", "Error receiving ping", e.Message, Logger.Level.Error);
                        }
                    }
                    break;
                case ContentMessageActions.Remove:
                    try
                    {
                        IContent c = null;
                        var cl = AllContent.FirstOrDefault(k => k.Id == cm.ContentList);
                        if (cl != null)
                        {
                            c = cl.FirstOrDefault(k => k.Id == cm.Id);
                            if (c != null)
                            {
                                c.IsInTransit = true;
                                cl.Remove((BaseContent)c);
                            }
                        }
                        var bc = (c as BaseContent);
                        LogImbService.LogMessage(this, String.Format("Removed content '{0}'",
                            (c == null) ? "Removed " + bc?.Name : " ITEM NOT FOUND "));
                    }
                    catch (Exception e)
                    {
                        LogImbService.LogException(this, String.Format("Failed to removed content "), e);
                        Logger.Log("Data Service", "Error removing poi", e.Message, Logger.Level.Error);
                    }
                    break;
                case ContentMessageActions.Add:
                case ContentMessageActions.Update:
                    try
                    {
                        var cl = AllContent.FirstOrDefault(k => k.Id == cm.ContentList);
                        if (cl == null)
                        {
                            cl = new ContentList { Id = cm.ContentList, ContentType = cm.ContentType, Service = this };
                            AllContent.Add(cl);
                            LogImbService.LogMessage(this, String.Format("Add content type '{0}' to contentlist (id={1})", cm.ContentType, cm.ContentList));
                        }
                        using (var ms = new MemoryStream())
                        {
                            ms.Write(cm.Content, 0, cm.Content.Length);
                            ms.Position = 0;

                            var obj = cl.FirstOrDefault(k => k.Id == cm.Id);
                            if (obj == null)
                            {
                                obj = (BaseContent)Activator.CreateInstance(cl.ContentType, null);
                                obj.IsInTransit = true;
                                try
                                {
                                    RuntimeTypeModel.Default.DeserializeWithLengthPrefix(ms, obj, cl.ContentType,
                                        PrefixStyle.Base128, 0);

                                    cl.Add(obj);
                                    LogImbService.LogMessage(this, String.Format(" Add new content '{0}' of (type is '{0}') to contentlist.",
                                         obj.Name ?? "-", cm.ContentType));
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("DataServer.Service", "serviceChannel_OnBuffer error 1 (unable to add new content)", e.Message, Logger.Level.Error, true);
                                    LogImbService.LogException(this, String.Format("Unable to add new content '{0}' of type '{1}' to contentlist '{2}'",
                                          cm.Id, cm.ContentType, cm.ContentList), e);
                                    throw;
                                }
                                finally
                                {
                                    obj.IsInTransit = false;
                                }
                            }
                            else
                            {
                                try
                                {
                                    obj.IsInTransit = true;
                                    // Clone label dictionary
                                    var diffLabels = new Dictionary<string, string>();
                                    if (obj.Labels != null)
                                    {
                                        foreach (KeyValuePair<string, string> label in obj.Labels)
                                        {
                                            diffLabels.Add(label.Key, label.Value);
                                        }
                                    }
                                    RuntimeTypeModel.Default.DeserializeWithLengthPrefix(ms, obj, cl.ContentType,
                                        PrefixStyle.Base128, 0);
                                    _baseContentDebounce.Add(obj); // Batch the 'ForceUpdate' routine for preformance!!
                                                                   //obj.ForceUpdate(true, true);

                                    // Notify label changed:
                                    var sb = new StringBuilder();
                                    sb.Append("Label changed: ");
                                    if (obj.Labels != null)
                                    {
                                        var safeList = obj.Labels.ToArray(); // Collection changed in obj.Labels
                                        foreach (KeyValuePair<string, string> currentLabel in safeList)
                                        {
                                            string oldLabelValue;
                                            if (diffLabels.TryGetValue(currentLabel.Key, out oldLabelValue))
                                            {
                                               if (currentLabel.Value != oldLabelValue) // label changed
                                               {
                                                    sb.AppendFormat("'{0}': '{0}'->'{1}';", currentLabel.Key, oldLabelValue, currentLabel.Value);
                                                    TriggerLabelChangedBySync(obj, currentLabel.Key, oldLabelValue, currentLabel.Value);
                                               }
                                            }
                                            else // new label
                                            {
                                                sb.AppendFormat("'{0}': '<not set>'->'{1}';", currentLabel.Key, currentLabel.Value);
                                                TriggerLabelChangedBySync(obj, currentLabel.Key, null, currentLabel.Value);
                                            }
                                        }
                                    }
                                    LogImbService.LogMessage(this, String.Format("Updated COMPLETE content of '{0}' (type={1}). {2}",
                                        obj.Name ?? "-", cm.ContentType, sb.ToString()));
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("DataServer.Service", "serviceChannel_OnBuffer error 2", e.Message, Logger.Level.Error, true);
                                    LogImbService.LogException(this, String.Format("Unable to add exsisting content '{0}' of type '{1}' to contentlist '{2}'",
                                        cm.Id, cm.ContentType, cm.ContentList), e);
                                    throw;
                                }
                                finally
                                {
                                    obj.IsInTransit = false;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log("DataService", "Error updating content object", e.Message, Logger.Level.Error);
                        LogImbService.LogException(this, String.Format("Unable to add/update content {0}.", cm.Id), e);
                    }
                    break;
            }
        }

        /// <summary>
        /// Notify label changed when BaseContent is synchronised over IMB bus
        /// </summary>
        private void TriggerLabelChangedBySync(BaseContent pConent, string pLabelKey, string pOldValue, string pNewValue)
        {
            // dsPoiLayer runs in Guid thread and uses label changed
            Execute.OnUIThread(() =>
            {
                var prevNotifying = pConent.IsNotifying;
                pConent.IsNotifying = false;
                pConent.TriggerLabelChanged(pLabelKey, pOldValue, pNewValue);
                pConent.IsNotifying = prevNotifying;
            });

        }

        public void Close()
        {
            if (mode == Mode.server)
            {
                mode = Mode.client;
                UpdateServiceVariable();
            }
            if (!IsLocal && Mode == Mode.client && serviceChannel != null)
                serviceChannel.connection.UnSubscribe(serviceChannel.EventName, false);
        }

        public string ToCapability()
        {
            return id + "^" + Name;
        }

        public virtual void Subscribe(Mode serviceMode)
        {
            Init(serviceMode, dsb);
            Execute.OnUIThread(() => IsSubscribed = true);
        }


        public void Unsubscribe()
        {
            Close();
            if (AppStateSettings.Instance.IsClosing) return;

            IsInitialized = false;
            if (Unsubscribed != null) Unsubscribed(this, null);

            if (IsFileBased)
                Execute.OnUIThread(() =>
                {
                    try
                    {
                        IsSubscribed = false;
                        foreach (var cl in AllContent)
                        {
                            cl.Clear();
                        }
                    }
                    catch (AggregateException) { }
                    //AllContent.Clear();
                });

        }

        public void Reset()
        {
            if (!CanReset) return;
            foreach (var cl in AllContent.Where(cl => cl.IsRessetable))
            {
                while (cl.Any()) cl.RemoveAt(0);
            }
        }

        /// <summary>
        /// Save the file to XML. 
        /// In case the optional parameter isDsd is set to true, only save the DSD information (settings and poitypes).
        /// </summary>
        /// <param name="isDsd"></param>
        /// <returns></returns>
        public bool SaveXml(bool isDsd = false)
        {
            if (!isDsd) return SaveXml(FileName);
            var fileName = Path.ChangeExtension(Path.Combine(Folder, Name), "dsd");
            return SaveXml(fileName, true);
        }

        /// <summary>
        /// Save the file to XML. 
        /// In case the optional parameter isDsd is set to true, only save the DSD information (settings and poitypes).
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="isDsd"></param>
        /// <returns></returns>
        public bool SaveXml(string fileName, bool isDsd = false)
        {
            if (!AllContent.Any()) return false;
            if (Settings == null) return false;
            var success = false;
            try
            {
#if !WINDOWS_PHONE
                //if (!isDsd && store.HasFile(FileName))
                //{
                //    var bck = FileName + ".bck";
                //    store.Delete(bck);
                //    store.Copy(FileName, bck);
                //}
#endif
                var xd = ToXml(true, isDsd);
                if (xd != null && store != null)
                {

                    FileStore.SaveString(string.Empty, fileName, xd.ToString());
                }
                success = true;
            }
            catch (Exception e)
            {
                Logger.Log("Service Save", "Error saving service", e.Message, Logger.Level.Error);
            }
            //            finally
            //            {
            //#if !WINDOWS_PHONE
            //                //                AppStateSettings.Instance.FinishDownload(g);
            //#endif
            //            }
            return success;
        }

        public void ToBin()
        {
            var sw = new Stopwatch();
            sw.Start();
            this.BinarySerializeObject(BinaryFileName);
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }

        public XDocument ToXml(bool includeMetaData, bool isDsd)
        {
            try
            {
                var res = new XDocument();
                var s = new XElement("Content");
                s.SetAttributeValue("Id", Id);
                s.SetAttributeValue("Name", Name);
                foreach (var cl in AllContent)
                {
                    if (string.IsNullOrEmpty(cl.Id)) continue;
                    if (isDsd)
                        if (!string.Equals(cl.Id, "settings", StringComparison.InvariantCultureIgnoreCase)
                            && !string.Equals(cl.Id, "poitypes", StringComparison.InvariantCultureIgnoreCase)) continue;
                    if (!includeMetaData)
                        if (string.Equals(cl.Id, "settings", StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(cl.Id, "poitypes", StringComparison.InvariantCultureIgnoreCase)) continue;

                    var content = new XElement(cl.Id);
                    s.Add(content);
                    foreach (IContent c in cl)
                    {
                        try
                        {
                            content.Add(c.ToXml(Settings));
                        }
                        catch (Exception e)
                        {
                            Logger.Log("DataServices", "Error saving xml for " + Name, e.Message, Logger.Level.Error);
                        }
                    }
                }
                res.Add(s);
                return res;
            }
            catch (Exception exception)
            {
                Logger.Log("DataServices", "Error saving xml for " + Name, exception.Message, Logger.Level.Error);
            }
            return null;
        }

        public string XmlNodeId
        {
            get { return "Service"; }
        }

        public XElement ToXml()
        {
            return ToXml(true, false).Root;
        }

        public void FromXml(XElement element)
        {
            FromXml(element, "."); // TODO REVIEW Is '.' the correct directory to pass here as default?
        }

        public void FromXml(XElement element, string directoryName)
        {
            try
            {
                if (SettingsList != null)
                {
                    SettingsList.Clear();
                }
                var ct = DataServerBase.ContentTypes;
                if (Id == Guid.Empty) Id = element.GetGuid("Id");
                if (string.IsNullOrEmpty(Name)) Name = element.GetString("Name");

                if (element == null) return;
                foreach (var xcl in element.Elements())
                {
                    try
                    {
                        var cl = AllContent.FirstOrDefault(k => k.Id == xcl.Name.LocalName);
                        if (xcl.Name.LocalName == "settings")
                        {
                            //BugFix: Jeroen | Somehow the cl can be empty!!! lets check and not crash!
                            if (cl.Any())
                            {
                                SettingsList = cl;
                                cl.Clear();
                            }
                        }
                        if (cl == null)
                        {
                            cl = new ContentList { Id = xcl.Name.LocalName, Service = this };
                            AllContent.Add(cl);
                        }

                        foreach (var xc in xcl.Elements())
                        {
                            if (!ct.ContainsKey(xc.Name.LocalName)) continue;

                            var tt = Activator.CreateInstance(ct[xc.Name.LocalName], null) as BaseContent;
                            if (tt == null)
                                continue;
                            tt.Service = this;
                            tt.FromXml(xc, directoryName);
                            cl.Add(tt);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log("DataService", "Error adding content list " + FileName + " - " + xcl.Name.LocalName, e.Message, Logger.Level.Error);
                    }
                }
            }
            catch (SystemException e)
            {
                Logger.Log("DataService", "Error opening local service file: " + FileName, e.Message, Logger.Level.Error);
            }
            //if (this is PoiService) ((PoiService)this).ToProto();
        }

        public void FromXmlFile(string filename, string xtext, string directoryName)
        {
            FromXml(xtext, directoryName);
        }

        public void FromXml(string xtext, string directoryName)
        {
            if (string.IsNullOrEmpty(xtext))
            {
                AppStateSettings.Instance.TriggerNotification("Document is empty: " + directoryName);
                return;
            }
            var xs = XDocument.Parse(xtext);
            //Console.WriteLine("FromXml(1) in {0}.", sw.Elapsed);
            var element = xs.Root;
            FromXml(element, directoryName);
        }

        public static T CreateInstance<T>(Type objType) where T : class
        {
            Func<T> returnFunc;
            if (DelegateStore<T>.Store.TryGetValue(objType.FullName, out returnFunc)) return returnFunc();
            var dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + objType.Name, objType, null, objType);
            var ilGen = dynMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Newobj, objType.GetConstructor(Type.EmptyTypes));
            ilGen.Emit(OpCodes.Ret);
            returnFunc = (Func<T>)dynMethod.CreateDelegate(typeof(Func<T>));
            DelegateStore<T>.Store[objType.FullName] = returnFunc;
            return returnFunc();
        }

        internal async Task<bool> OpenFile()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                if (AllContent.Any(k => k.Any()))
                    foreach (var cl in AllContent.Where(k => k.IsRessetable))
                        cl.Clear();
                var fileName = FileName;
                if (!store.HasFile(fileName)) return false;
                var t = store.GetString(string.Empty, fileName);
                FromXml(t.Result, Folder);
                //FromXml(t.Result, Folder);
                IsSubscribed = true;
                //Console.WriteLine("OpenFile in {0}.", sw.Elapsed);
                return true;
            });
            return true;
        }

        public virtual void ListReset(byte[] buffer, string channel)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(buffer, 0, buffer.Length);
                ms.Position = 0;
                var cl = Serializer.DeserializeWithLengthPrefix<List<BaseContent>>(ms, PrefixStyle.Base128, 72);

                var c = AllContent.FirstOrDefault(k => k.Id == channel);
                if (c == null)
                {
                    c = CreateContentList(channel, typeof(IContent));
                    AllContent.Add(c);
                    LogImbService.LogMessage(this, String.Format("Created contentlist for channel '{0}' and added to service.", channel));
                }
                c.Clear();
                
                foreach (var cli in cl)
                {
                    //cli.IsNotifying = false;
                    cli.Service = this;
                    c.Add(cli);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(String.Format("\nReceived on IMB the contentlist '{0}' (reset list), new content is:", channel));
                
                int count = 1;
                foreach(var cli in cl)
                {
                    sb.AppendLine(String.Format("{0}.) Content '{1}'({2})\n{3}", count, cli.Name ?? "", cli.Id, cli.ToXml().ToString()));
                    count++;
                }
                LogImbService.LogMessage(this, sb.ToString());
            }
        }


        public ContentList CreateContentList(string contentName, Type type)
        {
            var cl = AllContent.FirstOrDefault(k => k.Id == contentName);
            if (cl != null) return cl;

            cl = new ContentList { Id = contentName, ContentType = type, Service = this };
            AllContent.Remove(AllContent.FirstOrDefault(c => string.Equals(contentName, c.Id, StringComparison.InvariantCultureIgnoreCase)));
            AllContent.Add(cl);
            if (contentName == "settings")
                SettingsList = cl;
            return cl;
        }

        #region Enumerators (for e.g. the text finder)

        IEnumerator IEnumerable.GetEnumerator()
        {
            return AllContent.GetEnumerator();
        }

        private class AllContentEnumerator : IEnumerator<BaseContent>
        {
            private ContentListCollection _host;
            private int _contentIndex = 0;
            private int _withinContentIndex = 0;

            public AllContentEnumerator(ContentListCollection host)
            {
                this._host = host;
            }

            public void Reset()
            {
                _contentIndex = 0;
                _withinContentIndex = 0;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public BaseContent Current
            {
                get { return _host[_contentIndex][_withinContentIndex]; }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (_host == null) return false;

                    if (_host[_contentIndex] == null) return false;
                    if (_withinContentIndex < _host[_contentIndex].Count - 1)
                    {
                        _withinContentIndex++;
                        return true;
                    }
                    if (_contentIndex < _host.Count - 1)
                    {
                        _contentIndex++;
                        if (_host[_contentIndex].Count == 0)
                        {
                            continue;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public void Dispose()
            {
            }
        }

        IEnumerator<BaseContent> IEnumerable<BaseContent>.GetEnumerator()
        {
            return new AllContentEnumerator(AllContent);
        }

        IEnumerator<ContentList> IEnumerable<ContentList>.GetEnumerator()
        {
            return AllContent.GetEnumerator();
        }

        #endregion Enumerators (for e.g. the text finder)

        public void TriggerInitialized()
        {
            IsInitialized = true;
        }

        internal static class DelegateStore<T>
        {
#if !WINDOWS_PHONE
            internal static readonly IDictionary<string, Func<T>> Store = new ConcurrentDictionary<string, Func<T>>();
#else
            internal static IDictionary<string, Func<T>> Store = new Dictionary<string, Func<T>>();
#endif
        }
    }
}