namespace csShared.Interfaces
{
    public interface IStartpanel
    {
        ITimelineManager TimelineManager { get; set; }        
        string Id { get; set; }
    }
}
