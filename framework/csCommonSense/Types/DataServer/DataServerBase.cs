using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csCommon.Types;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Utils.IO;
using csImb;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using DataServer.SqlProcessing;
using IMB3;
using IMB3.ByteBuffers;
using ProtoBuf;
using ProtoBuf.Meta;
using csShared.Utils;
using PoiServer.PoI;
using csCommon.Logging;
using csCommon.Types.DataServer;

namespace DataServer {
    public class ServiceSubscribeEventArgs : EventArgs {
        public Service Service { get; set; }
    }

    [ProtoContract]
    class TypeSurrogate
    {
        [ProtoMember(1)]
        public string AssemblyQualifiedName { get; set; }
        // protobuf-net wants an implicit or explicit operator between the types
        public static implicit operator Type(TypeSurrogate value)
        {
            return value == null ? null : Type.GetType(value.AssemblyQualifiedName);
        }
        public static implicit operator TypeSurrogate(Type value)
        {
            return value == null ? null : new TypeSurrogate
            {
                AssemblyQualifiedName = value.AssemblyQualifiedName
            };
        }
    }
    public class DataServerBase : PropertyChangedBase, IDataServer {
        public const int CollaborativeObjectsSeparator = 70;
        private const string ServiceChannelExtentions = ".Services";
        private static Dictionary<string, Type> contentTypes;
        private string baseFolder;
        public csImb.csImb client;
        private ObservableCollection<PoiService> dynamicServices = new ObservableCollection<PoiService>();
        private int id;
        private bool isRunning;
        private Mode mode;
        private RuntimeTypeModel model;

        private TEventEntry privateChannel;
        private ServiceList services = new ServiceList();

        private PoiService activeService;

        public PoiService ActiveService
        {
            get { return activeService; }
            set
            {
                activeService = value; NotifyOfPropertyChange(() => ActiveService);
                if (AppState.Imb != null && AppState.Imb.Status != null)
                {
                    AppState.Imb.Status.Action = (value != null) ? value.Name : "";
                    AppState.Imb.UpdateStatus();
                }
            }
        }


        public DataServerBase(){
            Templates = new ObservableCollection<PoiService>();
        }

        private BindableCollection<Service> subscriptions;

        /// <summary>
        ///     determines which content should be synced
        /// </summary>
        public int SyncPriority { get; set; }

