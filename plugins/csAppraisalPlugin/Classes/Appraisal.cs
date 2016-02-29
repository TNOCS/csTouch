using System;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using csShared.Utils;

namespace csAppraisalPlugin.Classes
{
    [Serializable]
    public class Appraisal : PropertyChangedBase, ICloneable, IConvertibleXml
    {
        private Guid id;
        private string title;
        private string description;
        private ImageSource image;
        private CriteriaList criteria = new CriteriaList();
        private string fileName;
        private bool isSelected;

        public Appraisal()
        {
            id = Guid.NewGuid();
        }

        public Appraisal(string title, string fileName) : this()
        {
            this.title = title;
            this.fileName = fileName;
        }

        [XmlAttribute]
        public Guid Id
        {
            get { return id; }
            set { id = value; NotifyOfPropertyChange(()=>Id); }
        }

        [XmlAttribute]
        public DateTime CreationDate { get; set; }

        [XmlAttribute]
        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }

        [XmlElement]
        public string Description
        {
            get { return description; }
            set { description = value; NotifyOfPropertyChange(()=>Description); }
        }

        [XmlIgnore]
        public ImageSource Image
        {
            get { return image; }
            set { image = value; NotifyOfPropertyChange(()=>Image); }
        }

        [XmlElement(Type = typeof(Criterion))]
        public CriteriaList Criteria
        {
            get { return criteria; }
            set { criteria = value; NotifyOfPropertyChange(()=>Criteria); }
        }

        [XmlElement]
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; NotifyOfPropertyChange(()=>FileName); }
        }

        [XmlElement]
        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; NotifyOfPropertyChange(()=>IsSelected); }
        }

        private bool isCompare;

        [XmlElement]
        public bool IsCompare
        {
            get { return isCompare; }
            set { isCompare = value; NotifyOfPropertyChange(()=>IsCompare); }
        }

        private Color color = Colors.Red;

        [XmlElement]
        public Color Color
        {
            get { return color; }
            set { color = value; NotifyOfPropertyChange(() => Color); NotifyOfPropertyChange(() => Brush); }
        }
        
        public Brush Brush {get{ return new SolidColorBrush(Color);}}

        public object Clone()
        {
            var a = new Appraisal();
            a.FromXml(ToXml());
            return a;
        }

        public string XmlNodeId
        {
            get { return "Appraisal"; }
        }

        public XElement ToXml()
        {
            return new XElement(XmlExtensions.ToXml(this)); // TODO REVIEW Check whether okay.
        }

        public void FromXml(XElement element)
        {
            XmlExtensions.FromXml<Appraisal>(element.ToString()); // TODO REVIEW Check whether okay.
        }
    }
}
