//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Windows.Media;
//using Catfood.Shapefile;
//using DataServer;
//using SharpMap.Geometries;

// TODO This seems to be an obsolete piece of code. References are not correct and there appears to be hardcoded stuff in here.

namespace CbsShapeToPoiConverter { 
    internal class Program {
//        private static readonly List<DataService> DataServices = new List<DataService>(2);
//        private static readonly Dictionary<string, PoI> AllPois = new Dictionary<string, PoI>();
//
//        private static void Main(string[] args) {
//            // Pass the path to the shapefile in as the command line argument
//            if ((args.Length == 0) || (!File.Exists(args[0]))) {
//                Console.WriteLine("Usage: ShapeToPoiConverter [shapefile1..N.shp] [rd]");
//                Console.WriteLine("       rd: convert from Rijksdriehoek");
//                return;
//            }
//
//            var convertToRD = args.Last().ToLower().Equals("rd");
//
//            // construct shapefile with the path to the .shp file
//            foreach (var file in args.Where(File.Exists))
//            {
//                var ds = new DataService
//                {
//                    Id            = Guid.NewGuid(),
//                    ServiceId     = Guid.NewGuid(),
//                    ServiceName   = Path.GetFileNameWithoutExtension(file),
//                    LocalName     = Path.GetFileNameWithoutExtension(file),
//                    LocalFile     = Path.ChangeExtension(file, "poi"),
//                    PersistFolder = Path.GetDirectoryName(file)
//                };
//                DataServices.Add(ds);
//
//                PoiHelper.ToXmlDescription(ds, Path.ChangeExtension(file, "xml"));
//                var pois = ds.PointsOfInterest;
//
//                using (var shapefile = new Shapefile(file))
//                {
//                    Console.WriteLine("Processing {0}...", file);
//                    Console.WriteLine();
//
//                    // a shapefile contains one type of shape (and possibly null shapes)
//                    Console.WriteLine("Type: {0}, Shapes: {1:n0}", shapefile.Type, shapefile.Count);
//
//                    // a shapefile also defines a bounding box for all shapes in the file
//                    Console.WriteLine("Bounds: {0},{1} -> {2},{3}",
//                                      shapefile.BoundingBox.Left,
//                                      shapefile.BoundingBox.Top,
//                                      shapefile.BoundingBox.Right,
//                                      shapefile.BoundingBox.Bottom);
//                    Console.WriteLine();
//
//                    // enumerate all shapes
//                    foreach (var shape in shapefile)
//                    {
//                        Console.WriteLine("----------------------------------------");
//                        Console.WriteLine("Shape {0:n0}, Type {1}", shape.RecordNumber, shape.Type);
//
//                        var poi = new PoI { FillColor = Colors.Gray, CanEdit = false };
//
//                        // each shape may have associated metadata
//                        var metadataNames = shape.GetMetadataNames();
//                        if (metadataNames != null)
//                        {
//                            Console.WriteLine("Metadata:");
//                            var gemeenteNaam = string.Empty;
//                            var wijkNaam = string.Empty;
//                            var buurtNaam = string.Empty;
//                            foreach (var metadataName in metadataNames)
//                            {
//                                switch (metadataName)
//                                {
//                                    case "gm_naam": gemeenteNaam = shape.GetMetadata(metadataName); break;
//                                    case "wk_naam": wijkNaam     = shape.GetMetadata(metadataName); break;
//                                    case "bu_naam": buurtNaam    = shape.GetMetadata(metadataName); break;
//                                }
//                                poi.Labels.Add(metadataName, shape.GetMetadata(metadataName));
//                                //Console.WriteLine("{0}={1} ({2})", metadataName, shape.GetMetadata(metadataName),
//                                //shape.DataRecord.GetDataTypeName(shape.DataRecord.GetOrdinal(metadataName)));
//                            }
//                            poi.Name = string.IsNullOrEmpty(wijkNaam) && string.IsNullOrEmpty(buurtNaam)
//                                           ? gemeenteNaam
//                                           : string.IsNullOrEmpty(buurtNaam)
//                                                 ? string.Format("{0}, {1}", gemeenteNaam, wijkNaam)
//                                                 : string.Format("{0}, {1}", gemeenteNaam, buurtNaam);
//                            Console.WriteLine();
//                        }
//
//                        // cast shape based on the type
//                        switch (shape.Type)
//                        {
//                            case ShapeType.Point:
//                                // a point is just a single x/y point
//                                var shapePoint = shape as ShapePoint;
//                                if (shapePoint == null) continue;
//                                Console.WriteLine("Point={0},{1}", shapePoint.Point.X, shapePoint.Point.Y);
//                                break;
//
//                            case ShapeType.Polygon:
//                                // a polygon contains one or more parts - each part is a list of points which
//                                // are clockwise for boundaries and anti-clockwise for holes 
//                                // see http://www.esri.com/library/whitepapers/pdfs/shapefile.pdf
//                                var shapePolygon = shape as ShapePolygon;
//                                if (shapePolygon == null) continue;
//                                foreach (var part in shapePolygon.Parts)
//                                {
//                                    Console.WriteLine("Polygon part:");
//                                    poi.DrawingMode = DrawingModes.Polygon;
//                                    poi.StrokeColor = Colors.Gray;
//                                    poi.FillColor = Colors.Transparent;
//                                    poi.StrokeWidth = 3;
//                                    poi.Points = new List<Point>(shapePolygon.Parts.Count);
//                                    foreach (var point in part)
//                                    {
//                                        if (convertToRD)
//                                        {
//                                            double lat, lon;
//                                            ConvertRdCoordinates.RDToLatLong((int)point.X, (int)point.Y, out lat, out lon);
//                                            //Console.WriteLine("{0:000.000}, {1:000.000}", lon, lat);
//                                            poi.Points.Add(new Point(lon, lat));
//                                        }
//                                        else 
//                                        {
//                                            //Console.WriteLine("{0:000.000}, {1:000.000}", point.X, point.Y);
//                                            poi.Points.Add(new Point(point.X, point.Y));
//                                        }
//                                    }
//                                    //Console.WriteLine();
//                                }
//                                break;
//
//                        }
//
//                        if (AllPois.ContainsKey(poi.Name))
//                            Console.WriteLine("Warning: key {0} already defined!", poi.Name);
//                        else AllPois.Add(poi.Name, poi);
//                        pois.Add(poi);
//
//                        Console.WriteLine("----------------------------------------");
//                        Console.WriteLine();
//                    }
//                }
//            }
//
//            if (DataServices.Count > 1) AnalyseerGemeenteEnWijk(); // Dealing with gemeente and wijk
//
//            Save();
//
//            Console.WriteLine("Done");
//            Console.WriteLine();
//        }
//
//        private static readonly Dictionary<string, PurposeAreaYearBin> WijkBins = new Dictionary<string, PurposeAreaYearBin>();
//        private static readonly Dictionary<string, PurposeAreaYearBin> GemeenteBins = new Dictionary<string, PurposeAreaYearBin>();
//
//        private const string GemeenteWijkFunctieOppJaarSql =
//@"SELECT 
//  wijk_2011_gn1.gm_naam, 
//  wijk_2011_gn1.wk_naam, 
//  verblijfsobjectgebruiksdoel.gebruiksdoelverblijfsobject, 
//  verblijfsobjectactueelbestaand.oppervlakteverblijfsobject, 
//  pandactueelbestaand.bouwjaar
//FROM 
//  public.wijk_2011_gn1, 
//  public.adres, 
//  public.verblijfsobjectactueelbestaand, 
//  public.verblijfsobjectgebruiksdoel, 
//  public.verblijfsobjectpandactueel, 
//  public.pandactueelbestaand
//WHERE 
//  wijk_2011_gn1.wk_naam = adres.wijknaam AND
//  wijk_2011_gn1.gm_naam = adres.gemeentenaam AND
//  adres.adresseerbaarobject = verblijfsobjectactueelbestaand.identificatie AND
//  verblijfsobjectgebruiksdoel.identificatie = verblijfsobjectactueelbestaand.identificatie AND
//  verblijfsobjectpandactueel.identificatie = verblijfsobjectactueelbestaand.identificatie AND
//  verblijfsobjectpandactueel.gerelateerdpand = pandactueelbestaand.identificatie;
//";
//        /// <summary>
//        /// Vraag nmbrRecord panden op in een provincie met een offset.
//        /// </summary>
//        /// <param name="nmbrRecord"></param>
//        /// <param name="offset"></param>
//        /// <returns></returns>
//        public static string GemeenteWijkFunctieOppJaarSqlCommand(int nmbrRecord, int offset)
//        {
//            var cmd = GemeenteWijkFunctieOppJaarSql.TrimEnd(new[] { ';' });
//            return string.Format("{0} LIMIT {1} OFFSET {2}", cmd, nmbrRecord, offset);
//        }
//
//     
//        private static void Save()
//        {
//            foreach (var ds in DataServices)
//                PoiHelper.ToXml(ds);
//        }
    }
}