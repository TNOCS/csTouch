using Caliburn.Micro;
using csShared;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;

namespace csCommon.Plugins.Config
{

    public interface IConfig
    { }


    [Export(typeof(IConfig))]
    public class ConfigViewModel : Conductor<Screen>.Collection.OneActive
    {
        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public Brush AccentBrush
        {
            get { return AppState.AccentBrush; }
        }

        private void AddConfigScreen(Screen s)
        {
            if (AppState.ConfigTabs.All(k => k.DisplayName != s.DisplayName)) AppState.ConfigTabs.Insert(0, s);
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            AddConfigScreen(new BasicViewModel { DisplayName = "Basic" });
            AddConfigScreen(new PluginsViewModel { DisplayName = "Plugins" });
            AddConfigScreen(new LayoutViewModel { DisplayName = "Layout" });
            AddConfigScreen(new TimelineConfigViewModel { DisplayName = "Timeline" });
            AddConfigScreen(new ServerViewModel { DisplayName = "Server" });

            ActiveItem = AppState.ConfigTabs.First();
        }
    }
}

