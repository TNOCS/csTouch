using System;

namespace DataServer
{
    [Flags]
    public enum MessageType : byte
    {
        /// <summary>
        /// Create a new object
        /// </summary>
        Create = 0,
        /// <summary>
        /// Update an existing object
        /// </summary>
        Update = 1,
        /// <summary>
        /// Delete an existing object
        /// </summary>
        Delete = 2,
        /// <summary>
        /// Reset - update all objects
        /// </summary>
        Reset = 4,
        /// <summary>
        /// Request a reset after noticing that you are out-of-sync
        /// </summary>
        RequestReset = 8,
        /// <summary>
        /// Server communicating the current synchronization state. 
        /// Most likely scenario: after receiving a message of a imbClient (that can edit) and who is out-of-sync
        /// </summary>
        SyncState = 16,
        /// <summary>
        /// Move an object in a collection
        /// </summary>
        Move = 32
    }
}