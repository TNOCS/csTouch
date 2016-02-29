using System.Collections.Generic;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared;

namespace csAboutPlugin
{
    [Export(typeof(IAbout))]
    public class AboutViewModel:Screen
    {
        private Dictionary<string, string> aboutInfo = new Dictionary<string, string>();

        public Dictionary<string, string> AboutInfo
        {
            get { return aboutInfo; }
            set { aboutInfo = value; }
        }

        private List<AboutPlugin.License> licenseInfo = new List<AboutPlugin.License>();

        public List<AboutPlugin.License> LicenseInfo
        {
            get { return licenseInfo; }
            set { licenseInfo = value; }
        }

        //private Dictionary<string, string> licenseInfo = new Dictionary<string, string>();

        //public Dictionary<string, string> LicenseInfo
        //{
        //    get { return licenseInfo; }
        //    set { licenseInfo = value; }
        //}

        public AppStateSettings AppState { get { return AppStateSettings.Instance; }}
    }
}
