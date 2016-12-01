using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.MapPlugins.Search;
using csCommon.Plugins.DashboardPlugin;
using DataServer;
using ESRI.ArcGIS.Client;
using Humanizer;
using IMB3;
using IMB3.ByteBuffers;
using csCommon;
using csEvents;
using csImb;
using csCommon.csMapCustomControls.CircularMenu;
using csShared.Documents;
using csShared.FloatingElements;
using csShared.FloatingElements.Classes;
using csShared.Geo;
using csShared.Interfaces;
using csShared.StartPanel;
using csShared.TabItems;
using csShared.Timeline;
using csShared.Utils;
using Media = csImb.Media;

namespace csShared
{
    using csCommon.Imb;
    using csCommon.Logging;
    using csCommon.Utils;

    using csDataServerPlugin;

    using csEvents.Sensors;

    using Zandmotor.Controls.Plot;

    using Position = DataServer.Position;

    public delegate void CommandRecognizedHandler(string speech);

    public class Command
    {
        public string Id;
        public string[] Sentences;
        public CommandRecognizedHandler Handler;
        public KeyGesture Shortcut;
    }

    public class CommandCollection : ObservableCollection<Command>
    {
        public void AddCommand(string id, string[] sentences, CommandRecognizedHandler handler, KeyGesture shortcut = null)
        {
            Add(new Command { Id = id, Sentences = sentences, Handler = handler, Shortcut = shortcut});
        }

        public void RemoveCommand(string id)
        {
            var cc = this.Where(k => k.Id == id).ToList();
            if (!cc.Any()) return;
            foreach (var c in cc) Remove(c);
        }
    }

    public sealed class AppStateSettings : PropertyChangedBase
    {

        #region fields

        private static AppStateSettings instance;
        private BindableCollection<IPluginView> pluginViews;

        private bool isAppInitialized;
        private bool isClosing;
        private bool isFrameworkInitialized;
        private bool bottomPanelVisible;

        private Brush accentBrush                                         = Brushes.Blue;
        private Config config                                             = new Config();
        private ObservableCollection<DownloadProgress> downloadProgresses = new ObservableCollection<DownloadProgress>();
        private FloatingCollection floatingItems                          = new FloatingCollection();
        private BindableCollection<StartPanelSettings> startPanels        = new BindableCollection<StartPanelSettings>();
        private AppStates state                                           = AppStates.Starting;
        private SensorCollectionSet sensorSets                            = new SensorCollectionSet();
        private readonly EventList eventList                              = new EventList {Name = "Application events"};

        public SensorCollectionSet SensorSets
        {
            get { return sensorSets; }
            set
            {
                sensorSets = value;
                NotifyOfPropertyChange(() => SensorSets);
            }
        }

        #endregion

        #region events

        public event EventHandler AppStateChanged;
        public event EventHandler FrameworkInitialized;
        public event EventHandler AppInitialized;
        public event EventHandler<DropEventArgs> Drop;
        public event EventHandler ActivePluginChanged;
        public event EventHandler DataServerChanged;

        private static EventAggregator _eventAggregator;

        /// <summary>
        /// Caliburn Micro's event aggregator, ideal to send events between VM.
        /// </summary>
        /// <see cref="http://sonyarouje.com/tag/event-aggregator/"/>
        public EventAggregator EventAggregator
        {
            get { return _eventAggregator ?? (_eventAggregator = new EventAggregator()); }
        }

        #endregion

        #region notifications

        public delegate void NotificationEventHandler(NotificationEventArgs args);

        private AppStateSettings()
        {
            LogCs.LogMessage("Initialize AppStateSettings");
            ViewDef = new MapViewDef();
            ViewDef.InitBaseLayerProviders();

            StartPanels.Add(new StartPanelSettings
            {
                Orientation      = StartPanelOrientation.bottom,
                TimelineSettings = new TimelineSettings()
            });

            DashboardStateStates = DashboardStateCollection.Load();

            eventLists.AddEventList(eventList);
        }

        public Border MainBorder { get; set; }

        public Dictionary<string, object> Temp { get; set; }

        public event NotificationEventHandler NewNotification;

        public event NotificationEventHandler DeleteNotification;
        public event EventHandler DeleteAllTextNotifications;

        /// <summary>
        /// Trigger a notification. 
        /// NOTE Use Execute.OnUIThread to call this method (as the Brush is otherwise not created on the GUI thread).
        /// </summary>
        /// <param name="title"></param>
        /// <param name="header"></param>
        /// <param name="background"></param>
        /// <param name="image"></param>
        /// <param name="pathData"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public NotificationEventArgs TriggerNotification(string title, string header = "Notification",
            Brush background = null, ImageSource image = null, string pathData = "", long duration = 10000) {
            Brush brush1;
            if (background != null)
                brush1 = background;
            else
            {
                brush1 = new SolidColorBrush(Colors.Black);
                brush1.Opacity = 0.65; 
            }
            var nea = new NotificationEventArgs {
                Text = title,
                Header = header,
                Background = brush1,
                Duration = TimeSpan.FromMilliseconds(duration),
                Foreground = Brushes.White,
                Image = image,
                PathData = pathData
            };
            TriggerNotification(nea);
            return nea;
        }

        public void TriggerDeleteAllTextNotifications()
        {
            var handler = DeleteAllTextNotifications;
            if (handler != null) handler(this, null);
        }

        public void TriggerDeleteNotification(NotificationEventArgs args)
        {
            if (args == null) return;
            var handler = DeleteNotification;
            if (handler != null) Execute.OnUIThread(() => handler(args));
        }

        public void TriggerNotification(NotificationEventArgs args)
        {
            if (args == null) return;
            Execute.OnUIThread(() =>
            {
                var handler = NewNotification;
                if (handler != null) handler(args);
            });
        }

        #endregion

        #region Alarms

        public event EventHandler EventsLoaded;

        public void EventLoaded()
        {
            var handler = EventsLoaded;
            if (handler != null) handler(null, null);
        }

        private EventListCollection eventLists = new EventListCollection();

        public EventList EventList
        {
            get { return eventList; }
        }

        public EventListCollection EventLists
        {
            get { return eventLists; }
            set
            {
                eventLists = value;
                NotifyOfPropertyChange(() => EventLists);
            }
        }

        #endregion

        #region config

        private BindableCollection<Screen> configTabs = new BindableCollection<Screen>();

        public BindableCollection<Screen> ConfigTabs
        {
            get { return configTabs; }
            set
            {
                configTabs = value;
                NotifyOfPropertyChange(() => ConfigTabs);
            }
        }

        #endregion

        public delegate void ScriptCommandEvent(object sender, string command);

        public delegate void WaitForInteraction(object sender, string name, string command);

        private BindableCollection<Pin> pins = new BindableCollection<Pin>();

        private DataServerBase dataServer;

