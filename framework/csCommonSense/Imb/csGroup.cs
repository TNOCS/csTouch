using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using IMB3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace csImb
{
    using csCommon.Logging;
    using System.Diagnostics;

    public class csGroup : PropertyChangedBase
    {
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(() => Name); }
        }

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private TEventEntry commandChannel;

        private BindableCollection<long> clients = new BindableCollection<long>();

        public BindableCollection<long> Clients
        {
            get { return clients; }
            set { clients = value; }
        }

        private long owner;

        public long Owner
        {
            get { return owner; }
            set { owner = value; NotifyOfPropertyChange(() => Owner); }
        }

        private ImbClientStatus ownerClient;

        public ImbClientStatus OwnerClient
        {
            get { return ownerClient; }
            set { ownerClient = value; NotifyOfPropertyChange(() => OwnerClient); }
        }

        private bool followMap = true;

        public bool FollowMap
        {
            get { return followMap; }
            set
            {
                followMap = value;
                NotifyOfPropertyChange(() => FollowMap);
            }
        }

        private BindableCollection<Guid> layers = new BindableCollection<Guid>();

        // Service GUID that are shared
        public BindableCollection<Guid> Layers
        {
            get { return layers; }
            set { layers = value; NotifyOfPropertyChange(() => Layers); }
        }

        public bool IsMemberOfGroup 
        {
            get { return IsActive; }
        }

        public bool IsActive // Use IsMemberOfGroup; backwards compatible
        {
            get { return AppState.Imb != null && Clients.Contains(AppState.Imb.Imb.ClientHandle); }
        }

        internal void FromString(string v)
        {
            // parses group definition received from IMB (see function ImbGroupDefinitionString)
            var ss = v.Split('|');
            Owner = long.Parse(ss[0]);
            OwnerClient = AppState.Imb.FindClient(Owner);

            var imbClientHandlesInGroup = ss[1].Split(';').Where(k => !string.IsNullOrEmpty(k)).Select(long.Parse).ToList();
            var layers = ss[2].Split(';').Where(k => !string.IsNullOrEmpty(k)).Select(Guid.Parse).ToList();

            // Update administration for clients belonging to group
            var removedHandles = Clients.Except(imbClientHandlesInGroup).ToList();
            var addedHandles = imbClientHandlesInGroup.Except(Clients).ToList();
            var unchangedHandles = Clients.Intersect(imbClientHandlesInGroup).ToList();
            if (removedHandles.Count > 0)
            {
                // Converts addedHandles handle id's to names
                var clientNames = removedHandles.Select(
                    handleId =>
                    {
                        var fc = AppState.Imb.FindClient(handleId);
                        return (fc != null) ? fc.Name : handleId.ToString(CultureInfo.InvariantCulture);
                    });

                Execute.OnUIThread(() => AppState.TriggerNotification(string.Format("{0} left group {1}",
                    String.Join(",", clientNames), Name), pathData: MenuHelpers.GroupIcon));
                Clients.RemoveRange(addedHandles);
            }

            if (addedHandles.Count > 0)
            {
                // Converts addedHandles handle id's to names
                var clientNames = addedHandles.Select(
                    handleId =>
                        {
                            var fc = AppState.Imb.FindClient(handleId);
                            return (fc != null) ? fc.Name : handleId.ToString(CultureInfo.InvariantCulture);
                        });
                Execute.OnUIThread(() => AppState.TriggerNotification(string.Format("{0} joined group {1}",
                    String.Join(",", clientNames), Name), pathData: MenuHelpers.GroupIcon));
                Clients.AddRange(addedHandles);
            }


            // Update shared layers.
            var removedLayers = Layers.Except(layers).ToList();
            var addedLayers = layers.Except(Layers).ToList();
            Layers.RemoveRange(removedLayers);
            Layers.AddRange(addedLayers);

            if (addedHandles.Count + removedHandles.Count > 0) // Did group client list change?
            {
                NotifyOfPropertyChange(() => FullClients);
                NotifyOfPropertyChange(() => IsActive);
                NotifyOfPropertyChange(() => IsMemberOfGroup);
            }
            if (this.Owner == this.AppState.Imb.Imb.ClientHandle)
            {
                // Owner of group
                UpdateExtent(true);
                // SetImbGroup(); 
            }
            
        }

        public override string ToString()
        {
            return ImbGroupDefinitionString();
        }

        private string ImbGroupDefinitionString()
        {
            // <IMB client ID of owner of group>|<IMB client ID's that joined group (seperated by ;)>|<Layers shared in group>
            var r = Owner + "|";
            r = Clients.Aggregate(r, (current, c) => current + (c + ";"));
            r += "|";
            return Layers.Aggregate(r, (current, l) => current + (l.ToString() + ";"));
        }

        private bool showClients;

        public bool ShowClients
        {
            get { return showClients; }
            set { showClients = value; NotifyOfPropertyChange(() => ShowClients); }
        }

        public string CommandsChannelName { get { return Name + ".group.commands"; } }

        public void InitImb()
        {
            Debug.Assert(commandChannel == null, "Already initialized");
            commandChannel = AppState.Imb.Imb.Subscribe(CommandsChannelName, true);

            commandChannel.OnNormalEvent                          += CommandChannelOnNormalEvent;
            AppState.ViewDef.MapControl.ExtentChanged             += MapControl_ExtentChanged;
            AppState.ViewDef.MapControl.PreviewTouchDown          += MapControl_PreviewTouchDown;
            AppState.ViewDef.MapControl.PreviewMouseRightButtonUp += MapControl_PreviewMouseRightButtonUp;

            Clients.Add(AppState.Imb.Imb.ClientHandle);
            UpdateGroup();

        }

        void MapControl_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (AppState.Config.GetBool("CommonSenseFramework.GeoPointerInGroupIsEnabled", true))
            {
                TriggerGeoPointer(e.GetPosition(AppState.ViewDef.MapControl));
            }
        }

        void MapControl_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            // Shows GeoPoint (yellow circles) when touching on table; also send to other members in IMB group?
            if (AppState.Config.GetBool("CommonSenseFramework.GeoPointerInGroupIsEnabled", true))
            {
                var start = DateTime.Now;
                var posStart = e.GetTouchPoint(AppState.ViewDef.MapControl).Position;
                e.TouchDevice.Deactivated += (s, f) =>
                {
                    if (!(DateTime.Now.Subtract(start).TotalMilliseconds > 1000)) return;
                    var posEnd = e.TouchDevice.GetTouchPoint(AppState.ViewDef.MapControl).Position;
                    if ((Math.Abs(posStart.X - posEnd.X) < 15) && (Math.Abs(posStart.Y - posEnd.Y) < 15))
                    {
                        TriggerGeoPointer(posStart);
                    }
                };
            }
        }

        private void TriggerGeoPointer(Point pos)
        {
            var p = AppState.ViewDef.ViewToWorld(pos.X, pos.Y);
            var wm = new WebMercator();
            var p2 = (MapPoint)wm.FromGeographic(new MapPoint(p.Y, p.X));
            AppState.ViewDef.AddGeoPointer(new GeoPointerArgs { Position = p2, Duration = TimeSpan.FromSeconds(2) });
            AppState.Imb.SendMessage(CommandsChannelName, string.Format("point|{0}|{1}|{2}",
                p2.Y.ToString(CultureInfo.InvariantCulture),
                p2.X.ToString(CultureInfo.InvariantCulture),
                AppState.Imb.Imb.ClientHandle));
        }

        private string lastMapEvent;

        void CommandChannelOnNormalEvent(TEventEntry aEvent, IMB3.ByteBuffers.TByteBuffer aPayload)
        {
            var l  = aPayload.ReadString();
            var ss = l.Split('|');
            switch (ss[0])
            {
                case "point":
                    Execute.OnUIThread(() =>
                    {
                        var xp = Convert.ToDouble(ss[1], CultureInfo.InvariantCulture);
                        var yp = Convert.ToDouble(ss[2], CultureInfo.InvariantCulture);
                        var c = AppState.Imb.FindClient(long.Parse(ss[3]));
                        if (c != null)
                        {
                            var nea = new NotificationEventArgs()
                            {
                                Duration   = TimeSpan.FromSeconds(5),
                                Options    = new List<string> { "Zoom to" },
                                PathData   = MenuHelpers.PointerIcon,
                                Header     = "Pointer triggered by " + c.Name,
                                Background = AppState.AccentBrush,
                                Foreground = System.Windows.Media.Brushes.Black,

                            };

                            nea.OptionClicked += (e, f) => AppState.ViewDef.ZoomAndPoint(new Point(yp, xp), false);

                            AppState.TriggerNotification(nea);
                        }

                        AppState.ViewDef.AddGeoPointer(new GeoPointerArgs() { Position = new MapPoint(yp, xp), Duration = TimeSpan.FromSeconds(2) });
                    });
                    break;
                case "map":
                    if (!string.Equals(l, lastMapEvent) && FollowMap)
                    {
                        lastMapEvent = l;

                        var x = Convert.ToDouble(ss[1], CultureInfo.InvariantCulture);
                        var y = Convert.ToDouble(ss[2], CultureInfo.InvariantCulture);
                        var r = Convert.ToDouble(ss[3], CultureInfo.InvariantCulture);

                        Execute.OnUIThread(delegate
                        {
                            var w = (AppState.ViewDef.MapControl.ActualWidth / 2) * r;
                            var h = (AppState.ViewDef.MapControl.ActualHeight / 2) * r;
                            var env = new Envelope(x - w, y - h, x + w, y + h);
                            AppState.ViewDef.MapControl.Extent = env;
                        });
                        skipNext = true;
                    }
                    break;
            }
        }

        private bool skipNext;

        void MapControl_ExtentChanged(object sender, ESRI.ArcGIS.Client.ExtentEventArgs e)
        {
            UpdateExtent();
        }

        private void UpdateExtent(bool force = false)
        {
            Execute.OnUIThread(() =>
            {
                if (!FollowMap) return;
                if (skipNext && !force)
                {
                    skipNext = false;
                    return;
                }
                var extent = AppState.ViewDef.MapControl.Extent.Extent;
                var center = extent.GetCenter();
                //var r = AppState.ViewDef.MapControl.Extent.Extent.XMin.ToString(CultureInfo.InvariantCulture) + "|" +
                //           AppState.ViewDef.MapControl.Extent.Extent.YMin.ToString(CultureInfo.InvariantCulture) + "|" +
                //           AppState.ViewDef.MapControl.Extent.Extent.XMax.ToString(CultureInfo.InvariantCulture) + "|" +
                //           AppState.ViewDef.MapControl.Extent.Extent.YMax.ToString(CultureInfo.InvariantCulture);

                var c = string.Format("{0}|{1}|{2}",
                    center.X.ToString(CultureInfo.InvariantCulture),
                    center.Y.ToString(CultureInfo.InvariantCulture),
                    AppState.ViewDef.MapControl.Resolution.ToString(CultureInfo.InvariantCulture));

                if (string.Equals(c, lastMapEvent) && !force) return;
                AppState.Imb.SendMessage(CommandsChannelName, "map|" + c);
                lastMapEvent = c;
            });
        }

        public void StopImb()
        {
            Debug.Assert(commandChannel != null, "Already deinitialized" );

            foreach (var l in Layers)
            {
                var s = (PoiService)AppState.DataServer.Services.FirstOrDefault(k => k.Id == l);
                if (s != null && s.Layer != null) s.Layer.Stop();
            }

            AppState.ViewDef.MapControl.ExtentChanged             -= MapControl_ExtentChanged;
            AppState.ViewDef.MapControl.PreviewMouseRightButtonUp -= MapControl_PreviewMouseRightButtonUp;
            if (commandChannel != null)
            {
                commandChannel.OnNormalEvent -= CommandChannelOnNormalEvent;
                commandChannel.UnSubscribe();
                commandChannel = null;
            }
            Clients.Remove(AppState.Imb.Imb.ClientHandle);
            UpdateGroup();  
            
        }

        /// <summary>
        /// Converts Clients handle id to client objects
        /// </summary>
        public BindableCollection<ImbClientStatus> FullClients
        {
            get
            {
                var r = new BindableCollection<ImbClientStatus>();
                foreach (var c in Clients)
                {
                    var nc = AppState.Imb.FindClient(c);
                    //Debug.Assert((nc != null), "IMB Client not found");
                    if (nc != null) r.Add(nc);
                }
                return r;
            }
        }

        private string ImbVariableNameForGroup
        {
            get
            {
                return string.Format("{0}.group", Name);
            }
        }

        /// <summary>
        /// Puts the GROUP definition on IMB bus
        /// </summary>
        private void SetImbGroup()
        {
            LogCs.LogMessage(String.Format("Broadcast on IMB bus: IMB variable '{0}' to '{1}' (group information) ", 
                ImbVariableNameForGroup, ImbGroupDefinitionString()));
            AppState.Imb.Imb.SetVariableValue(ImbVariableNameForGroup, ImbGroupDefinitionString());
            
            base.NotifyOfPropertyChange(() => IsActive);
            base.NotifyOfPropertyChange(() => IsMemberOfGroup);
        }

        public void UpdateGroup()
        {
            SetImbGroup();
        }
    }
}