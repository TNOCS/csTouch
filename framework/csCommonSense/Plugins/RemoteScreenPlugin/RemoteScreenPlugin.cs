using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csCommon.RemoteScreenPlugin;
using csShared.Controls.Popups.MenuPopup;
using csShared.Controls.SlideTab;
using csShared.TabItems;
using ESRI.ArcGIS.Client.Geometry;
using csImb;
using csRemoteScreenPlugin.Share.SendTo;
using csShared;
using csShared.Documents;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;
using IMB3;
using Action = System.Action;
using Point = SharpMap.Geometries.Point;

namespace csRemoteScreenPlugin
{
    [Export(typeof (IPlugin))]
    public class RemoteScreenPlugin : PropertyChangedBase, IPlugin
    {
        private Screenshots screenshots;
        private DateTime lastFastMapChange = DateTime.Now;
        private TEventEntry fastMapChangeChannel;
        private int fastMapInterval = 200;

        private bool _disableMapEvents; // FIXME TODO _disableMapEvents is assigned but not used.
        private bool _isRunning;
        private IPluginScreen _screen;

        public bool AutoShowMedia
        {
            get
            {
                return AppState.Config.GetBool("RemoteScreen.AutoShowMedia", true);
            }
            set
            {
                AppState.Config.SetLocalConfig("RemoteScreen.AutoShowMedia", value.ToString());
                NotifyOfPropertyChange(()=>AutoShowMedia);
            }
        }
        

        private bool _hideFromSettings;

