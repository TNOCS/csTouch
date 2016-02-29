using System;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using csCommon.Plugins.DashboardPlugin;
using DataServer;

namespace csCommon.Plugins.Dashboards
{
  
     
    [Export(typeof(IScreen))]
    public class ServiceItemConfigViewModel : Screen
    {
        public override string DisplayName { get; set; }

        public DataServerBase DataServer { get; set; }

        private ServiceDashboardItem item;

        public ServiceDashboardItem Item
        {
            get { return item; }
            set { item = value; NotifyOfPropertyChange(()=>Item); }
        }
        
        


      
        public event EventHandler<string> ConfigChanged;

        private string vizType = "Focus Value";

        public string VizType
        {
            get { return vizType; }
            set
            {
                vizType = value; NotifyOfPropertyChange(()=>VizType);
                //
                SetConfig();
            }
        }

        public void SetConfig()
        {
            Item.Config = VizType;
            if (ConfigChanged != null) ConfigChanged(this, null);
        }

        private IScreen configScreen;

        public IScreen ConfigScreen
        {
            get { return configScreen; }
            set { configScreen = value; NotifyOfPropertyChange(() => ConfigScreen); }
        }

        

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            
            

        }

    }
}