        public DataServerBase DataServer
        {
            get { return dataServer; }
            set
            {
                dataServer = value;
                if (DataServerChanged != null) DataServerChanged(this, null);
            }
        }

        private DashboardConfiguration dashboards = new DashboardConfiguration();

        public DashboardConfiguration Dashboards
        {
            get { return dashboards; }
            set
            {
                dashboards = value;
                NotifyOfPropertyChange(() => Dashboards);
            }
        }

        public BindableCollection<Pin> Pins
        {
            get { return pins; }
            set
            {
                pins = value;
                NotifyOfPropertyChange(() => Pins);
            }
        }

        public double CornerRadius
        {
            get { return Instance.Config.GetInt("Layout.Floating.CornerRadius", 10); }
        }

        #region commands

        private CommandCollection commands = new CommandCollection();

        public CommandCollection Commands
        {
            get { return commands; }
            set
            {
                commands = value;
                NotifyOfPropertyChange(() => Commands);
            }
        }

        #endregion

        private DashboardStateCollection dashboardStates = new DashboardStateCollection();

        public DashboardStateCollection DashboardStateStates
        {
            get { return dashboardStates; }
            set { dashboardStates = value; }
        }

        private static string cacheFolder;
        private FloatingElement fullScreenFloatingElement;
        private ObservableCollection<MenuItem> mainMenuItems = new ObservableCollection<MenuItem>();

        private ObservableCollection<StartPanelTabItem> startPanelTabItems =
            new ObservableCollection<StartPanelTabItem>();

        private string activePlugin;
        private CircularMenuItem circularMenu;
        private BindableCollection<CircularMenuItem> circularMenus = new BindableCollection<CircularMenuItem>();
        private bool dockedFloatingElementsVisible                 = true;
        private ObservableCollection<string> excludedStartTabItems = new ObservableCollection<string>();
        private ObservableCollection<string> filteredStartTabItems = new ObservableCollection<string>();
        private csImb.csImb imb                                    = new csImb.csImb(); // Initialization data is obtained from App.config
        private bool bottomTabMenuVisible                          = true;
        private bool leftTabMenuVisible                            = true;

        public void TriggerCreateCache()
        {
            if (CreateCache != null) CreateCache(this, null);
        }

        private List<IFloatingShareContract> shareContract = new List<IFloatingShareContract>();

        public List<IFloatingShareContract> ShareContracts
        {
            get { return shareContract; }
            set
            {
                shareContract = value;
                NotifyOfPropertyChange(() => ShareContracts);
            }
        }

        public double BottomBarOpacity
        {
            get { return Config.GetDouble("BottomBar.Opacity", 1); }
            set
            {
                Config.SetLocalConfig("BottomBar.Opacity", value.ToString(CultureInfo.InvariantCulture), true);
                NotifyOfPropertyChange(() => BottomBarOpacity);
                NotifyOfPropertyChange(() => OpacityAccentBrush);
            }
        }

        public double LeftBarOpacity
        {
            get { return Config.GetDouble("LeftBar.Opacity", 1); }
            set
            {
                Config.SetLocalConfig("LeftBar.Opacity", value.ToString(CultureInfo.InvariantCulture), true);
                NotifyOfPropertyChange(() => LeftBarOpacity);
            }
        }

        private ObservableCollection<ISearchApi> searchApis = new ObservableCollection<ISearchApi>();

        public ObservableCollection<ISearchApi> SearchApis
        {
            get { return searchApis; }
            set
            {
                searchApis = value;
                NotifyOfPropertyChange(() => SearchApis);
            }
        }

        public event EventHandler IsOnlineChanged;

        public bool IsOnline
        {
            get { return Config.GetBool("App.IsOnline", true); }
            set
            {
                Config.SetLocalConfig("App.IsOnline", value.ToString(CultureInfo.InvariantCulture), true);
                NotifyOfPropertyChange(() => IsOnline);
                if (IsOnlineChanged != null) IsOnlineChanged(this, null);
            }
        }

        public double LeftPanelMaxWidth
        {
            get { return Config.GetDouble("Startpanel.Left.MaxWidth", 550); }
            set
            {
                Config.SetLocalConfig("Startpanel.Left.MaxWidth", value.ToString(CultureInfo.InvariantCulture), true);
                NotifyOfPropertyChange(() => LeftPanelMaxWidth);
            }
        }

        public double StartpanelMaxHeight
        {
            get { return Config.GetDouble("Startpanel.Bottom.MaxHeight", 300); }
            set
            {
                Config.SetLocalConfig("Startpanel.Bottom.MaxHeight", value.ToString(CultureInfo.InvariantCulture), true);
                NotifyOfPropertyChange(() => StartpanelMaxHeight);
            }
        }

        public event EventHandler MapStarted;

        public void TriggerMapStarted()
        {
            if (MapStarted != null) MapStarted(this, null);
        }

        public bool MapShortcuts
        {
            get { return Config.GetBool("Map.ShortcutsEnabled", false); }
            set
            {
                Config.SetLocalConfig("Map.ShortcutsEnabled", value.ToString(), true);
                NotifyOfPropertyChange(() => MapShortcuts);
                CheckMapShortcuts();
            }
        }

        public string MapShortcut1
        {
            get { return Config.Get("Map.Shortcut1", "Google Maps"); }
            set
            {
                Config.SetLocalConfig("Map.Shortcut1", value, true);
                NotifyOfPropertyChange(() => MapShortcut1);
                CheckMapShortcuts();
            }
        }

        public string MapShortcut2
        {
            get { return Config.Get("Map.Shortcut2", "Google Satellite"); }
            set
            {
                Config.SetLocalConfig("Map.Shortcut2", value, true);
                NotifyOfPropertyChange(() => MapShortcut2);
                CheckMapShortcuts();
            }
        }

        public void InitMapShortcuts()
        {
            cmMapShortCut1.Selected += cmMapShortCut1_ItemSelected;
            cmMapShortCut1.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/map-black.png";
            CheckMapShortcuts();
        }

        public void CheckMapShortcuts()
        {
            var s1 = false;
            var s2 = false;
            if (MapShortcuts && ViewDef.SelectedBaseLayer != null)
            {
                if (ViewDef.SelectedBaseLayer.Title != MapShortcut1 && !string.IsNullOrEmpty(MapShortcut1))
                {
                    s1 = true;
                }
                if (ViewDef.SelectedBaseLayer.Title != MapShortcut2 && !string.IsNullOrEmpty(MapShortcut2) &&
                    MapShortcut1 != MapShortcut2)
                {
                    s2 = true;
                }
            }
            if (s1 || s2)
            {
                if (!CircularMenus.Contains(cmMapShortCut1)) CircularMenus.Add(cmMapShortCut1);
            }
            else
            {
                if (CircularMenus.Contains(cmMapShortCut1)) CircularMenus.Remove(cmMapShortCut1);
            }
        }

        #region dashboard states

