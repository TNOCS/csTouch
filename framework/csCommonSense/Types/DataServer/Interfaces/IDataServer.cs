namespace DataServer
{
    public enum Mode
    {      
        client,
        server
    }

    public interface IDataServer
    {
        void Start(Mode _mode);
        void Stop();
        bool IsRunning { get; }

    }
}
