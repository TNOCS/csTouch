using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using csGeoLayers;
using csShared;
using csShared.FloatingElements;
using csShared.Interfaces;
using csShared.Utils;

namespace csCommon.Plugins.ServicesPlugin
{
    [Export(typeof(IPlugin))]
    public class ServicesPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }
        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"icons\globe.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        public string Name { get { return "ServicesPlugin"; } }

        public void Init() { }

        private MenuItem config;

        private ServicesViewModel svm;

        private readonly BindableCollection<ServiceInstanceViewModel> services = new BindableCollection<ServiceInstanceViewModel>();

        public BindableCollection<ServiceInstanceViewModel> Services { get { return services; } }

        public void Start()
        {
            IsRunning = true;
            config = new MenuItem { Name = "Services" };
            config.Clicked += (e, s) =>
            {
                try {
                    svm = new ServicesViewModel {Plugin = this};
                    var element = FloatingHelpers.CreateFloatingElement("Services", new Point(600, 400),
                        new Size(800, 600), svm);
                    element.CanFullScreen = true;
                    AppStateSettings.Instance.FloatingItems.AddFloatingElement(element);
                }
                catch (SystemException ex) {
                    Logger.Log("ServicesPlugin", "Cannot start Services plugin", ex.Message, Logger.Level.Error, true);
                }
            };
            AppState.MainMenuItems.Add(config);
        }

        public void Pause()
        {
            IsRunning = false;

        }

        public void StopAllServices()
        {
            foreach (var s in Services.Where(k => k.Instance.IsRunning)) s.Instance.Stop();
        }

        public void StartAllServices()
        {
            foreach (var s in Services.Where(k => !k.Instance.IsRunning)) s.Start();
        }

        private void AddInstance(ServiceInstance instance)
        {
            Services.Add(new ServiceInstanceViewModel { Instance = instance });
        }

        public void Stop()
        {
            IsRunning = false;
            StopAllServices();
            AppState.MainMenuItems.Remove(config);
        }

        private void AddModule(string title, string application, string folder)
        {
            AddInstance(new ServiceInstance
            {
                Title = title,
                Application = application,
                Folder = folder
            });
        }

        /// <summary>
        /// Define services.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="extension"></param>
        public void InitializeServices(string folder, string extension = "*.exe")
        {
            foreach (var exe in Directory.GetFileSystemEntries(folder, extension))
            {
                var title = Path.GetFileNameWithoutExtension(exe).SplitCamelCase();
                var file = Path.GetFileName(exe);
                AddModule(title, file, folder);
            }
        }

    }
}