        private const string FlipState = "Flip";
        private const string TimelineState = "TimelineVisible";
        private const string TimeStartState = "TimelineStart";
        private const string TimeEndState = "TimelineEnd";
        private const string TimeFixState = "TimelineFixed";
        private const string FocusTimeState = "FocusTime";
        private const string PlayerVisible = "PlayerVisible";
        private const string LayersState = "Layers";
        private const string AccLayersState = "AccLayers";

        public void SetDashboard(ref DashboardState dashboard)
        {
            dashboard.States[FlipState] = IsScreenFlipped.ToString();
            dashboard.States[TimelineState] = TimelineManager.Visible.ToString(); // TimelineVisible is deprecated.
            dashboard.States[PlayerVisible] = TimelineManager.PlayerVisible.ToString();
            dashboard.States[TimeEndState] = TimelineManager.End.ToString(CultureInfo.InvariantCulture);
            dashboard.States[TimeStartState] = TimelineManager.Start.ToString(CultureInfo.InvariantCulture);
            dashboard.States[TimeFixState] = TimelineManager.CanChangeTimeInterval.ToString();
            dashboard.States[FocusTimeState] = TimelineManager.FocusTime.ToString(CultureInfo.InvariantCulture);

            var layers = new Dictionary<string, Layer>();
            ViewDef.Layers.GetAllSubLayers("", ref layers);
            dashboard.States[LayersState] = string.Join("|",
                layers.Select(k => k.Key + "~" + k.Value.Visible.ToString() + "~" + k.Value.Opacity).ToArray());

            var acclayers = new Dictionary<string, Layer>();
            ViewDef.AcceleratedLayers.GetAllSubLayers("", ref acclayers);
            dashboard.States[AccLayersState] = string.Join("|",
                acclayers.Select(k => k.Key + "~" + k.Value.Visible.ToString() + "~" + k.Value.Opacity).ToArray());
        }

