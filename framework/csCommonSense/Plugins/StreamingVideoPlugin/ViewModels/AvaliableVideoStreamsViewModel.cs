
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Navigation;
using Caliburn.Micro;
using csCommon.Plugins.StreamingVideoPlugin.ImbCommands;
using csCommon.Plugins.StreamingVideoPlugin.ViewModels;
using csGeoLayers;
using csShared.FloatingElements;
using csStreamingVideoPlugin.Views;
using csShared;
using VlcVideoPlayer.Models;
using VlcVideoPlayer.ViewModels;


namespace csStreamingVideoPlugin.ViewModels
{
    public class AvaliableVideoStreamsViewModel : Screen
    {
        private AvaliableVideoStreamsView view;
        private VideoImbCommands imbCommands;
        public AvaliableVideoStreamsViewModel(StreamingVideoPlugin model)
        {
            Plugin = model;
            AvaliableVideoStreamsVM = new ObservableCollection<CsVideoStreamViewModel>();
            DisplayedFullVideoStreamsVM = new ObservableCollection<CsVideoStreamPlayerFullViewModel>();
            DisplayedThumbnailVideoStreamsVM = new ObservableCollection<CsVideoStreamPlayerThumbnailViewModel>();
            imbCommands = new VideoImbCommands(AppStateSettings.Instance.Imb);
            
        }

        private void SyncWithModel()
        {
            Plugin.Model.VideoStreamList.ForEach(x => CreateVideoStreamViewModel(x));
            Plugin.Model.VideoStreamList.CollectionChanged += VideoStreamListCollectionChanged;
        }

        private CsVideoStreamViewModel CreateVideoStreamViewModel(VideoStream videoStream)
        {
            var vm = new CsVideoStreamViewModel(this, videoStream);
            AvaliableVideoStreamsVM.Add(vm);
            return vm;
        }

        private void RemoveVideoStreamViewModel(VideoStream videoStream)
        {
            var vm = AvaliableVideoStreamsVM.FirstOrDefault(x => Object.ReferenceEquals(x.Model , videoStream));
            if (vm != null) AvaliableVideoStreamsVM.Remove(vm);
        }

        void VideoStreamListCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) e.OldItems.Cast<VideoStream>().ForEach(RemoveVideoStreamViewModel);
            if (e.NewItems != null)  e.NewItems.Cast<VideoStream>().ForEach(x => CreateVideoStreamViewModel(x));
        }

        

        private StreamingVideoPlugin plugin;

        public StreamingVideoPlugin Plugin
        {
            get { return plugin; }
            private set { plugin = value; NotifyOfPropertyChange(() => Plugin); }
        }

        protected override void OnViewLoaded(object v)
        {
            base.OnViewLoaded(v);
            view = (AvaliableVideoStreamsView)v;
            SyncWithModel();
            foreach(var videoVM in AvaliableVideoStreamsVM)
                DisplayedThumbnailVideoStreamsVM.Add(new CsVideoStreamPlayerThumbnailViewModel(videoVM));
        }

        private CsVideoStreamPlayerThumbnailViewModel selectedThumbnail;

        public ObservableCollection<CsVideoStreamViewModel> AvaliableVideoStreamsVM { get; private set; }
        public ObservableCollection<CsVideoStreamPlayerThumbnailViewModel> DisplayedThumbnailVideoStreamsVM { get; private set; }
        public ObservableCollection<CsVideoStreamPlayerFullViewModel> DisplayedFullVideoStreamsVM { get; private set; }


        public void OpenInSecondScreen(VideoStreamPlayerViewModel viewModel, int secondScreenId)
        {
            if ((viewModel == null) || (viewModel.Owner == null)) return;
            imbCommands.BroadcastCommand(new DisplayVideoCmd(viewModel.Owner.Model, secondScreenId));
        }

        public CsVideoStreamPlayerThumbnailViewModel SelectedDisplayedThumbnailVideoStreamVM
        {
            get { return selectedThumbnail; }
            set
            {
                if (selectedThumbnail != value)
                {
                    selectedThumbnail = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        readonly Random rnd = new Random();

        public void OpenWindowForVideoStream(VideoStreamPlayerViewModel videoVM)
        {
           // ViewLocator.AddNamespaceMapping("VlcVideoPlayer.ViewModels", "csStreamingVideoPlugin.Views");

            ViewLocator.AddNamespaceMapping("csCommon.Plugins.StreamingVideoPlugin.ViewModels", "csStreamingVideoPlugin.Views");
            var fullVideoDisplay = new CsVideoStreamPlayerFullViewModel(videoVM.Owner)
            {
                VideoFill = VlcVideoPlayer.WpfVlcPlayer.EVideoFill.FillKeepAspectRatio
            };
            int height = (videoVM.VideoHeight == 0) ? 50 : (int)(videoVM.VideoHeight *0.30);
            int width = (videoVM.VideoWidth == 0) ? 50 : (int)(videoVM.VideoWidth*0.30);

            var mousePosition = Mouse.Position;
            var x = ((view.ActualWidth - 600) / view.ActualWidth) * mousePosition.X + 300;
            var fe = FloatingHelpers.CreateFloatingElement(String.Format("Video {0}", videoVM.Owner.Model.Description), 
                new System.Windows.Point(x, 300 + rnd.Next(200) - 100),
                new System.Windows.Size(width,height),
                fullVideoDisplay);
            if (mousePosition != new System.Windows.Point(0, 0))
            {
                fe.AnimationSpeed = new TimeSpan(0, 0, 0, 0, 500);
                //fe.StartSize= new Size(0,0);
                fe.OriginPosition = mousePosition;
                fe.OriginSize = new System.Windows.Size(0, 0);
            }
            fe.CanFullScreen = true;
            fe.CanScale = true;
            fe.Closed += (sender, e) =>
            {
                DisplayedFullVideoStreamsVM.Remove(fullVideoDisplay);
            };
            DisplayedFullVideoStreamsVM.Add(fullVideoDisplay);
            AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
        }


    }
}
