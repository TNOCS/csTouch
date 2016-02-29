using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using csShared.Utils;
#if !WINDOWS_PHONE
using csShared.Controls.Popups.MapCallOut;
#endif
using ProtoBuf;

namespace DataServer
{
    public interface IModelPoiInstance
    {        
        PoI Poi { get; set; }
        IModel Model { get; set; }  
#if !WINDOWS_PHONE
        IEditableScreen ViewModel { get; set; }
#endif
        void Start();
        void Stop();
    }

#if !WINDOWS_PHONE
    public interface IEditableScreen : IScreen
    {
        bool CanEdit { get; set; }
        MapCallOutViewModel CallOut { get; set; }
    }
#endif

    public interface IModel
    {
        string Type { get; }
        string Id { get; set; }
        PoiService Service { get; set; }
        DataServerBase DataServer { get; set; }
        Model Model { get; set; }
        object Layer { get; set; }
        IModelPoiInstance GetPoiInstance(PoI poi);
        void RemovePoiInstance(PoI poi);
        void Start();
        void Stop();
    }

    [ProtoContract]
    public class Model : PropertyChangedBase, IConvertibleXml
    {
        public IModelPoiInstance Instance { get; set; }

        private string type;

        [ProtoMember(1)]
        public string Type
        {
            get { return type; }
            set { type = value; NotifyOfPropertyChange(() => Type); }
        }

        [ProtoMember(3)]
        public string Id { get; set; }

        private List<ModelParameter> parameters = new List<ModelParameter>();

        [ProtoMember(2, OverwriteList = true)]
        public List<ModelParameter> Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }

        public XElement ToXml()
        {
            var result = new XElement("Model");
            result.SetAttributeValue("Type",Type);
            result.SetAttributeValue("Id", Id);

            if (Parameters == null || !Parameters.Any()) return result;
            var xp = new XElement("InputParameters");
            foreach (var p in Parameters)
            {
                var xElement = new XElement(p.Name) { Value = p.Value };
                xElement.Add(new XAttribute("source", p.Source));
                xp.Add(xElement);
            }
            result.Add(xp);
            return result;
        }

        public string XmlNodeId
        {
            get { return "Model"; }
        }

        public void FromXml(XElement el)
        {
            Type = el.GetString("Type");
            Id = el.GetString("Id");
            if (string.IsNullOrEmpty(Id)) Id = Type;

            var xp = el.Element("InputParameters");
            if (xp == null) return;
            Parameters = new List<ModelParameter>();
            foreach (var l in xp.Elements())
            {
                Parameters.Add(new ModelParameter
                {
                    Direction = ModelParameterDirection.input,
                    Name = l.Name.LocalName,
                    Value = l.Value,
                    Source = (ModelParameterSource) Enum.Parse(typeof(ModelParameterSource), l.GetString("source", ModelParameterSource.direct.ToString()))
                });
            }
        }

        /// <summary>
        /// Return the string belonging to the InputParameter.
        /// </summary>
        /// <param name="name">Name of the InputParameter</param>
        /// <returns>Value of the input parameter</returns>
        public string GetString(string name)
        {
            var p = Parameters.FirstOrDefault(k => k.Name == name);
            return (p != null) ? p.Value : name;
        }

        /// <summary>
        /// Return the color belonging to the InputParameter.
        /// </summary>
        /// <param name="name">Name of the InputParameter</param>
        /// <param name="defaultColor">Default color value</param>
        /// <returns>Value of the input parameter as color</returns>
        public Color GetColor(string name, Color defaultColor)
        {
            var p = GetString(name);
            try {
                return string.IsNullOrEmpty(p)
                    ? defaultColor
                    : ColorReflector.ToColorFromHex(p);
            }
            catch (SystemException) {
                return defaultColor;
            }
        }

        /// <summary>
        /// Return the color belonging to the InputParameter.
        /// </summary>
        /// <param name="name">Name of the InputParameter</param>
        /// <param name="defaultColor">Default color value</param>
        /// <returns>Value of the input parameter as color</returns>
        public Color GetColorFromName(string name, Color defaultColor)
        {
            var p = GetString(name).ToLowerInvariant().UppercaseFirst();
            try {
                return string.IsNullOrEmpty(p)
                    ? defaultColor
                    : ColorReflector.GetThisColor(p);
            }
            catch (SystemException) {
                return defaultColor;
            }
        }

        /// <summary>
        /// Return the integer value belonging to the InputParameter.
        /// </summary>
        /// <param name="name">Name of the InputParameter</param>
        /// <param name="defaultValue">Default integer value</param>
        /// <returns>Value of the input parameter</returns>
        public int GetInt(string name, int defaultValue)
        {
            var p = GetString(name);
            int result;
            return int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) 
                ? result
                : defaultValue;
        }

        /// <summary>
        /// Return the boolean value belonging to the InputParameter.
        /// </summary>
        /// <param name="name">Name of the InputParameter</param>
        /// <param name="defaultValue">Default integer value</param>
        /// <returns>Value of the input parameter</returns>
        public bool GetBoolean(string name, bool defaultValue)
        {
            var p = GetString(name);
            bool result;
            return bool.TryParse(p, out result) 
                ? result
                : defaultValue;
        }

        private readonly CultureInfo dutchCultureInfo = new CultureInfo("NL-nl");

        /// <summary>
        /// Return the double value belonging to the InputParameter.
        /// Tries to parse the double using the invariant culture and the Dutch culture (i.e. using comma)
        /// </summary>
        /// <param name="name">Name of the InputParameter</param>
        /// <param name="defaultValue">Default double value</param>
        /// <returns>Value of the input parameter</returns>
        public double GetDouble(string name, double defaultValue)
        {
            var p = GetString(name);
            double result;
            return double.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                ? result
                : double.TryParse(p, NumberStyles.Any, dutchCultureInfo, out result)
                    ? result
                    : defaultValue;
        }

        internal Model Clone()
        {
            var r = new Model {Type = Type, Id = Id, Parameters = new List<ModelParameter>()};
            foreach (var p in Parameters)
            {
                r.Parameters.Add(new ModelParameter
                {
                    Direction = p.Direction,
                    Name = p.Name,
                    Value = p.Value,
                    Source = p.Source
                });
            }
            return r;
        }
    }
}