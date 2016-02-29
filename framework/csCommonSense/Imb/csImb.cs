using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Caliburn.Micro;
using csCommon.Utils.Collections;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using DataServer;
using IMB3;
using IMB3.ByteBuffers;
using Timer = System.Timers.Timer;

namespace csImb
{
    public class csImb : PropertyChangedBase
    {
        private static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        public const string CommandExit = "Exit";

        public const string CapabilityExit = "Exit";
        public const string Capability2D = "2D";
        public const string Capability3D = "3D";
        public const string CapabilityRemoteScreen = "RemoteScreen";

        private ObservableDictionary<long, ImbClientStatus> clients = new ObservableDictionary<long, ImbClientStatus>();

        private DateTime connectDate;
        private ImbClientStatus status = new ImbClientStatus();

        public csImb()
        {
            Enabled = true;
        }

        public DateTime ConnectDate
        {
            get { return connectDate; }
            set
            {
                connectDate = value;
                NotifyOfPropertyChange(() => ConnectDate);
            }
        }

        public ObservableDictionary<long, ImbClientStatus> Clients
        {
            get { return clients; }
            set { clients = value; NotifyOfPropertyChange(()=>Clients); }
        }

        private BindableCollection<ImbClientStatus> allClients = new BindableCollection<ImbClientStatus>();

        public BindableCollection<ImbClientStatus> AllClients
        {
            get { return allClients; }
            set { allClients = value; NotifyOfPropertyChange(()=>AllClients); }
        }
        

        public int Id
        {
            get
            {
                if (Imb != null && Imb.Connected) return Imb.ClientHandle;
                return 0;
            }
        }

