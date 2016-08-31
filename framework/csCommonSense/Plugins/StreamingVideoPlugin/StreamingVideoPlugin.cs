using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using csShared.TabItems;
using csStreamingVideoPlugin.ViewModels;
using VlcVideoPlayer.Models;
using System.IO;

namespace csStreamingVideoPlugin
{
    [Export(typeof(IPlugin))]
    public class StreamingVideoPlugin : PropertyChangedBase, IPlugin
    {

        private AvaliableVideoStreamsViewModel streamingVideoVM;
        private VideoStreams videoStreams;
        private StartPanelTabItem tabMainScreen;

        public StreamingVideoPlugin()
        {
            AppStateSettings.Instance.EventAggregator.Subscribe(this);
        }


        public string Name
        {
            get { return "Streaming Video Plugin"; }
        }

        public csShared.AppStateSettings AppState
        {
            get
            {
                return AppStateSettings.Instance;
            }
            set
            {
                // Do nothing
            }
        }

        private bool isRunning;
        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/SecurityCamera.png"; }
        }

        public int Priority
        {
            get { return 6; }
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

        public bool CanStop
        {
            get { return true; }
        }



        public void Init()
        {
            
            string fullPath = System.Reflection.Assembly.GetAssembly(typeof(StreamingVideoPlugin)).Location;
            string directory = Path.GetDirectoryName(fullPath);
            videoStreams = VideoStreams.Load(new FileInfo(Path.Combine(directory, AppStateSettings.Instance.Config.Get("StreamingVideo.ConfigFile", "streamingvideo.xml"))));
            streamingVideoVM = new AvaliableVideoStreamsViewModel(this);
        }

        public VideoStreams Model { get { return videoStreams; }}
        public AvaliableVideoStreamsViewModel ViewModel { get { return streamingVideoVM; }}

        public void Start()
        {
            tabMainScreen = new StartPanelTabItem
            {
                Name = "streamingVideo",
                HeaderStyle = TabHeaderStyle.Image,
                Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/SecurityCamera.png")),
                ModelInstance = streamingVideoVM
            };
            AppState.AddStartPanelTabItem(tabMainScreen);
            IsRunning = true;
        }

        public void Pause()
        {
            
        }

        public void Stop()
        {
            IsRunning = false;
            AppState.RemoveStartPanelTabItem(tabMainScreen);
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;


        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }
    }
}
