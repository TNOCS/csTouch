using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using DataServer;
using ProtoBuf;
using DataFormat = ProtoBuf.DataFormat;

namespace PoiServer.PoI
{
    public enum PaletteType
    {
        Gradient
    }

    [ProtoContract]
    public class Palette : PropertyChangedBase, IConvertibleXml
    {
        private List<PaletteStop> stops;

        [ProtoMember(1,OverwriteList = true)]
        public List<PaletteStop> Stops
        {
            get { return stops; }
            set { stops = value; NotifyOfPropertyChange(()=>Stops);}
        }

        private PaletteType type;

        [ProtoMember(2)]
        public PaletteType Type
        {
            get { return type; }
            set { type = value; NotifyOfPropertyChange(()=>Type); }
        }

        public string XmlNodeId
        {
            get { return "AnalysisMetaInfo"; }
        }

        public XElement ToXml()
        {
            var res = new XElement("Palette");
            res.SetAttributeValue("Type",Type.ToString());
         
            foreach (var s in Stops)
            {
                res.Add(s.ToXml());
            }            
            return res;
        }

        public void FromXml(XElement x) {
            if (x == null) return;
            Type = (PaletteType) Enum.Parse(typeof (PaletteType), x.GetString("Type", "Gradient"));
            var stps = x.Elements();
            Stops = new List<PaletteStop>();
            foreach (var s in stps)
            {
                var ps = new PaletteStop();
                ps.FromXml(s);
                Stops.Add(ps);
            }
        }

        public Palette Clone()
        {
            return new Palette {Type = Type, Stops = Stops.Select(k => k.Clone()).ToList()};
        }

        public LinearGradientBrush GradientBrush
        {
            get { return GetBrush(); }
        }

        public LinearGradientBrush GetBrush()
        {
            
            var gradientStops = new GradientStopCollection();
            foreach (var gs in Stops.Select(ps => new GradientStop {Color = ps.Color, Offset = ps.StopValue})) {
                gradientStops.Add(gs);
            }
            
            //rect.Fill = new LinearGradientBrush(stops);
            var linbrush = new LinearGradientBrush {GradientStops = gradientStops};
            linbrush.StartPoint = new Point(0,0);
            linbrush.EndPoint = new Point(1,0);
            return linbrush;
        }
    }

    [ProtoContract]
    public class PaletteStop : PropertyChangedBase, IConvertibleXml
    {

        //[ProtoMember(1, DataFormat = DataFormat.FixedSize)]
        //private string ColorString
        //{
        //    get { return Color.ToString(); }
        //    set { Color = ColorReflector.ToColorFromHex(value); }
        //}

        private Color color;
        
        public Color Color
        {
            get { return color; }
            set { color = value; NotifyOfPropertyChange(()=>Color); }
        }

        [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
        private string ColorString
        {
            get { return Color.ToString(); }
            set
            {
                Color = string.IsNullOrEmpty(value) ? new Color() : ColorReflector.ToColorFromHex(value);
            }
        }

        private double stopValue;

        [ProtoMember(2)]
        public double StopValue
        {
            get { return stopValue; }
            set { stopValue = value; NotifyOfPropertyChange(() => StopValue); }
        }

        public string XmlNodeId
        {
            get { return "PaletteStop"; }
        }

        public XElement ToXml()
        {
            var res = new XElement("PaletteStop");
            res.SetAttributeValue("StopValue", StopValue);
            res.SetAttributeValue("Color",Color);
            return res;
        }

        public void FromXml(XElement x)
        {
            Color = x.GetColor("Color");
            StopValue = x.GetDouble("StopValue");
        }

        public PaletteStop Clone()
        {
            return new PaletteStop { Color = Color, StopValue = StopValue };
        }
    }

    [ProtoContract]
    public class AnalysisMetaInfo
    {
        [ProtoMember(1)]
        public List<Highlight> Highlights { get; set; }

        public AnalysisMetaInfo()
        {
            Highlights = new List<Highlight>();
        }
    }

    [ProtoContract]
    public enum SelectionTypes
    {
        /// <summary>
        /// The Poi's Label contains an entry.
        /// </summary>
        Label,
        /// <summary>
        /// The data is contained in the sensor data.
        /// </summary>
        Sensor,
        /// <summary>
        /// Use the IsSelected PoI property
        /// </summary>
        Selected
    }

    [ProtoContract]
    public enum ValueTypes
    {
        /// <summary>
        /// String value
        /// </summary>
        String,
        /// <summary>
        /// A number
        /// </summary>
        Number,
        /// <summary>
        /// Percentage, in the range of [0-100], without % symbol. 
        /// </summary>
        Percentage,
        /// <summary>
        /// The same as Percentage, but any autogenerated legend will recompute the percentage range.
        /// </summary>
        PercentageLimitedRange
    }

    [ProtoContract]
    public enum VisualTypes { FillColor, Icon, Opacity, StrokeColor, StrokeWidth, SymbolSize }

    [ProtoContract]
    public enum ThresholdTypes {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        GreaterOrEqual,
        Greater,
        /// <summary>
        /// Only applicable to string values, returns true if the label contains the StringValue.
        /// </summary>
        Contains,
        /// <summary>
        /// Only applicable to string values, returns true if the label does not contain the StringValue.
        /// </summary>
        DoesNotContain
    }

    [ProtoContract]
    public enum HighlighterTypes { FilterThreshold, Highlight, Direct }


    
}