        public static Dictionary<string, Type> ContentTypes {
            get {
                if (contentTypes != null) return contentTypes;
                try
                {
                    contentTypes = new Dictionary<string, Type>();

                    var type = typeof(IContent);
                    var types = AppDomain.CurrentDomain.GetAssemblies().ToList()
                        .SelectMany(s => s.GetTypes())
                        .Where(type.IsAssignableFrom);

                    foreach (var t in types)
                    {
                        if (t.IsInterface) continue;
                        try
                        {
                            var tt = (IContent)Activator.CreateInstance(t, null);
                            contentTypes[tt.XmlNodeId] = t;
                        }
                        catch (Exception e)
                        {
                            Logger.Log("DataService", "Unable to initialize content type : " + t, e.Message,
                                Logger.Level.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    // FIXME TODO Deal with exception!
                }

                return contentTypes;
            }
        }

        public BindableCollection<Service> Subscriptions {
            get { return subscriptions; }
            set {
                subscriptions = value;
                NotifyOfPropertyChange(() => Subscriptions);
            }
        }

        public RuntimeTypeModel Model {
            get { return model; }
            set {
                try {
                    model = value;
                    // Net all versions have built-in serializer/deserialiser for System.Type
                    // Built in type: model.Add(typeof(Type), false).SetSurrogate(typeof(TypeSurrogate));
                    model.Add(typeof(PrivateMessage), true);
                    model.Add(typeof(BaseContent), true);
                    model.Add(typeof(ContentList), true);
                    model.Add(typeof(IContent), true);
                    model[typeof(BaseContent)].AddSubType(50, typeof(PoI));
                    model[typeof(BaseContent)].AddSubType(100, typeof(Event));
                    model[typeof(BaseContent)].AddSubType(150, typeof(ServiceSettings));
                    model[typeof(BaseContent)].AddSubType(180, typeof(SqlQuery));
                    model[typeof(BaseContent)].AddSubType(200, typeof(Track));
                    model[typeof(BaseContent)].AddSubType(250, typeof(Task));
                    model.Add(typeof(PoI), true);
                    model.Add(typeof(Track), true);
                    model.Add(typeof(PoIStyle), true);
                    model.Add(typeof(Event), true);
                    model.Add(typeof(Task), true);
                    model.Add(typeof(Color), false).SetSurrogate(typeof(MyColor));
                    model.Add(typeof(Color), false).Add("A", "R", "G", "B");
                    model.Add(typeof(Point), false).Add("X", "Y");
                    model.Add(typeof(BindableCollection<IContent>), true);
                    model.Add(typeof(MetaInfo), true);
                    model.Add(typeof(MetaInfoCollection), true);
                    model.Add(typeof(MetaTypes), true);
                    model.Add(typeof(Model), true);
                    model.Add(typeof(ModelParameter), true);
                    // TODO ERIK Check whether we need to serialize this too. Currently, it generates an exception when compiling the model.
                    //model.Add(typeof (Service), true).AddSubType(50, typeof(PoiService));
                    model.CompileInPlace();
                }
                catch (Exception e) {
                    Logger.Log("DataServer", "Error creating runtimetypemodel", e.Message, Logger.Level.Fatal);
                }
            }
        }

        public ServiceList Services {
            get { return services; }
            set {
                services = value;
                NotifyOfPropertyChange(() => Services);
            }
        }

        public ObservableCollection<PoiService> DynamicServices {
            get { return dynamicServices; }
            set {
                dynamicServices = value;
                NotifyOfPropertyChange(() => DynamicServices);
            }
        }

        public ObservableCollection<PoiService> Templates { get; set; }

        public bool AutoTracking { get; set; }

        #region IDataServer Members

        /// <summary>
        ///     Start server
        /// </summary>
        public void Start(Mode startMode)
        {
            
            mode = startMode;
            //Services.CollectionChanged += ServicesChanged;
            Model = RuntimeTypeModel.Default;
            Services.CollectionChanged += ServicesCollectionChanged;

            if (client != null)
            {
                if (client.IsConnected)
                    InitImb();
                else
                    client.Connected += (e, s) => InitImb();
            }

            isRunning = true;
        }

        /// <summary>
        ///     Stop server
        /// </summary>
        public void Stop(){
            isRunning = false;
            Services.CollectionChanged -= ServicesCollectionChanged;
            foreach (var s in Services.Where(k => k.IsInitialized && k.IsSubscribed)) UnSubscribe(s);
            //client.Close();
        }

        public bool IsRunning {
            get { return isRunning; }
            set {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        private void ServicesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e){
            DynamicServices.Clear();
            foreach (var ds in Services.Where(k => !k.StaticService && k is PoiService))
                DynamicServices.Add((PoiService)ds);
        }

        #endregion

        public event EventHandler<ServiceSubscribeEventArgs> Subscribed;
        public event EventHandler<ServiceSubscribeEventArgs> UnSubscribed;

        /// <summary>
        ///  Initialize imb connection
        /// </summary>
        private void InitImb() {
            id = client.Id;
            foreach (var v in client.Variables)
            {
                ProcessReceivedImbBusVariable(v.Key, v.Value);
            }
            client.Imb.OnVariable += Imb_OnVariable;
            privateChannel = client.Imb.Subscribe(client.Id + ServiceChannelExtentions);
            privateChannel.OnBuffer += ReceivedPrivateMessage;
        }

        public void SendFile(string imageId, byte[] image, Service service){
            SendPrivateMessage(service.Server, PrivateMessageActions.SendData, service.Id, imageId, image, Guid.Empty, "");
        }

        private void Imb_OnVariable(TConnection aConnection, string aVarName, byte[] aVarValue, byte[] aPrevValue)
        {
            var name = aVarName;
            var v = Encoding.UTF8.GetString(aVarValue, 0, aVarValue.Length);
            ProcessReceivedImbBusVariable(name, v);
        }

        private List<Guid> mHandledServices = new List<Guid>();

        private void ProcessReceivedImbBusVariable(string pImbVariableName, string pImbVariableContent)
        {
            if (pImbVariableName.StartsWith(Service.ServiceVariableName))
                ReceivedServiceNotificationOnImbBus(pImbVariableName, pImbVariableContent);
        }

        private Dictionary<string, string> mLastProcessedImbVariables = new Dictionary<string, string>(); // Key:Variable name ;Value:content

        public event  EventHandler<JoinSharedServiceEventArgs> OnNewSharedService;
        /// <summary>
        /// IMB bus variable updated 
        /// </summary>
        /// <param name="pImbVariableName"></param>
        /// <param name="pImbVariableContent"></param>
        private void ReceivedServiceNotificationOnImbBus(string pImbVariableName, string pImbVariableContent)
        {
            if (mLastProcessedImbVariables.ContainsKey(pImbVariableName))
                if (mLastProcessedImbVariables[pImbVariableName] == pImbVariableContent) return; // IMB variable didn't change since
            mLastProcessedImbVariables[pImbVariableName] = pImbVariableContent;
            // IMB variable message structure: "Service:<message>"
            var v = pImbVariableContent.Split('|');

            var sharedServiceGuid = Guid.Parse(pImbVariableName.Split(':')[1]);
            var shadowPoiService   = (PoiService)Services.FirstOrDefault(k => k.Id == sharedServiceGuid); // Check if service is already known
            var isSharedServiceAlreadyKnown = (shadowPoiService != null); // Is the shared service already known
            


            bool isSharedServiceStoppedNotification = (v.Length == 1); // When there is no service name
            bool isSharedServiceNotification = (v.Length == 2);



            if (isSharedServiceNotification)
            {
                var imbHandleSharingService = int.Parse(v[0]); // The IMB handle of client sharing this service
                var sharedServiceName = v[1];

                ImbClientStatus sharedServiceHosting = null;
                string hostingClient = "unknown";
                // Is the user friendly name known for IMB id?
                if (AppState.Imb.Clients.TryGetValue(imbHandleSharingService, out sharedServiceHosting))
                {
                    hostingClient = sharedServiceHosting.Name;
                };

                if (shadowPoiService == null)
                {
                    LogCs.LogMessage(
                        String.Format("Received notification on IMB bus of new shared service '{0}' (hosted by IMB client {1} ({2}))",
                        sharedServiceName, imbHandleSharingService, hostingClient));
                    shadowPoiService = CreateShadowServiceForSharedService(
                        sharedServiceName, sharedServiceGuid, imbHandleSharingService);
                }
                else
                {
                    if (shadowPoiService.Name != sharedServiceName)
                    {
                        LogCs.LogMessage(
                             String.Format("Received notification on IMB bus: shared service '{0}' is renamed to '{1}' ({2})", 
                             shadowPoiService.Name, sharedServiceName, sharedServiceGuid));
                        shadowPoiService.Name = sharedServiceName; // Update name
                        NotifyOfPropertyChange(() => DynamicServices);
                    }
                }
                Execute.OnUIThread(() =>
                {
                    // At startup, the active group may not be initialized
                    if (AppState.Imb.ActiveGroup == null && AppState.Imb.Groups != null && AppState.Imb.Groups.Count > 0) {
                        var group = AppState.Imb.Groups.FirstOrDefault(g => g.IsActive);
                        if (group != null)
                        {
                            LogCs.LogMessage(String.Format("Auto join IMB group '{0}' (first group where this IMB client is member of) ", group.Name));
                            AppState.Imb.JoinGroup(group);
                        }
                    }
                    // Is the shared service part of the active IMB group?
                    if (AppState.Imb.ActiveGroup != null && AppState.Imb.ActiveGroup.Layers.Contains(sharedServiceGuid))
                    {
                        if (isSharedServiceAlreadyKnown)
                        {
                            if (!shadowPoiService.IsLocal) return;
                            LogCs.LogMessage(String.Format("The shared service '{0}' is shared in active IMB group '{1}': auto subscribe",
                               shadowPoiService.Name, AppState.Imb.ActiveGroup.Name));
                            Subscribe(shadowPoiService);

                        }
                        else
                        {
                            shadowPoiService.Start();
                            LogCs.LogMessage(String.Format("The new created shared service '{0}' is shared in active IMB group '{1}': auto start service",
                                shadowPoiService.Name, AppState.Imb.ActiveGroup.Name));
                        }
                        AppState.TriggerNotification(sharedServiceName + " layer shared in " + AppState.Imb.ActiveGroup.Name, pathData: MenuHelpers.LayerIcon);
                    }
                    else
                    {
                        if (!mHandledServices.Contains(sharedServiceGuid))
                        {
                            mHandledServices.Add(sharedServiceGuid);
                            // Look like old code; The framework now tries to join the first group it sees. 
                            // Is some cases to group is found
                            bool handled = false;
                            if (OnNewSharedService != null)
                            {
                                var args = new JoinSharedServiceEventArgs(sharedServiceName, sharedServiceGuid, imbHandleSharingService);
                                OnNewSharedService(this, args); // Join new shared service or not?
                                if ((args.JoinSharedService.HasValue) && (args.JoinSharedService.Value))
                                {
                                    LogCs.LogMessage(String.Format("The eventhandler OnNewSharedService returned to join shared service '{0}'",
                                       shadowPoiService.Name));
                                    JoinSharedService(isSharedServiceAlreadyKnown, shadowPoiService);
                                    mHandledServices.Remove(shadowPoiService.Id);
                                    
                                }
                                handled = args.JoinSharedService.HasValue;
                            }
                            if (!handled)
                            {
                                if (AppState.Config.GetBool("CsFramework.ShowJoinServiceNotification", true))
                                {


                                    LogCs.LogMessage(String.Format("Ask user with notification message to join shared service '{0}'", shadowPoiService.Name));
                                    var nea = new NotificationEventArgs
                                    {
                                        Text = "Do you want to join?",
                                        Header = sharedServiceName + " now available",
                                        Duration = new TimeSpan(0, 0, 30),
                                        Background = AppState.AccentBrush,
                                        Image =
                                            new BitmapImage(
                                                new Uri(
                                                    @"pack://application:,,,/csCommon;component/Resources/Icons/layers4.png")),
                                        Foreground = Brushes.White,
                                        Options = new List<string> { "Yes", "No" }
                                    };
                                    nea.Closing += (services, e) => mHandledServices.Remove(sharedServiceGuid);
                                    nea.OptionClicked += (s, n) =>
                                    {
                                        mHandledServices.Remove(shadowPoiService.Id);
                                        if (n.Option == "Yes")
                                        {
                                            LogCs.LogMessage(String.Format("User confirmed to join shared service '{0}'", shadowPoiService.Name));
                                            JoinSharedService(isSharedServiceAlreadyKnown, shadowPoiService);
                                        }


                                    };
                                    AppState.TriggerNotification(nea);

                                }
                                else
                                {
                                    mHandledServices.Remove(shadowPoiService.Id);
                                    LogCs.LogMessage(String.Format("The config value 'CsFramework.ShowJoinServiceNotification' is false and event OnNewSharedService not set, no join dialog shown for shared service '{0}'",
                                        shadowPoiService.Name));
                                }
                            }
                        }
                    }
                });
                
            }
            else if (isSharedServiceStoppedNotification)
            {
                Execute.OnUIThread(() =>
                    {
                        LogCs.LogMessage(String.Format("Received notification on IMB bus: shared service '{0}' is not avaliable anymore.", shadowPoiService.Name));
                        AppState.TriggerNotification("Layer '" + shadowPoiService.Name + "' is not available anymore");
                        DeleteService(shadowPoiService);
                    });
                    

            } // else parse error!
        }


        private void JoinSharedService(bool pServiceAlreadyExsisted, PoiService pSharedService)
        {

            if (pServiceAlreadyExsisted)
            {
                // Request the sharing service IMB client to add this client to subscribtion list
                Subscribe(pSharedService);
            }
            else
                pSharedService.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pSharedServiceGuid">The ID of the service that is being shared by remote IMB client</param>
        /// <param name="PImbHandleSharingService">The IMB handle of the remote IMB client that is sharing</param>
        /// <returns></returns>
        private PoiService CreateShadowServiceForSharedService(string pSharedServiceName, Guid pSharedServiceGuid, int pImbHandleSharingService)
        {
            if (!FileStore.FolderExists("Services"))
                FileStore.CreateFolder("Services");
            if (!FileStore.FolderExists("Services\\" + pSharedServiceGuid))
                FileStore.CreateFolder("Services\\" + pSharedServiceGuid);
            var shadowPoiService = new PoiService
            {
                Id = pSharedServiceGuid,
                Name = pSharedServiceName,
                IsShared = true,
                IsLocal = false,
                StaticService = false, /* could be true of false: don't known is static of dynamic service */
                Server = pImbHandleSharingService,
                RelativeFolder = "Shared Layers",
                Mode = Mode.client,
                Folder = Path.Combine(Path.Combine(FileStore.GetLocalFolder() , "Services"), pSharedServiceGuid.ToString()),
                dsb = this
            };
            Services.Add(shadowPoiService);
            shadowPoiService.InitPoiService();
            LogCs.LogMessage(String.Format("Added shared service '{0}' (GUID: {1}) to local servicelist, the orginal service is hosted on IMB client with handle {2}.",
                    pSharedServiceName, pSharedServiceGuid, pImbHandleSharingService));
            LogCs.LogMessage(String.Format("Shared service '{0}' stores persistent data in folder '{1}'.", pSharedServiceName, shadowPoiService.Folder));
            NotifyOfPropertyChange(() => DynamicServices);
            return shadowPoiService;
        }
        private static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        public void Start(string folder, Mode myMode, string originFolder, bool autoStart = false, bool addLocalServices = true){
            Start(myMode);
            baseFolder = folder;
            if (addLocalServices) AddLocalDataServices(folder, myMode, originFolder, autoStart);
        }

        public void AddLocalDataServices(string folder, Mode myMode, string originFolder = "", bool autoStart = false)
        {
            if (string.IsNullOrEmpty(originFolder))
            {
                originFolder = Path.Combine(Directory.GetCurrentDirectory(), AppState.Config.Get("Poi.LocalFolder", "PoiLayers"));
            }

            if (!FileStore.FolderExists(folder)) return;

            var poiServiceImporters = PoiServiceImporters.Instance;
            foreach (var importer in poiServiceImporters)
            {
                if (!importer.SupportsMetaData)
                {
                    continue; // TODO This is currently correct, but not once we split data and metadata. - Skip all importers that cannot import data plus metadata (in our case, MetaInfo).
                }
                var files = FileStore.GetFiles(folder, string.Format("*.{0}", importer.DataFormatExtension));
                foreach (var file in files)
                {
                    try
                    {
                        var importData = importer.ImportData(new FileLocation(file));
                        if (!importData.Successful) continue;
                        var poiService = importData.Result;
                        if (!poiService.IsTemplate)
                        {
                            AddService(poiService, myMode);
                        }
                        else
                        {
                            Templates.Add(poiService);
                        }
                        if (autoStart)
                        {
                            poiService.AutoStart = true;
                        }
                        if (myMode != Mode.server) continue;
                        Subscribe(poiService);
                        if (poiService.Settings != null)
                        {
                            poiService.Settings.ShareOnline = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log("DataServerBase", "AddLocalDataServices", e.Message, Logger.Level.Error);
                        // Simply skip the file.
                    }
                }
            }

            var dirs = FileStore.GetFolders(folder);
            foreach (var d in dirs)
                AddLocalDataServices(d, myMode, originFolder, autoStart);
        }

        // TODO This (legacy) code is used in multiple spots in the code; we now have more than one way to load a data service.
        // Load .ds file and creates a service fot this ds file
        public PoiService AddLocalDataService(string folder, Mode _mode, string file, string originFolder = "", bool autoStart = false){
            if (string.IsNullOrEmpty(originFolder)) originFolder = Path.Combine(Directory.GetCurrentDirectory(), AppState.Config.Get("Poi.LocalFolder", "PoiLayers"));
            // Use GUID in filename is ID for service (important when file is cloned!)
            var f = file.Split('\\').Last();
            var stat = (f.StartsWith("~"));
            if (stat)
                f = f.Remove(0, 1);
            var idname = f.Substring(0, f.Length - 3).Split('.'); // p.Name.Substring(0, p.Name.Length - 3).Split('.');
            var guid = idname.Length > 1 && !string.IsNullOrEmpty(idname[1])
                ? Guid.Parse(idname[0])
                : Guid.NewGuid();
            if (idname.Length != 2) return null;
            var name = idname[1].Replace("_", "");


            var ps = new PoiService
            {
                IsLocal = true,
                Folder = folder,
                Id = guid,
                Name = name,
                StaticService = stat,
                RelativeFolder = folder.Replace(originFolder, string.Empty),
                AutoStart = autoStart
            };
            ps.InitPoiService();
            AddService(ps, _mode);

            if (_mode != Mode.server) return ps;
            Subscribe(ps);
            if (ps.Settings != null) ps.Settings.ShareOnline = true;
            return ps;
        }

        //        public void AddLocalDataServiceTemplate(string folder, string file) {
        //            var idName = Path.GetFileNameWithoutExtension(file); 
        //            var guid = Guid.NewGuid();
        //            if (string.IsNullOrEmpty(idName)) return;
        //            var ps = new PoiService {
        //                IsLocal = true,
        //                Folder = folder,
        //                Id = guid,
        //                Name = idName,
        //                IsTemplate = true,
        //                StaticService = false,
        //                RelativeFolder = folder.Replace(baseFolder, string.Empty)
        //            };
        //            ps.InitPoiService();
        //            Templates.Add(ps);
        //            //CheckSettings(ps);
        //        }
        //
        //        public PoiService AddLocalDataServiceFromCsv(string folder, Mode _mode, string file, string originFolder = "",
        //            bool autoStart = false)
        //        {
        //            PoiServiceImporterFromFileList poiServiceImporterFromFileList = PoiServiceImporterFromFileList.Instance;
        //            IImporter<FileLocation, PoiService> suitableImporter = poiServiceImporterFromFileList.FirstOrDefault(importer => importer.DataFormatExtension.Equals("csv"));
        //
        //            if (suitableImporter == null)
        //            {
        //                throw new NotImplementedException("Cannot find an importer for CSV files in the assembly.");
        //            }
        //
        //            IOResult<PoiService> ioResult = suitableImporter.ImportData(new FileLocation(Path.Combine(folder, file)));
        //            if (ioResult.Successful)
        //            {
        //                return ioResult.Result;
        //            }
        //            else
        //            {
        //                throw ioResult.Exception;
        //            }
        //        }

        //private static void CheckSettings(Service ps) {
        //    if (ps.SettingsList != null) return;
        //    ps.SettingsList = ps.CreateContentList("settings", typeof(ServiceSettings));
        //    ps.SettingsList.Add(new ServiceSettings());
        //    ps.AllContent.Remove(ps.AllContent.FirstOrDefault(c => string.Equals(ps.SettingsList.Id, c.Id, StringComparison.InvariantCultureIgnoreCase)));
        //    ps.AllContent.Add(ps.SettingsList);
        //}

        /// <summary>
        ///     received message on private channel
        /// </summary>
        private void ReceivedPrivateMessage(TEventEntry aEvent, int aTick, int aBufferId, TByteBuffer aBuffer){ // REVIEW TODO fix: removed async
            var pm = PrivateMessage.ConvertByteArrayToMessage(aBuffer.Buffer);
            if (pm == null)
            {
                LogCs.LogWarning(String.Format("Received IMB message: Corrupt message! ", pm.Id, pm.Action));
                return;
            }
            var s = Services.FirstOrDefault(k => k.Id == pm.Id);
            if (s == null)
            {
                LogCs.LogWarning(String.Format("Received IMB message: Service with GUID '{0}' is not found in servicelist (msg action is {1}). Ignore message. ",
                        pm.Id, pm.Action));
                return;
            }
            var reqClient = AppState.Imb.FindClient(pm.Sender);
            switch (pm.Action) {
                case PrivateMessageActions.RequestData:
                    SendData(pm.Sender, s, pm.ContentId, pm.OwnerId, pm.Channel);
                    break;
                case PrivateMessageActions.ResponseData:
                    if ((s.store.SaveBytes(s.Folder, pm.ContentId, pm.Content, false))) // REVIEW TODO fix: removed await
                        s.DataReceived(pm.ContentId, pm.Content);
                    break;
                case PrivateMessageActions.SubscribeRequest:
                    // TODO EV Why do you set server initialization to false? When adding a task, it is not synchronized anymore (RegisterContent is cancelled).
                    //s.IsInitialized = false;
                    if (!s.Subscribers.Contains(pm.Sender)) s.Subscribers.Add(pm.Sender);
                    SendToImbBusSubscriptedToService(s.Id, pm.Sender);
                    
                    LogCs.LogMessage(String.Format("Received IMB message: IMB client with handle {0} ({1}) requested to join service '{2}' (joined service).",
                        pm.Sender, (reqClient != null) ? reqClient.Name : "-", s.Name));
                    if (reqClient != null && (AppState.Imb.ActiveGroup == null || !AppState.Imb.ActiveGroup.Clients.Contains(reqClient.Id)))
                        Execute.OnUIThread(
                            () => AppState.TriggerNotification(reqClient.Name + " has joined " + s.Name, image: reqClient.Image));
                    break;
                case PrivateMessageActions.UnsubscribeRequest:
                    //s.IsInitialized = false;
                    if (s.Subscribers.Contains(pm.Sender)) s.Subscribers.Remove(pm.Sender);
                    SendPrivateMessage(pm.Sender, PrivateMessageActions.Unsubscribe, s.Id);
                    var unsubClient = AppState.Imb.FindClient(pm.Sender);
                    if (unsubClient != null) Execute.OnUIThread(() => AppState.TriggerNotification(unsubClient.Name + " has left " + s.Name, image: unsubClient.Image));

                    break;
                case PrivateMessageActions.ServiceReset: // requested for service reset
                    int priority = 2;
                    int.TryParse(pm.Channel, out priority);
                    SendServiceReset(pm.Sender, s.Id, priority);
                    //var resx = s.ToXml().ToString();
                    //SendPrivateMessage(pm.Sender,PrivateMessageActions.ResponseXml,s.Id,resx,Guid.Empty);
                    break;
                case PrivateMessageActions.RequestXml:
                    var res = s.ToXml().ToString();
                    SendPrivateMessage(pm.Sender, PrivateMessageActions.ResponseXml, s.Id, res, Guid.Empty);
                    break;
                case PrivateMessageActions.ResponseXml:
                    LogCs.LogMessage(String.Format("Received IMB message: Recieved service XML definition for service {0} ({1}), Load XML into service.",
                         s.Name, s.Id));
                    LogCs.ToFile(String.Format("./logging/ReceivedServiceDefinition_{0}_{1}_{2}", s.Name, s.Id, DateTime.Now.Ticks), pm.ContentId);
                    s.FromXml(pm.ContentId, s.Folder);
                    s.TriggerInitialized();
                    break;
                case PrivateMessageActions.Subscribe:
                    LogCs.LogMessage(String.Format("Received IMB message: Subscribe to (join) service {0} ({1}) mode={2}", s.Name, s.Id, mode));
                    s.Subscribe(mode);
                    s.SubscribeServiceChannel();
                    //Subscribe(s);
                    if (Subscribed != null) Subscribed(this, new ServiceSubscribeEventArgs { Service = s });
                    var npm = new PrivateMessage {
                        Action = PrivateMessageActions.ServiceReset,
                        Id = s.Id,
                        Sender = client.Id,
                        Channel = SyncPriority.ToString()
                    };
                    SendPrivateMessage(pm.Sender, npm);
                    break;
                case PrivateMessageActions.Unsubscribe:
                    s.Unsubscribe();
                    s.UnSubscribeServiceChannel();
                    if (UnSubscribed != null) UnSubscribed(this, new ServiceSubscribeEventArgs { Service = s });
                    break;
                case PrivateMessageActions.ListReset: // receiving lists from server
                    ListReset(s.Id, pm.Content, pm.Channel);
                    if (pm.ContentId == "First")
                    {
                        count = 0;
                    }
                    count += pm.Content.Length;
                    if (pm.ContentId == "Last")
                    {
                        var l = count;
                        s.TriggerInitialized();
                    }
                    LogCs.LogMessage(String.Format("Received IMB message: IMB client with handle {0} ({1}) ordered to reset content '{2}' of service '{3}'.",  
                        pm.Sender, (reqClient != null) ? reqClient.Name : "-", pm.Channel, s.Id));
                    break;
                case PrivateMessageActions.SendData:
                    var f = s.Folder + @"\_Media\" + pm.ContentId;
                    s.store.SaveBytes(f, pm.Content);
                    break;
            }
        }

        private long count = 0;

        /// <summary>
        ///     Send file (icon, image, audio) to a specific client
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="service"></param>
        /// <param name="contentId"></param>
        /// <param name="ownerId"></param>
        /// <param name="channel"></param>
        private void SendData(int receiver, Service service, string contentId, Guid ownerId, string channel){
            var data = service.store.GetBytes(service.Folder, contentId);
            if (data != null) SendPrivateMessage(receiver, PrivateMessageActions.ResponseData, service.Id, contentId, data, ownerId, channel);
        }

        public void SendEvent(string title, string content)
        {
        }

        /// <summary>
        ///     Received a complete content list from the server
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="buffer"></param>
        /// <param name="channel"></param>
        private void ListReset(Guid serviceId, byte[] buffer, string channel){
            Service s = Services.FirstOrDefault(k => k.Id == serviceId);
            if (s == null) return;
            s.ListReset(buffer, channel);
        }

        /// <summary>
        ///     Send all content lists from a service to a client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="serviceId"></param>
        private void SendServiceReset(int sender /* IMB client handle */, Guid serviceId, int priority)
        {

            var s = Services.FirstOrDefault(k => k.Id == serviceId);
            var channel = sender + ServiceChannelExtentions;

            foreach (var cl in s.AllContent)
            {
                try
                {
                    var pm = new PrivateMessage
                    { Action = PrivateMessageActions.ListReset, Sender = client.Id, Id = serviceId };
                    var ms = new MemoryStream();
                    var st = cl.ToList().Where(k => k.Priority <= priority).ToList();
                    Model.SerializeWithLengthPrefix(ms, st, typeof(List<BaseContent>), PrefixStyle.Base128, 72);
                    pm.Content = ms.ToArray();
                    pm.Channel = cl.Id;
                    if (cl == s.AllContent.First()) pm.ContentId = "First";
                    if (cl == s.AllContent.Last()) pm.ContentId = "Last";
                    client.Imb.SignalBuffer(channel, 0, pm.ConvertToStream().ToArray());
                    // Loggging:
                    var sb = new StringBuilder();
                    sb.AppendLine(string.Format("Send to IMB channel '{0}' action ListReset for content '{1}' (ContentId={2},From IMB client handle {3}) for service '{4}', content is: {5}", 
                        channel,cl.Id, pm.ContentId, client.Id, serviceId, Environment.NewLine));
                    var count = 1;
                    foreach(var x in st)
                    {
                        sb.AppendLine(string.Format("{0}.) {1} ", count, x.ToString()));
                        count++;
                        
                    }
                    LogImbService.LogMessage(sb.ToString());
                }
                catch (Exception ex)
                {
                    LogImbService.LogException(string.Format("SendServiceReset failed (broadcast service content on IMB)"), ex);
                }
            }

        }


        /// <summary>
        ///     Add and init a service
        /// </summary>
        /// <param name="s"></param>
        /// <param name="serviceMode"></param>
        public void AddService(Service s, Mode serviceMode){
            if (s.Id == Guid.Empty) s.Id = Guid.NewGuid();
            if (string.IsNullOrEmpty(s.Name)) s.Name = "New Service";


            s.Server = id;

            s.Init(serviceMode, this);
            Services.Add(s);
        }

        private void SendPrivateMessage(int server, PrivateMessageActions action, Guid serviceId, string contentId,
            byte[] data, Guid ownerId, string channelName)
        {
            if (server <= 0) return;
            var channel = server + ServiceChannelExtentions;
            var pm = new PrivateMessage
            {
                Action = action,
                Sender = client.Id,
                Id = serviceId,
                ContentId = contentId,
                Content = data,
                OwnerId = ownerId,
                Channel = channelName
            };
            var content = pm.ConvertToStream().ToArray();
            client.Imb.SignalBuffer(channel, 0, content);
        }

        public void SendPrivateMessage(int server, PrivateMessageActions action, Guid serviceId, string contentId,
                                       Guid ownerId){
            SendPrivateMessage(server, action, serviceId, contentId, null, ownerId, "");
        }

        public void SendPrivateMessage(int pToImbHandle, PrivateMessage pm){
            if (pToImbHandle <= 0) return;
            var channel = pToImbHandle + ServiceChannelExtentions;
            var content = pm.ConvertToStream().ToArray();
            switch(pm.Action)
            {
                case PrivateMessageActions.Subscribe:
                    var s = Services.FirstOrDefault(k => k.Id == pm.Id);
                    LogCs.LogMessage(String.Format("Send to IMB channel '{0}': IMB client handle '{1}' subscribed to service '{2}' {3} hosted on IMB handle '{4}' (joined service)",
                          channel, pToImbHandle, pm.Id, s != null ? s.Name : "-", pm.Sender));
                    break;
            }
            client.Imb.SignalBuffer(channel, 0, content);
        }

        public void SendPrivateMessage(int server, PrivateMessageActions action, Guid serviceId) {
            SendPrivateMessage(server, new PrivateMessage { Action = action, Sender = client.Id, Id = serviceId });
        }

        private void SendToImbBusSubscriptedToService(Guid pServiceId, int pImbClientHandle)
        {
            SendPrivateMessage(pImbClientHandle, PrivateMessageActions.Subscribe, pServiceId);
        }

        /// <summary>
        ///     Remove a service
        /// </summary>
        /// <param name="s"></param>
        public void RemoveService(Service s){
            if (!Services.Contains(s)) return;
            s.Close();
            Services.Remove(s);
        }

        /// <summary>
        ///     delete a service
        /// </summary>
        /// <param name="s"></param>
        public void DeleteService(PoiService s){
            if (s == null) return;
            if (s.IsSubscribed) UnSubscribe(s);
            if (s.Layer != null) s.Layer.Stop();
            if (s.Layer != null && s.Layer.Parent != null && s.Layer.Parent.ChildLayers.Contains(s.Layer))
            {
                s.Layer.Parent.ChildLayers.Remove(s.Layer);
                AppStateSettings.Instance.ViewDef.UpdateLayers();
            }
            Services.Remove(s);

            if (!s.IsLocal || s.Mode != Mode.client) return;
            try {

                if (File.Exists(s.FileName) && s.IsFileBased)
                {

                    File.Delete(s.FileName);
                }


            }
            catch (Exception e){
                Logger.Log("DataService", "Error delete dataservice at: " + s.FileName, e.Message,
                    Logger.Level.Error);
            }
            NotifyOfPropertyChange(() => DynamicServices);
        }

        #region imbClient service actions9

        public void Subscribe(Service s){
            Subscribe(s, s.Mode);
        }



        /// <summary>
        ///   Subscribe to a service. a local file will be openened from file, otherwise a subscription request will be send
        /// </summary>
        /// <param name="s"></param>
        /// <param name="pMode"></param>
        public async void Subscribe(Service s, Mode pMode){
            if (s == null) return;

            if (s.IsLocal){
                // a local file, just open it
                if (s.IsFileBased){
                    s.IsLoading = true;
                    if (!await s.OpenFile())
                    {
                        s.IsLoading = false;
                        return;
                    }

                    //s.IsInitialized = true;
                    Subscribed?.Invoke(this, new ServiceSubscribeEventArgs {Service = s});
                    s.TriggerInitialized();
                    AppStateSettings.Instance.TriggerScriptCommand(this, ScriptCommands.UpdateLayers);
                    s.IsLoading = false;
                }
                else {
                    s.Subscribe(pMode);
                    Subscribed?.Invoke(this, new ServiceSubscribeEventArgs {Service = s});
                    s.TriggerInitialized();
                }
            }
            else
                SendPrivateMessage(s.Server, PrivateMessageActions.SubscribeRequest, s.Id);

        }

        /// <summary>
        ///     unsubscribe from a service
        /// </summary>
        /// <param name="s"></param>
        public void UnSubscribe(Service s){
            if (s == null) return;
            if (s.Mode == Mode.server) s.MakeLocal();
            if (s.IsLocal){
                //if (s.Mode == Mode.client)
                //{
                s.CheckSave();
                Execute.OnUIThread(() => {
                    s.IsSubscribed = false;

                    s.Unsubscribe();

                });
                //}
                if (UnSubscribed != null) UnSubscribed(this, new ServiceSubscribeEventArgs { Service = s });
            }
            else
                SendPrivateMessage(s.Server, PrivateMessageActions.UnsubscribeRequest, s.Id);
        }

        #endregion

        public event EventHandler<TappedEventArgs> Tapped;

        public void TriggerTapped(TappedEventArgs tappedEventArgs){
            var handler = Tapped;
            if (handler != null) handler(this, tappedEventArgs);
        }
    }
}