        private void Dashboards_DashboardActivated(object sender, DashboardEventArgs e)
        {
            if (e.Dashboard == null) return;
            var s = e.Dashboard.States;

            if (s.ContainsKey(LayersState)) UpdateLayerStates(s[LayersState], ViewDef.Layers);
            if (s.ContainsKey(AccLayersState)) UpdateLayerStates(s[AccLayersState], ViewDef.AcceleratedLayers);

            if (s.ContainsKey(FlipState)) IsScreenFlipped = bool.Parse(s[FlipState]);
            if (s.ContainsKey(TimelineState))
                TimelineManager.Visible = bool.Parse(s[TimelineState]); // TimelineVisible is deprecated.
            if (s.ContainsKey(PlayerVisible)) TimelineManager.PlayerVisible = bool.Parse(s[PlayerVisible]);
            if (s.ContainsKey(TimeFixState)) TimelineManager.CanChangeTimeInterval = bool.Parse(s[TimeFixState]);
            DateTime time;
            if (s.ContainsKey(TimeStartState) &&
                DateTime.TryParse(s[TimeStartState], CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                TimelineManager.Start = time;
            if (s.ContainsKey(TimeEndState) &&
                DateTime.TryParse(s[TimeEndState], CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                TimelineManager.End = time;
            if (s.ContainsKey(FocusTimeState) &&
                DateTime.TryParse(s[FocusTimeState], CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                TimelineManager.FocusTime = time;
            TimelineManager.ForceTimeContentChanged();
        }

        private static void UpdateLayerStates(string ll, Layer gl)
        {
            var acclayers = new Dictionary<string, Layer>();
            gl.GetAllSubLayers("", ref acclayers);

            foreach (var l in ll.Split('|'))
            {
                var uu = l.Split('~');
                if (uu.Length != 3) continue;
                var la = uu[0];
                var vi = bool.Parse(uu[1]);
                var op = double.Parse(uu[2]);
                if (!acclayers.ContainsKey(la)) continue;
                var layer = acclayers[la];
                var startStopLayer = layer as IStartStopLayer;
                if (startStopLayer != null)
                {
                    var lss = startStopLayer;
                    if (vi && !lss.IsStarted)
                        lss.Start();
                    if (!vi && lss.IsStarted)
                        lss.Stop();
                }
                else
                {
                    layer.Visible = vi;
                }

                layer.Opacity = op;
                //Layer lay = FindLayer(la);
            }
        }

        #endregion

        private void cmMapShortCut1_ItemSelected(object sender, MenuItemEventArgs e)
        {
            ViewDef.ChangeMapType((ViewDef.SelectedBaseLayer.Title != MapShortcut1) ? MapShortcut1 : MapShortcut2);
        }

        private readonly CircularMenuItem cmMapShortCut1 = new CircularMenuItem();

        public bool IsScreenFlipped
        {
            get { return Config.GetBool("Screen.IsFlipped", false); }
            set
            {
                Config.SetLocalConfig("Screen.IsFlipped", value.ToString(), true);
                NotifyOfPropertyChange(() => IsScreenFlipped);
                NotifyOfPropertyChange(() => ScreenOrientation);
            }
        }

        public double ScreenOrientation
        {
            get { return (IsScreenFlipped) ? 180 : 0; }
        }

        [Obsolete("Deprecated: Please use AppStateSettings.TimelineManager.Visible instead!")]
        public bool TimelineVisible
        {
            get { return TimelineManager != null && TimelineManager.Visible; }
            set
            {
                TimelineManager.Visible = value;
                NotifyOfPropertyChange(() => TimelineVisible);
            }
        }

        public event EventHandler CreateCache;

        public string ActivePlugin
        {
            get { return activePlugin; }
            set
            {
                if (value == activePlugin) return;
                activePlugin = value;
                NotifyOfPropertyChange(() => ActivePlugin);
                if (ActivePluginChanged != null) ActivePluginChanged(this, null);
            }
        }

        public static string TemplateFolder
        {
            get
            {
                if (IsAccessibleFolder(FileStore.GetLocalFolder()))
                {
                    string templateFolder = Path.Combine(FileStore.GetLocalFolder(), "Templates");
                    if (! File.Exists(templateFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(templateFolder);
                        }
                        catch (Exception e)
                        {
                            return CacheFolder;
                        }
                    }
                    return templateFolder;
                }
                else
                {
                    return CacheFolder;
                }
            }    
        }


        public static string CacheFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(cacheFolder)) return cacheFolder;
                const string cacheFolderDefault = @"%TEMP%\cs\";
                var c = ConfigurationManager.AppSettings["CacheFolder"] ?? cacheFolderDefault;
                if (c[c.Length - 1] != '\\') c += @"\";
                cacheFolder = Environment.ExpandEnvironmentVariables(c);
                if (IsAccessibleFolder(cacheFolder)) return cacheFolder;
                var cacheFolderSolved = false;
                if (!Directory.Exists(cacheFolder)) // It may not exist; let's try to make it.
                {
                    try
                    {
                        Directory.CreateDirectory(cacheFolder);
                        cacheFolderSolved = IsAccessibleFolder(cacheFolder); // It should exist, so let's see whether we can write to it.
                    }
                    catch (Exception e)
                    {
                        cacheFolderSolved = false;
                    }
                }

                if (cacheFolderSolved) return cacheFolder;
                cacheFolder = Environment.ExpandEnvironmentVariables(cacheFolderDefault);
                if (!IsAccessibleFolder(cacheFolder))
                {
                    throw new Exception("Cannot establish a folder that is suitable for storing cached items. Adapt Windows %TEMP% variable to a directory with full access.");    
                }

                return cacheFolder;
            }
        }

        private static bool IsAccessibleFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return false;
            }
            try
            {
                // Try to get access to the directory, and see whether we can create files and folders.
                DirectorySecurity accessControlList = Directory.GetAccessControl(cacheFolder);
                if (accessControlList == null)
                    return false;
                var accessRules = accessControlList.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                    return false;

                bool readAllow = false, createDirAllow = false;
                bool readDeny = false, createDirDeny = false;

                // Read allow/deny
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((FileSystemRights.Read & rule.FileSystemRights) != FileSystemRights.Read) continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        readAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        readDeny = true;
                }

                // Create dir allow/deny
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((FileSystemRights.CreateDirectories & rule.FileSystemRights) != FileSystemRights.CreateDirectories) continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        createDirAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        createDirDeny = true;
                }

                return (readAllow && !readDeny) && (createDirAllow && !createDirDeny);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public BindableCollection<CircularMenuItem> CircularMenus
        {
            get { return circularMenus; }
            set
            {
                circularMenus = value;
                NotifyOfPropertyChange(() => CircularMenus);
            }
        }

        public CircularMenuItem CircularMenu
        {
            get { return circularMenu; }
            set
            {
                circularMenu = value;
                NotifyOfPropertyChange(() => CircularMenu);
            }
        }

        public bool IsTouchEnabled
        {
            get {
                return ViewDef.MapControl != null && ViewDef.MapControl.IsHitTestVisible;
            }
            set {
                Execute.OnUIThread(() => {
                    ViewDef.MapControl.IsHitTestVisible = value;
                    //ViewDef.MapControl.IsManipulationEnabled = value;
                });
            }
        }

        public csImb.csImb Imb
        {
            get { return imb; }
            set { imb = value; }
        }

        public ObservableCollection<string> FilteredStartTabItems
        {
            get { return filteredStartTabItems; }
            set
            {
                filteredStartTabItems = value;
                NotifyOfPropertyChange(() => FilteredStartTabItems);
            }
        }

        public ObservableCollection<string> ExcludedStartTabItems
        {
            get { return excludedStartTabItems; }
            set
            {
                excludedStartTabItems = value;
                NotifyOfPropertyChange(() => ExcludedStartTabItems);
            }
        }

        public ObservableCollection<StartPanelTabItem> StartPanelTabItems
        {
            get { return startPanelTabItems; }
            set
            {
                if (startPanelTabItems == value) return;
                startPanelTabItems = value;
                NotifyOfPropertyChange(() => StartPanelTabItems);
            }
        }

        public FloatingElement FullScreenFloatingElement
        {
            get { return fullScreenFloatingElement; }
            set
            {
                if (fullScreenFloatingElement == value) return;
                fullScreenFloatingElement = value;
                NotifyOfPropertyChange(() => FullScreenFloatingElement);
                OnFullScreenFloatingElementChanged(value);
            }
        }

        private void OnFullScreenFloatingElementChanged(FloatingElement value)
        {
            var handler = FullScreenFloatingElementChanged;
            if (handler != null) handler(value, null);
        }

        public ObservableCollection<MenuItem> MainMenuItems
        {
            get { return mainMenuItems; }
            set
            {
                if (mainMenuItems == value) return;
                mainMenuItems = value;
                NotifyOfPropertyChange(() => MainMenuItems);
            }
        }

        public bool DockedFloatingElementsVisible
        {
            get { return dockedFloatingElementsVisible; }
            set
            {
                if (dockedFloatingElementsVisible == value) return;
                dockedFloatingElementsVisible = value;
                NotifyOfPropertyChange(() => DockedFloatingElementsVisible);
            }
        }

        public bool BottomTabMenuVisible
        {
            get { return bottomTabMenuVisible; }
            set
            {
                if (bottomTabMenuVisible == value) return;
                bottomTabMenuVisible = value;
                NotifyOfPropertyChange(() => BottomTabMenuVisible);
            }
        }

        public bool LeftTabMenuVisible
        {
            get { return leftTabMenuVisible; }
            set
            {
                if (leftTabMenuVisible == value) return;
                leftTabMenuVisible = value;
                NotifyOfPropertyChange(() => LeftTabMenuVisible);
            }
        }

        #region imb properties and methods

        private ObservableCollection<TimeEvent> timeEvents = new ObservableCollection<TimeEvent>();

        public ObservableCollection<TimeEvent> TimeEvents
        {
            get { return timeEvents; }
            set { timeEvents = value; }
        }

        public void InitImb()
        {
            if (Imb == null) return;
            if (Imb.IsConnected) Imb.Close();
            var imbCfg = new csImbConfig(Config);
            Imb.Enabled = imbCfg.ImbIsEnabled; 
            if (Imb.IsConnected || !Imb.Enabled) return;
            Config.ApplicationName = Config.Get("App.Name", Config.ApplicationName);
            Imb.Status.Application = Config.ApplicationName;
            Imb.Status.UserId = Config.UserId;
            Imb.Status.Name = Config.Get("FullName", "");
            //Imb.Status.Name = Imb.Status.Name.Replace("[MACHINENAME]", Environment.MachineName);
            //Imb.Status.Name = Imb.Status.Name.Replace("[USERNAME]", Config.UserName);
            if (Application.Current.MainWindow != null)
            {
                Imb.Status.ResolutionX = (int)Application.Current.MainWindow.Width;
                Imb.Status.ResolutionY = (int)Application.Current.MainWindow.Height;
            }

            Imb.Status.Orientation = "horizontal";
            Imb.Status.Os = "Windows7";
            if (string.IsNullOrEmpty(Imb.Status.MyImage)) Imb.Status.MyImage = Config.Get("IMB.MyImage", "arnoud.org/tno/icons/person.png");
            if (string.IsNullOrEmpty(Imb.Status.Type)) Imb.Status.Type = imbCfg.ImbType;
            Imb.Status.Client = true;
            Imb.Status.Manunifactor = "Microsoft";
            Imb.Status.Action = "Map";

            Imb.Init(imbCfg.ImbHostName, imbCfg.ImbPortNumber, Imb.Status.Name, 1, imbCfg.ImbFederation);

            //Todo: Fix Jeroen
            if (!Imb.IsConnected) return;
            Imb.Status.Media.OnNormalEvent += Media_OnNormalEvent;
            Imb.Status.Media.OnBuffer += Media_OnBuffer;
        }

        static void Media_OnBuffer(TEventEntry aEvent, int aTick, int aBufferId, TByteBuffer aBuffer)
        {

        }


        private void Media_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            var m = Media.FromString(aPayload.ReadString());

            // find origin
            if (m.Type == "image")
            {
                var d = new Document
                {
                    Id = m.Location,
                    Location = m.Location,
                    FileType = FileTypes.image,
                    ShowThumbNail = false
                };
                var fe = FloatingHelpers.CreateFloatingElement(d);
                fe.OriginPosition = new Point(0, 200);
                fe.OriginSize = new Size(5, 5);


                FloatingItems.AddFloatingElement(fe);
            }
            if (m.Type == "web")
            {
                var d = new Document
                {
                    Id = m.Location,
                    Location = m.Location,
                    FileType = FileTypes.web
                };
                var fe = FloatingHelpers.CreateFloatingElement(d);
                FloatingItems.AddFloatingElement(fe);
            }
        }

