namespace csImb
{
    public delegate void ClientChangedEventHandler(object sender, ImbClientStatus e);
    public delegate void MediaReceivedEventHandler(object sender, MediaReceivedEventArgs e);
    public delegate void CommandReceivedEventHandler(object sender, Command c);

    public class MediaReceivedEventArgs
    {
        public Media Media { get; set; }
        public ImbClientStatus Client { get; set; }
        public ImbClientStatus Sender { get; set; }
    }
}