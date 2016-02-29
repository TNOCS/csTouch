using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Caliburn.Micro;
using csShared;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;
using csShared.Utils;

namespace csAboutPlugin
{
    [Export(typeof(IPlugin))]
    public class AboutPlugin : PropertyChangedBase, IPlugin
    {
        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(()=>HideFromSettings); }
        }
        
        public int Priority
        {
            get { return 3; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/about.png"; }           
        }

        #region IPlugin Members

        public string Name
        {
            get { return "AboutPlugin"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        public void Init()
        {
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;

            CreateMenuItem();
        }

        private static Dictionary<string, string> ReadAboutConfig()
        {
            var result = new Dictionary<string, string>();
            var file = Directory.GetCurrentDirectory() + "\\about.xml";
            if (!File.Exists(file)) return result;
            var txt = File.ReadAllText(file);

            try
            {
                var d = XDocument.Parse(txt);
                var xElement = d.Element("root");
                if (xElement != null)
                    foreach (var a in xElement.Elements())
                    {
                        result[a.FirstAttribute.Value] = a.Value;
                    }
            }
            catch (Exception e)
            {
                Logger.Log("Config", "Error parsing config string", e.Message, Logger.Level.Error);
            }
            return result;
        }

        public class License : PropertyChangedBase
        {
            public string Name         { get; set; }
            public string Url          { get; set; }
            public string LicenseType  { get; set; }
            public string Restrictions { get; set; }
        }

        private static List<License> ReadLicenseConfig()
        {
            var result = new List<License>();
            var file = Directory.GetCurrentDirectory() + "\\about.xml";
            if (!File.Exists(file)) return result;
            var txt = File.ReadAllText(file);

            try
            {
                var d = XDocument.Parse(txt);
                var xElement = d.Element("root");
                if (xElement != null )
                {
                    foreach (var licenses in xElement.Elements().Where(k=>k.Name.LocalName == "Licenses" ))
                    {
                        foreach (var a in licenses.Elements())
                        {
                            var name         = a.Element("Name").Value;
                            var url          = a.Element("URL").Value;
                            var license      = a.Element("License").Value;
                            var restrictions = a.Element("Restrictions").Value;
                            var l            = new License
                            {
                                Url          = url,
                                Name         = name,
                                LicenseType  = license,
                                Restrictions = restrictions
                            };
                            result.Add(l);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Config", "Error parsing config string", e.Message, Logger.Level.Error, true);
            }
            return result;
        }

        private void CreateMenuItem()
        {
            var about = new MenuItem
            {
                Name = "About &\nLicense",
            };
            about.Clicked += (e, s) =>
            {
                var viewModel = new AboutViewModel {AboutInfo = ReadAboutConfig(), LicenseInfo = ReadLicenseConfig()};
                var element = FloatingHelpers.CreateFloatingElement("About & License", new Point(600, 400), new Size(800, 600), viewModel);
                AppStateSettings.Instance.FloatingItems.AddFloatingElement(element);
            };
            AppState.MainMenuItems.Add(about);
        }

        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
            {
                if (AppState.MainMenuItems.All(k => k.Name != "About &\nLicense"))
                {
                    CreateMenuItem();
                }
            }
            else
            {
                var rem = AppState.MainMenuItems.FirstOrDefault(k => k.Name == "About &\nLicense");
                if (rem != null)
                    AppState.MainMenuItems.Remove(rem);
            }
        }

        public void GoTo()
        {

        }

        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();
        }

        public void Pause()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        #endregion
    }
}
