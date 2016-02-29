using System;

namespace DataServer
{
    public class LabelChangedEventArgs : EventArgs
    {
        public string Label { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}