using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TestFoV.FieldOfViewService;

namespace csModels.FieldOfViewModel
{
    public static class Kml
    {
        private static readonly XNamespace Kmlns = "http://www.opengis.net/kml/2.2";

        public static void CreateKmlFile(string fileName, IEnumerable<Location> locations)
        {
            try
            {
                var doc = new XmlDocument();
                var xdoc = new XDocument(
                                  new XDeclaration("1.0", Encoding.UTF8.HeaderName, String.Empty),
                                  new XElement(Kmlns + "kml",
                                  new XElement(Kmlns + "Document",
                                  new XElement(Kmlns + "name", "None"),
                                  new XElement(Kmlns + "Style",
                                  new XAttribute("id", "defaultStyle"),
                                  new XElement(Kmlns + "LineStyle", new XElement(Kmlns + "color", "ffffffff"), new XElement(Kmlns + "colorMode", "normal"), new XElement(Kmlns + "width", 1)),
                                  new XElement(Kmlns + "PolyStyle", new XElement(Kmlns + "color", "880000ff"), new XElement(Kmlns + "colorMode", "normal"), new XElement(Kmlns + "fill", 1), new XElement(Kmlns + "outline", 1))),
                                  BuildGeographicPolylineType("FoV", locations))));
                doc.LoadXml(xdoc.Root.ToString());
                using (var writer = XmlWriter.Create(fileName))
                {
                    doc.WriteTo(writer);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not create KML file!!!!");
            }
        }

        private static XElement BuildGeographicPolylineType(string pName, IEnumerable<Location> locations)
        {
            var sb = new StringBuilder();
            foreach (var pnt in locations)
            {
                sb.AppendFormat("{0},{1},{2} ",
                    pnt.Longitude.ToString(CultureInfo.InvariantCulture),
                    pnt.Latitude.ToString(CultureInfo.InvariantCulture),
                    (pnt.Altitude * 100 /* convert to meter */ ).ToString(CultureInfo.InvariantCulture));
            }
            var coords = new XElement(Kmlns + "coordinates", sb.ToString());

            return new XElement(Kmlns + "Placemark",
                new XElement(Kmlns + "name", pName),
                new XElement(Kmlns + "description", pName),
                new XElement(Kmlns + "styleUrl", @"#defaultStyle"),
                new XElement(Kmlns + "LineString", coords)
            );
        }
    }
}
