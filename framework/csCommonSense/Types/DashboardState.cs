using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Caliburn.Micro;
using csShared.Utils;

namespace csShared
{

    public class DashboardEventArgs : EventArgs
    {
        public DashboardState Dashboard { get; set; }
        public Dictionary<string, string> Parameters { get; set; }

    }

    public delegate void DashboardAdapterHandler(ref DashboardState dashboard);

    [Serializable]
    public class DashboardStateCollection : BindableCollection<DashboardState>
    {
        [NonSerialized]
        public static string DashboardFile = "dashboards.xml";

        private List<DashboardAdapterHandler> handlers = new List<DashboardAdapterHandler>();

        public event EventHandler<DashboardEventArgs> DashboardActivated;

        public void RegisterHandler(DashboardAdapterHandler handler)
        {
            handlers.Add(handler);
        }

        public void RemoveHandler(DashboardAdapterHandler handler)
        {
            if (handlers.Contains(handler)) handlers.Remove(handler);
        }

        public void SaveDashboard(string title)
        {
            var d = new DashboardState() { Title = title };
            foreach (var h in handlers)
            {
                h(ref d);
            }
            Add(d);
            Save();

            // save
        }

        public void GoToDashboard(DashboardState d, Dictionary<string, string> param = null)
        {

            if (d != null && DashboardActivated != null)
            {
                AppStateSettings.Instance.TriggerNotification("Dashboard",d.Title + " activated. Just a moment");
                DashboardActivated(this, new DashboardEventArgs() { Dashboard = d, Parameters = param });
            }


        }

        public void GoToDashboard(string title, Dictionary<string, string> param = null)
        {
            var d = this.FirstOrDefault(k => k.Title == title);
            GoToDashboard(d, param);
        }


        public void Save()
        {
            try
            {
                var xml = new FileStream(DashboardFile, FileMode.Create);
                var serializer2 = new XmlSerializer(typeof(DashboardStateCollection));
                serializer2.Serialize(xml, this);
                xml.Close();
            }
            catch (Exception e)
            {
                Logger.Log("Dashboards", "Error saving dashboard", e.Message, Logger.Level.Error);
            }
        }

        public static DashboardStateCollection Load()
        {
            if (!File.Exists(DashboardFile)) return new DashboardStateCollection();

            try
            {
                TextReader reader2 = new StreamReader(DashboardFile);
                var serializer2 = new XmlSerializer(typeof(DashboardStateCollection));
                var result = (DashboardStateCollection)serializer2.Deserialize(reader2);
                reader2.Close();
                return result;
            }
            catch (Exception e)
            {
                Logger.Log("Dashboards", "Error loading dashboard", e.Message, Logger.Level.Error);
            }
            return new DashboardStateCollection();
        }


        public void RemoveDashboard(DashboardState d)
        {
            if (Contains(d)) Remove(d);
            Save();
        }
    } 

    [Serializable]
    public class DashboardState : PropertyChangedBase
    {

        private string title;


        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }

        private Guid guid = Guid.NewGuid();

        public Guid Guid
        {
            get { return guid; }
            set { guid = value; }
        }

        private bool isPinned;

        public bool IsPinned
        {
            get { return isPinned; }
            set { isPinned = value; NotifyOfPropertyChange(()=>IsPinned); }
        }
        

        private SerializableDictionary<string, string> states = new SerializableDictionary<string, string>();

        public SerializableDictionary<string, string> States
        {
            get { return states; }
            set { states = value; }
        }

    } 
}
