// Copied from http://www.arcgis.com/home/item.html?id=1e432da7e74f4402bd43a5863167022d
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace csCommon.Types.Geometries.AdvancedGeometry.GeometryTransformers
{
	/// <summary>
	/// Transformer adding patterns to a path geometry.
	/// The pattern is defined by any PathGeometry and, optionaly, can be transformed by a composite transformation. 
	/// Patterns can be added at the starts/middle/end of the path or of each segment.
	/// </summary>
	public class PatternTransformer : IGeometryTransformer
	{
		#region Constructors

		private static readonly ResourceDictionary _resourceDictionary;
		/// <summary>
		/// Initializes a new instance of the <see cref="PatternTransformer"/> class.
		/// </summary>
		public PatternTransformer()
		{
			BySegment = true;
			AtStart = false;
			AtEnd = false;
			AtMiddle = true;

			// Set a default composite transform
		    CompositeTransform = new TransformGroup();
		    CompositeTransform.Children.Add(new ScaleTransform() {ScaleX = 10, ScaleY = 10});
                CompositeTransform.Children.Add(new RotateTransform()); 
                CompositeTransform.Children.Add(new TranslateTransform());
                CompositeTransform.Children.Add(new SkewTransform());
		}

		static PatternTransformer()
		{
            var uri = new Uri("/csCommon;component/Types/Geometries/AdvancedGeometry/GeometryTransformers/PatternTemplates.xaml", UriKind.Relative);
			_resourceDictionary = new ResourceDictionary { Source = uri };
		} 
		#endregion

		#region PathGeometry Pattern 
		/// <summary>
		/// Gets or sets the pattern to add to the geometry.
		/// </summary>
		/// <value>The pattern.</value>
		public PathGeometry Pattern { get; set; }
		
		#endregion

		#region CompositeTransform
		/// <summary>
		/// Gets or sets the composite transform.
		/// </summary>
		/// <value>The composite transform.</value>
        public TransformGroup CompositeTransform { get; set; }

		#endregion

		#region Properties managing patterns position : AtStart/AtEnd/AtMiddle/BySegment
		/// <summary>
		/// Gets or sets a value indicating whether patterns are by segment.
		/// </summary>
		/// <value><c>true</c> if [by segment]; otherwise, <c>false</c>.</value>
		public bool BySegment { get; set; }
		/// <summary>
		/// Gets or sets a value indicating whether there is pattern at start of segment/figure.
		/// </summary>
		/// <value><c>true</c> if [at start]; otherwise, <c>false</c>.</value>
		/// 
		public bool AtStart { get; set; }
		/// <summary>
		/// Gets or sets a value indicating whether there is pattern at end of segment/figure.
		/// </summary>
		/// <value><c>true</c> if [at end]; otherwise, <c>false</c>.</value>
		public bool AtEnd { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether there is pattern at middle of segment/figure.
		/// </summary>
		/// <value><c>true</c> if [at middle]; otherwise, <c>false</c>.</value>
		public bool AtMiddle { get; set; }
		
		#endregion

		#region IsFillSymbol
		/// <summary>
		/// Gets or sets a value indicating whether this instance is working on fill symbol.
		/// </summary>
		/// <remarks></remarks>
		/// <value>
		/// 	<c>true</c> if this instance is fill symbol; otherwise, <c>false</c>.
		/// </value>
		public bool IsFillSymbol { get; set; }
		
		#endregion

		/// <summary>
		/// Gets or sets the fill rule.
		/// </summary>
		/// <value>The fill rule.</value>
		public FillRule FillRule { get; set; }

		#region IGeometryTransformer method : Transform
		/// <summary>
		/// Transforms the specified geometry.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		public void Transform(Geometry geometry)
		{
			var path = geometry as PathGeometry;
			if (path == null || path.Figures == null || !path.Figures.Any())
				return;
			if (Pattern == null)
				throw new Exception("Pattern must be initialized");


			// To allow chaining pattern transformers (which must not add patterns to patterns) and to take into accound the wraparound option
			// we have to evaluate the number of pathFigures to process (1 in standard case, 2 or more if wraparound and geometry crossing dataline)
			var firstPathFigure = path.Figures.First();
			foreach(var pathFigure in path.Figures.ToArray())
			{
				if (pathFigure.Segments.Count == firstPathFigure.Segments.Count && pathFigure.StartPoint.Y == firstPathFigure.StartPoint.Y) // Heuristic to enhance???
					AddPatterns(path, pathFigure);
				else
					break;
			}

			path.FillRule = FillRule;
		}

		private void AddPatterns(PathGeometry path, PathFigure pathFigure)
		{
			pathFigure.IsFilled = IsFillSymbol; // should be done by the framework ??

			if (AtStart)
			{
				if (BySegment)
				{
					var point = pathFigure.StartPoint;
					foreach (var segment in pathFigure.Segments)
					{
						if (segment.EndPoint() != point)
						{
							path.Concat(CreatePattern(point, segment.OrientationAtStart(point) + 180));
							point = segment.EndPoint();
						}
					}
				}
				else
					path.Concat(CreatePattern(pathFigure.StartPoint, pathFigure.OrientationAtStart() + 180));
			}

			if (AtEnd)
			{
				if (BySegment)
				{
					var point = pathFigure.StartPoint;
					foreach (var segment in pathFigure.Segments)
					{
						if (segment.EndPoint() != point)
						{
							path.Concat(CreatePattern(segment.EndPoint(), segment.OrientationAtEnd(point)));
							point = segment.EndPoint();
						}
					}
				}
				else
					path.Concat(CreatePattern(pathFigure.EndPoint(), pathFigure.OrientationAtEnd()));
			}

			if (AtMiddle)
			{
				if (BySegment)
				{
					var point = pathFigure.StartPoint;
					foreach (var segment in pathFigure.Segments)
					{
						path.Concat(CreatePattern(segment.MiddlePoint(point), segment.OrientationAtMiddle(point)));
						point = segment.EndPoint();
					}
				}
				else
					path.Concat(CreatePattern(pathFigure.MiddlePoint(), pathFigure.OrientationAtMiddle()));
			}
		}

		#endregion

		#region internal PatternName
		/// <summary>
		/// Internal way to set the pattern from theresource dictionary PatternTemplate.xaml
		/// </summary>
		/// <value>The name of the pattern.</value>
		internal string PatternName
		{
			set
			{
				if (_resourceDictionary.Contains(value))
				{
					Pattern = _resourceDictionary[value] as PathGeometry;
				}
				if (Pattern == null)
					throw new Exception("Unknown pattern name");
			}
		}
		
		#endregion

		#region private IEnumerable<PathFigure> CreatePattern(Point point, double rotation)
        private IEnumerable<PathFigure> CreatePattern(System.Windows.Point point, double rotation)
        {
            //var compositeTransform = new TransformGroup();
            // TODO Check if conversion from compositeTransform to TransformGroup is oke
            var scaleTransform = CompositeTransform.Children.OfType<ScaleTransform>().FirstOrDefault();
            var skewTransform = CompositeTransform.Children.OfType<SkewTransform>().FirstOrDefault();
            var translateTransform = CompositeTransform.Children.OfType<TranslateTransform>().FirstOrDefault();
            var rotateTransform = CompositeTransform.Children.OfType<RotateTransform>().FirstOrDefault();

            var compositeTransform = new TransformGroup();
            compositeTransform.Children.Add(new ScaleTransform() { ScaleX = scaleTransform.ScaleX, ScaleY = scaleTransform.ScaleY});
            compositeTransform.Children.Add(new RotateTransform() { Angle = rotation + rotateTransform.Angle});
            compositeTransform.Children.Add(new TranslateTransform() {X = point.X + translateTransform.X, Y = point.Y + translateTransform.Y});
            compositeTransform.Children.Add(new SkewTransform() { AngleX = skewTransform .AngleX, AngleY = skewTransform.AngleY});


									
			return Pattern.Transform(compositeTransform);
		}
		
		#endregion

	}

}
