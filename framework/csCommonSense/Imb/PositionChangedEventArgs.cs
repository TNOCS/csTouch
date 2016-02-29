using System;

namespace csImb
{
    public class PositionChangedEventArgs : EventArgs
    {
        public PositionChangedEventArgs(Position position, ImbClientStatus status)
        {
            ClientStatus = status;
            Position = position;
        }

        public ImbClientStatus ClientStatus { get; set; }

        public Position Position { get; private set; }
    }
}