using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace csCommon.Types.Geometries.AdvancedGeometry.GeometryTransformers
{
	/// <summary>
	/// Geometry helper extension methods
	/// </summary>
	public static class GeometryExtension
	{
		#region Transform
		/// <summary>
		/// Transforms the specified point and returns the result.
		/// </summary>
		/// <param name="point">The point.</param>
		/// <param name="transform">The transform.</param>
		/// <returns></returns>
        public static System.Windows.Point Transform(this System.Windows.Point point, Transform transform)
		{
			return transform.Transform(point);
		}

		/// <summary>
		/// Transforms the specified path segment and returns the result.
		/// </summary>
		/// <param name="pathSegment">The path segment.</param>
		/// <param name="transform">The transform.</param>
		/// <returns></returns>
        public static PathSegment Transform(this PathSegment pathSegment, TransformGroup transform)
		{
			PathSegment ret;
			if (pathSegment is LineSegment)
				ret = new LineSegment { Point = ((LineSegment)pathSegment).Point.Transform(transform) };
			else if (pathSegment is ArcSegment)
			{
				var arcSegment = (ArcSegment) pathSegment;
			    var scaleTransform = transform.Children.OfType<ScaleTransform>().FirstOrDefault();
                var rotateTransform = transform.Children.OfType<RotateTransform>().FirstOrDefault();
                var size = new Size { Height = arcSegment.Size.Height * scaleTransform.ScaleY, Width = arcSegment.Size.Width * scaleTransform.ScaleX };
                ret = new ArcSegment { Point = arcSegment.Point.Transform(transform), Size = size, IsLargeArc = arcSegment.IsLargeArc, RotationAngle = rotateTransform.Angle };
			}
			else if (pathSegment is BezierSegment)
			{
				var bezierSegment = (BezierSegment)pathSegment;
				ret = new BezierSegment
				{
					Point1 = bezierSegment.Point1.Transform(transform),
					Point2 = bezierSegment.Point2.Transform(transform),
					Point3 = bezierSegment.Point3.Transform(transform),
				};
			}
			else if (pathSegment is QuadraticBezierSegment)
			{
				var bezierSegment = (QuadraticBezierSegment)pathSegment;
				ret = new QuadraticBezierSegment
				{
					Point1 = bezierSegment.Point1.Transform(transform),
					Point2 = bezierSegment.Point2.Transform(transform),
				};
			}
			else
				throw new Exception("Transform To implement");
			return ret;
		}

		/// <summary>
		/// Transforms the specified path figure and returns the result.
		/// </summary>
		/// <param name="pathFigure">The path figure.</param>
		/// <param name="transform">The transform.</param>
		/// <returns></returns>
        public static PathFigure Transform(this PathFigure pathFigure, TransformGroup transform)
		{
			var ret = new PathFigure
			                 	{
			                 		StartPoint = pathFigure.StartPoint.Transform(transform),
			                 		IsClosed = pathFigure.IsClosed,
			                 		IsFilled = pathFigure.IsFilled
			                 	};

			foreach (var segment in pathFigure.Segments)
				ret.Segments.Add(segment.Transform(transform));
			return ret;
		}

		/// <summary>
		/// Transforms the specified path geometry and returns the result (as enumeration of PathFigure).
		/// </summary>
		/// <param name="pathGeometry">The path geometry.</param>
		/// <param name="transform">The transform.</param>
		/// <returns></returns>
        public static IEnumerable<PathFigure> Transform(this PathGeometry pathGeometry, TransformGroup transform)
		{
			return pathGeometry.Figures.Select(pathFigure => pathFigure.Transform(transform));
		}

		#endregion

		#region EndPoint

		/// <summary>
		/// Returns the end point of a path segment.
		/// </summary>
		/// <param name="pathSegment">The path segment.</param>
		/// <returns></returns>
        public static System.Windows.Point EndPoint(this PathSegment pathSegment)
		{
			if (pathSegment is LineSegment)
				return (pathSegment as LineSegment).Point;

			if (pathSegment is ArcSegment)
				return (pathSegment as ArcSegment).Point;

			if (pathSegment is BezierSegment)
				return (pathSegment as BezierSegment).Point3;

			if (pathSegment is QuadraticBezierSegment)
				return (pathSegment as QuadraticBezierSegment).Point2;

			throw new Exception("EndPoint to implement");
		}

		/// <summary>
		/// Returns the end point of a path figure.
		/// </summary>
		/// <param name="pathFigure">The path figure.</param>
		/// <returns></returns>
        public static System.Windows.Point EndPoint(this PathFigure pathFigure)
		{
			return pathFigure.Segments.Last().EndPoint();
		}
		#endregion

		#region MiddlePoint

		/// <summary>
		/// Returns the middle point of a path segment.
		/// </summary>
		/// <param name="pathSegment">The path segment.</param>
		/// <param name="startPoint">The start point.</param>
		/// <returns></returns>
        public static System.Windows.Point MiddlePoint(this PathSegment pathSegment, System.Windows.Point startPoint)
		{
			if (pathSegment is BezierSegment)
				return BezierSegmentPoint(pathSegment as BezierSegment, startPoint, 0.5);

			// Very approximative way to find the middle of a segment (except for linesegment :-)
			var endPoint = pathSegment.EndPoint();
            return new System.Windows.Point { X = (startPoint.X + endPoint.X) / 2, Y = (startPoint.Y + endPoint.Y) / 2 };
		}

        private static System.Windows.Point BezierSegmentPoint(BezierSegment quadraticBezier, System.Windows.Point startPoint, double t)
		{
			double t1 = 1 - t;
            return new System.Windows.Point
			       	{
			       		X = t1*t1*t1*startPoint.X + 3*t1*t1*t*quadraticBezier.Point1.X +
			       		    3*t1*t*t*quadraticBezier.Point2.X + t*t*t*quadraticBezier.Point3.X,
			       		Y = t1*t1*t1*startPoint.Y + 3*t1*t1*t*quadraticBezier.Point1.Y +
			       		    3*t1*t*t*quadraticBezier.Point2.Y + t*t*t*quadraticBezier.Point3.Y
			       	};
		}


		/// <summary>
		/// Returns the middle point of a path figure.
		/// </summary>
		/// <param name="pathFigure">The path figure.</param>
		/// <returns></returns>
        public static System.Windows.Point MiddlePoint(this PathFigure pathFigure)
		{
			int middle = pathFigure.Segments.Count/2;

			System.Windows.Point startPoint = (middle == 0 ? pathFigure.StartPoint : pathFigure.Segments[middle - 1].EndPoint());
			return pathFigure.Segments[middle].MiddlePoint(startPoint);
		}
		#endregion

		#region OrientationAtStart

		private const double radiansToDegrees = 180/Math.PI;
		/// <summary>
		/// The orientation at the origin of the path segment.
		/// </summary>
		/// <param name="pathSegment">The path segment.</param>
		/// <param name="startPoint">The start point.</param>
		/// <returns></returns>
        public static double OrientationAtStart(this PathSegment pathSegment, System.Windows.Point startPoint)
		{
            System.Windows.Point endPoint;

			if (pathSegment is LineSegment)
			{
				endPoint = ((LineSegment)pathSegment).Point;
			}
			else if (pathSegment is BezierSegment)
			{
				endPoint = ((BezierSegment)pathSegment).Point1;
			}
			else if (pathSegment is QuadraticBezierSegment)
			{
				endPoint = ((QuadraticBezierSegment)pathSegment).Point1;
			}
			else
				throw new Exception("OrientationAtStart to implement");

			return Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X) * radiansToDegrees;
		}

		/// <summary>
		/// The orientation at the origin of the path figure.
		/// </summary>
		/// <param name="pathFigure">The path figure.</param>
		/// <returns></returns>
		public static double OrientationAtStart(this PathFigure pathFigure)
		{
			return pathFigure.Segments[0].OrientationAtStart(pathFigure.StartPoint);
		}
		
		#endregion

		#region OrientationAtEnd
		/// <summary>
		/// The orientation at the end of the path segment.
		/// </summary>
		/// <param name="pathSegment">The path segment.</param>
		/// <param name="startSegmentPoint">The starting point of the segment.</param>
		/// <returns></returns>
        public static double OrientationAtEnd(this PathSegment pathSegment, System.Windows.Point startSegmentPoint)
		{
            System.Windows.Point endPoint;
            System.Windows.Point startPoint;

			if (pathSegment is LineSegment)
			{
				startPoint = startSegmentPoint;
				endPoint = ((LineSegment)pathSegment).Point;
			}
			else if (pathSegment is BezierSegment)
			{
				startPoint = ((BezierSegment)pathSegment).Point2;
				endPoint = ((BezierSegment)pathSegment).Point3;
			}
			else if (pathSegment is QuadraticBezierSegment)
			{
				startPoint = ((QuadraticBezierSegment)pathSegment).Point1;
				endPoint = ((QuadraticBezierSegment)pathSegment).Point2;
			}
			else
				throw new Exception("OrientationAtEnd to implement");

			return Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X) * radiansToDegrees;
		}

		/// <summary>
		/// The orientation at the end of the path figure.
		/// </summary>
		/// <param name="pathFigure">The path figure.</param>
		/// <returns></returns>
		public static double OrientationAtEnd(this PathFigure pathFigure)
		{
			// Find last non null segment
			for (int index = pathFigure.Segments.Count - 1; index >= 0; index--)
			{
				var startPoint = index > 0 ? pathFigure.Segments[index - 1].EndPoint() : pathFigure.StartPoint;
				if (startPoint != pathFigure.Segments[index].EndPoint())
					return pathFigure.Segments[index].OrientationAtEnd(startPoint);
			}
			return 0; 
		}

		#endregion

		#region OrientationAtMiddle

		/// <summary>
		/// The orientation at the middle of the path segment (very approximative implementation....).
		/// </summary>
		/// <param name="pathSegment">The path segment.</param>
		/// <param name="startPoint">The start point.</param>
		/// <returns></returns>
        public static double OrientationAtMiddle(this PathSegment pathSegment, System.Windows.Point startPoint)
		{
			if (pathSegment is LineSegment)
				return pathSegment.OrientationAtEnd(startPoint);

			// Very approximative way to find the middle orientation of a segment :-( TO DO a day....
			return (pathSegment.OrientationAtStart(startPoint) + pathSegment.OrientationAtEnd(startPoint)) / 2.0;
		}

		/// <summary>
		/// The orientation at the middle of the path figure.
		/// </summary>
		/// <param name="pathFigure">The path figure.</param>
		/// <returns></returns>
		public static double OrientationAtMiddle(this PathFigure pathFigure)
		{
			int middle = pathFigure.Segments.Count / 2;

            System.Windows.Point startPoint = (middle == 0 ? pathFigure.StartPoint : pathFigure.Segments[middle - 1].EndPoint());
			return pathFigure.Segments[middle].OrientationAtMiddle(startPoint);
		}
		#endregion

		#region Concat
		/// <summary>
		/// Concats path figures to a path geometry.
		/// </summary>
		/// <param name="pathGeometry">The path geometry.</param>
		/// <param name="pathFigures">The path figures.</param>
		public static void Concat(this PathGeometry pathGeometry, IEnumerable<PathFigure> pathFigures)
		{
			foreach (var pathFigure in pathFigures)
				pathGeometry.Figures.Add(pathFigure);
		}
		#endregion
	}
}
