using csStreamingVideoPlugin.ViewModels;
using VlcVideoPlayer.Models;
using VlcVideoPlayer.ViewModels;

namespace csCommon.Plugins.StreamingVideoPlugin.ViewModels
{
    public class CsVideoStreamViewModel : VideoStreamViewModel
    {
        public CsVideoStreamViewModel(AvaliableVideoStreamsViewModel avaliableVideoStream, VideoStream videoStream) : 
            base(videoStream)
        {
            AvaliableVideoStreamsVM = avaliableVideoStream;
        }

        public AvaliableVideoStreamsViewModel  AvaliableVideoStreamsVM { get; private set; }

        
    }
}
