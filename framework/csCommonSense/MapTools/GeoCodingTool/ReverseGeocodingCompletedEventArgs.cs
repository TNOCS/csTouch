using System;

namespace csCommon.MapTools.GeoCodingTool
{
    public class ReverseGeocodingCompletedEventArgs : EventArgs
    {
        public ReverseGeocodingCompletedEventArgs(string first, string result, Address address)
        {
            Address = address;
            First = first;
            Result = result;
        }

        public Address Address { get; private set; }
        public string First { get; private set; }
        public string Result { get; private set; }
    }
}