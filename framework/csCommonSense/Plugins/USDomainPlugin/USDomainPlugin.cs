using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using IMB3;
using IMB3.ByteBuffers;

using SessionsEvents;

using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System.Windows.Media.Imaging;
using System;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation;
using csShared.Controls.SlideTab;
using csShared.FloatingElements;
using csShared.TabItems;
using System.Timers;

namespace csUSDomainPlugin
{
    [Export(typeof(IPlugin))]
    public class USDomainPlugin : PropertyChangedBase, IPlugin
    {
        #region IPlugin Members

        public bool CanStop { get { return true; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }
        private IPluginScreen _screen;

        public IPluginScreen Screen
        {
            get { return _screen; }
            set { _screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool _hideFromSettings;

        public bool HideFromSettings
        {
            get { return _hideFromSettings; }
            set { _hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 3; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Plugins/USDomainPlugin/icons/usdomains.png"; }           
        }

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }


        public string Name
        {
            get { return "USDomain"; }
        }

        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
                AppState.AddStartPanelTabItem(spti);
            else
                AppState.RemoveStartPanelTabItem(spti);
        }

        private StartPanelTabItem spti;

        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
        }

        public void Pause()
        {
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateVisibility();

            // clear all domains and legend
            CurrentDomain = null;
            if (DomainView!=null)
                DomainView.UnselectDomain();
            UpdateLegend();
            ClearImageLayers();
        }


        #endregion



        // IMB defaults
        
        public const string DefaultIMBRemoteHost = "localhost";
        public const Int32 DefaultIMBRemotePort = 4000;
        public const string DefaultIMBFederation = "USidle";
        
        public const string IMBOwnerName = "USSurfaceClient";
        public const Int32 IMBOwnerID = 91;
        
        FloatingElement configView = null;

        public void Init()
        {
            // plugin requires pallette adjustment for LibraryBar colors

            // 0xFF, 0xB0, 0xC8, 0xD2
            // normal border brush #8C8E94
            // TabItemHotBorderBrush #3C7FB1
            // 0xFF, 0x3C, 0x7F, 0xB1 iets te donker
            // 0xFF, 0xA7, 0xD9, 0xF5 te licht

            /*
            Color LibraryBarColor = ((SolidColorBrush)AppState.AccentBrush).Color; 
            SurfacePalette myPalette = new Microsoft.Surface.Presentation.SurfacePalette
            {
                LibraryControlScrollAreaBackgroundColor = LibraryBarColor
            };
            SurfaceColors.SetDefaultApplicationPalette(myPalette);
            */

            // try to connect IMB
            IMBOpen(
                AppStateSettings.Instance.Config.Get("IMB.Host", DefaultIMBRemoteHost), 
                Convert.ToInt32(AppStateSettings.Instance.Config.Get("IMB.Port", DefaultIMBRemotePort.ToString())),
                AppStateSettings.Instance.Config.Get("IMB.IdleFederation", DefaultIMBFederation));

            // load USDomain and USConfig
            var fViewModel = IoC.GetInstance(typeof(IUSDomain), "");
            if (fViewModel != null)
            {
                // set plugin for reference in view model
                ((USDomainViewModel)fViewModel).Plugin = this;
                Element = FloatingHelpers.CreateFloatingElement("Urban Strategy", DockingStyles.Right, fViewModel, Icon,Priority);
                Element.StartSize = new Size(Element.StartSize.Value.Width, Element.StartSize.Value.Width*0.5);
                Element.SwitchWidth = 550;
                var configModel = IoC.GetInstance(typeof(IUSConfig), "");
                ((USConfigViewModel)configModel).Plugin = this;
                Element.ModelInstanceBack = configModel;
                // set plugin for reference in config view model
                Element.CanFlip = true;
                

                spti = new StartPanelTabItem
                {
                    ModelInstance = fViewModel,
                    Position = StartPanelPosition.left,
                    HeaderStyle = TabHeaderStyle.Image,
                    Name = "Urban Strategy",
                    Image = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Plugins/USDomainPlugin/icons/usdomains.png", UriKind.RelativeOrAbsolute))
                };
            }
            AppState.ViewDef.MapControl.ExtentChanging += new EventHandler<ExtentEventArgs>(MapControl_ExtentChanging);
            AppState.ViewDef.MapControl.ExtentChanged += new EventHandler<ExtentEventArgs>(MapControl_ExtentChanged);

            AppState.ViewDef.MapControl.MouseClick += MapControl_MouseClick;
            AppState.ViewDef.MapControl.MapGesture += MapControl_MapGesture;
            //AppState.ViewDef.MapControl.TouchUp += MapControl_TouchUp;
            //AppState.ViewDef.MapControl.TouchDown += MapControl_TouchDown;
            AppState.TimelineManager.PropertyChanged += TimelineManager_PropertyChanged;
            
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
            Application.Current.Exit += new ExitEventHandler(HandleClose);
            //AppState.TagVisualizer.Definitions.Add(new TagVisualizationDefinition());

            var configMenuItem = new csShared.MenuItem();
            configMenuItem.Clicked += delegate
            { 
                if (configView == null)
                {
                    USConfigViewModel configViewModel = (USConfigViewModel)IoC.GetInstance(typeof(IUSConfig), "");
                    configViewModel.Plugin = this;
                    configView = FloatingHelpers.CreateFloatingElement("Urban Strategy config", DockingStyles.None, configViewModel, Icon, Priority);
                    configView.Width = 450;
                    configView.StartSize = new Size(450, 250);
                }
                if (!AppState.FloatingItems.Contains(configView))
                    AppState.FloatingItems.AddFloatingElement(configView);
                else
                    AppState.FloatingItems.RemoveFloatingElement(configView);
                //UpdateConfig();
            };
            configMenuItem.Name = "Config\nUS";
            AppState.MainMenuItems.Add(configMenuItem);
        }

        private void MapControl_MapGesture(object sender, Map.MapGestureEventArgs e)
        {
            if (e.Gesture == GestureType.Tap)
            {
                SignalSelectID(e.MapPoint.X, e.MapPoint.Y);
                e.Handled = true;
            }
        }

        bool SignalSelectID(double x, double y)
        {
            if (domainsEvent != null)
            {
                GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");

                TByteBuffer Payload = new TByteBuffer();
                Int32 command = Sessions.scSelectID;
                Payload.Prepare(command);
                Payload.Prepare(x);
                Payload.Prepare(y);
                for (int c = gl.ChildLayers.Count - 1; c >= 0; c--)
                {
                    ElementLayer wi = gl.ChildLayers[c] as ElementLayer;
                    if (wi.Visible)
                        Payload.Prepare(domains[wi.ID].eventName);
                }
                Payload.PrepareApply();
                Payload.QWrite(command);
                Payload.QWrite(x);
                Payload.QWrite(y);
                for (int c = gl.ChildLayers.Count - 1; c >= 0; c--)
                {
                    ElementLayer wi = gl.ChildLayers[c] as ElementLayer;
                    if (wi.Visible)
                        Payload.QWrite(domains[wi.ID].eventName);
                }
                domainsEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
                return true;
            }
            else
                return false;
        }

        private Int32 fLastHour = -1;

        bool SignalSelectTime()
        {
            if (domainsEvent != null)
            {
                GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");

                TByteBuffer Payload = new TByteBuffer();
                if (fSelectTimeHour != fLastHour)
                {
                    Int32 command = Sessions.scSelectTime;
                    Payload.Prepare(command);
                    Payload.Prepare(fSelectTimeHour);
                    for (int c = gl.ChildLayers.Count - 1; c >= 0; c--)
                    {
                        ElementLayer wi = gl.ChildLayers[c] as ElementLayer;
                        if (wi.Visible)
                            Payload.Prepare(domains[wi.ID].eventName);
                    }
                    Payload.PrepareApply();
                    Payload.QWrite(command);
                    Payload.QWrite(fSelectTimeHour);
                    for (int c = gl.ChildLayers.Count - 1; c >= 0; c--)
                    {
                        ElementLayer wi = gl.ChildLayers[c] as ElementLayer;
                        if (wi.Visible)
                            Payload.QWrite(domains[wi.ID].eventName);
                    }
                    domainsEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
                    fLastHour = fSelectTimeHour;
                    return true;
                }
                else
                    return true;
            }
            else
                return false;
        }

        Int32 fSelectTimeHour;
        Timer fSelectTimeTimer=null;

        void TimelineManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.CompareTo("CurrentTime")==0)
            {
            }
            else if (e.PropertyName.CompareTo("Start") == 0)
            {
            }
            else if (e.PropertyName.CompareTo("End") == 0)
            {
            }
            else if (e.PropertyName.CompareTo("FocusTime") == 0)
            {
                fSelectTimeHour = (AppState.TimelineManager.FocusTime.DayOfYear - 1) * 24 + AppState.TimelineManager.FocusTime.Hour;
                //SignalSelectTime(fSelectTimeHour, Sessions.scPreSelectTime);
                if (fSelectTimeTimer == null)
                {
                    fSelectTimeTimer = new Timer(1000);
                    fSelectTimeTimer.AutoReset = false;
                    fSelectTimeTimer.Elapsed += fSelectTimeTimer_Elapsed;
                    fSelectTimeTimer.Enabled = true;
                }
                else
                {
                    fSelectTimeTimer.Stop();
                    fSelectTimeTimer.Start();
                }
            }
            else
            {
            }
        }