        public ImbClientStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                NotifyOfPropertyChange(() => Status);
            }
        }

        private BindableCollection<csGroup> groups = new BindableCollection<csGroup>();

        public BindableCollection<csGroup> Groups
        {
            get { return groups; }
            set { groups = value; NotifyOfPropertyChange(()=>Groups); }
        }
        

        public TConnection Imb { get; set; }

        public bool Enabled { get; set; }

        public bool IsConnected
        {
            get { return (Imb != null && Imb.Connected); }
        }

        public List<ImbClientStatus> ScreenshotReceivingClients
        {
            get { return Clients.Where(k => k.Key != Id && k.Value.AllCapabilities.Contains("receivescreenshot")).Select(k => k.Value).ToList(); }
        }

        public event ClientChangedEventHandler ClientAdded;
        public event ClientChangedEventHandler ClientRemoved;
        public event ClientChangedEventHandler ClientChanged;
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event CommandReceivedEventHandler CommandReceived;
        public event MediaReceivedEventHandler MediaReceived;

        public void Init(string host, int port, string name, int id, string federation, string alternativeHost, int alternativePort)
        {
            Init(host, port, name, id, federation, Status, alternativeHost, alternativePort);
        }

        public void Init(string host, int port, string name, int id, string federation)
        {
            Init(host, port, name, id, federation, Status, "", 0);
        }

        public void Close()
        {
            // disconnect
            if (Imb != null) Imb.Close();
        }

        readonly Timer ot = new Timer();
        private bool reconnecting;
        private bool connecting;
        
        public void Init(string host, int port, string name, int id, string federation, ImbClientStatus clientStatus, string alternativeHost, int alternativePort)
        {
            if (connecting || AppState.IsClosing) return;
            connecting = true;
            ConnectDate = DateTime.Now;
            if (Imb != null)
            {
                Imb.OnDisconnect   -= ImbOnOnDisconnect;
                Imb.OnVariable     -= Imb_OnVariable;
                Imb.OnStatusUpdate -= Imb_OnStatusUpdate;
                ClientAdded        -= CsImbClientAdded;                
            }

            Imb = new TConnection(host, port, name, id, federation);
            Imb.OnDisconnect += ImbOnOnDisconnect;
            
            Status = clientStatus;

            if (!Imb.Connected)
            {
                // try alternative host
                // TODO EV Why was this uncommented?
                //if (alternativeHost != "") Init(alternativeHost, alternativePort, name, id, federation, clientStatus, "", 0);
                connecting = false;
                reconnecting = false;
                return;
            }

            var tries = 0;
            
            while (Imb.ClientHandle == 0 && tries<1000) {Thread.Sleep(5);
                tries += 1;
            }
            reconnecting = false;
            Imb.OnVariable += Imb_OnVariable;
            Imb.OnStatusUpdate += Imb_OnStatusUpdate;
            ot.Interval = 5000;
            ot.Elapsed += (f, b) =>
            {
                try
                {
                    if (Imb.FClient.Connected)
                    {
                        Imb.SignalString(Id + ".online", "");
                    }
                    else if (!reconnecting)
                    {
                        reconnecting = true;
                        Init(host, port, name, id, federation);
                    }
                }
                catch (Exception)
                {
                    reconnecting = true;
                    Init(host, port, name, id, federation);
                }
            };
            ot.Start();
            UpdateStatus();
            if (Status.Client)
            {
                Status.Media = Imb.Subscribe(Id + ".Media");
                Status.Commands = Imb.Subscribe(Id + ".Commands");
                //Status.Positions = Imb.Subscribe(Id + ".Position");

                Status.Media.OnNormalEvent += Media_OnNormalEvent;
                Status.Media.OnBuffer += Media_OnBuffer;
                Status.Commands.OnNormalEvent += Commands_OnNormalEvent;
                //Status.Positions.OnNormalEvent += Positions_OnNormalEvent;
            }

            ClientAdded += CsImbClientAdded;

            if (Connected != null) Connected(this, null);
            connecting = false;
        }


        private void Media_OnBuffer(TEventEntry aEvent, int aTick, int aBufferID, TByteBuffer aBuffer) {
            Execute.OnUIThread(() => {
                using (var stream = new MemoryStream(aBuffer.Buffer)) {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.EndInit();

                    var m = new Media {Image = image};
                    TriggerMediaReceived(m);
                }
            });
        }

        private void Positions_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            var d = aPayload.ReadString();
            var s = aEvent.EventName.Split('.');
            if (s.Length != 3) return;
            var cl = Clients.FirstOrDefault(k => k.Key == Int32.Parse(s[1]));
            cl.Value.UpdatePosition(d);
        }

        private void ImbOnOnDisconnect(TConnection aConnection)
        {
            if (Disconnected != null) Disconnected(this, null);
        }

        public void TriggerMediaReceived(string url, int sender)
        {
            var cs = new ImbClientStatus();
            if (sender != 0) cs = FindClient(sender);
            var m = new Media
            {
                Location = url,
                Sender = sender.ToString(CultureInfo.InvariantCulture)
            };
            if (MediaReceived!=null) MediaReceived(this, new MediaReceivedEventArgs
            {
                Media = m,
                Sender = cs
            });
        }

        public void TriggerMediaReceived(Media m)
        {
            var cs = new ImbClientStatus();
            if (MediaReceived!=null) MediaReceived(this, new MediaReceivedEventArgs
            {
                Media = m,
                Sender = cs
            });
        }

        private void Media_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            var pl = aPayload.ReadString();
            var m = Media.FromString(pl);
            if (m == null) return;
            if (MediaReceived == null) return;
            var s = FindClient(Convert.ToInt32(m.Sender));
            if (MediaReceived!=null) MediaReceived(this, new MediaReceivedEventArgs
            {
                Media = m,
                Sender = s
            });
        }

        private void CsImbClientAdded(object sender, ImbClientStatus e)
        {
            if (e.Commands != null)
                e.Commands.OnNormalEvent += Commands_OnNormalEvent;
        }

        private void Imb_OnStatusUpdate(TConnection aConnection, string aModelUniqueClientID, string aModelName, int aProgress, int aStatus)
        {
            if (aStatus != 0 && Disconnected != null) Disconnected(this, null);
            UpdateStatus();
        }

        private void Commands_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            var c = Command.FromString(aPayload.ReadString());
            if (CommandReceived != null) CommandReceived(this, c);
        }

        public Dictionary<string, string> Variables = new Dictionary<string, string>(); 

        private void Imb_OnVariable(TConnection aConnection, string aVarName, byte[] aVarValue, byte[] aPrevValue)
        {
            var value = Encoding.UTF8.GetString(aVarValue, 0, aVarValue.Length);
            Variables[aVarName] = value;
            if (aVarName.EndsWith(".status"))
            {
                ParseClient(aVarName, value);
            }
            if (aVarName.EndsWith(".group"))
            {
                ParseGroup(aVarName,value);
            }
        }

        #region groups

        public void CreateGroup(csGroup group)
        {
            if (Imb == null) return;
            Status.Id = Imb.ClientHandle;
            if (group.Owner == 0) {
                group.Owner = AppState.Imb.Imb.ClientHandle;
                group.OwnerClient = AppState.Imb.Status;
            }
            Groups.Add(group);
            group.UpdateGroup();
            JoinGroup(group);
        }

        public void JoinGroup(csGroup ng) {
            if (ng == null) return;
            foreach (var g in Groups.Where(k => k.IsActive && k != ng)) {
                LeaveGroup(g);
            }
            ActiveGroup = ng;
            if (ng.IsActive) return;
            AppState.TriggerNotification("You joined " + ng.Name, pathData: MenuHelpers.GroupIcon);
            ng.Clients.Add(AppState.Imb.Imb.ClientHandle);
            ng.UpdateGroup();
            ng.InitImb();
        }

        private csGroup activeGroup;

        public csGroup ActiveGroup
        {
            get {
                if (activeGroup != null) return activeGroup;
                if (Groups != null && Groups.Count > 0)
                    activeGroup = Groups.FirstOrDefault(g => g.IsActive);
                return activeGroup;
            }
            set { activeGroup = value; NotifyOfPropertyChange(()=>ActiveGroup); }
        }
            
        public void LeaveGroup(csGroup g)
        {
            if (g == null) return;
            if (g.IsActive)
            {
                foreach (var l in g.Layers)
                {
                    var s = (PoiService)AppState.DataServer.Services.FirstOrDefault(k => k.Id == l);
                    if (s != null && s.Layer !=null) s.Layer.Stop();
                }
                ActiveGroup = null;
                AppState.TriggerNotification("You left " + g.Name, pathData: MenuHelpers.GroupIcon);
                g.Clients.Remove(AppState.Imb.Imb.ClientHandle);
                g.UpdateGroup();               
                g.StopImb();
            };      
        }

        /// <summary>
        /// Only the owner can delete a group.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool CanDeleteGroup(csGroup group) {
            return group != null
                   && Groups.Contains(@group)
                   && AppState.Imb != null
                   && AppState.Imb.Imb != null
                   && AppState.Imb.Imb.ClientHandle == group.Owner;
        }

        internal void DeleteGroup(csGroup group) {
            if (!CanDeleteGroup(group)) return;
            Execute.OnUIThread(() =>
            {
                Groups.Remove(@group);
                Imb.SetVariableValue(@group.Name + ".group", "");
                AppState.TriggerNotification(@group.Name + " has been deleted", pathData: MenuHelpers.GroupIcon);
            });            
        }

        #endregion

        private void ParseClient(string aVarName, string value)
        {
            var id = Int32.Parse(aVarName.Split('.')[0]);

            if (id == Id) return;

            if (value == "")
            {
                if (!clients.ContainsKey(id)) return;
                if (clients[id].Client)
                {
                    clients[id].Media.UnPublish();
                    clients[id].Commands.UnPublish();
                }
                Execute.OnUIThread(() =>
                {
                    OnClientRemoved(id);
                    Clients.Remove(id);
                    AllClients.Remove(AllClients.FirstOrDefault(k => k.Id == id));
                        
                });

                // First call OnClientRemoved before you remove the client. Otherwise you will generate an exception.
            }
            else
            {
                if (clients.ContainsKey(id))
                {
                    clients[id].FromString(value);
                    OnClientChanged(clients[id]);
                }
                else
                {
                    var st = new ImbClientStatus();
                    st.FromString(value);
                    if (!string.Equals(st.Application, Status.Application, StringComparison.CurrentCultureIgnoreCase) 
                        && !string.Equals(st.Application, "*")
                        && !string.Equals(Status.Application, "*")) return;
                    if (st.Client)
                    {
                        st.Media = Imb.Publish(st.Id + ".Media");
                        st.Commands = Imb.Publish(st.Id + ".Commands");
                        //st.Positions = Imb.Subscribe(st.Id + ".Position");
                        //st.Positions.OnNormalEvent += Positions_OnNormalEvent;
                    }
                    Execute.OnUIThread(() =>
                    {
                        if (!clients.ContainsKey(id))
                        {
                            Clients.Add(id, st);
                            AllClients.Add(st);
                            OnClientAdded(st);
                        }
                        else
                        {
                            AllClients.Remove(AllClients.FirstOrDefault(k => k.Id == id));
                            Clients[id] = st;
                        }
                    });
                }
            }
        }

        private void ParseGroup(string aVarName, string value) {
            var id = aVarName.Split('.')[0];

            var group = groups.FirstOrDefault(k => Equals(k.Name, id)); // Equals made static.

            if (string.IsNullOrEmpty(value)) {
                if (CanDeleteGroup(group)) {
                    Execute.OnUIThread(() => {
                        Groups.Remove(group);
                        AppState.TriggerNotification(group.Name + " has been deleted", pathData: MenuHelpers.GroupIcon);
                    });
                    // First call OnClientRemoved before you remove the client. Otherwise you will generate an exception.
                }
            }
            else {
                if (group != null) {
                    group.FromString(value);
                }
                else {
                    var st = new csGroup {Name = id};
                    st.FromString(value);
                    Groups.Add(st);
                    var s = id + " group was created";
                    if (st.OwnerClient != null) s += " by " + st.OwnerClient.Name;
                    Execute.OnUIThread(() => AppState.TriggerNotification(s, pathData: MenuHelpers.GroupIcon));
                }
            }
        }
        
        private void OnClientChanged(ImbClientStatus st)
        {
            var handler = ClientChanged;
            if (handler != null) handler(this, st);
        }

        private void OnClientRemoved(int id)
        {
            var handler = ClientRemoved;
            if (handler != null) handler(this, clients[id]);
        }

        private void OnClientAdded(ImbClientStatus st)
        {
            var handler = ClientAdded;
            if (handler != null) handler(this, st);
        }

        //public void UpdateLocation(double lat, double lon, double precision, double course, double speed)
        //{
        //    if (IsConnected && Imb != null)
        //    {
        //        SendMessage(Id + ".Position", lat.ToString(CultureInfo.InvariantCulture) + "|" + lon.ToString(CultureInfo.InvariantCulture) + "|" + precision.ToString(CultureInfo.InvariantCulture) + "|" + course.ToString(CultureInfo.InvariantCulture) + "|" + speed.ToString(CultureInfo.InvariantCulture));
        //    }
        //}

        public void SendCommand(long id, string name)
        {
            var c = new Command
            {
                CommandName = name,
                DateTime = DateTime.Now,
                SenderId = Id,
                SenderName = Imb.ClientHandle.ToString(CultureInfo.InvariantCulture)
            };
            SendMessage(id + ".Commands", c.ToString());
        }

        public void SendCommand(int id, string name, string data)
        {
            var c = new Command
            {
                CommandName = name,
                DateTime = DateTime.Now,
                Data = data,
                SenderId = Id,
                SenderName = Imb.ClientHandle.ToString(CultureInfo.InvariantCulture)
            };
            SendMessage(id + ".Commands", c.ToString());
        }


        public void UpdateStatus()
        {
            if (Imb == null) return;
            Status.Id = Imb.ClientHandle;
            Imb.SetVariableValue(Status.Id + ".status", Status.ToString());
        }

        public void SendMessage(string channel, string message)
        {
            if (IsConnected) Imb.SignalString(channel, message);
        }


        public ImbClientStatus FindClient(long id)
        {
            if (id == Imb.ClientHandle) return Status; 
            return Clients.ContainsKey(id)
                       ? Clients[id]
                       : null;
        }

        public void SendImage(ImbClientStatus client, string file)
        {
            if (File.Exists(file))
            {
                BitmapSource bs = new BitmapImage(new Uri(file,UriKind.RelativeOrAbsolute));
                SendImage(client,bs);
            }
        }

        public void SendImage(int screenId, string file)
        {
            foreach (var c in Clients.Values)
            {
                if (c.AllCapabilities.Any(k => k == "Screen:" + screenId)) SendImage(c,file);                
            }
        }

        public void SendImage(ImbClientStatus client, BitmapSource image)
        {
            if (image == null || client == null) return;
            var imgEncoder = new PngBitmapEncoder();
            imgEncoder.Frames.Add(BitmapFrame.Create(image));
            MemoryStream ms = new MemoryStream();
            imgEncoder.Save(ms);
            client.Media.SignalBuffer(0, ms.GetBuffer());      

        }

        public void SendElementImage(ImbClientStatus client, FrameworkElement element)
        {
            if (element != null)
            {
                var imgEncoder = new PngBitmapEncoder();
                RenderTargetBitmap bmpSource = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bmpSource.Render(element);
                imgEncoder.Frames.Add(BitmapFrame.Create(bmpSource));
                MemoryStream ms = new MemoryStream();
                imgEncoder.Save(ms);               
                client.Media.SignalBuffer(0, ms.GetBuffer());                
            }
        }

        public void StopStream(Guid id)
        {
        }

        public void SetAction(string action)
        {
            Status.Action = action;
            UpdateStatus();
        }

        public void TriggerCommand(string name, string data)
        {
            var c = new Command
            {
                CommandName = name,
                Data = data
            };
            if (CommandReceived != null) CommandReceived(this, c);
        }

        #region XML Message Handling infrastructure

        #region Delegates

        public delegate void IncomingEventBufferHandler(string channelName, byte[] pBuffer);

        public delegate void IncomingEventObjectHandler(string channelName, object pObject);

        public delegate void OnImbMessageReceived(string channelName, object message);

        #endregion

        private readonly Dictionary<string, List<OnImbMessageReceived>> channelCallbacks = new Dictionary<string, List<OnImbMessageReceived>>();

        /// <summary>
        ///   Map to channel specific serializers. User to translate object into buffer and vice versa.
        /// </summary>
        private readonly Dictionary<string, XmlSerializer> channelSerializers = new Dictionary<string, XmlSerializer>();

        private string bufferAsString;
        public event IncomingEventObjectHandler IncomingEventObject;
        public event IncomingEventBufferHandler IncomingEventBuffer;

        /// <summary>
        ///   Specify that publishing on the specified event will start.
        ///   The specified serializer will be used to convert the specified object into the IMB byte buffer.
        /// </summary>
        /// <param name="channelName"> The name of the event. </param>
        /// <param name="pSerializer"> The serializer to apply in case the specfied event message is sent. </param>
        public void Publish(string channelName, XmlSerializer pSerializer)
        {
            if (!IsConnected) return;
            Imb.Publish(channelName);

            // Add to map of known transmission serializers.
            if (!channelSerializers.ContainsKey(channelName)) channelSerializers.Add(channelName, pSerializer);
        }

        /// <summary>
        ///   Subscribes on updates on the specified event.
        ///   The specified serializer will be used to convert the incoming IMB byte buffer into an actual object.
        /// </summary>
        /// <param name="channelName"> The name of the channel. </param>
        /// <param name="serializer"> The serializer to apply in case the specfied event message is received. </param>
        /// <param name="callback"> The callback. </param>
        public void Subscribe(string channelName, XmlSerializer serializer, OnImbMessageReceived callback)
        {
            if (!IsConnected) return;
            var subscribedEvent = Imb.Subscribe(channelName);

            if (!channelSerializers.ContainsKey(channelName)) channelSerializers.Add(channelName, serializer);
            if (!channelCallbacks.ContainsKey(channelName))
                channelCallbacks.Add(channelName, new List<OnImbMessageReceived>
                {
                   callback
                });
            else channelCallbacks[channelName].Add(callback);

            subscribedEvent.OnBuffer += (aEvent, aTick, aBufferID, aBuffer) => OnImbBuffer(channelName, aBuffer.Buffer);
        }

        /// <summary>
        ///   Unsubscribes from updates on the specified event.
        /// </summary>
        /// <param name="channelName"> The name of the channel. </param>
        public void Unsubscribe(string channelName)
        {
            if (!IsConnected) return;
            Imb.UnSubscribe(channelName);

            // Remove from map of known reception serializers.
            if (channelSerializers.ContainsKey(channelName)) channelSerializers.Remove(channelName);
            if (channelCallbacks.ContainsKey(channelName)) channelCallbacks.Remove(channelName);
        }

        /// <summary>
        ///   Publishes the object on the channel.
        /// </summary>
        /// <param name="channelName"> The name of the channel. </param>
        /// <param name="objectToPublish"> The object to publish. </param>
        public void PublishBuffer(string channelName, object objectToPublish)
        {
            if (!IsConnected) return;
            XmlSerializer serializer;
            if (channelSerializers.TryGetValue(channelName, out serializer))
                try
                {
                    var stringWriter = new StringWriter();
                    serializer.Serialize(stringWriter, objectToPublish);

                    var stringBuilder = stringWriter.GetStringBuilder();
                    if (stringBuilder == null || stringBuilder.Length <= 0) return;
                    var stringAsBuffer = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                    Imb.SignalBuffer(channelName, 0, stringAsBuffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            else Console.WriteLine("No serializer available to convert object. Channel name: {0}", channelName);
        }

        /// <summary>
        ///   Called when an imb data buffer has been received.
        /// </summary>
        /// <param name="channelName"> The name of the event describing the buffer contents. </param>
        /// <param name="buffer"> The buffer data. </param>
        private void OnImbBuffer(string channelName, byte[] buffer)
        {
            var rxObject = Event2Object(channelName, buffer);
            if (rxObject != null)
            {
                List<OnImbMessageReceived> callbacks;
                if (channelCallbacks.TryGetValue(channelName, out callbacks))
                {
                    foreach (var callback in callbacks) callback(channelName, rxObject);
                    return;
                }
                OnIncomingEventObject(channelName, rxObject);
            }
            else OnIncomingEventBuffer(channelName, buffer);
        }

        private void OnIncomingEventBuffer(string channelName, byte[] buffer)
        {
            var handler = IncomingEventBuffer;
            if (handler != null) handler(channelName, buffer);
        }

        private void OnIncomingEventObject(string channelName, object rxObject)
        {
            var handler = IncomingEventObject;
            if (handler != null) handler(channelName, rxObject);
        }

        /// <summary>
        ///   Convert a byte buffer to an object instance depending on event name.
        /// </summary>
        /// <param name="channelName"> The name of the event. </param>
        /// <param name="buffer"> The byte buffer. </param>
        /// <returns> The object instance or null if not found or on error. </returns>
        private object Event2Object(string channelName, byte[] buffer)
        {
            XmlSerializer serializer;
            if (channelSerializers.TryGetValue(channelName, out serializer))
                try
                {
                    bufferAsString = Encoding.UTF8.GetString(buffer);
                    var reader = new StringReader(bufferAsString);
                    return serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            return null;
        }

        #endregion XML Message Handling infrastructure

        public void SetScreenId(int screenId)
        {
            var c = Status.AllCapabilities.FirstOrDefault(k => k.StartsWith("Screen:"));
            if (c != null) Status.AllCapabilities.Remove(c);
            Status.AddCapability("Screen:" + screenId);            
            UpdateStatus();
        }



        
    }
}