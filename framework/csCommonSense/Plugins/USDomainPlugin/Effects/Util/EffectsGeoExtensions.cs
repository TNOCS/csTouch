using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using csShared.Utils;
using DotSpatial.Projections;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using Newtonsoft.Json.Linq;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csUSDomainPlugin.Effects.Util
{
    public static class EffectsGeoExtensions
    {
        public static Graphic ToGraphic(this string wkt, SpatialReference sourceSpatialReference = null, SpatialReference targetSpatialReference = null)
        {
            if (string.IsNullOrWhiteSpace(wkt) || !wkt.StartsWith("POLYGON ((")) return null;

            const string coordinatePrefix = "POLYGON ((";
            const string coordinateSuffix = "))";

            if (sourceSpatialReference == null)
            {
                sourceSpatialReference = new SpatialReference(4326);
            }

            if (targetSpatialReference == null)
            {
                targetSpatialReference = new SpatialReference(4326);
            }

            var src = ProjectionInfo.FromEpsgCode(sourceSpatialReference.WKID);
            var dest = ProjectionInfo.FromEpsgCode(targetSpatialReference.WKID);
            var sr = targetSpatialReference;

            var strCoordinates = wkt.Substring(coordinatePrefix.Length, wkt.Length - coordinateSuffix.Length - coordinatePrefix.Length);
            var coordinatePairs = strCoordinates.Split(',');
            var coordinates = coordinatePairs.Select(pair =>
            {
                var coordinate = pair.Split(' ');
                if (coordinate.Length != 2) throw new ArgumentException(string.Format("Cannot parse coordinate from WKT: {0}", pair));

                return new {X = float.Parse(coordinate[0], CultureInfo.InvariantCulture), Y = float.Parse(coordinate[1], CultureInfo.InvariantCulture)};
//                return new {X = float.Parse(coordinate[0], CultureInfo.InvariantCulture) + 155000, Y = float.Parse(coordinate[1], CultureInfo.InvariantCulture)+463000};
            });

            var polygon = new Polygon();
            polygon.SpatialReference = sr;
            if (src == dest)
            {
                var pointCollection = new PointCollection(coordinates.Select(c => new MapPoint(c.X, c.Y)));
                polygon.Rings.Add(pointCollection);
            }
            else
            {
                var points = coordinates.SelectMany(c => new double[2] {c.X, c.Y}).ToArray();
                Reproject.ReprojectPoints(points, null, src, dest, 0, points.Length / 2);
                var pointCollection = new PointCollection(Enumerable.Range(0, points.Length / 2).Select(i => new MapPoint(points[i * 2], points[(i * 2) + 1], sr)));
                polygon.Rings.Add(pointCollection);
            }

            var graphic = new Graphic();
            graphic.Geometry = polygon;
            graphic.Symbol = new SimpleFillSymbol
            {
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = 4,
                Fill = new SolidColorBrush(Colors.Blue) { Opacity = 0.8f }
            };

            return graphic;
        }

        public static GraphicCollection ToGraphic(this JObject geoJson, SpatialReference sourceSpatialReference = null)
        {
            var graphics = new GraphicCollection();

            if (sourceSpatialReference == null)
            {
                sourceSpatialReference = new SpatialReference(4326);
            }

            var src = ProjectionInfo.FromEpsgCode(sourceSpatialReference.WKID);
            var dest = ProjectionInfo.FromEpsgCode(4326);
            var sr = new SpatialReference(4326);

            var jFeatures = geoJson["features"] as JArray;
            if (jFeatures == null) return null;

            foreach (var jFeature in jFeatures.OfType<JObject>())
            {
                var jGeometry = jFeature["geometry"];
                if (jGeometry == null) continue;

                var geometryType = (string)jGeometry["type"];

                switch (geometryType)
                {
                    case "MultiPolygon":
                        var jPolygons = jGeometry["coordinates"] as JArray;
                        if (jPolygons == null) continue;

                        foreach (var jPolygon in jPolygons.OfType<JArray>())
                        {
                            if (jPolygon.Count == 0) continue;
                            var polygon = new Polygon();
                            polygon.SpatialReference = sr;

                            foreach (var jLinearRing in jPolygon.OfType<JArray>())
                            {
                                if (src == dest)
                                {
                                    var pointCollection = new PointCollection(jLinearRing.OfType<JArray>().Select(point=>new MapPoint((double)point[0], (double)point[1], sourceSpatialReference)));
                                    polygon.Rings.Add(pointCollection);
                                }
                                else
                                {
                                    // reproject
                                    var points = jLinearRing.OfType<JArray>().SelectMany(point=>new double[2]{(double)point[0], (double)point[1]}).ToArray();
                                    Reproject.ReprojectPoints(points, null, src, dest, 0, points.Length/2);
                                    var pointCollection = new PointCollection(Enumerable.Range(0, points.Length / 2).Select(i=>new MapPoint(points[i*2], points[(i*2)+1], sr)));
                                    polygon.Rings.Add(pointCollection);
                                }
                            }

                            var graphic = new Graphic();
                            graphic.Geometry = polygon;
                            graphic.Symbol = new SimpleFillSymbol
                            {
                                BorderBrush = new SolidColorBrush(Colors.Black),
                                BorderThickness = 4,
                                Fill = new SolidColorBrush(Colors.Blue) { Opacity = 0.8f}
                            };
                            
                            graphics.Add(graphic);
                        }

                        break;
                    default:
                        throw new NotSupportedException(string.Format("Geometry type {0} is not supported. Supported geometry types are: MultiPolygon.", geometryType));
                }

            }
            return graphics;
        }
    }
}
