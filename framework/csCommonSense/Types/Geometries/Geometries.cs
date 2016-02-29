using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Catfood.Shapefile;
using csCommon.Types.DataServer.PoI.IO;
using csShared.Utils;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;

namespace csCommon.Types.Geometries
{
    // TODO 1 Integrate into the workflow of conversion! :) (Why save WktText with PoIs instead of the real geometry!?)
    // TODO 2 I don't get it. 
    // We use ESRI polygons, there are C# standard points e.d. from System.Drawing and System.Windows,
    // and we add our own geometries with hardly any added functionalities.
    // TODO 3 Now we have Equals and HashCode, but we have public access to the data that determines the hash code. Change it and objects get lost.

    public enum GeometryEnum
    {
        Point,
        LineString,
        Polygon,
        MultiPoint,
        MultiLineString,
        MultiPolygon
    }

    public abstract class BaseGeometry
    {
        // public GeometryEnum Genum { get; set; } // TODO This is weird. Why set an enum AND create derived classes.

        public virtual bool IsRegion { get {  return false; } }

        public abstract bool Contains(Point p);

        public new abstract bool Equals(object other);
    };

    public class Point : BaseGeometry
    {
        private readonly double x;
        private readonly double y;
        private readonly double z;

        public Point(double x, double y, double z = 0)
        {
            this.z = z;
            this.y = y;
            this.x = x;
        }

        public double X
        {
            get { return x; }
        }

        public double Y
        {
            get { return y; }
        }

        public double Z
        {
            get { return z; }
        }

        public override bool Contains(Point p)
        {
            return false;
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((Point) other);
        }

