using Caliburn.Micro;
using ProtoBuf;
using System.Diagnostics;

namespace DataServer
{
    [ProtoContract]
    [DebuggerDisplay("{Name} = {Value}, {Direction}, {Source}")]
    public class ModelParameter : PropertyChangedBase
    {
        private ModelParameterDirection direction;

        [ProtoMember(1)]
        public ModelParameterDirection Direction
        {
            get { return direction; }
            set { direction = value; NotifyOfPropertyChange(() => Direction); }
        }

        private string name;

        [ProtoMember(2)]
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(() => Name); }
        }

        private string pValue;

        [ProtoMember(3)]
        public string Value
        {
            get { return pValue; }
            set { pValue = value; NotifyOfPropertyChange(() => Value); }
        }

        private ModelParameterSource source;

        [ProtoMember(4)]
        public ModelParameterSource Source
        {
            get { return source; }
            set { source = value; NotifyOfPropertyChange(() => Source); }
        }
    }
}