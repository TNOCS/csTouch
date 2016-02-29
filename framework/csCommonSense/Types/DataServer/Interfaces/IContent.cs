using System;
using csCommon.Types.DataServer.Interfaces;

namespace DataServer
{
    public interface IContent : IConvertibleXmlWithSettings
    {
        Guid Id { get; set; }
        long RevisionId { get; set; }
        string UserId { get; set; }
        int Priority { get; set; }
        DateTime Date { get; set; }
        string TimelineString { get;}
        event EventHandler<ChangedEventArgs> Changed; 
        /// <summary>
        ///     Indicates that the object has just been sent across, so all property or collection changes should not trigger sending the object again.
        ///     This is in order to avoid infinite loops i.e. a client changes something, which is sent to a server, which updates the object,
        ///     thereby causing it to be send again and so on and so forth.
        ///     This property should be set to true before sending, and to false upon receipt.
        /// </summary>
        bool IsInTransit { get; set; }
    }

    public class ChangedEventArgs : EventArgs
    {
        
    }
}