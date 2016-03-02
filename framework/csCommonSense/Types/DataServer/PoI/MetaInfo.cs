using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Types.DataServer.PoI.Templates;
using csCommon.Utils;
using Newtonsoft.Json.Linq;
using OxyPlot;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DataServer
{
    [ProtoContract]
    [DebuggerDisplay("Section: {Section}, Title: {Title}, Label: {Label}")]
    public class MetaInfo : PropertyChangedBase, ITemplateObject
    {
        private string stringFormat = NumberFormats.GetFormatString(NumberFormats.UNFORMATTED);
        private string title;
        private string section;
        private string description;
        private double minValue = double.MinValue;
        private double stepSize = 1;

        [ProtoMember(1)]
        public string Label { get; set; }

        [ProtoMember(2)]
        public string Title
        {
            get { return title; }
            set
            {
                if (string.Equals(title, value)) return;
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        [ProtoMember(3)]
        public string Description
        {
            get { return description; }
            set
            {
                if (string.Equals(description, value)) return;
                description = value;
                NotifyOfPropertyChange(() => Description);
            }
        }

        [ProtoMember(4)]
        public MetaTypes Type { get; set; }
        [ProtoMember(5)]
        public double MaxValue { get; set; }

        [ProtoMember(6)]
        public double MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }

        [ProtoMember(15)]
        public double StepSize
        {
            get { return stepSize; }
            set { stepSize = value; }
        }

        private bool visibleInCallOut = true;
        [ProtoMember(7)]
        public bool VisibleInCallOut
        {
            get { return visibleInCallOut; }
            set
            {
                if (visibleInCallOut == value) return;
                visibleInCallOut = value;
                NotifyOfPropertyChange(() => VisibleInCallOut);
            }
        }
        [ProtoMember(8)]
        public string DefaultValue { get; set; }

        private bool isEditable = false; // Default false.
        [ProtoMember(9)]
        public bool IsEditable
        {
            get { return isEditable; }
            set
            {
                if (isEditable == value) return;
                isEditable = value;
                NotifyOfPropertyChange(() => IsEditable);
                NotifyOfPropertyChange(() => InEditMode);
            }
        }

        private bool isSearchable;
        [ProtoMember(10)]
        public bool IsSearchable
        {
            get { return isSearchable; }
            set
            {
                if (isSearchable == value) return;
                isSearchable = value;
                NotifyOfPropertyChange(() => IsSearchable);
            }
        }

        [ProtoMember(11, OverwriteList = true)]
        public List<string> Options { get; set; }

        [ProtoMember(13)]
        public string Section
        {
            get { return section; }
            set
            {
                if (section == value) return;
                section = value;
                NotifyOfPropertyChange(() => Section);
            }
        }

        [ProtoMember(14)]
        private bool editActive { get; set; }
        public bool EditActive
        {
            get { return editActive; }
            set
            {
                if (editActive == value) return;
                editActive = value;
                NotifyOfPropertyChange(() => InEditMode);
                NotifyOfPropertyChange(() => EditActive);
            }
        }

        private bool inEditMode;

        /// <summary>
        ///   Derived property, tells whether we can edit the current label.
        /// </summary>
        public bool InEditMode {
            get {
                return isEditable && (inEditMode || editActive);
            }
            set
            {
                if (inEditMode == value) return;
                inEditMode = value;
                NotifyOfPropertyChange(() => InEditMode);
            }
        }

        [ProtoMember(12)]
        public string StringFormat
        {
            get { return stringFormat; }
            set
            {
                if (string.Equals(stringFormat, value)) return;
                stringFormat = value;
                NotifyOfPropertyChange(() => StringFormat);
            }
        }

        public string XmlNodeId
        {
            get { return "MetaInfo"; }
        }

        public void FromXml(XElement element)
        {
            Label = element.GetString("Label");
            Title = element.GetString("Title").RestoreInvalidCharacters();
            Description = element.GetString("Description").RestoreInvalidCharacters();
            if (element.GetString("Type") != null)
            {
                Type = (MetaTypes)Enum.Parse(typeof(MetaTypes), element.GetString("Type"));
            }
            MaxValue         = element.GetDouble("MaxValue");
            MinValue         = element.GetDouble("MinValue");
            StepSize         = element.GetDouble("StepSize", 1);
            VisibleInCallOut = element.GetBool("VisibleInCallOut");
            DefaultValue     = element.GetString("DefaultValue");
            IsEditable       = element.GetBool("CanEdit", false); // CanEdit default false.
            IsSearchable     = element.GetBool("IsSearchable", true);
            StringFormat     = element.GetString("StringFormat");
            if (element.Attributes(XName.Get("NumberFormat")).Any())
            {
                StringFormat = element.GetString("NumberFormat"); // Legacy: old files store it under this key.                
            }
            Section    = element.GetString("Section");
            EditActive = element.GetBool("EditActive");
            var o      = element.GetString("Options");
            if (!string.IsNullOrEmpty(o))
            {
                Options = o.Trim(new[] { '[', ']' }).Split(',').ToList();
            }
        }

        public XElement ToXml()
        {
            var r = new XElement(XmlNodeId);
            if (!string.IsNullOrEmpty(Section)) r.SetAttributeValue("Section", Section);
            r.SetAttributeValue("Label", Label);
            r.SetAttributeValue("Title", Title.RemoveInvalidCharacters());
            if (!string.IsNullOrEmpty(Description)) r.SetAttributeValue("Description", Description.RemoveInvalidCharacters());
            r.SetAttributeValue("Type", Type);
            r.SetAttributeValue("VisibleInCallOut", VisibleInCallOut);
            if (!MaxValue.IsZero() && !double.IsNaN(MaxValue)) r.SetAttributeValue("MaxValue", MaxValue);
            if (!MinValue.IsZero() && !double.IsNaN(MinValue)) r.SetAttributeValue("MinValue", MinValue);
            if (!StepSize.IsZero() && !double.IsNaN(MinValue)) r.SetAttributeValue("StepSize", StepSize);
            if (!string.IsNullOrEmpty(DefaultValue)) r.SetAttributeValue("DefaultValue", DefaultValue);
            r.SetAttributeValue("CanEdit", IsEditable); // if (!CanEdit)   SdJ removed these assumptions on default values.
            r.SetAttributeValue("IsSearchable", IsSearchable); // if (!IsSearchable) 
            r.SetAttributeValue("EditActive", EditActive); // if (EditActive) 
            if (Options != null) 
                r.SetAttributeValue("Options", string.Join(",", Options));
            if (!string.IsNullOrEmpty(StringFormat)) r.SetAttributeValue("StringFormat", StringFormat);
            return r;
        }

        public string ToGeoJson()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{\"label\":\"{0}\",", Label);
            sb.AppendFormat("\"type\":\"{0}\",", Type);
            if (Type == MetaTypes.options && Options != null && Options.Count > 0)
                sb.AppendFormat("\"options\":[{0}],", string.Format("\"{0}\"", string.Join("\",\"", Options)));
            if (!string.IsNullOrEmpty(Title)) sb.AppendFormat("\"title\":\"{0}\",", Title.RemoveInvalidCharacters());
            if (!string.IsNullOrEmpty(Description)) sb.AppendFormat("\"description\":\"{0}\",", Description.RemoveInvalidCharacters());
            if (!string.IsNullOrEmpty(Section)) sb.AppendFormat("\"section\":\"{0}\",", Section);
            if (!string.IsNullOrEmpty(StringFormat)) sb.AppendFormat("\"stringFormat\":\"{0}\",", StringFormat);
            sb.AppendFormat("\"visibleInCallOut\":{0},", VisibleInCallOut.ToString().ToLower());
            sb.AppendFormat("\"canEdit\":{0},", IsEditable.ToString().ToLower());
            sb.AppendFormat("\"isSearchable\":{0},", IsSearchable.ToString().ToLower());
            if (!MaxValue.IsZero()) sb.AppendFormat(CultureInfo.InvariantCulture, "\"maxValue\":{0},", MaxValue);
            if (! (Math.Abs(MinValue - double.MinValue) < 0.00001)) sb.AppendFormat(CultureInfo.InvariantCulture, "\"minValue\":{0},", MinValue);
            sb.Remove(sb.Length - 1, 1); // remove the last ,
            sb.Append("}");
            return sb.ToString();
        }

        public IConvertibleGeoJson FromGeoJson(string geoJson, bool newObject)
        {
            var jObject = JObject.Parse(geoJson);
            return FromGeoJson(jObject, newObject);
        }

        public IConvertibleGeoJson FromGeoJson(JObject geoJsonObject, bool newObject = true)
        {
            var newMetaInfo = (newObject) ? new MetaInfo() : this;
            var toXml = newMetaInfo.ToXml(); // Copy the GeoJson attributes directly to XML.
            string legacyNumberFormat = null; // Old files store stringformat and numberformat, with the second one being correct. We need to do numberformat last, so the string format is updated correctly.
            foreach (var keyValuePair in geoJsonObject)
            {
                var key   = FirstLetterToUpper(keyValuePair.Key);
                var value = string.Equals(key, "Options", StringComparison.InvariantCultureIgnoreCase)
                    ? string.Join(",", keyValuePair.Value.ToObject<string[]>()) 
                    : keyValuePair.Value.ToObject<string>();
                if (key == "NumberFormat")
                {
                    legacyNumberFormat = value;
                }
                else
                {
                    toXml.SetAttributeValue(key, value);                    
                }
            }
            if (legacyNumberFormat != null)
            {
                toXml.SetAttributeValue("StringFormat", legacyNumberFormat);                
            }
            newMetaInfo.FromXml(toXml);
            return newMetaInfo;
        }

        private static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public ITemplateObject New()
        {
            return new MetaInfo();
        }

        public string Id { get { return Label; } }
    }

    [ProtoContract]
    public class MetaInfoCollection : List<MetaInfo> { }
}