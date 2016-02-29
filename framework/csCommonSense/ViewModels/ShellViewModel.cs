using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using Caliburn.Micro;
using csCommon.Plugins.Config;
using csShared;
using csShared.FloatingElements;
using csShared.FloatingElements.Classes;
using csShared.Interfaces;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;

namespace csCommon
{
    [Export(typeof(IShell))]
    public class ShellViewModel : Screen, IShell
    {
       
        private readonly CompositionContainer container;
        private readonly IFloating floating;
        private readonly IModule module;
        private IPlugins plugins;
        private IPopups popups;
        private IStartpanel startpanelBottom;
        private IStartpanel startpanelTop;

        [ImportingConstructor]
        public ShellViewModel(CompositionContainer container)
        {
            // load visuals
            this.container = container;
            plugins = container.GetExportedValue<IPlugins>();
            popups = container.GetExportedValue<IPopups>();
            floating = container.GetExportedValueOrDefault<IFloating>();
            AppState.Container = this.container;
            AppState.FullScreenFloatingElementChanged += (e, s) => NotifyOfPropertyChange(() => FullScreen);
            
            // load module
            AppState.State = AppStates.Starting;

            //Load configuration
            AppState.Config.LoadOfflineConfig();
            AppState.Config.LoadLocalConfig();
            AppState.Config.UpdateValues();
            BackgroundWorker barInvoker = new BackgroundWorker();
            barInvoker.DoWork += delegate
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                AppState.ViewDef.CheckOnlineBaseLayerProviders();
            };
            barInvoker.RunWorkerAsync();
            
            
            
            var b = container.GetExportedValues<IModule>();
            module = b.FirstOrDefault();
            if (module == null) return;

            AppState.Config.ApplicationName = module.Name;

            AppState.State = AppStates.AppInitializing;
            AppState.ViewDef.RememberLastPosition = AppState.Config.GetBool("Map.RememberLastPosition", true);

            AppState.MapStarted += (e, f) =>
            {
                AppState.StartFramework(true, true, true, true);

                AppState.ShareContracts.Add(new EmailShareContract());
                AppState.ShareContracts.Add(new QrShareContract());

                var g = AppState.AddDownload("Init", "StartPoint");

                // start framework
                AppState.State = AppStates.FrameworkStarting;
                AddMainMenuItems();

                // set map
                if (AppState.ViewDef.RememberLastPosition)
                {
                    var extent = (AppState.Config.Get("Map.Extent", "-186.09257071294,-101.374056570352,196.09257071294,204.374056570352"));
                    if (!string.IsNullOrEmpty(extent))
                        AppState.ViewDef.MapControl.Extent = (Envelope)new EnvelopeConverter().ConvertFromString(extent);
                }

                // start app
                AppState.State = AppStates.AppStarting;

                // init plugins


                module.StartApp();

                // app ready
                AppState.State = AppStates.AppStarted;
                AppState.Imb.UpdateStatus();
                AppState.FinishDownload(g);

                // Show timeline player (EV: I've tried to turn it on earlier during the startup process, but that didn't work 
                // (most likely, because something wasn't loaded or initialized correctly).
                AppState.TimelineManager.PlayerVisible = AppState.Config.GetBool("Timeline.PlayerVisible", false);
            };
            
            module.InitializeApp();

        }

