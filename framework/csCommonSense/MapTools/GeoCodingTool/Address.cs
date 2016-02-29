using System;
using System.Xml.Linq;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.MapTools.GeoCodingTool
{
    public class Address
    {
        public string StreetNumber     { get; set; }
        public string Route            { get; set; }
        public string Locality         { get; set; }
        public string Country          { get; set; }
        public string PostalCode       { get; set; }
        public string FormattedAddress { get; set; }
        public MapPoint Position       { get; set; }

        public Address() {} 

        public Address(XContainer e) {
            if (e == null) return;
            var element = e.Element("formatted_address");
            if (element != null) FormattedAddress = element.Value;
            foreach (var el in e.Elements("address_component"))
            {
                var type = el.Element("type");
                if (type != null) {
                    XElement xElement;
                    switch (type.Value)
                    {
                        case "street_number":
                            xElement = el.Element("long_name");
                            if (xElement != null) StreetNumber = xElement.Value;
                            break;
                        case "route":
                            xElement = el.Element("long_name");
                            if (xElement != null) Route = xElement.Value;
                            break;
                        case "locality":
                            xElement = el.Element("long_name");
                            if (xElement != null) Locality = xElement.Value;
                            break;
                        case "postal_code":
                            xElement = el.Element("long_name");
                            if (xElement != null) PostalCode = xElement.Value;
                            break;
                        case "country":
                            xElement = el.Element("long_name");
                            if (xElement != null) Country = xElement.Value;
                            break;
                    }
                }
                Console.WriteLine(el);
            }
        }
    }
}