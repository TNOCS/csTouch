using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using Caliburn.Micro;
using csShared;
using csShared.Geo.Content;
using csShared.Interfaces;
using System.ComponentModel.Composition;

namespace csGeoLayers.Plugins.ContentDirectory
{
    [Export(typeof(IContentDirectory))]
    public class ContentDirectoryViewModel : Screen, IContentDirectory
    {

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private List<IContent> _content;

        public List<IContent> Content
        {
            get { return _content; }
            set { _content = value; NotifyOfPropertyChange(()=>Content); }
        }

        public Brush AccentBrush
        {
            get { return AppState.AccentBrush; }
        }

        public void AddContent(IContent content)
        {
            content.Add();
            AppState.TriggerNotification(content.Name + " content added");
        }

        public void RemoveContent(IContent content)
        {
            content.Remove();
            AppState.TriggerNotification(content.Name + " content removed");
        }

        public void ConfigContent(IContent content)
        {
            content.Configure();            
        }

        private string _filter;

        public string Filter
        {
            get { return _filter; }
            set { 
                _filter = value; 
                NotifyOfPropertyChange(()=>Filter);
                UpdateContent();

            }
        }
        
        

        private IPlugin _plugin;

	    public IPlugin Plugin
	    {
		    get { return _plugin;}
		    set { _plugin = value; NotifyOfPropertyChange(()=>Plugin);}
	    }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            UpdateContent();
        }

        public void UpdateContent()
        {
            if (string.IsNullOrEmpty(Filter))
            {
                Content = AppStateSettings.Instance.ViewDef.Content.ToList();
            }
            else
            {
                Content =
                    AppStateSettings.Instance.ViewDef.Content.Where(k => k.Name.ToLower().Contains(Filter.ToLower())).ToList();
            }
        }

        



        


    }
}