        public void Minimize()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Indication that one of the floating elements in running in fullscreen mode (e.g. after double mouse click or 4 finger gesture
        /// </summary>
        public bool FullScreen
        {
            get { return AppState.FullScreenFloatingElement == null; }
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public IPlugins Plugins
        {
            get { return plugins; }
            set
            {
                plugins = value;
                NotifyOfPropertyChange(() => Plugins);
            }
        }

        public IPopups Popups
        {
            get { return popups; }
            set
            {
                popups = value;
                NotifyOfPropertyChange(() => Popups);
            }
        }

        public IModule Module
        {
            get { return module; }
        }

        public IFloating FloatingViews
        {
            get { return floating; }
        }

        public IStartpanel StartpanelBottom
        {
            get { return startpanelBottom; }
            set
            {
                startpanelBottom = value;
                NotifyOfPropertyChange(() => StartpanelBottom);
            }
        }

        public IStartpanel StartpanelTop
        {
            get { return startpanelTop; }
            set
            {
                startpanelTop = value;
                NotifyOfPropertyChange(() => StartpanelTop);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);


        private void AddMainMenuItems()
        {
            // add tools            
            if (AppState.Config.GetBool("App.CanExit", true))
            {
                var canExit = new MenuItem
                {
                    Name = "Exit"
                };
                canExit.Clicked += (e1, s1) =>
                    AppState.CloseApplication();
                AppState.MainMenuItems.Add(canExit);
            }

            // add tools            
            if (AppState.Config.GetBool("App.CanExitAll", false))
            {
                var canExitAll = new MenuItem
                {
                    Name = "Exit All"
                };
                canExitAll.Clicked += (e1, s1) =>
                {
                    foreach (var c in AppState.Imb.Clients.Where(k => k.Value.Capabilities.Contains(csImb.csImb.CapabilityExit)))
                    {
                        AppState.Imb.SendCommand(c.Value.Id, csImb.csImb.CommandExit);
                    }
                    AppState.CloseApplication();
                };

                AppState.MainMenuItems.Add(canExitAll);
            }

            if (AppState.Config.GetBool("App.CanLogoff", false))
            {
                var canLogoff = new MenuItem
                {
                    Name = "Sign Out"
                };
                canLogoff.Clicked += (e1, s1) => AppState.LogOff();
                AppState.MainMenuItems.Add(canLogoff);
            }

            // EV Removed, as it didn't seem to be working, and we already have it in the map plugin.
            //if (AppState.Config.GetBool("App.CanCreateCache", true))
            //{
            //    var canCreateCache = new MenuItem
            //    {
            //        Name = "Create Cache"
            //    };
            //    canCreateCache.Clicked += (e1, s1) => AppState.TriggerCreateCache();
            //    AppState.MainMenuItems.Add(canCreateCache);
            //}

            

            var config = new MenuItem
            {
                Name = "Config \nApp"
            };
            config.Clicked += (e, s) =>
                                  {
                                      OpenConfig();
                                  };
            AppState.MainMenuItems.Add(config);

            AppState.Commands.AddCommand("Config", new[] { "Config" },(c)=> OpenConfig(),new KeyGesture(Key.C,ModifierKeys.Alt));


            var flipScreen = new MenuItem();
            flipScreen.Clicked += delegate { AppState.IsScreenFlipped = !AppState.IsScreenFlipped; };
            flipScreen.Name = "Flip\nScreen";
            flipScreen.Commands = new List<string> { "Flip Screen", "Rotate Screen" };
            AppState.MainMenuItems.Add(flipScreen);


            var procs = Process.GetProcessesByName("AppLauncher");
            if (procs.Any())
            {
                var p = procs.FirstOrDefault();
                var settings = new MenuItem
                {
                    Name = "Back to App Dashboard"
                };
                settings.Clicked += (e, s) =>
                {
                    //SwitchToThisWindow(p.Handle, true);

                    var element = AutomationElement.FromHandle(p.MainWindowHandle);
                    element.SetFocus();
                    //SetForegroundWindow(p.Handle);
                };
                AppState.MainMenuItems.Add(settings);
            }




            //var screenshot = new MenuItem { Name = "Screen-\nshot" };
            //screenshot.Clicked += (e, s) =>
            //                          {

            //                              Capture();
            //                          };
            //AppState.MainMenuItems.Add(screenshot);
        }

        private static void OpenConfig()
        {
            var viewModel = new ConfigViewModel();
            var element = FloatingHelpers.CreateFloatingElement("Application Status", new Point(600, 400), new Size(800, 600),
                viewModel);
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(element);
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            var sv = (ShellView)view;

            AppState.MainBorder = sv.BMain;

            Application.Current.MainWindow.KeyDown += MainWindow_KeyDown;

        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            foreach (var c in AppState.Commands.Where(k => k.Shortcut != null))
            {
                if (c.Shortcut.Matches(Application.Current.MainWindow, e))
                {
                    c.Handler("");
                }                
            }
        }


    }
}