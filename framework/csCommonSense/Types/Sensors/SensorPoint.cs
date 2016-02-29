using System.Collections.Generic;
using ESRI.ArcGIS.Client.Geometry;

namespace csEvents.Sensors
{
    public class SensorPoint
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _id;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        
        public override string ToString()
        {
            return Name + " (" + Id + ")";
        }

        private Dictionary<string,DataSet> data = new Dictionary<string, DataSet>();

        public Dictionary<string,DataSet> Data
        {
            get { return data; }
            set { data = value;  }
        }

        private MapPoint location;

        public MapPoint Location
        {
            get { return location; }
            set { location = value;  }
        }
        
        

    }
}