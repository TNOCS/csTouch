using System;
using System.Linq;
using Caliburn.Micro;
using DataServer;
using Humanizer;
using Newtonsoft.Json;

namespace csModels.Flow
{

    public class FlowCollection : BindableCollection<FlowStop>
    {
         
        public string ToJson()
        {
            string json = JsonConvert.SerializeObject(this);
            return json;
        }

        internal void AddFlowStop(FlowStop fs)
        {
            var s = this.FirstOrDefault(k => k.Id == fs.Id);
            if (s != null)
            {
                s.Name = fs.Name;
                s.Eta = fs.Eta;
                s.Direction = fs.Direction;
                s.Etd = fs.Etd;
                s.Source = fs.Source;
                s.Target = fs.Target;
            }
            else
            {
                Add(fs);    
            }
            
        }
    }

    public class FlowStop : PropertyChangedBase
    {

        private string id;

        public string Id
        {
            get { return id; }
            set { id = value; }
        }
        

        private PoI target;

        [JsonIgnore]
        public PoI Target
        {
            get { return target; }
            set { target = value;  }
        }

        private PoI source;

         [JsonIgnore]
        public PoI Source
        {
            get { return source; }
            set { source = value; }
        }

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(()=>Name); }
        }

        private DateTime eta;

        public DateTime Eta
        {
            get { return eta; }
            set { eta = value; }
        }

        public string FlowString
        {
            get { return Eta.ToShortTimeString(); }
        }

        public string EtaString
        {
            get
            {
                var r = "ETA: " + Eta.ToShortTimeString() + " (" + Eta.Humanize() + ")";
                if (Etd>Eta) r+="\nETD:" + Etd.ToShortTimeString() + " (" + Etd.Humanize() + ")";
                return r;
            }
        }

        private double flowBarLeft;

        public double FlowBarLeft
        {
            get { return flowBarLeft; }
            set { flowBarLeft = value; NotifyOfPropertyChange(()=>FlowBarLeft); }
        }

        private double flowBarWidth;

        public double FlowBarWidth
        {
            get { return flowBarWidth; }
            set { flowBarWidth = value; NotifyOfPropertyChange(()=>FlowBarWidth); }
        }
        
        

        private DateTime etd;

        public DateTime Etd
        {
            get { return etd; }
            set { etd = value; }
        }

        private FlowDirection direction;

        public FlowDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }
        



    }

    public enum FlowDirection
    {
        target,
        source
    }
}