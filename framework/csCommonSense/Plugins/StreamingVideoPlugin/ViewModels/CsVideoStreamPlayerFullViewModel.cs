using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VlcVideoPlayer.ViewModels;

namespace csCommon.Plugins.StreamingVideoPlugin.ViewModels
{
    public class CsVideoStreamPlayerFullViewModel : VideoStreamPlayerFullViewModel
    {
        public CsVideoStreamPlayerFullViewModel(VideoStreamViewModel owner) : base(owner)
        {

        }

        public void TakeScreenshot()
        {
            //TakeSnapshot();
        }

       


    }
}