        public bool CanStop { get { return true; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        public bool HideFromSettings
        {
            get { return _hideFromSettings; }
            set { _hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        #region IPlugin Members

        public string Name
        {
            get { return "RemoteScreenPlugin"; }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }
        private StartPanelTabItem spti;

        private long following;

        public long Following
        {
            get { return following; }
            set { following = value; NotifyOfPropertyChange(()=>Following); }
        }
        

        public void Init()
        {
            AppState.ShareContracts.Add(new SendToContract());

            if (AppState.Imb.Enabled)
            {
                AppState.Imb.ClientAdded += ImbClientAdded;
                AppState.Imb.ClientRemoved += ImbClientRemoved;
                AppState.Imb.AllClients.CollectionChanged -= AllClients_CollectionChanged;
                AppState.Imb.AllClients.CollectionChanged += AllClients_CollectionChanged;
                AppState.Imb.Imb.OnVariable += Imb_OnVariable;
                AppState.Imb.CommandReceived += Imb_CommandReceived;
                fastMapChangeChannel = AppState.Imb.Imb.Publish(AppState.Imb.Id + ".fastmapextent");
                fastMapInterval = AppState.Config.GetInt("Map.FastMapUpdateInterval", 200);
                AppState.Imb.Status.AddCapability("receivescreenshot");

                AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
                AppState.ViewDef.MapManipulationDelta += ViewDefMapManipulationDelta;


                foreach (var c in AppState.Imb.Clients) AddClient(c.Value);
                if (AppState.ViewDef.MapControl != null)
                {
                    AppState.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
                    AppState.ViewDef.MapControl.ExtentChanging += MapControlExtentChanging;
                }

                var viewModel = new ContactsViewModel() {Plugin = this};

                spti = new StartPanelTabItem
                {
                    ModelInstance = viewModel,
                    Position = StartPanelPosition.left,
                    HeaderStyle = TabHeaderStyle.Image,
                    Name = "Contacts",
                    
                    Image = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/person.png", UriKind.RelativeOrAbsolute))
                };

                //AppState.ConfigTabs.Add(new EsriMapSettingsViewModel() { DisplayName = "Map" });

            }

        }

        void AllClients_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateClientCount();
        }

        public static string ScreenOnCommand = "ScreenOn";
        public static string ScreenOffCommand = "ScreenOff";
        public static string FollowMapOnCommand = "MapOn";
        public static string FollowMapOffCommand = "MapOff";

        void Imb_CommandReceived(object sender, csImb.Command c)
        {
            Execute.OnUIThread(() =>
            {
                
           
            if (c.CommandName == FollowMapOnCommand)
            {
                var client = AppState.Imb.FindClient(c.SenderId);
                if (client != null)
                {
                    AppState.TriggerNotification(client.Name + " is now following your map position", pathData: MenuHelpers.MapIcon);
                    UpdateMapExtent();
                    client.IsFollowingMyMap = true;
                }
                
            }
            if (c.CommandName == FollowMapOffCommand)
            {
                var client = AppState.Imb.FindClient(c.SenderId);
                if (client != null)
                {
                    AppState.TriggerNotification(client.Name + " stopped following your map position", pathData: MenuHelpers.MapIcon);
                    client.IsFollowingMyMap = false;
                }
                
            }
            if (c.CommandName == ScreenOnCommand)
            {
                var client = AppState.Imb.FindClient(c.SenderId);
                if (client!=null) AppState.TriggerNotification(client.Name + " is now following your screen",pathData:MenuHelpers.ScreenIcon);
            }
            if (c.CommandName == ScreenOffCommand)
            {
                var client = AppState.Imb.FindClient(c.SenderId);
                if (client != null) AppState.TriggerNotification(client.Name + " stopped following your screen",pathData:MenuHelpers.ScreenIcon);
            }
            if (c.CommandName == "Exit")
            {
                AppState.CloseApplication();
            }
            });
        }
         
        void MapControlExtentChanged(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {
            UpdateMapExtent();
            MapControlExtentChanging(sender, e);
        }

        private void UpdateMapExtent()
        {
            if (AppState.Imb != null && AppState.Imb.IsConnected)
            {
                string r = AppState.ViewDef.MapControl.Extent.Extent.XMin.ToString(CultureInfo.InvariantCulture) + "|" +
                           AppState.ViewDef.MapControl.Extent.Extent.YMin.ToString(CultureInfo.InvariantCulture) + "|" +
                           AppState.ViewDef.MapControl.Extent.Extent.XMax.ToString(CultureInfo.InvariantCulture) + "|" +
                           AppState.ViewDef.MapControl.Extent.Extent.YMax.ToString(CultureInfo.InvariantCulture);

                AppState.Imb.SendMessage(AppState.Imb.Id + ".mapextent", r);
            }
        }


        void MapControlExtentChanging(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {

            if (AppState.Imb != null && AppState.Imb.IsConnected && fastMapChangeChannel.Subscribers && lastFastMapChange.AddMilliseconds(fastMapInterval) < DateTime.Now)
            {
                string r = AppState.ViewDef.MapControl.Extent.Extent.XMin.ToString(CultureInfo.InvariantCulture) + "|" +
                           AppState.ViewDef.MapControl.Extent.Extent.YMin.ToString(CultureInfo.InvariantCulture) + "|" +
                           AppState.ViewDef.MapControl.Extent.Extent.XMax.ToString(CultureInfo.InvariantCulture) + "|" +
                           AppState.ViewDef.MapControl.Extent.Extent.YMax.ToString(CultureInfo.InvariantCulture);

                fastMapChangeChannel.SignalString(r);
                lastFastMapChange = DateTime.Now;
            }
        }

        public void Start()
        {
            //AppState.FussConnection.MessageBroker.OnUnknownMessageReceived += MessageBroker_OnUnknownMessageReceived;
            AppState.Imb.MediaReceived += Imb_MediaReceived;
            IsRunning = true;
            AppState.Imb.Status.AddCapability(csImb.csImb.CapabilityRemoteScreen);
            if (AppState.Imb.Status.AllowFollowMap) AppState.Imb.Status.AddCapability(csImb.csImb.Capability2D);
            
            int screenId = AppState.Config.GetInt("Screen.Id", -1);
            if ( screenId != -1)
            {
                AppState.Imb.SetScreenId(screenId);                
            }

            if (spti != null && !AppState.StartPanelTabItems.Contains(spti))
            {                
                AppState.StartPanelTabItems.Add(spti);
            }
            

            ScreenOn();
        }


        public void ScreenOn()
        {
            Execute.OnUIThread(() =>
            {
                screenshots = new Screenshots { Target = Application.Current.MainWindow, Imb = AppState.Imb };
                screenshots.Start(AppState.Config.GetInt("Screenshot.Interval", 2000));
            });
        }

        public void ScreenOff()
        {
            Execute.OnUIThread(() =>
            {
                if (screenshots != null) screenshots.Stop();
            });
        }

        void Imb_MediaReceived(object sender, MediaReceivedEventArgs e)
        {
            Document d = new Document() {Location = e.Media.Location, OriginalUrl = e.Media.Location, Image = e.Media.Image};
            if (AutoShowMedia)
                AppState.FloatingItems.AddFloatingElement(FloatingHelpers.CreateFloatingElement(d));
            
            //e.Media.Sender
            var cl = AppState.Imb.FindClient(Convert.ToInt32(e.Media.Sender));
            if (cl != null)
            {
                AppState.TriggerNotification(d.FileType + " received from " + cl.Name);
            }
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Stop()
        {
            ScreenOff();
            AppState.Imb.MediaReceived -= Imb_MediaReceived;
            IsRunning = false;
            AppState.Imb.Status.RemoveCapability(csImb.csImb.CapabilityRemoteScreen);
            if (AppState.StartPanelTabItems.Contains(spti)) AppState.StartPanelTabItems.Remove(spti);
        }

        public IPluginScreen Screen
        {
            get { return _screen; }
            set
            {
                _screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/remote.png"; }
        }

        public int Priority
        {
            get { return 1000; }
        }

        #endregion

        private void Imb_OnVariable(TConnection aConnection, string aVarName, byte[] aVarValue, byte[] aPrevValue)
        {
            if (aVarName == "mapextent")
            {
                _disableMapEvents = true;
                try
                {
                    string m = Encoding.UTF8.GetString(aVarValue);
                    string[] s = m.Split('|');
                    if (s.Length > 2)
                    {
                        double lon = Convert.ToDouble(s[0], CultureInfo.InvariantCulture);
                        double lat = Convert.ToDouble(s[1], CultureInfo.InvariantCulture);
                        if (!string.IsNullOrEmpty(s[2])) Convert.ToDouble(s[2], CultureInfo.InvariantCulture);
                        AppState.ViewDef.Center = new Point(lon, lat);
                        if (s.Length > 3)
                        {
                            if (!string.IsNullOrEmpty(s[3]))
                            {
                                double r = Convert.ToDouble(s[3], CultureInfo.InvariantCulture);
                                AppState.ViewDef.Resolution = r;
                            }
                            else if (!string.IsNullOrEmpty(s[2]))
                            {
                                int l = Convert.ToInt32(s[2]);
                            }
                        }
                        else
                        {
                        }
                    }
                }
                finally
                {
                    _disableMapEvents = false;
                }
            }
        }


        private void ViewDefMapManipulationDelta(object sender, EventArgs e)
        {
            return;
            // FIXME TODO: Unreachable code
//            if (!_disableMapEvents)
//            {
//                try
//                {
//                    string r = AppState.ViewDef.Center.Y.ToString(CultureInfo.InvariantCulture) + "|" +
//                               AppState.ViewDef.Center.X.ToString(CultureInfo.InvariantCulture) + "|" +
//                               "5|" +
//                               AppState.ViewDef.Resolution.ToString(CultureInfo.InvariantCulture);
//                    // +          AppState.ViewDef.NearestLevel;
//                    AppState.Imb.Imb.SetVariableValue("mapextent", r);
//                }
//                catch
//                {
//                }
//            }
        }

        private void UpdateClientCount()
        {
            spti.TabText = AppState.Imb.Clients.Count(k => k.Value.Client).ToString();
            if (spti.TabText == "0") spti.TabText = "";
        }

        private void ImbClientRemoved(object sender, ImbClientStatus e)
        {            
            
            e.IsFollowingMyMap = false;
        }

        public byte[] GetStringToBytes(string value)
        {
            SoapHexBinary shb = SoapHexBinary.Parse(value);
            return shb.Value;
        }

        private void ImbClientAdded(object sender, ImbClientStatus e)
        {
            if (this.IsRunning)
            {
                AddClient(e);
                
            }
        }

        private void AddClient(ImbClientStatus e)
        {
            if (!e.Client) return;
            AppState.TriggerNotification(e.Name + " is now online",image:e.Image);            
        }

       


        private FloatingElement GetFloatingElement(int id)
        {
            return
                AppState.FloatingItems.Where(
                    k => k.ModelInstance is IRemoteScreen && ((IRemoteScreen) k.ModelInstance).Client.Id == id).
                    FirstOrDefault();
        }

        private TEventEntry followMap;

        public void Follow2D(long id)
        {
            var old = AppState.Imb.FindClient(Following);
            if (old != null)
            {
                old.IsFollowing = false;
                AppState.Imb.SendCommand(Following, FollowMapOffCommand);
                AppState.TriggerNotification("You stopped following the map of " + old.Name, pathData:MenuHelpers.MapIcon);
            }
            Following = id;
            if (AppState.Imb != null && AppState.Imb.IsConnected)
            {
                if (followMap != null)
                {
                    
                    followMap.ClearAllStreams();
                    followMap.OnNormalEvent -= followMap_OnNormalEvent;    
                    AppState.Imb.Imb.UnSubscribe(followMap.EventName);
                }
                if (id != 0)
                {
                    var c = AppState.Imb.FindClient(id);
                    if (c == null) return;
                    c.IsFollowing = true;
                    followMap = AppState.Imb.Imb.Subscribe(id + ".mapextent");
                    followMap.OnNormalEvent += followMap_OnNormalEvent;
                    AppState.Imb.SendCommand(id, FollowMapOnCommand);
                    AppState.TriggerNotification("You are now following the map of " + c.Name, pathData: MenuHelpers.MapIcon);
                }
                
            }

        }

        void followMap_OnNormalEvent(TEventEntry aEvent, IMB3.ByteBuffers.TByteBuffer aPayload)
        {
            var s = aPayload.ReadString();
            var ss = s.Split('|');
            double xmin = Convert.ToDouble(ss[0], CultureInfo.InvariantCulture);
            double ymin = Convert.ToDouble(ss[1], CultureInfo.InvariantCulture);
            double xmax = Convert.ToDouble(ss[2], CultureInfo.InvariantCulture);
            double ymax = Convert.ToDouble(ss[3], CultureInfo.InvariantCulture);
            Execute.OnUIThread(delegate
                                   {
                                       AppState.ViewDef.MapControl.Extent = new Envelope(xmin, ymin, xmax, ymax);    
                                   });
            
        
            
        }  


        private void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
        }

        internal void ExitClient(int p)
        {
            AppState.Imb.SendCommand(p,"Exit");
            
        }
    }
}