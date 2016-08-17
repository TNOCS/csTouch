using csCommon.Utils.Collections;
using csEvents.Sensors;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Color = System.Windows.Media.Color;

namespace DataServer
{
    [ProtoContract]
    [DebuggerDisplay("PoI {Name}, Id={Id}, Type={PoiTypeId}, Category={NEffectiveStyle.Category}")]
    public class PoI : BaseContent
    {
        private bool enabled = true;
        private bool isSelected;

        public PoI()
        {
            Labels = new Dictionary<string, string>();
        }

        public event EventHandler<IModelPoiInstance> OnModelLoaded;

        public void RaiseOnModelLoaded(IModelPoiInstance pModel)
        {
            var handler = OnModelLoaded;
            if (handler != null) OnModelLoaded(this, pModel);
            if (this.Service != null)
            {
                this.Service.RaiseModelLoaded(this, pModel);
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected == value) return;
                isSelected = value;
                Data["Data.IsSelected"] = isSelected ? "1" : "0";
                //EV Removed, is already called later in the OnSelected event handling
                //UpdateAnalysisStyle();
                //TriggerUpdated();
                OnSelected(this);
            }
        }

        /// <summary>
        /// Z-index for ordering POI's on layer. High number are drawn on top of low numbers.
        /// </summary>
        public int ZIndex
        {
            get
            {
                object result;
                if (Data.TryGetValue("Zindex", out result)) return (int)result;
                return 0;
            }
            set
            {
                Data["Zindex"] = value;
            }
        }

        public new string PoiId // REVIEW TODO Added new.
        {
            get { return ContentId; }
            set { ContentId = value; }
        }

        [ProtoMember(17)]
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                NotifyOfPropertyChange(() => Enabled);
            }
        }

        public override string XmlNodeId
        {
            get { return "Poi"; }
        }

        [XmlIgnore]
        public double StrokeWidth
        {
            get { return NEffectiveStyle.StrokeWidth.Value; }
            set
            {
                if (Style == null)
                    Style = new PoIStyle();
                Style.StrokeWidth = value;
            }
        }

        [XmlIgnore]
        public Color FillColor
        {
            get { return NEffectiveStyle.FillColor.Value; }
            set
            {
                if (Style == null)
                    Style = new PoIStyle();
                Style.FillColor = value;
            }
        }

        [XmlIgnore]
        public Color StrokeColor
        {
            get { return NEffectiveStyle.StrokeColor.Value; }
            set
            {
                if (Style == null)
                    Style = new PoIStyle();
                Style.StrokeColor = value;
            }
        }

        [XmlIgnore]
        public DrawingModes DrawingMode
        {
            get { return NEffectiveStyle.DrawingMode.Value; }
            set
            {
                if (Style == null)
                    Style = new PoIStyle();

                Style.DrawingMode = value;
            }
        }

        [XmlIgnore]
        public string Category
        {
            get { return (NEffectiveStyle != null) ? NEffectiveStyle.Category : ""; }
            set
            {
                if (Style == null)
                    Style = new PoIStyle();
                Style.Category = value;
            }
        }



        private void OnSelected(PoI poi)
        {
            var handler = Selected;
            if (handler != null) handler(this, new PoiSelectedEventArgs { PoI = poi });
        }


        public override string ToString()
        {
            return Name + " (" + PoiTypeId + " - " + Id + ")";
        }


        public event EventHandler<PoiSelectedEventArgs> Selected;

        public void SetLabel(string key, string value, bool callLabelChanged = false)
        {
            string oldValue = "";
            if (callLabelChanged)
            {
                Labels.TryGetValue(key, out oldValue);
            }
            Labels[key] = value;
            NotifyOfPropertyChange(() => Labels);
            // TriggerLabelChanged MUST be called! 
            // If not: the label is not saved / synchronised
            // See also comment MetaLabel.cs from Arnould
            /* property callLabelChanged added for backward compatible; don't change working of old code.. */
            if (callLabelChanged) 
            {
                if (oldValue != value) TriggerLabelChanged(key, oldValue, value);
            }
        }

        

        public object Clone()
        {
            var clone = new PoI
            {
                Id          = Guid.NewGuid(), //<--
                Name        = Name, //<--
                InnerText   = InnerText, //<-- kan leeg zijn, dan negeer je hem
                Service     = Service,
                Orientation = Orientation, //<-- kan 0 zijn, dan negeer je hem
                Position    = Position, //<-- V
                Points      = Points, //<-- later
                ContentId   = ContentId, //<-- kan leeg zijn, dan negeer je hem
                PoiTypeId   = PoiTypeId, //<-- kan leeg zijn, dan negeer je hem
                Geometry    = Geometry,
                Layer       = Layer, //<-- kan leeg zijn, dan negeer je hem
                Date        = Date, //<-- kan leeg zijn, dan negeer je hem
                Style       = Style == null ? null : Style.Clone() as PoIStyle,
                MetaInfo    = MetaInfo,
                Labels      = new Dictionary<string, string>(Labels), //<-- V
                Data        = Data,
                MaxItems    = MaxItems //<-- kan 0 zijn, dan negeer je hem
            };
            return clone;
        }

        public void TriggerSelectedEvent(object sender, PoiSelectedEventArgs ea)
        {
            if (Selected != null) Selected(sender, ea);
        }

        public override XElement ToXml(ServiceSettings settings)
        {
            var res = ToXmlBase(settings);

            if (!Enabled) res.SetAttributeValue("Enabled", false);
            return res;
        }

        public override void FromXml(XElement res, string directoryName)
        {
            FromXmlBase(ref res, directoryName);
            Enabled = res.GetBool("Enabled", true);
        }

        public PoI GetInstance()
        {
            var result = Clone() as PoI;
            if (result == null) return null;
            result.Style = null;
            result.PoiType = this;
            result.PoiTypeId = ContentId;
            return result;
        }

        /// <summary>
        ///     Sets the sensor value.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="sensorName"></param>
        /// <param name="sensorValue"></param>
        public void SetSensorValue(DateTime dateTime, string sensorName, double sensorValue)
        {
            if (Sensors == null) Sensors = new SensorSet();
            if (!Sensors.ContainsKey(sensorName)) Sensors[sensorName] = new DataSet { Data = new ConcurrentObservableSortedDictionary<DateTime, double>() };
            Sensors[sensorName].Data[dateTime] = sensorValue;
        }

        /// <summary>
        ///     Get the sensor value at the focus time.
        /// </summary>
        /// <param name="sensorName"></param>
        /// <param name="defaultSensorValue"></param>
        /// <returns></returns>
        public double GetSensorValue(string sensorName, double defaultSensorValue = 0)
        {
            return Sensors == null || !Sensors.ContainsKey(sensorName)
                ? defaultSensorValue
                : Sensors[sensorName].FocusValue;
        }

        public static void FindModels(BaseContent p, ref List<Model> res)
        {
            if (p.PoiType == null) return;
            if (p.PoiType.Models != null)
            {
                foreach (var m in p.PoiType.Models)
                {
                    if (res.Count(k => k.Id == m.Id) == 0) res.Add(m.Clone());
                }
            }

            FindModels(p.PoiType, ref res);
        }

        public List<Model> FindModels()
        {
            var res = new List<Model>();
            FindModels(this, ref res);
            return res;
        }


        public bool OpenOnAdd;
    }
}