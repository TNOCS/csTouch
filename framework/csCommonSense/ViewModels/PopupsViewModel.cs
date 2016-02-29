using System.Collections.Specialized;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;

namespace csCommon
{
    [Export(typeof (IPopups))]
    public class PopupsViewModel : Screen, IPopups
    {
        private BindableCollection<IPopupScreen> _popups = new BindableCollection<IPopupScreen>();

        [ImportingConstructor]
        public PopupsViewModel()
        {
            _appStateSettings.Popups.CollectionChanged += Plugins_CollectionChanged;
        }

        public AppStateSettings _appStateSettings {
            get { return AppStateSettings.GetInstance(); }
        }

        public BindableCollection<IPopupScreen> Popups
        {
            get { return _popups; }
            set {
                _popups = value;
                NotifyOfPropertyChange(() => Popups);
            }
        }

        public FloatingCollection FloatingItems {
            get { return AppStateSettings.Instance.FloatingItems; }
        }

        protected override void OnViewLoaded(object view) {
            base.OnViewLoaded(view);


            //_popups = new BindableCollection<IPlugin>()
            //               {
            //                   _appStateSettings.Container.GetExportedValues<IPlugin>().Where(k=>k.Name == "CameraPlugin").FirstOrDefault()
            //               };
            //_appStateSettings.Plugins
            //_popups = _appStateSettings.Plugins;
        }

        private void Plugins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (IPopupScreen a in e.NewItems) {
                    if (a != null) Popups.Add(a);
                    
                }
            }
            if (e.OldItems != null)
                foreach (IPopupScreen a in e.OldItems) if (a != null && Popups.Contains(a)) Popups.Remove(a);
            //Plugins.Clear();            
            //foreach (var a in _appStateSettings.Plugins.Where(k => k.Screen != null).Select(k => k.Screen))
            //{

            //}
        }

        
    }
}