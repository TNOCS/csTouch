using System.IO;
using Caliburn.Micro;
using csShared;
using Newtonsoft.Json;

namespace csCommon.Plugins.NotebookPlugin
{
    public class NotebookCollection : BindableCollection<Notebook>
    {
        public string ActiveNotebookConfig
        {
            get { return AppStateSettings.Instance.Config.Get("Notebooks.Active", Count > 0 ? this[0].Name : string.Empty); }
            set
            {
                AppStateSettings.Instance.Config.SetLocalConfig("Notebooks.Active", value, true);
                NotifyOfPropertyChange("ActiveNotebookConfig");
            }
        }

        private Notebook activeNotebook;

        public Notebook ActiveNotebook
        {
            get { return activeNotebook; }
            set { activeNotebook = value;
                if (value == null) return;
                NotifyOfPropertyChange("ActiveNotebook");
                ActiveNotebookConfig = value.Name;
            }
        }
        
        public void Save()
        {
            var c = JsonConvert.SerializeObject(this);
            AppStateSettings.Instance.Config.SetLocalConfig("Notebooks",c,true);
        }

        public void Load()
        {
            var c = AppStateSettings.Instance.Config.Get("Notebooks", "");
            if (string.IsNullOrEmpty(c)) return;
            var temp = JsonConvert.DeserializeObject<NotebookCollection>(c);
            ClearItems();
            foreach (var nb in temp)
            {
                Add(nb);
                nb.Available = Directory.Exists(nb.Folder);
                nb.LoadItems();
            }
        }
    }
}