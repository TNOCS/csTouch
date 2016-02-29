using System.Globalization;
using Caliburn.Micro;
using csEvents.Sensors;
#if !WINDOWS_PHONE
using csShared.Documents;
#endif
namespace DataServer
{
    public class MetaLabel : PropertyChangedBase
    {
        public MetaInfo Meta { get; set; }

        private BaseContent poI;

        public BaseContent PoI
        {
            get { return poI; }
            set
            {
                poI = value;
                NotifyOfPropertyChange(() => PoI);
                //PoI.LabelChanged += (e, s) => NotifyOfPropertyChange(() => Data);
            }
        }

#if !WINDOWS_PHONE

        private Document document;

        public Document Document
        {
            get { return document; }
            set { document = value; NotifyOfPropertyChange(()=>Document); }
        }
#endif

        public DataSet Sensor
        {
            get
            {
                return PoI.Sensors.ContainsKey(Meta.Label) ? PoI.Sensors[Meta.Label] : null;
            }
            set
            {
                if (PoI.Sensors[Meta.Label] == value) return;
                PoI.Sensors[Meta.Label] = value;
                NotifyOfPropertyChange(() => Sensor);
            }
        }

        public string Data
        {
            get
            {
                var useData = Meta.Label.StartsWith("Data.");
                if ((useData && !PoI.Data.ContainsKey(Meta.Label)) || !PoI.Labels.ContainsKey(Meta.Label)) return null;
                var label = useData ? PoI.Data[Meta.Label].ToString() : PoI.Labels[Meta.Label];
                if (string.IsNullOrEmpty(Meta.StringFormat)) return label;
                switch (Meta.Type)
                {
                    case MetaTypes.number:
                        double result;
                        return double.TryParse(label, NumberStyles.Any, CultureInfo.InvariantCulture, out result) 
                            ? string.Format(CultureInfo.InvariantCulture, Meta.StringFormat, result) 
                            : label;
                    default:
                        // SdJ added this so we can also format text.
                       return string.Format(CultureInfo.InvariantCulture, Meta.StringFormat, label);
                }
            }
            set
            {
                var useData = Meta.Label.StartsWith("Data.");
                if ((useData && PoI.Data.ContainsKey(Meta.Label) && string.Equals(PoI.Data[Meta.Label].ToString(), value)) 
                    || !PoI.Labels.ContainsKey(Meta.Label) || string.Equals(PoI.Labels[Meta.Label], value)) return;
                string oldValue;
                if (useData)
                {
                    oldValue = PoI.Data[Meta.Label].ToString();
                    PoI.Data[Meta.Label] = value;
                    PoI.OnDataChanged(Meta.Label, oldValue, value);
                }
                else
                {
                    oldValue = PoI.Labels[Meta.Label];
                    PoI.Labels[Meta.Label] = value;
                    // TODO Arnoud: als ik deze commentarieer, dan doet hij het goed. Anders na een edit ben ik alles kwijt.
                    PoI.TriggerLabelChanged(Meta.Label, oldValue, value);
                }
                NotifyOfPropertyChange(() => Data);
            }
        }
    }
}
