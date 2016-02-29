using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace csCommon.Types.Geometries.AdvancedGeometry.GeometryTransformers
{
	/// <summary>
	/// Smooth Bezier transformer
	/// </summary>
	public class SmoothBezierTransformer : IGeometryTransformer
	{
		/// <summary>
		/// Gets or sets a value indicating whether the main figure is filled (because the framework doesn't initialize IsFilled property)
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is fill symbol; otherwise, <c>false</c>.
		/// </value>
		public bool IsFillSymbol { get; set; }

		#region Transform
		/// <summary>
		/// Transforms the specified geometry.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		public void Transform(Geometry geometry)
		{
			var path = geometry as PathGeometry;
			if (path == null || path.Figures == null || !path.Figures.Any())
				return;

			var pathFigures = path.Figures.ToList();
			path.Figures.Clear();
			foreach (var pathFigure in pathFigures)
				path.Figures.Add(Smooth(pathFigure));


		} 

		private PathFigure Smooth(PathFigure pathFigure)
		{
			if (pathFigure.Segments.Count <= 1)
				return pathFigure; // need at least 2 segments

			// The path is closed if the last end point equal the start point
			var startPoint = pathFigure.StartPoint;
			var endPoint = pathFigure.EndPoint();
			bool isClosed = (startPoint == endPoint);
			if (isClosed && pathFigure.Segments.Count <= 2)
				return pathFigure; // need at least 3 segments to get a closed polygon


			var newPathFigure = new PathFigure
			{
				StartPoint = pathFigure.StartPoint,
				//IsFilled = pathFigure.IsFilled, Not initialized by framework
				IsFilled = IsFillSymbol,
				IsClosed = pathFigure.IsClosed
			};

			int nbKnotPoints = isClosed ? pathFigure.Segments.Count : pathFigure.Segments.Count + 1;

            var knotPoints = new System.Windows.Point[nbKnotPoints];
			int index = 0;
			knotPoints[index++] = startPoint;
			foreach (var segment in pathFigure.Segments)
			{
				if (index < nbKnotPoints)
					knotPoints[index++] = segment.EndPoint();
				else
				{
					Debug.Assert(isClosed && index == nbKnotPoints); // should only happen for last segment when closed
				}
			}

            System.Windows.Point[] firstControls;
            System.Windows.Point[] secondControls;

			if (isClosed)
				ClosedBezierSpline.GetCurveControlPoints(knotPoints, out firstControls, out secondControls);
			else
				OpenBezierSpline.GetCurveControlPoints(knotPoints, out firstControls, out secondControls);

			int nbSegment = firstControls.Length;
			for (int i = 0; i < nbSegment; i++)
			{
				var point1 = firstControls[i];
				var point2 = secondControls[i];

				var point3 = (i + 1 >= nbKnotPoints ? knotPoints[0] : knotPoints[i + 1]); // Close the path if needed

				var bezierSegment = new BezierSegment { Point1 = point1, Point2 = point2, Point3 = point3 };
				newPathFigure.Segments.Add(bezierSegment);
			}

			return newPathFigure;
		}
		#endregion

		// Calculate the bezier segments to get a smooth curve
		// Code coming from Oleg V. Polikarpotchkin 
		// see http://www.codeproject.com/KB/graphics/BezierSpline.aspx and http://www.codeproject.com/KB/graphics/ClosedBezierSpline.aspx
		// -----------------------------------------------------------------------------------------------------
		#region SmoothBezier helpers

		#region OpenBezierSpline
		/// <summary>
		/// Bezier Spline methods
		/// </summary>
		internal static class OpenBezierSpline
		{
			/// <summary>
			/// Get open-ended Bezier Spline Control Points.
			/// </summary>
			/// <param name="knots">Input Knot Bezier spline points.</param>
			/// <param name="firstControlPoints">Output First Control points
			/// array of knots.Length - 1 length.</param>
			/// <param name="secondControlPoints">Output Second Control points
			/// array of knots.Length - 1 length.</param>
			/// <exception cref="ArgumentNullException"><paramref name="knots"/>
			/// parameter must be not null.</exception>
			/// <exception cref="ArgumentException"><paramref name="knots"/>
			/// array must contain at least two points.</exception>
            public static void GetCurveControlPoints(System.Windows.Point[] knots,
                out System.Windows.Point[] firstControlPoints, out System.Windows.Point[] secondControlPoints)
			{
				if (knots == null)
					throw new ArgumentNullException("knots");
				int n = knots.Length - 1;
				if (n < 1)
					throw new ArgumentException
					("At least two knot points required", "knots");
				if (n == 1)
				{ // Special case: Bezier curve should be a straight line.
                    firstControlPoints = new System.Windows.Point[1];
					// 3P1 = 2P0 + P3
					firstControlPoints[0].X = (2 * knots[0].X + knots[1].X) / 3;
					firstControlPoints[0].Y = (2 * knots[0].Y + knots[1].Y) / 3;

                    secondControlPoints = new System.Windows.Point[1];
					// P2 = 2P1 – P0
					secondControlPoints[0].X = 2 *
						firstControlPoints[0].X - knots[0].X;
					secondControlPoints[0].Y = 2 *
						firstControlPoints[0].Y - knots[0].Y;
					return;
				}

				// Calculate first Bezier control points
				// Right hand side vector
				double[] rhs = new double[n];

				// Set right hand side X values
				for (int i = 1; i < n - 1; ++i)
					rhs[i] = 4 * knots[i].X + 2 * knots[i + 1].X;
				rhs[0] = knots[0].X + 2 * knots[1].X;
				rhs[n - 1] = (8 * knots[n - 1].X + knots[n].X) / 2.0;
				// Get first control points X-values
				double[] x = GetFirstControlPoints(rhs);

				// Set right hand side Y values
				for (int i = 1; i < n - 1; ++i)
					rhs[i] = 4 * knots[i].Y + 2 * knots[i + 1].Y;
				rhs[0] = knots[0].Y + 2 * knots[1].Y;
				rhs[n - 1] = (8 * knots[n - 1].Y + knots[n].Y) / 2.0;
				// Get first control points Y-values
				double[] y = GetFirstControlPoints(rhs);

				// Fill output arrays.
                firstControlPoints = new System.Windows.Point[n];
                secondControlPoints = new System.Windows.Point[n];
				for (int i = 0; i < n; ++i)
				{
					// First control point
                    firstControlPoints[i] = new System.Windows.Point(x[i], y[i]);
					// Second control point
					if (i < n - 1)
                        secondControlPoints[i] = new System.Windows.Point(2 * knots
							[i + 1].X - x[i + 1], 2 *
							knots[i + 1].Y - y[i + 1]);
					else
                        secondControlPoints[i] = new System.Windows.Point((knots
							[n].X + x[n - 1]) / 2,
							(knots[n].Y + y[n - 1]) / 2);
				}
			}

			/// <summary>
			/// Solves a tridiagonal system for one of coordinates (x or y)
			/// of first Bezier control points.
			/// </summary>
			/// <param name="rhs">Right hand side vector.</param>
			/// <returns>Solution vector.</returns>
			private static double[] GetFirstControlPoints(double[] rhs)
			{
				int n = rhs.Length;
				double[] x = new double[n]; // Solution vector.
				double[] tmp = new double[n]; // Temp workspace.

				double b = 2.0;
				x[0] = rhs[0] / b;
				for (int i = 1; i < n; i++) // Decomposition and forward substitution.
				{
					tmp[i] = 1 / b;
					b = (i < n - 1 ? 4.0 : 3.5) - tmp[i];
					x[i] = (rhs[i] - x[i - 1]) / b;
				}
				for (int i = 1; i < n; i++)
					x[n - i - 1] -= tmp[n - i] * x[n - i]; // Backsubstitution.

				return x;
			}
		} 
		#endregion

		#region ClosedBezierSpline
		/// <summary>
		/// Closed Bezier Spline Control Points calculation.
		/// </summary>
		internal static class ClosedBezierSpline
		{
			/// <summary>
			/// Get Closed Bezier Spline Control Points.
			/// </summary>
			/// <param name="knots">Input Knot Bezier spline points.</param>
			/// <param name="firstControlPoints">Output First Control points array of the same 
			/// length as the <paramref name="knots"/> array.</param>
			/// <param name="secondControlPoints">Output Second Control points array of of the same
			/// length as the <paramref name="knots"/> array.</param>
            public static void GetCurveControlPoints(System.Windows.Point[] knots, out System.Windows.Point[] firstControlPoints, out System.Windows.Point[] secondControlPoints)
			{
				int n = knots.Length;
				if (n <= 2)
				{
                    firstControlPoints = new System.Windows.Point[0];
                    secondControlPoints = new System.Windows.Point[0];
					return;
				}

				// Calculate first Bezier control points

				// The matrix.
				double[] a = new double[n], b = new double[n], c = new double[n];
				for (int i = 0; i < n; ++i)
				{
					a[i] = 1;
					b[i] = 4;
					c[i] = 1;
				}

				// Right hand side vector for points X coordinates.
				double[] rhs = new double[n];
				for (int i = 0; i < n; ++i)
				{
					int j = (i == n - 1) ? 0 : i + 1;
					rhs[i] = 4 * knots[i].X + 2 * knots[j].X;
				}
				// Solve the system for X.
				double[] x = Cyclic.Solve(a, b, c, 1, 1, rhs);

				// Right hand side vector for points Y coordinates.
				for (int i = 0; i < n; ++i)
				{
					int j = (i == n - 1) ? 0 : i + 1;
					rhs[i] = 4 * knots[i].Y + 2 * knots[j].Y;
				}
				// Solve the system for Y.
				double[] y = Cyclic.Solve(a, b, c, 1, 1, rhs);

				// Fill output arrays.
                firstControlPoints = new System.Windows.Point[n];
                secondControlPoints = new System.Windows.Point[n];
				for (int i = 0; i < n; ++i)
				{
					// First control point.
                    firstControlPoints[i] = new System.Windows.Point(x[i], y[i]);
					// Second control point.
					if (i + 1 < n)
                        secondControlPoints[i] = new System.Windows.Point(2 * knots[i + 1].X - x[i + 1], 2 * knots[i + 1].Y - y[i + 1]);
					else
                        secondControlPoints[i] = new System.Windows.Point(2 * knots[0].X - x[0], 2 * knots[0].Y - y[0]);

				}
			}
		}

		/// <summary>
		/// Solves the cyclic set of linear equations.
		/// </summary>
		/// <remarks>
		/// The cyclic set of equations have the form
		/// ---------------------------
		/// b0 c0  0 · · · · · · ß
		///	a1 b1 c1 · · · · · · ·
		///  · · · · · · · · · · · 
		///  · · · a[n-2] b[n-2] c[n-2]
		/// a  · · · · 0  a[n-1] b[n-1]
		/// ---------------------------
		/// This is a tridiagonal system, except for the matrix elements 
		/// a and ß in the corners.
		/// </remarks>
		private static class Cyclic
		{
			/// <summary>
			/// Solves the cyclic set of linear equations. 
			/// </summary>
			/// <remarks>
			/// All vectors have size of n although some elements are not used.
			/// The input is not modified.
			/// </remarks>
			/// <param name="a">Lower diagonal vector of size n; a[0] not used.</param>
			/// <param name="b">Main diagonal vector of size n.</param>
			/// <param name="c">Upper diagonal vector of size n; c[n-1] not used.</param>
			/// <param name="alpha">Bottom-left corner value.</param>
			/// <param name="beta">Top-right corner value.</param>
			/// <param name="rhs">Right hand side vector.</param>
			/// <returns>The solution vector of size n.</returns>
			public static double[] Solve(double[] a, double[] b,
			double[] c, double alpha, double beta, double[] rhs)
			{
				// a, b, c and rhs vectors must have the same size.
				if (a.Length != b.Length || c.Length != b.Length ||
								rhs.Length != b.Length)
					throw new ArgumentException
					("Diagonal and rhs vectors must have the same size.");
				int n = b.Length;
				if (n <= 2)
					throw new ArgumentException
					("n too small in Cyclic; must be greater than 2.");

				double gamma = -b[0]; // Avoid subtraction error in forming bb[0].
				// Set up the diagonal of the modified tridiagonal system.
				double[] bb = new Double[n];
				bb[0] = b[0] - gamma;
				bb[n - 1] = b[n - 1] - alpha * beta / gamma;
				for (int i = 1; i < n - 1; ++i)
					bb[i] = b[i];
				// Solve A · x = rhs.
				double[] solution = Tridiagonal.Solve(a, bb, c, rhs);
				double[] x = new Double[n];
				for (int k = 0; k < n; ++k)
					x[k] = solution[k];

				// Set up the vector u.
				double[] u = new Double[n];
				u[0] = gamma;
				u[n - 1] = alpha;
				for (int i = 1; i < n - 1; ++i)
					u[i] = 0.0;
				// Solve A · z = u.
				solution = Tridiagonal.Solve(a, bb, c, u);
				double[] z = new Double[n];
				for (int k = 0; k < n; ++k)
					z[k] = solution[k];

				// Form v · x/(1 + v · z).
				double fact = (x[0] + beta * x[n - 1] / gamma)
					/ (1.0 + z[0] + beta * z[n - 1] / gamma);

				// Now get the solution vector x.
				for (int i = 0; i < n; ++i)
					x[i] -= fact * z[i];
				return x;
			}
		}

		/// <summary>
		/// Tridiagonal system solution.
		/// </summary>
		public static class Tridiagonal
		{
			/// <summary>
			/// Solves a tridiagonal system.
			/// </summary>
			/// <remarks>
			/// All vectors have size of n although some elements are not used.
			/// </remarks>
			/// <param name="a">Lower diagonal vector; a[0] not used.</param>
			/// <param name="b">Main diagonal vector.</param>
			/// <param name="c">Upper diagonal vector; c[n-1] not used.</param>
			/// <param name="rhs">Right hand side vector</param>
			/// <returns>system solution vector</returns>
			public static double[] Solve(double[] a, double[] b, double[] c, double[] rhs)
			{
				// a, b, c and rhs vectors must have the same size.
				if (a.Length != b.Length || c.Length != b.Length || rhs.Length != b.Length)
					throw new ArgumentException("Diagonal and rhs vectors must have the same size.");
				if (b[0] == 0.0)
					throw new InvalidOperationException("Singular matrix.");
				// If this happens then you should rewrite your equations as a set of 
				// order N - 1, with u2 trivially eliminated.

				ulong n = Convert.ToUInt64(rhs.Length);
				double[] u = new Double[n];
				double[] gam = new Double[n]; // One vector of workspace, gam is needed.
			
				double bet = b[0];
				u[0] = rhs[0] / bet;
				for (ulong j = 1;j < n;j++) // Decomposition and forward substitution.
				{
					gam[j] = c[j-1] / bet;
					bet = b[j] - a[j] * gam[j];
					if (bet == 0.0)  
						// Algorithm fails.
						throw new InvalidOperationException("Singular matrix.");
				   u[j] = (rhs[j] - a[j] * u[j - 1]) / bet;
				}
				for (ulong j = 1;j < n;j++) 
					u[n - j - 1] -= gam[n - j] * u[n - j]; // Backsubstitution.

				return u;
			}
		}	

		#endregion


		#endregion
	}
}
