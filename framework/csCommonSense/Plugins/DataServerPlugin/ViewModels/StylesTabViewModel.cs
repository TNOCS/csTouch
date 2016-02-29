using System.Linq;
using Caliburn.Micro;
using DataServer;
using csShared;

namespace csDataServerPlugin
{

    public class StylesTabViewModel : Screen
    {

        

        private BindableCollection<BaseContent> styles;

        public BindableCollection<BaseContent> Styles
        {
            get { return styles; }
            set { styles = value; NotifyOfPropertyChange(() => Styles); }
        }

        private PoiService service;

        public PoiService Service
        {
            get { return service; }
            set { service = value; NotifyOfPropertyChange(()=>Service); }
        }



        public DataServerBase Dsb { get; set; }

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

       
        
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            Styles = new BindableCollection<BaseContent>();
            Styles.AddRange(Service.PoITypes.Where(k => k.Style != null).ToList());
        }

        public new void Refresh() // FIXME TODO "new" keyword missing?
        {
            foreach (var p in Service.PoIs) { p.UpdateAnalysisStyle(); p.TriggerUpdated();}
        }

       

        

      
    }
}