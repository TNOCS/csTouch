using System;
using Caliburn.Micro;

namespace csImb
{
    public class Message : PropertyChangedBase
    {
        private int _senderId;

        public int SenderId
        {
            get { return _senderId; }
            set { _senderId = value; }
        }

        private string _senderName;

        public string SenderName
        {
            get { return _senderName; }
            set { _senderName = value; }
        }

        private DateTime _dateTime;

        public DateTime DateTime
        {
            get { return _dateTime; }
            set { _dateTime = value; }
        }

        
    }
}
