using System;

namespace DataServer
{
    public class PoiSelectedEventArgs : EventArgs
    {
        public PoI PoI { get; set; }
        //public InputDevice Device { get; set; }
        public bool NewValue { get; set; }
        public System.Windows.Point ScreenPos { get; set; }

    }
}