        //private ObservableCollection<TimeEvent> _timeEvents = new ObservableCollection<TimeEvent>();

        //public ObservableCollection<TimeEvent> TimeEvents
        //{
        //    get { return _timeEvents; }
        //    set { _timeEvents = value; }
        //}


        public void InitImb(string server, int port, string federation, string name, int id)
        {
            Config.SetLocalConfig(csImbConfig.CfgNameImbHost, server);
            Config.SetLocalConfig(csImbConfig.CfgNameImbPort, port.ToString(CultureInfo.InvariantCulture));
            Config.SetLocalConfig(csImbConfig.CfgNameImbFederation, federation);
            Config.SetLocalConfig("FullName", name, true);
            Config.UserId = id;
            Imb.Status.Name = name;
            InitImb();
        }

        #endregion

        #region startpanel

        public void RemoveStartPanelTabItem(StartPanelTabItem spti)
        {
            StartPanelTabItems.Remove(spti);
            if (StartPanelTabItems.Count == 0) BotomPanelVisible = false;
        }

        public void AddStartPanelTabItem(StartPanelTabItem spti)
        {
            if (spti == null) throw new NullReferenceException("spti");
            StartPanelTabItems.Add(spti);
            BotomPanelVisible = true;
        }


        public void RenameStartPanelTabItem(string oldName, string newName)
        {
            foreach (var tb in StartPanelTabItems.Where(tb => string.Equals(tb.Name, oldName)))
            {
                tb.Name = newName;
                return; // only rename the first
            }
        }

        public void ActivateStartPanelTabItem(string oldName)
        {
            foreach (var tb in StartPanelTabItems.Where(tb => string.Equals(tb.Name, oldName)))
            {
                tb.IsSelected = true;
                return; // only select the first
            }
        }

        public void UpdateStartPanelTabItem(StartPanelTabItem spti)
        {

        }

        #endregion

        #region busyindication properties and methods

        private readonly object dlpLock = new object();

        public bool BusyIndicator
        {
            get
            {
                lock (dlpLock) {
                    return DownloadProgresses.Any();
                }
            }
        }

        public ObservableCollection<DownloadProgress> DownloadProgresses
        {
            get { return downloadProgresses; }
            set
            {
                downloadProgresses = value;
                NotifyOfPropertyChange(() => DownloadProgresses);
            }
        }

        public Guid AddDownload(string name, string myState)
        {
            lock (DownloadProgresses)
            {
                var dp = new DownloadProgress
                {
                    Name = name,
                    Id = Guid.NewGuid(),
                    Progress = 0,
                    State = myState
                };
                DownloadProgresses.Add(dp);
                if (DownloadProgresses.Count == 1) NotifyOfPropertyChange("BusyIndicator");
                return dp.Id;
            }
        }

        public void FinishDownload(Guid id)
        {
            lock (DownloadProgresses)
            {
                var dp = DownloadProgresses.FirstOrDefault(k => k.Id == id);
                if (dp == null) return;
                DownloadProgresses.Remove(dp);
                if (!DownloadProgresses.Any()) NotifyOfPropertyChange("BusyIndicator");
            }
        }

        public void UpdateDownload(Guid id, double progress, string myState)
        {
            lock (DownloadProgresses)
            {
                var dp = DownloadProgresses.FirstOrDefault(k => k.Id == id);
                if (dp == null) return;
                dp.Progress = progress;
                dp.State = myState;
            }
        }

        #endregion

        #region properties

        /// <summary>
        ///     App is closing, thread loops can stop now
        /// </summary>
        public bool IsClosing
        {
            get { return isClosing; }
            set
            {
                isClosing = value;
                NotifyOfPropertyChange(() => IsClosing);
            }
        }

        public AppStates State
        {
            get { return state; }
            set
            {
                if (state == value) return;
                state = value;
                NotifyOfPropertyChange(() => State);
                OnAppStateChanged();
                Logger.Log("App", "State Changed", state.ToString(), Logger.Level.Info);
            }
        }

        private void OnAppStateChanged()
        {
            var handler = AppStateChanged;
            if (handler != null) handler(this, null);
        }

        public bool IsFrameworkInitialized
        {
            get { return isFrameworkInitialized; }
            set
            {
                if (isFrameworkInitialized == value) return;
                isFrameworkInitialized = value;
                NotifyOfPropertyChange(() => IsFrameworkInitialized);
                if (FrameworkInitialized != null) FrameworkInitialized(this, null);
            }
        }

        public static AppStateSettings Instance
        {
            get { return GetInstance(); }
        }

        public bool IsAppInitialized
        {
            get { return isAppInitialized; }
            set
            {
                if (isAppInitialized == value) return;
                isAppInitialized = value;
                NotifyOfPropertyChange(() => IsAppInitialized);
            }
        }

        private Color accentColor;

        public Color AccentColor
        {
            get { return accentColor; }
            set
            {
                if (accentColor == value) return;
                accentColor = value;
                NotifyOfPropertyChange(() => AccentColor);
            }
        }


        public Brush AccentBrush
        {
            get { return accentBrush; }
            set
            {
                if (Equals(accentBrush, value)) return;
                accentBrush = value;
                NotifyOfPropertyChange(() => AccentBrush);
                NotifyOfPropertyChange(() => OpacityAccentBrush);
            }
        }

        public Brush OpacityAccentBrush
        {
            get { return new SolidColorBrush(AccentColor) { Opacity = BottomBarOpacity }; }
        }

        public FrameworkElement MapControl { get; set; }

        public BindableCollection<IPluginView> PluginViews
        {
            get { return pluginViews; }
            set
            {
                pluginViews = value;
                NotifyOfPropertyChange(() => PluginViews);
            }
        }

        public bool BotomPanelVisible
        {
            get { return bottomPanelVisible; }
            set
            {
                bottomPanelVisible = value;
                NotifyOfPropertyChange(() => BotomPanelVisible);
            }
        }

