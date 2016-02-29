using System;

namespace csEvents
{
    public class NewEventArgs : EventArgs
    {
        public NewEventArgs()
        {
        }

        public NewEventArgs(IEvent e)
        {
            this.e = e;
        }

        /// <summary>
        /// IEvent implementation
        /// </summary>
        public IEvent e { get; set; }
    }
}