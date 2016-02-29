
﻿using System;
using System.Collections.Generic;
using ProtoBuf;


namespace DataServer
{
    [ProtoContract]
    public class SensorData
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public string SensorName { get; set; }
        [ProtoMember(3)]
        public List<KeyValuePair<DateTime, double>> DataItemsDouble { get; set; }
        [ProtoMember(4)]
        public List<KeyValuePair<double, double>> DataItemsDoubleDouble { get; set; }
        [ProtoMember(5)]
        public csEvents.Sensors.Sensor Sensor { get; set; }
        //[ProtoMember(4)]
        //public List<KeyValuePair<DateTime, float>> DataItemsFloat { get; set; }
        //[ProtoMember(5)]
        //public List<KeyValuePair<DateTime, bool>> DataItemsBool { get; set; }
        //[ProtoMember(6)]
        //public List<KeyValuePair<DateTime, string>> DataItemsString { get; set; }
    }

    [ProtoContract]
    public class SensorDataCollection
    {
        public SensorDataCollection()
        {
            SensorDataItems = new List<SensorData>();
        }

        [ProtoMember(1)]
        public List<SensorData> SensorDataItems { get; set; }
    }
}

