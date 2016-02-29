using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;

namespace csCommon.Plugins.NotebookPlugin
{

    

    [Export(typeof(IScreen))]
    public class NotebookConfigViewModel : Screen
    {

        private string newName;

        public string NewName
        {
            get { return newName; }
            set { newName = value; NotifyOfPropertyChange(()=>NewName); }
        }
        

        private ScreenshotPlugin plugin;

        public ScreenshotPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; NotifyOfPropertyChange(()=>Plugin ); }
        }
        
      
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
            set { }
        }
        

        public ITimelineManager Timeline
        {
            get { return AppState.TimelineManager; }
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
          
        }

        public void Remove(Notebook nb)
        {
            
            if (Plugin.Notebooks.Contains(nb))
            {
                Plugin.Notebooks.Remove(nb);
            }
        }

        public void AddNotebook()
        {
            if (Plugin.Notebooks.Any(k => k.Name.ToLower() == NewName.ToLower()))
            {
                AppState.TriggerNotification("Could not create notebook. This name is already in use");
                return;
            }
            var nb = new Notebook() {Name = NewName};
            var ff = Plugin.ScreenshotFolder;
            if (ff.Any())
            {
                var f = Path.Combine(ff.First(), NewName);
                if (!Directory.Exists(f)) Directory.CreateDirectory(f);
                nb.Folder = f;
                nb.Available = Directory.Exists(f);
                
                Plugin.Notebooks.Add(nb);
                Plugin.Notebooks.Save();

                Plugin.Notebooks.ActiveNotebook = nb;
            }
        }


    }


}

