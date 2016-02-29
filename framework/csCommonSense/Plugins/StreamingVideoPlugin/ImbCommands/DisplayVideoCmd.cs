using System;
using VlcVideoPlayer.Models;


namespace csCommon.Plugins.StreamingVideoPlugin.ImbCommands
{
    public class DisplayVideoCmd : BaseCommand
    {
        
        public DisplayVideoCmd(ref string[] theParams)
        {
            if (theParams.Length != 3) throw new ArgumentException("Number of parameters doesnt match command");
            SecondScreenId = Convert.ToInt32(theParams[1]);
            Model = new VideoStream()
            {
                VideoUrl = theParams[2]
            };
        }

        public DisplayVideoCmd(VideoStream model, int pSecondScreenId)
        {
            Model = model;
            SecondScreenId = pSecondScreenId;
        }

        public int SecondScreenId { get; private set; }
        public VideoStream Model { get; private set; }

        public override string ToImbMessage()
        {
            return string.Format("{0}||{1}|{2}", typeof(DisplayVideoCmd).Name, SecondScreenId, Model.VideoUrl);
        }
    }
}
