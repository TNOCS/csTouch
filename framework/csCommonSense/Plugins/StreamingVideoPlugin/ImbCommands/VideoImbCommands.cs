using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csShared;
using IMB3;

namespace csCommon.Plugins.StreamingVideoPlugin.ImbCommands
{
    public class VideoImbCommands
    {
        public class DisplayVideoCmdEventArgs : EventArgs
        {
            public DisplayVideoCmdEventArgs(DisplayVideoCmd command)
            {
                Command = command;
            }

            public DisplayVideoCmd Command { get; private set; }
        }

        public delegate void DisplayVideoCmdEventHandler(DisplayVideoCmdEventArgs e);

        private TEventEntry videoImbChannelOut;
        private TEventEntry videoImbChannelIn;
        private string videoImbChannelName = ".streamingVideo";



        public event DisplayVideoCmdEventHandler DisplayVideoEvent;

        public csImb.csImb ImbBus { get; private set; }



        public VideoImbCommands(csImb.csImb imbBus)
        {
            ImbBus = imbBus;
            Init();
        }

        private void Init()
        {
            if (ImbBus != null && ImbBus.Imb != null)
            {
                videoImbChannelOut = ImbBus.Imb.Publish(ChannelName());
                videoImbChannelIn = ImbBus.Imb.Subscribe(ChannelName());
                videoImbChannelIn.OnNormalEvent += videoImbChannelIn_OnNormalEvent;
                ImbBus.ClientAdded += ImbBus_ClientAdded;
                ImbBus.ClientRemoved += ImbBus_ClientRemoved;
                ImbBus.IncomingEventObject += ImbBus_IncomingEventObject;
            }
        }

        void videoImbChannelIn_OnNormalEvent(TEventEntry aEvent, IMB3.ByteBuffers.TByteBuffer aPayload)
        {
            var cmdLine = aPayload.ReadString();
            var cmdParams = cmdLine.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
            if (cmdParams.Length < 1) return;
            switch (cmdParams[0])
            {
                case "DisplayVideoCmd":
                    var cmd = new DisplayVideoCmd(ref cmdParams);
                    if (DisplayVideoEvent != null)
                        DisplayVideoEvent(new DisplayVideoCmdEventArgs(cmd));
                    break;
            }
        }

        void ImbBus_IncomingEventObject(string channelName, object pObject)
        {
            
        }

        void ImbBus_ClientRemoved(object sender, csImb.ImbClientStatus e)
        {
           
        }

        void ImbBus_ClientAdded(object sender, csImb.ImbClientStatus e)
        {
           
        }

        private void SubscribeToChannel(int clientId)
        {
            
        }

        public void BroadcastCommand(BaseCommand command)
        {
            //ImbBus.SendMessage(ChannelName(), command.ToImbMessage());
            foreach (var imbClient in ImbBus.Clients)
               ImbBus.SendMessage(ChannelName(imbClient.Value.Id), command.ToImbMessage());
        }

        private string ChannelName(int clientId)
        {
            return String.Format("{0}{1}", clientId, videoImbChannelName);
        }

        private string ChannelName()
        {
            return ChannelName(ImbBus.Imb.ClientHandle);
        }

        private void Connect()
        {
            
        }
    }
}
