using System;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csPresenterPlugin.Events;
using csPresenterPlugin.ViewModels;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using csShared.TabItems;

namespace csPresenterPlugin
{
    [Export(typeof(IPlugin))]
    public class PresenterPlugin : PropertyChangedBase, IPlugin, IHandle<FolderEvent> {
        public PresenterPlugin() {
            AppStateSettings.Instance.EventAggregator.Subscribe(this);
        }

        private TnoPresenterViewModel presenter;

        public bool CanStop { get { return true; } }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        
        public DataServer.DataServerBase DataServer
        {
            get
            {
                return AppState.DataServer;
            }
            

        }        

        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public int Priority
        {
            get { return 6; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/presenter.png"; }
        }

        #region IPlugin Members

        public string Name
        {
            get { return "PresenterPlugin"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get { return AppStateSettings.Instance; }
            set { }
        }

        public StartPanelTabItem st { get; set; }

        public void Init()
        {
            presenter = new TnoPresenterViewModel { Plugin = this };
        }

        public void Start()
        {
            
            st = new StartPanelTabItem {
                Name = "Background",
                HeaderStyle = TabHeaderStyle.Image,
                Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/presenter.png")),
                ModelInstance = presenter
            };
            AppState.AddStartPanelTabItem(st);
            IsRunning = true;
        }

        public void Pause()
        {
            
        }

        public void Stop()
        {
            IsRunning = false;
            AppState.RemoveStartPanelTabItem(st);
        }

        
        #endregion

        public void Handle(FolderEvent folderEvent) {
            switch (folderEvent.Action) {
                case FolderAction.Add:
                    presenter.AddFolder(folderEvent.FolderName);
                    break;
                case FolderAction.Remove:
                    presenter.RemoveFolder(folderEvent.FolderName);
                    break;
            }
        }
    }

    public interface IFileWatcher
    {}
}