        public CompositionContainer Container { get; set; }

        public MapViewDef ViewDef { get; set; }

        public TimelineManager TimelineManager
        {
            get { return timelineManager; }
            set
            {
                if (Equals(value, timelineManager)) return;
                timelineManager = value;
                NotifyOfPropertyChange(() => TimelineManager);
            }
        }

        public BindableCollection<StartPanelSettings> StartPanels
        {
            get { return startPanels; }
            set
            {
                startPanels = value;
                NotifyOfPropertyChange(() => StartPanels);
            }
        }

        public Config Config
        {
            get { return config; }
            set { config = value; }
        }

        public FloatingCollection FloatingItems
        {
            get { return floatingItems; }
            set
            {
                floatingItems = value;
                NotifyOfPropertyChange(() => FloatingItems);
            }
        }

        private MediaCache mediaC = new MediaCache();
        public MediaCache MediaC
        {
            get { return mediaC; }
            set { mediaC = value; }
        }

        #endregion

        public void MapLoaded() { }

        #region plugins

        private BindableCollection<IPlugin> plugins = new BindableCollection<IPlugin>();
        private BindableCollection<IPopupScreen> popups = new BindableCollection<IPopupScreen>();

        public BindableCollection<IPopupScreen> Popups
        {
            get { return popups; }
            set
            {
                popups = value;
                NotifyOfPropertyChange(() => Popups);
            }
        }

        public BindableCollection<IPlugin> Plugins
        {
            get { return plugins; }
            set
            {
                plugins = value;
                NotifyOfPropertyChange(() => Plugins);
            }
        }

        public IPlugin GetPlugin(string name, bool reuse)
        {
            if (reuse)
            {
                var result = Plugins.FirstOrDefault(k => k.Name == name);
                if (result != null) return result;
            }
            else
            {
                if (Plugins.Any(k => k.Name == name)) return null;
            }
            if (Container == null) return null;
            var b = Container.GetExportedValues<IPlugin>();
            var plugin = b.FirstOrDefault(k => k.Name == name);
            if (plugin == null) return null;
            plugin.AppState = this;
            Plugins.Add(plugin);

            return plugin;
        }

        public IPlugin InitializeAndStartPlugin(string pPluginIdentifierName, bool reuse)
        {
            try
            {
                var p = InitializePlugin(pPluginIdentifierName, reuse);
                if (p != null)
                {
                    p.Start();
                    LogCs.LogMessage(String.Format("Plugin '{0}' initialized and started.", pPluginIdentifierName));
                }
                else
                {
                    Logger.Log("Plugins", String.Format("Error loading plugin : {0} (not found) ", pPluginIdentifierName), "", Logger.Level.Error);
                }
                return p;
            }
            catch (Exception e)
            {
                LogCs.LogException(String.Format("Failed to load plugin {0} (exception)", pPluginIdentifierName), e);
                Logger.Log("Plugins", "Error loading plugin : " + pPluginIdentifierName, e.Message, Logger.Level.Error);
                return null;
            }
        }

        public IPlugin InitializePlugin(string name, bool reuse)
        {
            var p = GetPlugin(name, reuse);
            if (p == null) return null;
            if (!p.IsRunning) p.Init();
            return p;
        }

        #endregion

        #region maptools

        private BindableCollection<IMapToolPlugin> mapToolPlugins = new BindableCollection<IMapToolPlugin>();

        public BindableCollection<IMapToolPlugin> MapToolPlugins
        {
            get { return mapToolPlugins; }
            set
            {
                mapToolPlugins = value;
                NotifyOfPropertyChange(() => MapToolPlugins);
            }
        }

        public IMapToolPlugin GetMapToolPlugin(string name, bool reuse)
        {
            if (reuse)
            {
                var result = MapToolPlugins.FirstOrDefault(k => k.Name == name);
                if (result != null) return result;
            }
            if (Container != null)
            {
                var b = Container.GetExportedValues<IMapToolPlugin>();
                var plugin = b.FirstOrDefault(k => k.Name == name);
                if (plugin != null)
                {
                    MapToolPlugins.Add(plugin);
                }

                return plugin;
            }
            return null;
        }

        public IMapToolPlugin InitializeAndStartMapToolPlugin(string name, bool reuse)
        {
            var p = InitializeMapToolPlugin(name, reuse);
            if (p != null) p.Start();
            return p;
        }

        public IMapToolPlugin InitializeMapToolPlugin(string name, bool reuse)
        {
            var p = GetMapToolPlugin(name, reuse);
            if (p == null) return null;
            p.Init();
            return p;
        }

        #endregion

        #region Application and Framework management

        public void StartFramework(bool logger, bool timeline, bool useImb, bool mapplugins)
        {
            if (logger) Logger.Instance.StartWork();
            if (timeline) InitTimeline();

            var ac = Config.Get("App.AccentColor", "#FF649EC9");
            var convertFromString = ColorConverter.ConvertFromString(ac);
            AccentColor = convertFromString != null ? (Color)convertFromString : Colors.Blue;
            AccentBrush = new SolidColorBrush(AccentColor);
            if (useImb) InitImb();

            DashboardStateStates.RegisterHandler(SetDashboard);
            DashboardStateStates.DashboardActivated += Dashboards_DashboardActivated;

            MainMenuItems.CollectionChanged += MainMenuItems_CollectionChanged;

            if (!mapplugins) return;

            // TODO These plugins must be en/disabled in the config, or simply bundled or not with the assembly.
            InitializeAndStartMapToolPlugin("GeoCodingMapTool", true);
            InitializeAndStartMapToolPlugin("BagGeoCodingMapTool", true);
            InitializeAndStartMapToolPlugin("MeasureMapTool", true);
            //InitializeAndStartMapToolPlugin("DinoTool", true);
            //InitializeAndStartMapToolPlugin("RangeMapTool", true);
            InitializeAndStartMapToolPlugin("DrivingRouteTool", true);
            InitializeAndStartMapToolPlugin("WalkingRouteTool", true);
            InitializeAndStartMapToolPlugin("CameraTool", true);
            InitializeAndStartMapToolPlugin("FieldOfViewTool", true);
            //InitializeAndStartMapToolPlugin("AccessibilityMapTool", true);
            InitializeAndStartPlugin("MainMenu", true);

            if (TimelineManager != null)
            {
                EventLists.SetTime(TimelineManager.Start, TimelineManager.End);
                TimelineManager.TimeContentChanged += (e, f) => EventLists.SetTime(TimelineManager.Start, TimelineManager.End);
            }
            if (ViewDef != null && ViewDef.MapControl != null)
            {
                EventLists.SetMapExtent(ViewDef.MapControl.Extent);
                ViewDef.MapControl.ExtentChanged += (e, f) => EventLists.SetMapExtent(ViewDef.MapControl.Extent);
            }

            NotifyOfPropertyChange(() => ScreenOrientation);
        }

