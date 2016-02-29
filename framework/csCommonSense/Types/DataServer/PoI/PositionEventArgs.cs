using System;

namespace DataServer
{
    public class PositionEventArgs : EventArgs
    {
        public Position Position { get; set; }
    }
}