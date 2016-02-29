using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using csGeoLayers;
using csShared;
using Newtonsoft.Json;

namespace csCommon.Plugins.DashboardPlugin
{
    public class DashboardConfiguration : BindableCollection<Dashboard>
    {

        private BindableCollection<DashboardItem> toolbox = new BindableCollection<DashboardItem>();

        public BindableCollection<DashboardItem> Toolbox
        {
            get { return toolbox; }
            set { toolbox = value;
                NotifyOfPropertyChange("Toolbox");
            }
        }        

        private bool hasEmptyDashboard = true;

        public bool HasEmptyDashboard
        {
            get { return hasEmptyDashboard; }
            set { hasEmptyDashboard = value; }
        }
        
        private Dashboard activeDashboard;

        [JsonIgnore]
        public Dashboard ActiveDashboard
        {
            get { return activeDashboard; }
            set
            {
                if (activeDashboard == value || value==null) return;
                if (activeDashboard!=null) activeDashboard.IsActive = false;
                activeDashboard = value;
                activeDashboard.IsActive = true;
                if (ActiveDashboardChanged != null) ActiveDashboardChanged(this, value);
                Execute.OnUIThread(() => AppStateSettings.Instance.TriggerNotification(string.Join("[b]",value.Title,"[/b] activated"), "Dashboard",
                    AppStateSettings.Instance.AccentBrush,
                    pathData:
                        "M61.352,34.599C60.224,34.282,59.033,34.956,58.719,36.1L50.119,67.534C49.957,67.527 49.796,67.506 49.633,67.506 45.598,67.506 41.978,69.749 40.187,73.361 37.607,78.565 39.739,84.9 44.944,87.482 46.413,88.212 47.987,88.583 49.621,88.583 53.654,88.583 57.272,86.34 59.063,82.728 61.647,77.526 59.513,71.191 54.309,68.607 54.298,68.602 54.286,68.598 54.275,68.592L62.855,37.23C63.166,36.089,62.495,34.911,61.352,34.599z M55.225,80.825C54.162,82.968 52.015,84.299 49.621,84.299 48.654,84.299 47.723,84.079 46.848,83.646 43.761,82.114 42.495,78.356 44.025,75.267 45.09,73.124 47.237,71.793 49.633,71.793 50.598,71.793 51.531,72.013 52.404,72.446 55.491,73.979 56.757,77.738 55.225,80.825z M49.625,15.151C24.813,15.151 4.625,35.338 4.625,60.151 4.625,62.518 6.544,64.437 8.911,64.437 11.278,64.437 13.197,62.518 13.197,60.151 13.197,40.064 29.538,23.722 49.626,23.722 69.714,23.722 86.055,40.063 86.055,60.151 86.055,62.518 87.974,64.437 90.341,64.437 92.708,64.437 94.627,62.518 94.627,60.151 94.625,35.338 74.437,15.151 49.625,15.151z"));
            }
        }

        public event EventHandler<DashboardItem> ActiveDashboardItemChanged;

        private DashboardItem activeDashboardItem;

        [JsonIgnore]
        public DashboardItem ActiveDashboardItem
        {
            get { return activeDashboardItem; }
            set { activeDashboardItem = value; 
                NotifyOfPropertyChange("ActiveDashboardItem");
                if (ActiveDashboardItemChanged != null) ActiveDashboardItemChanged(this, value);
            }
        }

        public Dashboard GetOrCreateActiveDashboard()
        {
            if (ActiveDashboard == null)
            {
                if (this.All(k => k.IsEmpty))
                    Add(new Dashboard()
                {
                    Title = "New Dashboard",
                    DashboardItems = new BindableCollection<DashboardItem>()       
                });
                ActiveDashboard = this.First(k=>!k.IsEmpty);
            }
            return ActiveDashboard;
        }

        public event EventHandler<Dashboard> ActiveDashboardChanged;

        public async Task Save() // REVIEW TODO fix: async void -> Task
        {
            await Save("dashboards.config");
        }

        public async Task Save(string file) // REVIEW TODO fix: async void -> Task
        {
            var s = await JsonConvert.SerializeObjectAsync(this);
            File.WriteAllText(file,s);
        }

        public Dashboard GetDefault()
        {
            var d = this.FirstOrDefault(k => k.IsDefault);
            if (d == null && this.Any()) d = this.First();
            return d;
        }

        public void SetDefault(Dashboard d)
        {
            foreach (var i in this) i.IsDefault = false;
            if (this.Contains(d)) d.IsDefault = true;
        }

       

        public void Load(string file) // REVIEW TODO fix: Async removed
        {
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                var result = JsonConvert.DeserializeObject<DashboardConfiguration>(json);
                Clear();
                if (result == null) return;
                foreach (var r in result)
                {
                    Add(r);
                    foreach (var di in r.DashboardItems)
                    {
                        var t = Type.GetType(di.Type);
                        if (t != null)
                        {
                            di.ViewModel = (IDashboardItemViewModel)Activator.CreateInstance(t);
                            di.ViewModel.Item = di;

                        }
                        di.Dashboard = r;
                    }
                }
                if (HasEmptyDashboard && this.All(k => !k.IsEmpty))
                {
                    Insert(0,new Dashboard() { IsEmpty = true, Title = "Map", CanEdit = false, Opacity = 0});
                }
                if (this.Any() && this.All(k => !k.IsDefault)) this.First().IsDefault = true;
            }
            
        }


        internal void CheckForEmptyDashboard()
        {
            if (HasEmptyDashboard && this.All(k => !k.IsEmpty))
            {
                Insert(0, new Dashboard() { IsEmpty = true, Title = "Map", CanEdit = false, Opacity = 0 });
            }
            if (this.Any() && this.All(k => !k.IsDefault)) this.First().IsDefault = true;
        }
    }
}