        void MainMenuItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null) return;
            foreach (MenuItem ni in e.NewItems)
            {
                if (ni.Commands == null || !ni.Commands.Any()) continue;
                var ni1 = ni;
                Commands.AddCommand(ni.Name, ni.Commands.ToArray(), f => ni1.ForceClick());
            }
        }

        public void RaiseAppInitialized()
        {
            var handler = AppInitialized;
            if (handler != null) handler(this, null);
        }
        public event ClosingApplicationEvent ClosingApplication;

        private void OnClosingApplication() {
            var handler = ClosingApplication;
            if (handler != null) handler(this);
        }

        public void CloseApplication()
        {
            Execute.OnUIThread(() =>
            {
                OnClosingApplication();

                IsClosing = true;

                foreach (var p in Plugins.Where(k => k.IsRunning))
                    p.Stop();

                Logger.Log("Application", "Close", "", Logger.Level.Info);
                Logger.Instance.StopWork();
                if (Imb != null && Imb.Imb != null)
                    Imb.Imb.Close();
                Application.Current.Shutdown();
            });
        }

        public void LogOff()
        {
            Config.LogOff();
            CloseApplication();
        }

        public static AppStateSettings GetInstance()
        {
            //if (instance != null) return instance;
            return instance ?? (instance = new AppStateSettings());
            //lock (Padlock)
            //{p
            //    if (instance != null) return instance;
            //    instance = new AppStateSettings {ViewDef = new MapViewDef()};
            //    instance.ViewDef.InitBaseLayerProviders();
            //    instance.StartPanels.Add(new StartPanelSettings
            //    {
            //        Orientation = StartPanelOrientation.bottom,
            //        TimelineSettings = new TimelineSettings()
            //    });
            //    instance.Dashboards = DashboardStateCollection.Load();
            //    return instance;
            //}
        }

        //private void CurrentExit(object sender, ExitEventArgs e)
        //{
        //    CloseApplication();
        //}

        #endregion

        public void AddCircularMenu(CircularMenuItem m)
        {
            var old = CircularMenus.FirstOrDefault(k => k.Id == m.Id);
            if (old == null) CircularMenus.Add(m); //CircularMenus.Remove(old);
        }

        public void RemoveCircularMenu(string id)
        {
            var cm = CircularMenus.FirstOrDefault(k => k.Id == id);
            if (cm != null) CircularMenus.Remove(cm);
        }

        public event ScriptCommandEvent ScriptCommand;

        public void TriggerScriptCommand(object sender, string command)
        {
            if (ScriptCommand != null) ScriptCommand(sender, command);
        }

        public event WaitForInteraction InteractionOccurred;

        public void InteractionStarted(object sender, string name, string command)
        {
            var handler = InteractionOccurred;
            if (handler != null) handler(sender, name, command);
        }

        public event EventHandler FullScreenFloatingElementChanged;

        private void InitTimeline() {
            var initTime    = DateTime.Now;
            TimelineManager = new TimelineManager {
                CurrentTime = initTime,
                Start       = initTime.AddHours(-8),
                End         = initTime.AddHours(8)
            };
            TimelineManager.TimelinePlayer = new NowTimelinePlayer { Timeline = TimelineManager };
            TimelineManager.TimelinePlayer.Init();
            TimelineManager.TimelinePlayer.Begin();
            TimelineManager.CalculateInterval();
            TimelineManager.FocusTime = TimelineManager.CurrentTime.AddHours(-4);
            TimelineManager.ForceTimeChanged();
            TimelineManager.Visible = Config.GetBool("Timeline.Visible", false);
            TimelineManager.FocusVisible = Config.GetBool("Timeline.FocusVisible", true);
            TimelineManager.EventsVisible = Config.GetBool("Timeline.EventsVisible", true);
        }

        /// <summary>
        /// Create an POI instance from POI type. Used when icon is dragged on map.
        /// </summary>
        /// <param name="pPoiType"></param>
        /// <param name="pPosition"></param>
        public static PoI CreatePoiByPoiType(PoI pPoiType, Position pPosition)
        {
            if (!pPoiType.Data.ContainsKey("layer")) return null;
            var poiLayer = pPoiType.Data["layer"] as dsLayer;
            if (poiLayer == null) return null;

            var newPoi = new PoI
            {
                PoiTypeId = pPoiType.ContentId,
                PoiType = pPoiType,
                UserId = AppStateSettings.Instance.Imb.Status.Name,
                Name = pPoiType.PoiId,
                Date = DateTime.Now,
                Layer = pPoiType.Layer,
                Position = pPosition
            };
            foreach (var poiLabel in pPoiType.Labels) newPoi.Labels[poiLabel.Key] = poiLabel.Value;
            foreach (var pl in pPoiType.EffectiveMetaInfo) if (pl.DefaultValue != null && !newPoi.Labels.ContainsKey(pl.Label)) newPoi.Labels[pl.Label] = pl.DefaultValue;
            newPoi.MetaInfo = null;


            newPoi.Service = poiLayer.Service;

            if (pPoiType.NEffectiveStyle.AddMode.Value == AddModes.EditFirst)
            {
                var pp = new PoiPopupViewModel { PoI = newPoi, Layer = poiLayer, Service = poiLayer.Service };


                // poiLayer.OpenPoiPopup(newPoi);
                var s = new Size(400, 650);
                var kmlpoint = new KmlPoint(newPoi.Position.Longitude, newPoi.Position.Latitude);
                var pt = AppStateSettings.Instance.ViewDef.MapPoint(kmlpoint);

                if (pt.X <= AppStateSettings.Instance.MainBorder.ActualWidth / 2)
                    pt.X += s.Width / 2 + 25;
                else
                    pt.X -= s.Width / 2 + 25;
                pt.Y = AppStateSettings.Instance.MainBorder.ActualHeight / 2;
                var fe = FloatingHelpers.CreateFloatingElement(pPoiType.Name, pt, s, pp);

                newPoi.Data["FloatingElement"] = fe;
                pp.IsNewPoi = true;
                pp.FeElement = fe;
                AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
            }
            else
            {
                if (newPoi.NEffectiveStyle.AddMode == AddModes.OpenAfter)
                {
                    newPoi.OpenOnAdd = true;
                }

                poiLayer.Service.PoIs.Add(newPoi);
                poiLayer.Service.UpdateContentList();
            }
            return newPoi;
        }

        private void InstanceDrop(object sender, DropEventArgs se)
        {
           if (se.EventArgs == null) return;
            object draggedObject = se.EventArgs.Cursor.Data;
            // Convcert screen position to lat/lon position
            var pos = se.EventArgs.Cursor.GetPosition(Instance.ViewDef.MapControl);
            var position = Instance.ViewDef.ViewToWorld(pos.X, pos.Y);
            var latlon = new Position(position.Y, position.X);
            // Is it document?
            var document = draggedObject as Document;
            if (document != null)
            {
                DocumentDroppedOnMap(document, se.Orientation, se.Pos);
                return;
            }
            // Is it PoI type?
            var poiType = draggedObject as PoI;
            if (poiType != null)
            {
                CreatePoiByPoiType(poiType, latlon);
            };

            // DataSet?
            var dataSet = draggedObject as DataSet;
            if (dataSet != null)
            {
                DataSetDroppedOnMap(dataSet, se.Pos);
            }

        }

        private void DataSetDroppedOnMap(DataSet pDataSet, Point pPoint)
        {

            var pvm = new PlotViewModel();
            if (pDataSet.Sensor != null) pvm.DisplayName = "Sensor Data";
            pDataSet.Grouping = GroupingOptions.none;
            pvm.DataSets.Add(pDataSet);
            var fe = FloatingHelpers.CreateFloatingElement(pDataSet.Title, pPoint, new Size(450, 350), pvm);
            fe.CanStream = true;
            fe.CanFullScreen = true;
            fe.AllowStream = true;
            FloatingItems.AddFloatingElement(fe);
        }


        private static void DocumentDroppedOnMap(Document pDocument, double pOrientation, Point pPoint)
        {
            var fe = new FloatingElement
            {
                Document = pDocument,
                OpacityDragging = 0.5,
                OpacityNormal = 1.0,
                CanDrag = true,
                DropTag = "document",
                CanMove = true,
                CanRotate = true,
                CanScale = true,
                StartOrientation =
                    (pDocument.OriginalRotation.HasValue)
                        ? pDocument.OriginalRotation + 90
                        : pOrientation,
                Background = Brushes.DarkOrange,
                //MaxSize = new Size(500, (500.0 / pf.Width) * pf.Height),                                 
                StartPosition = pPoint,
                StartSize = new Size(400, 400),
                Width = 500,
                Height = 500,
                ShowsActivationEffects = false,
                RemoveOnEdge = true,
                Contained = true,
                CanFullScreen = true,
                Title = pDocument.Name,
                Foreground = Brushes.White,
                DockingStyle = DockingStyles.None,
                CanStream = true,
                ShareUrl = pDocument.ShareUrl
            };
            if (!string.IsNullOrEmpty(pDocument.ShareUrl)) fe.Contracts.Add("link", pDocument.ShareUrl.Replace("^", string.Empty).Replace(" ", "%20"));
            if (!string.IsNullOrEmpty(pDocument.OriginalUrl)) fe.Contracts.Add("document", pDocument.OriginalUrl.Replace("^", string.Empty));
            Instance.FloatingItems.Add(fe);
        }

        public void StartDrop(DropEventArgs se)
        {
            InstanceDrop(this, se);
            var handle = Drop;
            if (handle != null) handle(this, se);
        }

        public void SendDocument(ImbClientStatus client, Document d)
        {
            if (client == null || client.Media == null) return;
            var m = new Media
            {
                Location = d.OriginalUrl,
                Type = d.FileType.ToString(),
                Sender = Imb.Id.ToString(CultureInfo.InvariantCulture)
            };
            client.Media.SignalString(m.ToString());

            TriggerNotification(d.FileType + " send to " + client.Name);
        }


        public void SetScreenId(int screenId)
        {
            if (Imb != null) Imb.SetScreenId(screenId);
            Config.SetLocalConfig("Screen.Id", screenId.ToString(CultureInfo.InvariantCulture));
            TriggerNotification("Screen ID is now " + screenId);
        }

        #region Visible state management

        private bool circulairMenusVisible = true;

        public bool CirculairMenusVisible
        {
            get { return circulairMenusVisible; }
            set { circulairMenusVisible = value; NotifyOfPropertyChange(() => CirculairMenusVisible); }
        }

        public bool CanMinimize { get { return Config.GetBool("App.CanMinimize", false); } set { Config.SetLocalConfig("App.CanMinimize", value.ToString()); NotifyOfPropertyChange(() => CanMinimize); } }

        public void UpdateFullScreen()
        {
            FullScreen = FullScreen; // Updates the property.
        }

        public bool AllowFullScreenChange { get { return Config.GetBool("App.AllowFullScreenChange", true); } set { Config.SetLocalConfig("App.AllowFullScreenChange", value.ToString()); NotifyOfPropertyChange(() => AllowFullScreenChange); } }

        public bool FullScreen
        {
            get
            {
                return Config.GetBool("App.FullScreen", true);
            }
            set
            {
                Config.SetBool("App.FullScreen", value);
                if (!value)
                {
                    CanMinimize = false; // Remove minimize button, too. TODO improve: remove the minimize toggle button completely.
                }

                Window mainWindow = Application.Current.MainWindow;
                mainWindow.WindowStyle = FullScreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
                if (mainWindow.WindowStyle.Equals(WindowStyle.None))
                {
                    mainWindow.WindowState = WindowState.Maximized;
                    WindowCollection windowCollection = Application.Current.Windows;
                    foreach (Window window in windowCollection)
                    {
                        if (window != mainWindow)
                        {
                            window.WindowState = WindowState.Minimized; // Hide all other windows.
                        }
                    }
                }

                NotifyOfPropertyChange(() => FullScreen);
            }
        }


        private bool bottomTabMenuVisibleState, leftTabMenuVisibleState, dockedFloatingElementsVisibleState, timelineVisibleState;
        private TimelineManager timelineManager;

        /// <summary>
        /// Store the visible state, i.e. the tab menus that are visible.
        /// </summary>
        public void StoreVisibleState()
        {
            bottomTabMenuVisibleState = bottomTabMenuVisible;
            leftTabMenuVisibleState = leftTabMenuVisible;
            dockedFloatingElementsVisibleState = dockedFloatingElementsVisible;
            timelineVisibleState = TimelineManager.Visible; // TimelineVisible is deprecated.
        }

        /// <summary>
        /// Restore the visible state.
        /// </summary>
        public void RestoreVisibleState()
        {
            BottomTabMenuVisible = bottomTabMenuVisibleState;
            LeftTabMenuVisible = leftTabMenuVisibleState;
            DockedFloatingElementsVisible = dockedFloatingElementsVisibleState;
            TimelineManager.Visible = timelineVisibleState; // TimelineVisible is deprecated.
        }

        #endregion Visible state management

        public bool ShowScaleLine
        {
            get { return instance.Config.GetBool("Map.ScaleLine", false); }
            set
            {
                if (value.Equals(instance.Config.GetBool("Map.ScaleLine", false))) return;
                instance.Config.Set("Map.ScaleLine", value.ToString());
                NotifyOfPropertyChange(() => ShowScaleLine);
            }
        }


        internal void ExpandLeftTab()
        {
            TriggerScriptCommand(this, "ExpandLeftTab");
        }
    }

    public delegate void ClosingApplicationEvent(object sender);
}