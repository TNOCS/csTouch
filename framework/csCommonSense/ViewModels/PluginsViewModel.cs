using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;

namespace csCommon
{
    [Export(typeof (IPlugins))]
    public class PluginsViewModel : Screen, IPlugins
    {
        private BindableCollection<IPluginScreen> _plugins = new BindableCollection<IPluginScreen>();

        [ImportingConstructor]
        public PluginsViewModel() {
            _appStateSettings.Plugins.CollectionChanged += Plugins_CollectionChanged;
        }

        public AppStateSettings _appStateSettings {
            get { return AppStateSettings.GetInstance(); }
        }

        public BindableCollection<IPluginScreen> Plugins {
            get { return _plugins; }
            set {
                _plugins = value;
                NotifyOfPropertyChange(() => Plugins);
            }
        }

        public FloatingCollection FloatingItems {
            get { return AppStateSettings.Instance.FloatingItems; }
        }

        protected override void OnViewLoaded(object view) {
            base.OnViewLoaded(view);


            //_plugins = new BindableCollection<IPlugin>()
            //               {
            //                   _appStateSettings.Container.GetExportedValues<IPlugin>().Where(k=>k.Name == "CameraPlugin").FirstOrDefault()
            //               };
            //_appStateSettings.Plugins
            //_plugins = _appStateSettings.Plugins;
        }

        private void Plugins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (IPlugin a in e.NewItems) {
                    if (a.Screen != null) Plugins.Add(a.Screen);
                    a.PropertyChanged += a_PropertyChanged;
                }
            }
            if (e.OldItems != null) foreach (IPlugin a in e.OldItems) if (a.Screen != null && Plugins.Contains(a.Screen)) Plugins.Remove(a.Screen);
            //Plugins.Clear();            
            //foreach (var a in _appStateSettings.Plugins.Where(k => k.Screen != null).Select(k => k.Screen))
            //{

            //}
        }

        private void a_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != "Screen") return;
            var p = (IPlugin) sender;
            if ((p.Screen != null) && (!Plugins.Contains(p.Screen))) Plugins.Add(p.Screen);
            if ((p.Screen == null) && (Plugins.Contains(p.Screen))) Plugins.Remove(p.Screen);
        }
    }
}