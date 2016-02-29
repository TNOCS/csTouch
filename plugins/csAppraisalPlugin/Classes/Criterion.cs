using System;
using System.Diagnostics;
using System.Xml.Serialization;
using Caliburn.Micro;

namespace csAppraisalPlugin.Classes
{
    [Serializable]
    [DebuggerDisplay("Criterion {Title}, value={AssignedValue}, weight={Weight}, Id={Id}")]
    public class Criterion : PropertyChangedBase
    {
        private Guid id;
        private string title;
        private double weight;
        private double assignedValue;

        public Criterion()
        {
            id = Guid.NewGuid();
        }

        public Criterion(Guid id)
        {
            this.id = id;
        }

        public Criterion(Guid id, string title) : this(id)
        {
            this.title = title;
        }

        public Criterion(string title) : this()
        {
            this.title = title;
        }

        public Criterion(string title, double weight, double assignedValue) : this(title)
        {
            this.weight = weight;
            this.assignedValue = assignedValue;
        }

        [XmlAttribute]
        public Guid Id
        {
            get { return id; }
            set { id = value; NotifyOfPropertyChange(() => Id); }
        }

        [XmlAttribute]
        public string Title
        {
            get { return title; }
            set
            {
                if (value == title) return;
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        [XmlIgnore]
        public string Name { get { return title; } }

        [XmlAttribute]
        public double Weight
        {
            get { return weight; }
            set { weight = value; NotifyOfPropertyChange(()=>Weight); }
        }

        [XmlAttribute]
        public double AssignedValue
        {
            get { return assignedValue; }
            set { assignedValue = value; NotifyOfPropertyChange(() => AssignedValue); }
        }
    }
}