        protected bool Equals(Point other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode*397) ^ Y.GetHashCode();
                hashCode = (hashCode*397) ^ Z.GetHashCode();
                return hashCode;
            }
        }
    }

    public class LineString : BaseGeometry
    {
        public readonly List<Point> Line = new List<Point>();

        public override bool Contains(Point p)
        {
            if (Line.Count < 3) return false;
            if (!Line.First().Equals(Line.Last())) return false; // Not a closed shape.

// METHOD USING GRAPHICS REGION
//
//            var points = new PointF[Line.Count];
//            var types = new byte[Line.Count];
//            int index = 0;
//            foreach (Point point in Line)
//            {
//                points[index] = new PointF((float) point.X, (float) point.Y);
//                types[index] = 1;
//                    // Endpoint of a line: http://msdn.microsoft.com/en-us/library/system.drawing.drawing2d.graphicspath.pathtypes(v=vs.110).aspx
//                index++;
//            }
//
//            var graphicsPath = new GraphicsPath(points, types, FillMode.Alternate);
//            var region = new Region(graphicsPath);
//
//            var pF = new PointF {X = (float) p.X, Y = (float) p.Y};
//            return region.IsVisible(pF);
//        }
//

            // METHOD USING SIMPLE ALGORITHM: https://social.msdn.microsoft.com/Forums/windows/en-us/95055cdc-60f8-4c22-8270-ab5f9870270a/determine-if-the-point-is-in-the-polygon-c
            bool inside = false;
            var oldPoint = new Point(Line[Line.Count - 1].X, Line[Line.Count - 1].Y);
            foreach (Point point in Line)
            {
                var newPoint = new Point(point.X, point.Y);
                Point p1;
                Point p2;
                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.X < p.X) == (p.X <= oldPoint.X)
                    && (p.Y - p1.Y)*(p2.X - p1.X)
                    < (p2.Y - p1.Y)*(p.X - p1.X))
                {
                    inside = !inside;
                }

                oldPoint = newPoint;
            }

            return inside;
        }

        protected bool Equals(LineString other)
        {
            return Equals(Line, other.Line);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LineString) obj);
        }

        public override int GetHashCode()
        {
            return (Line != null ? Line.GetHashCode() : 0);
        }
    }

    public class Polygon : BaseGeometry
    {
        public readonly List<LineString> LineStrings = new List<LineString>();

        public override bool IsRegion { get { return true; } }

        public override bool Contains(Point p)
        {
            bool inclusive = true;
            foreach (LineString lineString in LineStrings)
            {
                if (inclusive && !lineString.Contains(p)) return false;
                if (!inclusive && lineString.Contains(p)) return false;
                inclusive = !inclusive;
            }
            return true;
        }

        protected bool Equals(Polygon other)
        {
            return Equals(LineStrings, other.LineStrings);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Polygon) obj);
        }

        public override int GetHashCode()
        {
            return (LineStrings != null ? LineStrings.GetHashCode() : 0);
        }
    }

    public class MultiPoint : BaseGeometry
    {
        public readonly List<Point> Points = new List<Point>();

        public override bool Contains(Point p)
        {
            return false;
        }

        protected bool Equals(MultiPoint other)
        {
            return Equals(Points, other.Points);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MultiPoint) obj);
        }

        public override int GetHashCode()
        {
            return (Points != null ? Points.GetHashCode() : 0);
        }
    }

    public class MultiLineString : BaseGeometry
    {
        public readonly List<LineString> Lines = new List<LineString>();

        public override bool Contains(Point p)
        {
            return false;
        }

        protected bool Equals(MultiLineString other)
        {
            return Equals(Lines, other.Lines);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MultiLineString) obj);
        }

        public override int GetHashCode()
        {
            return (Lines != null ? Lines.GetHashCode() : 0);
        }
    }

    public class MultiPolygon : BaseGeometry
    {
        public readonly List<Polygon> Polygons = new List<Polygon>();

        public override bool IsRegion { get { return true; } }

        public override bool Contains(Point p)
        {
            return Polygons.Any(polygon => polygon.Contains(p));
        }

        protected bool Equals(MultiPolygon other)
        {
            return Equals(Polygons, other.Polygons);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((MultiPolygon) obj);
        }

        public override int GetHashCode()
        {
            return (Polygons != null ? Polygons.GetHashCode() : 0);
        }
    }

    //Todo later
    public class CircularString : BaseGeometry
    {
        public override bool IsRegion { get { return false; } }

        public override bool Contains(Point p)
        {
            return false;
        }

        protected bool Equals(CircularString other)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CircularString) obj);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public class Geometries : List<BaseGeometry>
    {
    };

    public static class Extensions
    {
        private static readonly WebMercator WebMercator = new WebMercator();

        /// <summary>
        /// Convert the System.Windows.Point list to a list of ESRI MapPoints
        /// </summary>
        /// <param name="points"></param>
        /// <param name="convert">If null (default, do not convert. If true, convert geographic to web mercator. If false, convert from web mercator to geographic.</param>
        /// <returns></returns>
        public static PointCollection ToPointCollection(this List<System.Windows.Point> points, bool? convert = null)
        {
            var pc = new PointCollection();
            if (points.Count == 0) return pc;

            switch (convert) {
                case null:
                    foreach (var p in points) pc.Add(new MapPoint(p.X, p.Y));
                    break;
                case true:
                    foreach (var p in points) pc.Add(WebMercator.FromGeographic(new MapPoint(p.X, p.Y)) as MapPoint);
                    break;
                case false:
                    foreach (var p in points) pc.Add(WebMercator.ToGeographic(new MapPoint(p.X, p.Y)) as MapPoint);
                    break;
            }
            return pc;
        }

        private static readonly Regex Regex = new Regex("(\\d*.\\d*) (\\d*.\\d*)",
            RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        ///     Convert a shape to Well Known Text.
        /// </summary>
        public static string ConvertToWkt(Shape shape, CoordinateType projection = CoordinateType.Xy)
        {
            var sb = new StringBuilder();
            switch (shape.Type)
            {
                case ShapeType.Point:
                    // POINT(123.45 543.21)
                    var shapePoint = shape as ShapePoint;
                    if (shapePoint == null) return string.Empty;
                    sb.AppendFormat(CultureInfo.InvariantCulture, "POINT({0:0.000000} {1:0.000000})", shapePoint.Point.X,
                        shapePoint.Point.Y);
                    break;
                case ShapeType.MultiPoint:
                    // MULTIPOINT(1234.56 6543.21, 1 2, 3 4, 65.21 124.78), three points                    
                    var multiPoint = shape as ShapeMultiPoint;
                    if (multiPoint == null) return string.Empty;
                    sb.Append("MULTIPOINT(");
                    ConvertPointsToString(sb, multiPoint.Points, projection);
                    sb.Append(")");
                    break;
                case ShapeType.Polygon:
                    // POLYGON((101.23 171.82, 201.32 101.5, 215.7 201.953, 101.23 171.82))
                    // exterior ring, no interior rings 
                    // POLYGON((10 10, 20 10, 20 20, 10 20, 10 10), (13 13, 17 13, 17 17, 13 17, 13 13))
                    // exterior ring, one interior ring 
                    // MULTIPOLYGON(((0 0,10 20,30 40,0 0),(1 1,2 2,3 3,1 1)), ((100 100,110 110,120 120,100 100))), two polygons: the first one has an interior ring
                    var polygon = shape as ShapePolygon;
                    if (polygon == null) return string.Empty;
                    sb.Append("MULTIPOLYGON(");
                    foreach (var part in polygon.Parts)
                    {
                        sb.Append("((");
                        ConvertPointsToString(sb, part, projection);
                        sb.Append(")),");
                    }
                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                    sb.Append(")");
                    break;
                case ShapeType.PolygonZ:
                    // POLYGON((101.23 171.82, 201.32 101.5, 215.7 201.953, 101.23 171.82))
                    // exterior ring, no interior rings 
                    // POLYGON((10 10, 20 10, 20 20, 10 20, 10 10), (13 13, 17 13, 17 17, 13 17, 13 13))
                    // exterior ring, one interior ring 
                    // MULTIPOLYGON(((0 0,10 20,30 40,0 0),(1 1,2 2,3 3,1 1)), ((100 100,110 110,120 120,100 100))), two polygons: the first one has an interior ring
                    var polygonZ = shape as ShapePolygonZ;
                    if (polygonZ == null) return string.Empty;
                    sb.Append("MULTIPOLYGON(");
                    foreach (var part in polygonZ.Parts)
                    {
                        sb.Append("((");
                        ConvertPointsToString(sb, part, projection);
                        sb.Append(")),");
                    }
                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                    sb.Append(")");
                    break;
                case ShapeType.PolyLine:
                    // LINESTRING(100.0 200.0, 201.5 102.5, 1234.56 123.89), three vertices
                    // MULTILINESTRING((1 2, 3 4), (5 6, 7 8, 9 10), (11 12, 13 14)), first and last linestrings have 2 vertices each one; the second linestring has 3 vertices
                    var polyLine = shape as ShapePolyLine;
                    if (polyLine == null) return string.Empty;
                    sb.Append("MULTILINESTRING(");
                    foreach (var part in polyLine.Parts)
                    {
                        sb.Append("(");
                        ConvertPointsToString(sb, part, projection);
                        sb.Append("),");
                    }
                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                    sb.Append(")");
                    break;
                    //This works:
                    //var polyLine = shape as ShapePolyLine;
                    //if (polyLine == null) return string.Empty;
                    //sb.Append("MULTIPOLYGON(");
                    //foreach (var part in polyLine.Parts)
                    //{
                    //    sb.Append("((");
                    //    ConvertPointsToString(sb, part, projection);
                    //    sb.Append(")),");
                    //}
                    //sb.Remove(sb.Length - 1, 1); // remove the last comma
                    //sb.Append(")");
                    //break;
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Convert a base geometry to Well Known Text.
        /// </summary>
        public static string ConvertToWkt(this BaseGeometry geometry, CoordinateType projection = CoordinateType.Xy)
        {
            var sb = new StringBuilder();
            if (geometry as Point != null)
            {
                // POINT(123.45 543.21)
                var shapePoint = geometry as Point;
                if (shapePoint == null) return string.Empty;
                sb.AppendFormat(CultureInfo.InvariantCulture, "POINT({0} {1})", shapePoint.X, shapePoint.Y);                
            }
            else if (geometry as MultiPoint != null)
            {
                // MULTIPOINT(1234.56 6543.21, 1 2, 3 4, 65.21 124.78), three points                    
                var multiPoint = geometry as MultiPoint;
                if (multiPoint == null) return string.Empty;
                sb.Append("MULTIPOINT(");
                ConvertPointsToString(sb, multiPoint.Points, projection);
                sb.Append(")");                
            }
            else if (geometry as Polygon != null)
            {
                // POLYGON((101.23 171.82, 201.32 101.5, 215.7 201.953, 101.23 171.82))
                // exterior ring, no interior rings 
                // POLYGON((10 10, 20 10, 20 20, 10 20, 10 10), (13 13, 17 13, 17 17, 13 17, 13 13))
                // exterior ring, one interior ring 
                // MULTIPOLYGON(((0 0,10 20,30 40,0 0),(1 1,2 2,3 3,1 1)), ((100 100,110 110,120 120,100 100))), two polygons: the first one has an interior ring
                var polygon = geometry as Polygon;
                if (polygon == null) return string.Empty;
                sb.Append("POLYGON(");
                foreach (LineString part in polygon.LineStrings)
                {
                    sb.Append("(");
                    ConvertPointsToString(sb, part.Line, projection);
                    sb.Append("),");
                }
                sb.Remove(sb.Length - 1, 1); // remove the last comma
                sb.Append(")");                
            }
            else if (geometry as MultiPolygon != null)
            {
                // POLYGON((101.23 171.82, 201.32 101.5, 215.7 201.953, 101.23 171.82))
                // exterior ring, no interior rings 
                // POLYGON((10 10, 20 10, 20 20, 10 20, 10 10), (13 13, 17 13, 17 17, 13 17, 13 13))
                // exterior ring, one interior ring 
                // MULTIPOLYGON(((0 0,10 20,30 40,0 0),(1 1,2 2,3 3,1 1)), ((100 100,110 110,120 120,100 100))), two polygons: the first one has an interior ring
                var mpolygon = geometry as MultiPolygon;
                if (mpolygon == null) return string.Empty;
                sb.Append("MULTIPOLYGON(");
                foreach (Polygon poly in mpolygon.Polygons)
                {
                    sb.Append("(");
                    foreach (LineString part in poly.LineStrings)
                    {
                        sb.Append("(");
                        ConvertPointsToString(sb, part.Line, projection);
                        sb.Append("),");
                    }
                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                    sb.Append("),");
                }
                sb.Remove(sb.Length - 1, 1); // remove the last comma
                sb.Append(")");                
            }
            else if (geometry as LineString != null)
            {
                // LINESTRING(100.0 200.0, 201.5 102.5, 1234.56 123.89), three vertices
                // MULTILINESTRING((1 2, 3 4), (5 6, 7 8, 9 10), (11 12, 13 14)), first and last linestrings have 2 vertices each one; the second linestring has 3 vertices
                var polyLine = geometry as LineString;
                if (polyLine == null) return string.Empty;
                sb.Append("LINESTRING(");
                sb.Append("(");
                ConvertPointsToString(sb, polyLine.Line, projection);
                sb.Append("),");
                sb.Remove(sb.Length - 1, 1); // remove the last comma
                sb.Append(")");
            }
            else if (geometry as MultiLineString != null)
            {
                // LINESTRING(100.0 200.0, 201.5 102.5, 1234.56 123.89), three vertices
                // MULTILINESTRING((1 2, 3 4), (5 6, 7 8, 9 10), (11 12, 13 14)), first and last linestrings have 2 vertices each one; the second linestring has 3 vertices
                var mline = geometry as MultiLineString;
                if (mline == null) return string.Empty;
                sb.Append("MULTILINESTRING(");
                foreach (LineString part in mline.Lines)
                {
                    sb.Append("(");
                    ConvertPointsToString(sb, part.Line, projection);
                    sb.Append("),");
                }
                sb.Remove(sb.Length - 1, 1); // remove the last comma
                sb.Append(")");
            }
            else
            {
                throw new Exception("Unrecognized geometry, cannot parse to WKT.");
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Convert a base geometry to GeoJson.
        /// </summary>
        public static string ConvertToGeoJson(this BaseGeometry geometry, CoordinateType projection = CoordinateType.Xy)
        {
            string wkt = geometry.ConvertToWkt(projection);
            return new WellKnownTextIO(wkt).ToGeoJson();
        }

        /// <summary>
        ///     Convert a Well Known Text string to a base geometry.
        /// </summary>
        public static BaseGeometry ConvertFromWkt(this string wkt)
        {
            BaseGeometry result = null; // = new BaseGeometry();
            if (wkt.StartsWith("POINT"))
            {
                result = ParsePoint(wkt);
                //result.Genum = GeometryEnum.Point;
            }
            else if (wkt.StartsWith("LINESTRING"))
            {
                result = ParseLineString(wkt);
                //result.Genum = GeometryEnum.LineString;
            }
            else if (wkt.StartsWith("POLYGON Z"))
            {
                result = ParsePolygon(wkt, true);
                //result.Genum = GeometryEnum.Polygon;
            }
            else if (wkt.StartsWith("POLYGON"))
            {
                result = ParsePolygon(wkt);
                //result.Genum = GeometryEnum.Polygon;
            }
            else if (wkt.StartsWith("MULTIPOINT"))
            {
                result = ParseMultiPoint(wkt);
                //result.Genum = GeometryEnum.MultiPoint;
            }
            else if (wkt.StartsWith("MULTILINESTRING"))
            {
                result = ParseMultiLineString(wkt);
                //result.Genum = GeometryEnum.MultiLineString;
            }
            else if (wkt.StartsWith("MULTIPOLYGON"))
            {
                result = ParseMultiPolygon(wkt);
                //result.Genum = GeometryEnum.MultiPolygon;
            }
            return result;
        }

        /// <summary>
        ///     Convert a GeoJson string to a base geometry.
        /// </summary>
        public static BaseGeometry ConvertFromGeoJson(this string geoJson)
        {
            var wellKnownTextIo = new WellKnownTextIO();
            wellKnownTextIo.FromGeoJson(geoJson, false); // Into this object.
            return wellKnownTextIo.WktText.ConvertFromWkt();
        }

        public static Point ParsePoint(string wkt)
        {
            Point result = null; // = new Point();
            wkt = wkt.Replace("POINT", "").Replace("(", "").Replace(")", "");

            foreach (Match match in Regex.Matches(wkt))
            {
                double x, y;
                if (!double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out x) ||
                    !double.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out y))
                    continue;
                result = new Point(x, y);
            }
            return result;
        }

        public static LineString ParseLineString(string wkt)
        {
            var result = new LineString();
            wkt = wkt.Replace("LINESTRING", "").Replace("(", "").Replace(")", "");
            string[] points = wkt.Split(',');
            foreach (string p in points)
            {
                result.Line.Add(ParsePoint(p));
            }
            return result;
        }

        public static Polygon ParsePolygon(string wkt, bool isPolygonZ = false)
        {
            var result = new Polygon();
            wkt = wkt.Replace(isPolygonZ ? "POLYGON Z" : "POLYGON", "");
            string[] points = wkt.Split(new[] {"),("}, StringSplitOptions.None);
            foreach (string p in points)
            {
                result.LineStrings.Add(ParseLineString(p));
            }
            return result;
        }

        public static MultiPoint ParseMultiPoint(string wkt)
        {
            var result = new MultiPoint();
            wkt = wkt.Replace("MULTIPOINT", "").Replace("(", "").Replace(")", "");
            string[] points = wkt.Split(',');
            foreach (string p in points)
            {
                result.Points.Add(ParsePoint(p));
            }
            return result;
        }

        public static MultiLineString ParseMultiLineString(string wkt)
        {
            var result = new MultiLineString();
            wkt = wkt.Replace("MULTILINESTRING", "");
            string[] points = wkt.Split(new[] {"),("}, StringSplitOptions.None);
            foreach (string p in points)
            {
                result.Lines.Add(ParseLineString(p));
            }
            return result;
        }

        public static MultiPolygon ParseMultiPolygon(string wkt)
        {
            var result = new MultiPolygon();
            wkt = wkt.Replace("MULTIPOLYGON", "");
            string[] points = wkt.Split(new[] {")),(("}, StringSplitOptions.None);
            foreach (string p in points)
            {
                result.Polygons.Add(ParsePolygon(p));
            }
            return result;
        }

        /// <summary>
        ///     Convert a collection of PointD to a string: X1 Y1, X2 Y2 etc. (notice the absence of a comma at the end).
        /// </summary>
        /// <param name="sb">Currently active stringbuilder to hold the results</param>
        /// <param name="points">List or array of points</param>
        /// <param name="projection">Projection that we are using (currently, only RD is supported; others are not projected).</param>
        /// <returns></returns>
        private static void ConvertPointsToString(StringBuilder sb, IList<PointD> points, CoordinateType projection)
        {
            int lastElement = points.Count - 1;
            if (projection == CoordinateType.Rd)
            {
                // Same loop as below, but since we check the projection at the start, we don't need to do it anymore for every point.
                for (int i = 0; i <= lastElement; i++)
                {
                    System.Windows.Point point = CoordinateUtils.Rd2LonLatAsPoint(points[i].X, points[i].Y);
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0.000000} {1:0.000000}", point.X, point.Y);
                    if (i < lastElement) sb.Append(",");
                }
            }
            else
            {
                for (int i = 0; i <= lastElement; i++)
                {
                    PointD point = points[i];
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0.000000} {1:0.000000}", point.X, point.Y);
                    if (i < lastElement) sb.Append(",");
                }
            }
        }

        /// <summary>
        ///     Convert a collection of PointD to a string: X1 Y1, X2 Y2 etc. (notice the absence of a comma at the end).
        /// </summary>
        /// <param name="sb">Currently active stringbuilder to hold the results</param>
        /// <param name="points">List or array of points</param>
        /// <param name="projection">Projection that we are using (currently, only RD is supported; others are not projected).</param>
        /// <returns></returns>
        private static void ConvertPointsToString(StringBuilder sb, IList<Point> points, CoordinateType projection)
        {
            int lastElement = points.Count - 1;
            if (projection == CoordinateType.Rd)
            {
                // Same loop as below, but since we check the projection at the start, we don't need to do it anymore for every point.
                for (int i = 0; i <= lastElement; i++)
                {
                    System.Windows.Point point = CoordinateUtils.Rd2LonLatAsPoint(points[i].X, points[i].Y);
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}", point.X, point.Y);
                    if (i < lastElement) sb.Append(",");
                }
            }
            else
            {
                for (int i = 0; i <= lastElement; i++)
                {
                    Point point = points[i];
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}", point.X, point.Y);
                    if (i < lastElement) sb.Append(",");
                }
            }
        }
    }
}