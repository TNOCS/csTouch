using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
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

        public BindableCollection<Guid> Layers
        {
            get { return layers; }
            set { layers = value; NotifyOfPropertyChange(() => Layers); }
        }

        public bool IsActive
        {
            get { return AppState.Imb != null && Clients.Contains(AppState.Imb.Imb.ClientHandle); }
        }

        internal void FromString(string v)
        {
            var ss = v.Split('|');
            Owner = long.Parse(ss[0]);
            OwnerClient = AppState.Imb.FindClient(Owner);
            var oldList = Clients.ToList();
            Clients.Clear();
            Clients.AddRange(ss[1].Split(';').Where(k => !string.IsNullOrEmpty(k)).Select(long.Parse));

            Layers.Clear();
            Layers.AddRange(ss[2].Split(';').Where(k => !string.IsNullOrEmpty(k)).Select(Guid.Parse));

            // show notifications
            foreach (var c in Clients)
            {
                if (oldList.Contains(c)) continue;
                var fc = AppState.Imb.FindClient(c);
                if (fc != null) Execute.OnUIThread(() => AppState.TriggerNotification(fc.Name + " joined " + Name, pathData: MenuHelpers.GroupIcon));

                if (Owner == AppState.Imb.Imb.ClientHandle) UpdateExtent(true);
            }

            foreach (var c in oldList)
            {
                if (Clients.Contains(c)) continue;
                var fc = AppState.Imb.FindClient(c);
                if (fc != null) Execute.OnUIThread(() =>
                    AppState.TriggerNotification(fc.Name + " left " + Name, pathData: MenuHelpers.GroupIcon));
            }

            if (Owner == AppState.Imb.Imb.ClientHandle) 
                UpdateGroup();
            else 
                TriggerUpdates();
        }

        public override string ToString()
        {
            var r = Owner + "|";
            r  = Clients.Aggregate(r, (current, c) => current + (c + ";"));
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
            commandChannel = AppState.Imb.Imb.Subscribe(CommandsChannelName, true);

            commandChannel.OnNormalEvent                          += CommandChannelOnNormalEvent;
            AppState.ViewDef.MapControl.ExtentChanged             += MapControl_ExtentChanged;
            AppState.ViewDef.MapControl.PreviewTouchDown          += MapControl_PreviewTouchDown;
            AppState.ViewDef.MapControl.PreviewMouseRightButtonUp += MapControl_PreviewMouseRightButtonUp;
        }

        void MapControl_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            TriggerGeoPointer(e.GetPosition(AppState.ViewDef.MapControl));
        }

        void MapControl_PreviewTouchDown(object sender, TouchEventArgs e)
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
            AppState.ViewDef.MapControl.ExtentChanged             -= MapControl_ExtentChanged;
            AppState.ViewDef.MapControl.PreviewMouseRightButtonUp -= MapControl_PreviewMouseRightButtonUp;
            commandChannel.OnNormalEvent                          += CommandChannelOnNormalEvent;

            commandChannel.UnSubscribe();
            commandChannel = null;
        }

        public void TriggerUpdates()
        {
            NotifyOfPropertyChange(() => IsActive);
            NotifyOfPropertyChange(() => FullClients);
        }

        public BindableCollection<ImbClientStatus> FullClients
        {
            get
            {
                var r = new BindableCollection<ImbClientStatus>();
                foreach (var c in Clients)
                {
                    var nc = AppState.Imb.FindClient(c);
                    if (nc != null) r.Add(nc);
                }
                return r;
            }
        }

        public void UpdateGroup()
        {
            AppState.Imb.Imb.SetVariableValue(Name + ".group", ToString());
            TriggerUpdates();
        }
    }
}