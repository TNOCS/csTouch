using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Zandmotor.Controls.Plot
{
    public class MathFunctions
    {
        /// <summary>
        /// Uses the Douglas Peucker algorithm to reduce the number of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns></returns>
        public List<Point> DouglasPeuckerReduction(List<Point> points, Double tolerance)
        {
            if (points == null || points.Count < 3)
                return points;

            var firstPoint = 0;
            var lastPoint = points.Count - 1;
            var pointIndexsToKeep = new List<Int32>();

            //Add the first and last index to the keepers
            pointIndexsToKeep.Add(firstPoint);
            pointIndexsToKeep.Add(lastPoint);

            //The first and the last point cannot be the same
            while (points[firstPoint].Equals(points[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(points, firstPoint, lastPoint, tolerance, ref pointIndexsToKeep);

            var returnPoints = new List<Point>();
            pointIndexsToKeep.Sort();
            foreach (Int32 index in pointIndexsToKeep)
            {
                returnPoints.Add(points[index]);
            }

            return returnPoints;
        }

        /// <summary>
        /// Douglases the peucker reduction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="firstPoint">The first point.</param>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="pointIndexsToKeep">The point index to keep.</param>
        private void DouglasPeuckerReduction(List<Point>
            points, Int32 firstPoint, Int32 lastPoint, Double tolerance,
            ref List<Int32> pointIndexsToKeep)
        {
            Double maxDistance = 0;
            Int32 indexFarthest = 0;

            for (Int32 index = firstPoint; index < lastPoint; index++)
            {
                Double distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        /// <summary>
        /// The distance of a point from a line made from point1 and point2.
        /// </summary>
        /// <param name="pt1">The PT1.</param>
        /// <param name="pt2">The PT2.</param>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public Double PerpendicularDistance(Point point1, Point point2, Point point)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            Double area = Math.Abs(.5 * (point1.X * point2.Y + point2.X *
                point.Y + point.X * point1.Y - point2.X * point1.Y - point.X *
                point2.Y - point1.X * point.Y));
            Double bottom = Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
            if (bottom == 0) return 0;
            Double height = area / bottom * 2;

            return height;

            //Another option
            //Double A = Point.X - Point1.X;
            //Double B = Point.Y - Point1.Y;
            //Double C = Point2.X - Point1.X;
            //Double D = Point2.Y - Point1.Y;

            //Double dot = A * C + B * D;
            //Double len_sq = C * C + D * D;
            //Double param = dot / len_sq;

            //Double xx, yy;

            //if (param < 0)
            //{
            //    xx = Point1.X;
            //    yy = Point1.Y;
            //}
            //else if (param > 1)
            //{
            //    xx = Point2.X;
            //    yy = Point2.Y;
            //}
            //else
            //{
            //    xx = Point1.X + param * C;
            //    yy = Point1.Y + param * D;
            //}
            
            //Double d = DistanceBetweenOn2DPlane(Point, new Point(xx, yy));
        }


        public double CalculateDifference(List<Point> p1, List<Point> p2)
        {
            if (p1.Count < 2 || p2.Count < 2) 
                return -1;

            var tempxs1 = p2.Select(y => y.X).ToList();
            var tempxs2 = p1.Select(k => k.X).ToList();
            var totalxs = tempxs1.ToList();
            totalxs.AddRange(tempxs2);
            totalxs = totalxs.Distinct().OrderBy(k=>k).ToList();

            var idxa = 0;
            var idxb = 0;
            var result = 0.0;

            foreach (var x in totalxs)
            {
                while (idxb < p2.Count && p2[idxb].X <= x)
                    idxb++;
                while (idxa < p1.Count && p1[idxa].X <= x)
                    idxa++;
                if (idxa ==0 || idxb ==0 || idxa >= p1.Count || idxb >= p2.Count)
                    continue;
                //do interpolation
                var p1a = p1[idxa-1];
                var p1b = p1[idxa];
                var p2a = p2[idxb - 1];
                var p2b = p2[idxb];

                var x1 = (x - p1a.X) / (p1b.X - p1a.X);
                var x2 = (x - p2a.X) / (p2b.X - p2a.X);

                var val1 = p1a.Y + x1 * (p1b.Y - p1a.Y);
                var val2 = p2a.Y + x2 * (p2b.Y - p2a.Y);

                result += Math.Sqrt((val1 - val2)*(val1 - val2));
            }
            return result;
        }

    }
}
