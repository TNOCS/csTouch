using System;
using System.Diagnostics;
using System.Xml.Serialization;
using Caliburn.Micro;

namespace csAppraisalPlugin.Classes
{
    [Serializable]
    [DebuggerDisplay("Function {Name}, selected={IsSelected}, default={IsDefault}, Id={Id}")]
    public class Function : PropertyChangedBase
    {
        private Guid id;
        private string name;
        private bool isSelected;
        private bool isDefault;

        public Function()
        {
            Id = Guid.NewGuid();
        }

        public Function(string name) : this()
        {
            this.name = name;
        }

        public Function(string name, bool isSelected) : this()
        {
            this.name = name;
            this.isSelected = isSelected;
        }

        [XmlAttribute]
        public Guid Id
        {
            get { return id; }
            set { id = value; NotifyOfPropertyChange(()=>Id); }
        }

        [XmlAttribute]
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(()=>Name); }
        }

        [XmlAttribute]
        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; NotifyOfPropertyChange(()=>IsSelected); }
        }

        [XmlAttribute]
        public bool IsDefault
        {
            get { return isDefault; }
            set { isDefault = value; NotifyOfPropertyChange(()=>IsDefault); }
        }

        public Function Clone()
        {
            var r = new Function {
                                     Id = Guid.NewGuid(),
                                     Name = Name
                                 };
            return r;
        }
    }
}