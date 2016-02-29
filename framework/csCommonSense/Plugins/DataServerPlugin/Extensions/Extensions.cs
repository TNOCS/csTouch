using System.IO;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client.Geometry;

namespace csDataServerPlugin.Extensions
{
    public static class Extensions
    {

        public static string GetShapeFileBaseName(this FileInfo file)
        {
            
            return file.Name.Replace(".shp", "").Replace(".SHP","").Replace("_"," ");
        }

        public static CoordinateType GetCoordinateType(this Catfood.Shapefile.Shapefile shapeFile, string file)
        {
            //var projFile = Path.ChangeExtension(file, "prj");
            //if (File.Exists(projFile))
            //{
            //    var wkt = File.ReadAllLines(projFile);
            //    var proj = string.Join(" ", wkt);
            //    var cs = CoordinateSystemWktReader.Parse(proj);
            //    Console.WriteLine(cs);
            //}
            return CoordinateType.Rd;
        }

         public static MapPoint ToMapPoint(this Position point)
         {
             if (point == null) return new MapPoint(0,0);
             return new MapPoint
             {
                 X = point.Longitude,
                 Y = point.Latitude
             };
         }

         public static SensorCollection ToSensorCollection(this SensorSet sensorSet) {
             var sc = new SensorCollection { Title = sensorSet.Title };
             sc.AddRange(sensorSet.Values);
             return sc;
         }
    }
}