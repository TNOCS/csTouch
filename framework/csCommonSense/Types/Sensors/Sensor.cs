using Caliburn.Micro;
using ProtoBuf;

namespace csEvents.Sensors
{
    [ProtoContract]
    public class Sensor : PropertyChangedBase
    {
        [ProtoMember(1)] 
        public string Id { get; set; }

        //[ProtoMember(2)]
        private string _description;

        [ProtoMember(2)]
        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyOfPropertyChange(() => Description); }
        }

    }
}