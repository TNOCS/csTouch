using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Caliburn.Micro;
using ProtoBuf;


namespace DataServer
{
    

    [ProtoContract]
    public class Event : BaseContent
    {
        
        public override string XmlNodeId
        {
            get
            {
                return "Event";
            }
        }

       

        private Guid poiId;

        [ProtoMember(10)]
        public new Guid PoiId  // REVIEW TODO Added new.
        {
            get { return poiId; }
            set { poiId = value; NotifyOfPropertyChange(()=>PoiId); }
        }

          
        

        private PoI poI;

        public PoI PoI
        {
            get { return poI; }
            set { poI = value; NotifyOfPropertyChange(()=>PoI); }
        }

        private string icon;

        public string Icon
        {
            get { return icon; }
            set { icon = value; NotifyOfPropertyChange(()=>Icon); }
        }

        private BindableCollection<BitmapSource> images;

        public BindableCollection<BitmapSource> Images
        {
            get { return images; }
            set { images = value;
                NotifyOfPropertyChange(() => Images);
            }
        }
        
        

       
        
        private byte[] iconByteArray;

	public byte[] IconByteArray
	{
		get { return iconByteArray;}
		set { iconByteArray = value; NotifyOfPropertyChange(()=>IconByteArray);}
	}

        private BitmapSource pictue;

	public BitmapSource Picture
	{
		get { return pictue;}
		set { pictue = value; NotifyOfPropertyChange(()=>Picture);}
	}
	
	

       

        public override string ToString()
        {
            return Name;
        }

        public override XElement ToXml(ServiceSettings settings)
        {

            var res = ToXmlBase(settings);
            if (!string.IsNullOrEmpty(ContentId)) res.SetAttributeValue("EventId", ContentId);
            if (!string.IsNullOrEmpty(PoiTypeId)) res.SetAttributeValue("EventTypeId", PoiTypeId);
            if (!string.IsNullOrEmpty(Icon)) res.SetAttributeValue("Icon",Icon);
            if (PoiId!=Guid.Empty) res.SetAttributeValue("PoiId", PoiId);
            
            return res;
        }


        public override void FromXml(XElement res, string directoryName)
        {
            FromXmlBase(ref res,directoryName);
            var n = res.GetString("Name");
            ContentId = res.GetString("EventId");
            if (string.IsNullOrEmpty(ContentId)) ContentId = n;
            PoiTypeId = res.GetString("EventTypeId");
            Icon = res.GetString("Icon");
            


            if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(n)) Name = n;
            if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(ContentId)) Name = ContentId;
        }

        public Event Clone()
        {
            Event e = new Event();
            e.ContentId = e.ContentId;
            e.PoiTypeId = e.PoiTypeId;
            e.PoiType = e;
            e.Style = Style.Clone() as PoIStyle;
            
            e.Labels = new Dictionary<string, string>();
            foreach (var lk in Labels)
            {
                e.Labels[lk.Key] = lk.Value;
            }
            

            return e;
        }
        

    }
}