        void fSelectTimeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DomainView.Dispatcher.BeginInvoke(new TSignalEvent(SignalSelectTime), null);
            //SignalSelectTime();
            fSelectTimeTimer.Stop();
        }
        
        void MapControl_MouseClick(object sender, Map.MouseEventArgs e)
        {
            SignalSelectID(e.MapPoint.X, e.MapPoint.Y);
            e.Handled = true;
        }

        void MapControl_ExtentChanged(object sender, ExtentEventArgs e)
        {
            // for mouse events
            MapControl_ExtentChanging(sender, e);
        }

        void MapControl_ExtentChanging(object sender, ExtentEventArgs e)
        {
            // for touch events
            if (Connection != null && Connection.Connected && CurrentSession != null && Follow)
            {
                TByteBuffer Payload = new TByteBuffer();
                Payload.Prepare(AppState.ViewDef.MapControl.Extent.XMin);
                Payload.Prepare(AppState.ViewDef.MapControl.Extent.YMin);
                Payload.Prepare(AppState.ViewDef.MapControl.Extent.XMax);
                Payload.Prepare(AppState.ViewDef.MapControl.Extent.YMax);
                Payload.PrepareApply();
                Payload.QWrite(AppState.ViewDef.MapControl.Extent.XMin);
                Payload.QWrite(AppState.ViewDef.MapControl.Extent.YMin);
                Payload.QWrite(AppState.ViewDef.MapControl.Extent.XMax);
                Payload.QWrite(AppState.ViewDef.MapControl.Extent.YMax);
                Connection.SignalEvent(CurrentSession.eventName + ".VisibleExtent", TEventEntry.TEventKind.ekNormalEvent, Payload, false);
            }
        }

        public void ZoomMap(double xMin, double yMin, double xMax, double yMax)
        {
            var envelope = new Envelope(xMin, yMin, xMax, yMax);
            AppState.ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 0, 500);
            AppState.ViewDef.MapControl.ZoomTo(envelope);
        }

        public double DomainImageOpacity
        {
            get 
            {
                if (CurrentDomain != null)
                {
                    GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
                    ElementLayer wi = gl.ChildLayers[CurrentDomain.domain] as ElementLayer;
                    if (wi != null)
                        return wi.Opacity;
                    else
                        return 0.7;
                }
                else
                    return 0.7;
            }
            set
            {
                if (CurrentDomain != null)
                {
                    GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
                    ElementLayer wi = gl.ChildLayers[CurrentDomain.domain] as ElementLayer;
                    if (wi != null)
                        wi.Opacity = value;
                }
            }
        }

        //public void AddOrSetGraphicLayer(double xMin, double yMin, double xMax, double yMax, /*DynamicPolygons, */double aOpacity, string aDomain)
        /*
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi == null)
            {
                domainImage = new Image { IsHitTestVisible = false, Stretch = Stretch.Fill };
                domainImage.Source = aSource;

                var envelope = new Envelope(xMin, yMin, xMax, yMax);
                ElementLayer.SetEnvelope(domainImage, envelope);

                wi = new ElementLayer() { ID = aDomain };
                wi.Children.Add(domainImage);
                wi.Initialize();
                wi.Opacity = aOpacity;
                wi.Visible = true;

                gl.ChildLayers.Add(wi);

            }
            else
            {
                if (wi.Children.Count > 0)
                    domainImage = wi.Children[0] as Image;
                else
                    domainImage = new Image { IsHitTestVisible = false, Stretch = Stretch.Fill };
                domainImage.Source = aSource;

                var envelope = new Envelope(xMin, yMin, xMax, yMax);
                ElementLayer.SetEnvelope(domainImage, envelope);

                wi.FullExtent.XMin = xMin;
                wi.FullExtent.XMax = xMax;
                wi.FullExtent.YMin = yMin;
                wi.FullExtent.YMax = yMax;

                wi.Opacity = aOpacity;
                wi.Visible = true;
            }
        }
        */

        public void AddOrSetImageLayer(double xMin, double yMin, double xMax, double yMax, BitmapSource aSource, double aOpacity, string aDomain)
        {
            Image domainImage;
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi == null)
            {
                domainImage = new Image { IsHitTestVisible = false, Stretch = Stretch.Fill };
                domainImage.Source = aSource;

                var envelope = new Envelope(xMin, yMin, xMax, yMax);
                ElementLayer.SetEnvelope(domainImage, envelope);

                wi = new ElementLayer() { ID = aDomain };
                wi.Children.Add(domainImage);
                wi.Initialize();
                wi.Opacity = aOpacity;
                wi.Visible = true;

                gl.ChildLayers.Add(wi);
            }
            else
            {
                if (wi.Children.Count > 0)
                    domainImage = wi.Children[0] as Image;
                else
                    domainImage = new Image { IsHitTestVisible = false, Stretch = Stretch.Fill };
                domainImage.Source = aSource;

                var envelope = new Envelope(xMin, yMin, xMax, yMax);
                ElementLayer.SetEnvelope(domainImage, envelope);

                wi.FullExtent.XMin = xMin;
                wi.FullExtent.XMax = xMax;
                wi.FullExtent.YMin = yMin;
                wi.FullExtent.YMax = yMax;

                wi.Opacity = aOpacity;
                wi.Visible = true;
            }
        }

        void ClearImageLayers()
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            if (gl != null)
                gl.ChildLayers.Clear();
        }

        void ToggleVisibilityImageLayer(string aDomain)
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi != null)
                wi.Visible = !wi.Visible;
        }

        void SetVisibilityImageLayer(string aDomain, bool aVisible)
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi != null)
                wi.Visible = aVisible;
        }

        bool GetVisibilityImageLayer(string aDomain)
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi != null)
                return wi.Visible;
            else
                return false;
        }

        public void FocusLayer()
        {
            if (CurrentDomain != null)
            {
                SessionExtent zoomExtent = CurrentDomain.extent.Inflate(1.5);
                ZoomMap(zoomExtent.xMin, zoomExtent.yMin, zoomExtent.xMax, zoomExtent.yMax);
            }
        }

        public bool FocusLayer(string aDomain, double aOpacity)
        {
            bool res = false;
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi == null)
            {
                SelectDomain(aDomain, aOpacity);
                res = true;
                // todo: mark selected entry in list box as selected
            }
            // retry after trying to selecting domain layer
            wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi != null)
            {
                SessionExtent zoomExtent = domains[aDomain].extent.Inflate(1.5);
                ZoomMap(zoomExtent.xMin, zoomExtent.yMin, zoomExtent.xMax, zoomExtent.yMax);
            }
            return res;
        }

        // imb
        public delegate void TUpdateSessionList();
        public delegate void TUpdateDomainList();
        public delegate void TUpdateConnectionStatus();
        public delegate bool TSignalEvent();

        public TConnection Connection { get; set; }
        public TEventEntry sessionsEvent;
        public TEventEntry privateEvent;

        private TEventEntry domainsEvent;

        public string SelectSession(int aSelectedSessionID)
        {
            string EventName;
            string Description;
            lock (sessions)
            {
                // find event name with session
                try
                {
                    CurrentSession = sessions[aSelectedSessionID];
                    EventName = CurrentSession.eventName;
                    Description = CurrentSession.description;
                    SessionExtent zoomExtent = CurrentSession.extent.Inflate(2);
                    ZoomMap(zoomExtent.xMin, zoomExtent.yMin, zoomExtent.xMax, zoomExtent.yMax);
                }
                catch
                {
                    CurrentSession = null;
                    EventName = "";
                    Description = "";
                }
            }
            ClearAllLayers();
            if (domainsEvent != null)
            {
                domainsEvent.UnSubscribe();
                domainsEvent.OnNormalEvent -= sessionsEvent_OnNormalEvent;
                domainsEvent = null;
            }   
            if (EventName != "")
            {
                domainsEvent = Connection.Subscribe(EventName + "." + Sessions.DomainsEventNamePostFix, false);
                domainsEvent.OnNormalEvent += sessionsEvent_OnNormalEvent;
                Sessions.SignalRequestDomains(domainsEvent.EventName, privateEvent);
            }
            if (Element != null)
                Element.Title = "Urban Strategy - " + Description;
            return Description;
        }

        public void UnselectOtherDomains(string aDomain)
        {
            foreach (DomainNewEvent domain in domains.Values)
            {
                if (domain.domain.CompareTo(aDomain) != 0)
                    SetVisibilityImageLayer(domain.domain, false);
            }
        }

        public bool ToggleDomainVisibility(string aDomain, double aOpacity)
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi == null)
            {
                DomainNewEvent domain = domains[aDomain];
                if (domain != null)
                {
                    AddOrSetImageLayer(
                        domain.extent.xMin,
                        domain.extent.yMin,
                        domain.extent.xMax,
                        domain.extent.yMax,
                        domain.dynamicImage.ImageSrc,
                        aOpacity, aDomain);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                wi.Visible = !wi.Visible;
                return true;
            }
        }

        public bool SetDomainVisibile(string aDomain, double aOpacity)
        {
            GroupLayer gl = AppState.ViewDef.FindOrCreateGroupLayer(@"Urban Strategy");
            ElementLayer wi = gl.ChildLayers[aDomain] as ElementLayer;
            if (wi == null)
            {
                DomainNewEvent domain = domains[aDomain];
                if (domain != null)
                {
                    AddOrSetImageLayer(
                        domain.extent.xMin,
                        domain.extent.yMin,
                        domain.extent.xMax,
                        domain.extent.yMax,
                        domain.dynamicImage.ImageSrc,
                        aOpacity, aDomain);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                wi.Visible = true;
                return true;
            }
        }

        public void SelectDomain(string aDomain, double aOpacity, bool aUnselectOtherDomains = false)
        {
            lock (domains)
            {
                try
                {
                    if (aDomain != "")
                    {

                        if (GetVisibilityImageLayer(aDomain))
                        {
                            if (CurrentDomain != domains[aDomain])
                            {
                                SetVisibilityImageLayer(aDomain, true);
                                CurrentDomain = domains[aDomain];
                                if (aUnselectOtherDomains)
                                    UnselectOtherDomains(aDomain);
                            }
                        }
                        else
                        {
                            CurrentDomain = domains[aDomain];
                            AddOrSetImageLayer(
                                 CurrentDomain.extent.xMin,
                                 CurrentDomain.extent.yMin,
                                 CurrentDomain.extent.xMax,
                                 CurrentDomain.extent.yMax,
                                 CurrentDomain.dynamicImage.ImageSrc,
                                 aOpacity, aDomain);
                            if (aUnselectOtherDomains)
                                UnselectOtherDomains(aDomain);
                        }
                    }
                    else
                    {
                        CurrentDomain = null;
                        if (aUnselectOtherDomains)
                            UnselectOtherDomains("");
                    }
                }
                catch
                {
                    CurrentDomain = null;
                    if (aUnselectOtherDomains)
                        UnselectOtherDomains("");
                }
            }
            UpdateLegend();
        }

        private void sessionsEvent_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            Int32 command;
            Int32 action;
            SessionNewEvent s;
            aPayload.Read(out command);
            switch (command)
            {
                case Sessions.scSession:
                    aPayload.Read(out action);
                    switch (action)
                    {
                        case Sessions.actionNew:
                            SessionNewEvent sessionNewEvent = new SessionNewEvent(aPayload);
                            lock (sessions)
                            {
                                if (sessions.ContainsKey(sessionNewEvent.sessionID))
                                {
                                    s = sessions[sessionNewEvent.sessionID];
                                    s.description = sessionNewEvent.description;
                                    s.icon = sessionNewEvent.icon;
                                }
                                else
                                    sessions.Add(sessionNewEvent.sessionID, sessionNewEvent);
                            }
                            if (DomainView != null)
                                DomainView.Dispatcher.BeginInvoke(new TUpdateSessionList(DomainView.UpdateSessionList), null);
                            break;
                        case Sessions.actionChange:
                            SessionChangeEvent sessionChangeEvent = new SessionChangeEvent(aPayload);
                            lock (sessions)
                            {
                                if (sessions.ContainsKey(sessionChangeEvent.sessionID))
                                {
                                    s = sessions[sessionChangeEvent.sessionID];
                                    s.description = sessionChangeEvent.description;
                                    s.icon = sessionChangeEvent.icon;
                                }
                            }
                            if (DomainView != null)
                                DomainView.Dispatcher.BeginInvoke(new TUpdateSessionList(DomainView.UpdateSessionList), null);
                            break;
                        case Sessions.actionDelete:
                            SessionDeleteEvent sessionDeleteEvent = new SessionDeleteEvent(aPayload);
                            if (sessions.Remove(sessionDeleteEvent.sessionID))
                            {
                                if (DomainView != null)
                                    DomainView.Dispatcher.BeginInvoke(new TUpdateSessionList(DomainView.UpdateSessionList), null);
                            }
                            break;

                    }
                    break;
                case Sessions.scDomain:
                    aPayload.Read(out action);
                    switch (action)
                    {
                        case Sessions.actionNew:
                            DomainNewEvent domainNewEvent = new DomainNewEvent(aPayload);
                            lock (domains)
                            {
                                if (!domains.ContainsKey(domainNewEvent.domain))
                                {
                                    domains.Add(domainNewEvent.domain, domainNewEvent);
                                    if (DomainView != null)
                                        DomainView.Dispatcher.BeginInvoke(new TUpdateDomainList(DomainView.UpdateDomainList), null);
                                }
                            }
                            break;
                        case Sessions.actionDelete:
                            DomainDeleteEvent domainDeleteEvent = new DomainDeleteEvent(aPayload);
                            lock (domains)
                            {
                                if (domains.ContainsKey(domainDeleteEvent.domain))
                                {
                                    DynamicImage dynamicImage = domains[domainDeleteEvent.domain].dynamicImage;
                                    if (dynamicImage != null)
                                        dynamicImage.UnSubscribe();
                                    if (domains.Remove(domainDeleteEvent.domain))
                                    {
                                        if (DomainView != null)
                                            DomainView.Dispatcher.BeginInvoke(new TUpdateDomainList(DomainView.UpdateDomainList), null);
                                    }
                                }
                            }
                            break;
                    }
                    break;
            }
        }

        public bool Follow { get; set; }

        public void ClearAllLayers()
        {
            lock (domains)
            {
                foreach (DomainNewEvent domain in domains.Values)
                {
                    if (domain.dynamicImage != null)
                        domain.dynamicImage.UnSubscribe();
                }
                domains.Clear();
                CurrentDomain = null;
            }
            UnselectOtherDomains("");
            if (DomainView != null)
                DomainView.UpdateDomainList();
            UpdateLegend();
        }

        public void ClearAllSessions()
        {
            ClearAllLayers();
            lock (sessions)
            {
                sessions.Clear();
                CurrentSession = null;
            }
            if (DomainView != null)
                DomainView.UpdateSessionList();
        }
        
        public void IMBClose()
        {
            if (Connection != null) 
                Connection.Close();
            ClearAllSessions();
        }

        public bool IMBOpen(string aRemoteHost, int aRemotePort, string aFederation)
        {
            IMBClose();
            Connection = new TConnection(aRemoteHost, aRemotePort, IMBOwnerName, IMBOwnerID, aFederation);
            //Connection.OnDisconnect += new TConnection.TOnDisconnect(Connection_OnDisconnected);
            if (Connection.Connected)
            {
                sessionsEvent = Connection.Subscribe(Sessions.SessionsEventName);
                sessionsEvent.OnNormalEvent += new TEventEntry.TOnNormalEvent(sessionsEvent_OnNormalEvent);
                privateEvent = Connection.Subscribe("Clients" + "." + string.Format("{0:x8}", Connection.UniqueClientID));
                privateEvent.OnNormalEvent += new TEventEntry.TOnNormalEvent(sessionsEvent_OnNormalEvent);
                Sessions.SignalRequestSessions(sessionsEvent, privateEvent);
            }
            if (DomainView != null)
                DomainView.UpdateSessionList();
            return Connection.Connected;
        }

        public bool IMBConnected { get { return Connection != null && Connection.Connected; } }

        private void HandleClose(object sender, ExitEventArgs e) { IMBClose(); }

        public SessionNewEvent CurrentSession { get; set; }
        public DomainNewEvent CurrentDomain { get; set; }

        // list of sessions
        public Dictionary<Int32, SessionNewEvent> sessions = new Dictionary<Int32, SessionNewEvent>();

        // list of domains
        public Dictionary<string, DomainNewEvent> domains = new Dictionary<string, DomainNewEvent>();

        public USDomainView DomainView { get; set; }

        FloatingElement legendView = null;

        internal void UpdateLegend()
        {
            if (legendView != null && legendView.ModelInstance != null)
            {
                if (CurrentDomain != null)
                {
                    //legendView.Height = CurrentDomain.palette.Count * 28 + 24 + 30;
                    ((USLegendaViewModel)legendView.ModelInstance).Title = CurrentDomain.domain;
                    ((USLegendaViewModel)legendView.ModelInstance).Palette = CurrentDomain.palette;
                }
                else
                {
                    ((USLegendaViewModel)legendView.ModelInstance).Title = "";
                    ((USLegendaViewModel)legendView.ModelInstance).Palette = null;
                }
            }
        }

        internal void ShowLegend()
        {
            if (legendView == null)
            {
                USLegendaViewModel legendViewModel = (USLegendaViewModel)IoC.GetInstance(typeof(IUSLegenda), "");
                legendViewModel.Plugin = this;
                legendView = FloatingHelpers.CreateFloatingElement("Legend", DockingStyles.None, legendViewModel, Icon, Priority);
                legendView.Width = 150;
                legendView.StartSize = new Size(150, 250);
            }
            if (!AppState.FloatingItems.Contains(legendView))
                AppState.FloatingItems.AddFloatingElement(legendView);
            else
                AppState.FloatingItems.RemoveFloatingElement(legendView);
            UpdateLegend();
        }

        internal void ShowLegend(string aDomain)
        {
            if (legendView == null)
            {
                // new legend visible: make visible for this domain
                USLegendaViewModel legendViewModel = (USLegendaViewModel)IoC.GetInstance(typeof(IUSLegenda), "");
                legendViewModel.Plugin = this;
                legendView = FloatingHelpers.CreateFloatingElement("Legend", DockingStyles.None, legendViewModel, Icon, Priority);
                legendView.Width = 150;
                legendView.StartSize = new Size(150, 250);
                CurrentDomain = domains[aDomain];
                AppState.FloatingItems.AddFloatingElement(legendView);
                UpdateLegend();
            }
            else
            {
                if (CurrentDomain != domains[aDomain])
                {
                    // switch to new domain and make legend visible
                    CurrentDomain = domains[aDomain];
                    if (!AppState.FloatingItems.Contains(legendView))
                        AppState.FloatingItems.AddFloatingElement(legendView);
                    UpdateLegend();
                }
                else
                {
                    // toggle visibility
                    if (!AppState.FloatingItems.Contains(legendView))
                    {
                        AppState.FloatingItems.AddFloatingElement(legendView);
                        UpdateLegend();
                    }
                    else
                        AppState.FloatingItems.RemoveFloatingElement(legendView);
                }
            }
        }
    }
}
