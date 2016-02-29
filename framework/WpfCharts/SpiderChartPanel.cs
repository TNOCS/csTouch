using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfCharts
{
    public class SpiderChartPanel : Panel
    {
        private const int InteractivePointRadius = 15;

        private static readonly Pen DashedPen = new Pen(Brushes.Black, 1)
        {
                                                                              DashStyle = new DashStyle {
                                                                                                            Dashes = new DoubleCollection {
                                                                                                                                              2,
                                                                                                                                              8
                                                                                                                                          }
                                                                                                        }
                                                                          };

        private static readonly Pen Pen = new Pen(Brushes.Black, 4) { EndLineCap = PenLineCap.Round };
        private static readonly Pen InvisiblePen = new Pen(Brushes.Transparent, 2 * InteractivePointRadius + 30) {  StartLineCap = PenLineCap.Round };
        private static readonly Typeface LabelFont = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private readonly Dictionary<int, ChartLine> capturedLines = new Dictionary<int, ChartLine>();

        private int spokes;
        private double deltaAngle, radius, minRadius, maxRadius, deltaRadius, deltaTickRadius, spokeLength;
        private Point center;
        private ChartLine nearestLine;
 
        public SpiderChartPanel()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += (sender, args) => nearestLine = null;
            TouchDown += (sender, args) =>
                             {
                                 if (!IsInteractive) return;
                                 var device = args.Device as TouchDevice;
                                 if (device == null) return;
                                 device.Updated += DeviceOnUpdated;
                             };
            TouchUp += (sender, args) =>
                           {
                               if (!IsInteractive) return;
                               var device = args.Device as TouchDevice;
                               if (device == null) return;
                               capturedLines.Remove(device.Id);
                               device.Updated -= DeviceOnUpdated;
                           };
        }

        

        #region ValuesChanged

        /// <summary>
        /// ValuesChanged Routed Event
        /// </summary>
        public static readonly RoutedEvent ValuesChangedEvent = EventManager.RegisterRoutedEvent("ValuesChanged",
            RoutingStrategy.Bubble, typeof(ValuesChangedEventHandler), typeof(SpiderChartPanel));

        /// <summary>
        /// Occurs when the PointDataSource values are changed
        /// </summary>
        public event ValuesChangedEventHandler ValuesChanged
        {
            add { AddHandler(ValuesChangedEvent, value); }
            remove { RemoveHandler(ValuesChangedEvent, value); }
        }

        /// <summary>
        /// A helper method to raise the ValuesChanged event.
        /// </summary>
        /// <param name="arg"> </param>
        /// <param name="arg2"> </param>
        protected ValuesChangedEventArgs RaiseValuesChangedEvent(string arg, List<double> arg2)
        {
            return RaiseValuesChangedEvent(this, arg, arg2);
        }

        /// <summary>
        /// A static helper method to raise the ValuesChanged event on a target element.
        /// </summary>
        /// <param name="target">UIElement or ContentElement on which to raise the event</param>
        /// <param name="name"> </param>
        /// <param name="values"> </param>
        internal static ValuesChangedEventArgs RaiseValuesChangedEvent(DependencyObject target, string name, List<double> values)
        {
            if (target == null) return null;

            var args = new ValuesChangedEventArgs(name, values) {
                                                                    RoutedEvent = ValuesChangedEvent
                                                                };
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion
        
        public delegate void ValuesChangedEventHandler(object sender, ValuesChangedEventArgs e);

        /// <summary>
        /// Provides custom event args for the ValuesChanged event.
        /// </summary>
        public class ValuesChangedEventArgs : RoutedEventArgs
        {
            /// <summary>
            /// ValuesChangedEventArgsEventArgs Constructor
            /// </summary>
            /// <param name="name">Specifies the value for the Name property.</param>
            /// <param name="values">Specifies the value for the PointDataSource property.</param>
            internal ValuesChangedEventArgs(string name, List<double> values)
            {
                Name = name;
                PointDataSource = values;
            }

            /// <summary>
            /// Gets the value of the Name property.
            /// This property indicates the name of the data source.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Gets the value of the PointDataSource property.
            /// This property indicates the changed values.
            /// </summary>
            public List<double> PointDataSource { get; private set; }
        }



        //public event ValuesChangedEventHandler ValuesChanged;

        //public void OnValuesChanged(string name, List<double> values)
        //{
        //    var handler = ValuesChanged;
        //    if (handler != null) handler(this, new ValuesChangedEventArgs(name, values));
        //}

        private void DeviceOnUpdated(object sender, EventArgs e)
        {
            var device = sender as TouchDevice;
            if (device == null) return;
            var position = device.GetTouchPoint(this).Position;
            var smallestDistance = double.MaxValue;
            var nearestIndexOnLine = -1;
            var scale = Maximum - Minimum;
            double angle;
            nearestLine = capturedLines.ContainsKey(device.Id)
                                       ? capturedLines[device.Id]
                                       : null;
            if (nearestLine == null)
                foreach (var line in Lines)
                {
                    for (var j = 0; j < line.PointDataSource.Count; j++)
                    {
                        angle = j * deltaAngle;
                        var curRadius = minRadius + (line.PointDataSource[j] - Minimum) * deltaRadius / scale;
                        var p2 = GetPoint(center, curRadius, angle);
                        var dist = Distance(p2, position);
                        if (dist > InteractivePointRadius || dist > smallestDistance) continue;
                        smallestDistance = dist;
                        nearestLine = capturedLines[device.Id] = line;
                        nearestIndexOnLine = j;
                    }
                }
            else
            {
                var line = nearestLine;
                for (var j = 0; j < line.PointDataSource.Count; j++)
                {
                    angle = j * deltaAngle;
                    var curRadius = minRadius + (line.PointDataSource[j] - Minimum) * deltaRadius / scale;
                    var p2 = GetPoint(center, curRadius, angle);
                    var dist = Distance(p2, position);
                    if (dist > InteractivePointRadius || dist > smallestDistance) continue;
                    smallestDistance = dist;
                    nearestIndexOnLine = j;
                }
            }

            if (nearestIndexOnLine < 0 || nearestLine == null) return;
            ComputeNewValue(position, nearestIndexOnLine);
        }

        private void ComputeNewValue(Point position, int nearestIndexOnLine)
        {
            var scale = Maximum - Minimum;
            var newValue = Math.Max(Minimum, Math.Min(Minimum + scale * (Distance(center, position) - minRadius) / deltaRadius, Maximum));
            nearestLine.PointDataSource[nearestIndexOnLine] = newValue;
            InvalidateVisual();
            RaiseValuesChangedEvent(nearestLine.Name, nearestLine.PointDataSource);
        }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !IsInteractive) return;
            var position = e.GetPosition(this);

            var smallestDistance = double.MaxValue;
            var nearestIndexOnLine = -1;
            var scale = Maximum - Minimum;
            double angle;
            if (nearestLine == null)
                foreach (var line in Lines.Where(k=>k.CanEdit))
                {
                    for (var j = 0; j < line.PointDataSource.Count; j++)
                    {
                        angle = j*deltaAngle;
                        var curRadius = minRadius + (line.PointDataSource[j] - Minimum)*deltaRadius/scale;
                        var p2 = GetPoint(center, curRadius, angle);
                        var dist = Distance(p2, position);
                        if (dist > InteractivePointRadius || dist > smallestDistance) continue;
                        smallestDistance = dist;
                        nearestLine = line;
                        nearestIndexOnLine = j;
                    }
                }
            else
            {
                var line = nearestLine;
                for (var j = 0; j < line.PointDataSource.Count; j++)
                {
                    angle = j * deltaAngle;
                    var curRadius = minRadius + (line.PointDataSource[j] - Minimum) * deltaRadius / scale;
                    var p2 = GetPoint(center, curRadius, angle);
                    var dist = Distance(p2, position);
                    if (dist > InteractivePointRadius || dist > smallestDistance) continue;
                    smallestDistance = dist;
                    nearestIndexOnLine = j;
                }

            }

            if (nearestIndexOnLine < 0 || nearestLine == null) return;
            ComputeNewValue(position, nearestIndexOnLine);
        }

        #region Minimum

        /// <summary>
        ///   Minimum Dependency Property
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(SpiderChartPanel), new FrameworkPropertyMetadata((double)0, OnMinimumChanged));

        /// <summary>
        ///   Gets or sets the Minimum property. This dependency property 
        ///   indicates ....
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        ///   Handles changes to the Minimum property.
        /// </summary>
        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldMinimum = (double)e.OldValue;
            var newMinimum = target.Minimum;
            target.InvalidateVisual();
            target.OnMinimumChanged(oldMinimum, newMinimum);
        }

        /// <summary>
        ///   Provides derived classes an opportunity to handle changes to the Minimum property.
        /// </summary>
        protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
        }

        #endregion

        #region Maximum

        /// <summary>
        ///   Maximum Dependency Property
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(SpiderChartPanel), new FrameworkPropertyMetadata((double)1, OnMaximumChanged));

        /// <summary>
        ///   Gets or sets the Maximum property. This dependency property 
        ///   indicates ....
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        ///   Handles changes to the Maximum property.
        /// </summary>
        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldMaximum = (double)e.OldValue;
            var newMaximum = target.Maximum;
            target.InvalidateVisual();
            target.OnMaximumChanged(oldMaximum, newMaximum);
        }

        /// <summary>
        ///   Provides derived classes an opportunity to handle changes to the Maximum property.
        /// </summary>
        protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
        }

        #endregion

        #region HoleRadius

        /// <summary>
        /// HoleRadius Dependency Property
        /// </summary>
        public static readonly DependencyProperty HoleRadiusProperty =
            DependencyProperty.Register("HoleRadius", typeof(double), typeof(SpiderChartPanel),
                new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.AffectsRender, OnHoleRadiusChanged));

        /// <summary>
        /// Gets or sets the HoleRadius property. This dependency property 
        /// indicates the size of the hole inside the control (default is 0, or no hole).
        /// </summary>
        public double HoleRadius
        {
            get { return (double)GetValue(HoleRadiusProperty); }
            set { SetValue(HoleRadiusProperty, value); }
        }

        /// <summary>
        /// Handles changes to the HoleRadius property.
        /// </summary>
        private static void OnHoleRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldHoleRadius = (double)e.OldValue;
            var newHoleRadius = target.HoleRadius;
            target.InvalidateVisual();
            target.OnHoleRadiusChanged(oldHoleRadius, newHoleRadius);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the HoleRadius property.
        /// </summary>
        protected virtual void OnHoleRadiusChanged(double oldHoleRadius, double newHoleRadius)
        {
        }

        #endregion

        #region Ticks

        /// <summary>
        ///   Ticks Dependency Property
        /// </summary>
        public static readonly DependencyProperty TicksProperty = DependencyProperty.Register("Ticks", typeof(int), typeof(SpiderChartPanel),
            new FrameworkPropertyMetadata(10, OnTicksChanged));

        /// <summary>
        ///   Gets or sets the Ticks property. This dependency property 
        ///   indicates how many ticks you wish to display along the axis between min and max.
        /// </summary>
        public int Ticks
        {
            get { return (int)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        /// <summary>
        ///   Handles changes to the Ticks property.
        /// </summary>
        private static void OnTicksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldTicks = (int)e.OldValue;
            var newTicks = target.Ticks;
            target.InvalidateVisual();
            target.OnTicksChanged(oldTicks, newTicks);
        }

        /// <summary>
        ///   Provides derived classes an opportunity to handle changes to the Ticks property.
        /// </summary>
        protected virtual void OnTicksChanged(double oldTicks, double newTicks)
        {
        }

        #endregion

        #region Lines

        /// <summary>
        /// Lines Dependency Property (acts as a kind of ItemsSource, triggering a redraw when the collection is changed)
        /// </summary>
        public static readonly DependencyProperty LinesProperty =
            DependencyProperty.Register("Lines", typeof(IEnumerable<ChartLine>), typeof(SpiderChartPanel), new FrameworkPropertyMetadata(null, OnLinesChanged));

        /// <summary>
        /// Gets or sets the Lines property.
        /// </summary>
        public IEnumerable<ChartLine> Lines
        {
            get { return (IEnumerable<ChartLine>)GetValue(LinesProperty); }
            set { SetValue(LinesProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Lines property.
        /// </summary>
        private static void OnLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldLines = (IEnumerable<ChartLine>)e.OldValue;
            var newLines = target.Lines;

            // Remove handler
            var oldValueINotifyCollectionChanged = (e.OldValue as IEnumerable) as INotifyCollectionChanged;
            if (oldValueINotifyCollectionChanged != null)
                oldValueINotifyCollectionChanged.CollectionChanged -= target.LinesCollectionChanged;

            // Add handler in case the Lines collection implements INotifyCollectionChanged
            var newValueINotifyCollectionChanged = (e.NewValue as IEnumerable) as INotifyCollectionChanged;
            if (newValueINotifyCollectionChanged != null)
                newValueINotifyCollectionChanged.CollectionChanged += target.LinesCollectionChanged;

            target.InvalidateVisual();
            target.OnLinesChanged(oldLines, newLines);
        }

        private void LinesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Lines property.
        /// </summary>
        protected virtual void OnLinesChanged(IEnumerable<ChartLine> oldLines, IEnumerable<ChartLine> newLines)
        {
        }

        #endregion

        #region IsInteractive

        /// <summary>
        /// IsInteractive Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsInteractiveProperty =
            DependencyProperty.Register("IsInteractive", typeof(bool), typeof(SpiderChartPanel),
                new FrameworkPropertyMetadata(false, OnIsInteractiveChanged));

        /// <summary>
        /// Gets or sets the IsInteractive property. This dependency property 
        /// indicates whether the diagrams are interactive via touch of mouse.
        /// </summary>
        public bool IsInteractive
        {
            get { return (bool)GetValue(IsInteractiveProperty); }
            set { SetValue(IsInteractiveProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsInteractive property.
        /// </summary>
        private static void OnIsInteractiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldIsInteractive = (bool)e.OldValue;
            var newIsInteractive = target.IsInteractive;
            target.InvalidateVisual();
            target.OnIsInteractiveChanged(oldIsInteractive, newIsInteractive);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsInteractive property.
        /// </summary>
        protected virtual void OnIsInteractiveChanged(bool oldIsInteractive, bool newIsInteractive)
        {
        }

        #endregion

        #region BackgroundColor

        /// <summary>
        /// BackgroundColor Dependency Property
        /// </summary>
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(SpiderChartPanel),
                new FrameworkPropertyMetadata(Colors.Transparent, OnBackgroundColorChanged));

        /// <summary>
        /// Gets or sets the BackgroundColor property. This dependency property 
        /// indicates the background color to use. It does not use the regular background
        /// property, since we may need to draw a hole inside.
        /// </summary>
        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        /// <summary>
        /// Handles changes to the BackgroundColor property.
        /// </summary>
        private static void OnBackgroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (SpiderChartPanel)d;
            var oldBackgroundColor = (Color)e.OldValue;
            var newBackgroundColor = target.BackgroundColor;
            target.InvalidateVisual();
            target.OnBackgroundColorChanged(oldBackgroundColor, newBackgroundColor);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the BackgroundColor property.
        /// </summary>
        protected virtual void OnBackgroundColorChanged(Color oldBackgroundColor, Color newBackgroundColor)
        {
        }

        #endregion

        // TODO If the Line's use a Fill color, draw a hole in it
        // TODO Implement IsInteractive
        // TODO When a background color is specified, and the background is not Null, create a layer with a hole?

        protected override void OnRender(DrawingContext dc)
        {
            if (Children.Count < 1 || Ticks < 0) return;

            spokes = Children.Count;
            deltaAngle = 360D / spokes;
            center = new Point(RenderSize.Width / 2, RenderSize.Height / 2);
            radius = Math.Min(RenderSize.Width, RenderSize.Height) / 2;
            //dc.DrawEllipse(null, pen, center, radius, radius);

            // Draw the background ticks between minRadius and maxRadius
            var scale = Maximum - Minimum;
            minRadius = Math.Max(radius / scale, HoleRadius);
            maxRadius = radius * .8;
            deltaRadius = (maxRadius - minRadius);
            deltaTickRadius = deltaRadius / (Ticks - 1);
            spokeLength = maxRadius + 10;

            DrawBackground(dc);

            for (var i = HoleRadius > 0 ? 1 : 0; i < Ticks; i++)
            {
                var curRadius = minRadius + i*deltaTickRadius;
                var angle = 0D;
                var p1 = GetPoint(center, curRadius, angle);
                for (var j = 0; j < spokes; j++)
                {
                    angle = (j + 1)*deltaAngle;
                    var p2 = GetPoint(center, curRadius, angle);
                    dc.DrawLine(DashedPen, p1, p2);
                    p1 = p2;
                }
                // Draw the labels
                p1 = new Point(p1.X + 5, p1.Y - 15);
                if (i == 0)
                    dc.DrawText(new FormattedText(Minimum.ToString(CultureInfo.InvariantCulture), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, LabelFont, 14, Brushes.Black), p1);
                else if (i == Ticks - 1)
                    dc.DrawText(new FormattedText(Maximum.ToString(CultureInfo.InvariantCulture), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, LabelFont, 14, Brushes.Black), p1);
            }

            // Draw the spokes
            for (var i = 0; i < spokes; i++)
            {
                var angle = i * deltaAngle;
                var p1 = GetPoint(center, spokeLength, angle);
                var p2 = HoleRadius <= 0
                             ? center
                             : GetPoint(center, HoleRadius, angle);
                dc.DrawLine(Pen, p1, p2);
                if (IsInteractive)
                    dc.DrawLine(InvisiblePen, p1, p2);
            }

            // Draw the chart lines
            if (Lines == null) return;
            if (scale <= 0) return;
            
            foreach (var line in Lines)
            {
                if (line.PointDataSource.Any())
                {
                    var angle = 0D;
                    var curRadius = minRadius + (line.PointDataSource[0] - Minimum)*deltaRadius/scale;
                    var p1 = GetPoint(center, curRadius, angle);
                    var myPathFigure = new PathFigure
                                           {
                                               StartPoint = p1,
                                               Segments = new PathSegmentCollection()
                                           };
                    var pts = new PointCollection(spokes) {p1};
                    for (var j = 1; j < spokes; j++)
                    {
                        angle = j*deltaAngle;
                        curRadius = minRadius +
                                    ((j >= line.PointDataSource.Count ? Minimum : line.PointDataSource[j]) - Minimum)*
                                    deltaRadius/scale;
                        var p2 = GetPoint(center, curRadius, angle);
                        myPathFigure.Segments.Add(new LineSegment {Point = p2});
                        pts.Add(p2);
                    }
                    myPathFigure.Segments.Add(new LineSegment {Point = p1});
                    var myPathGeometry = new PathGeometry {Figures = new PathFigureCollection {myPathFigure}};
                    var pen = new Pen(new SolidColorBrush(line.LineColor), line.LineThickness);
                    //dc.DrawGeometry(line.FillColor == Colors.Transparent 
                    //    ? null 
                    //    : new SolidColorBrush(line.FillColor), pen, myPathGeometry);

                    if (HoleRadius > 0 || line.FillColor == Colors.Transparent)
                    {
                        var hole = new EllipseGeometry(center, HoleRadius, HoleRadius);
                        var geo = new CombinedGeometry(myPathGeometry, hole)
                                      {
                                          GeometryCombineMode = GeometryCombineMode.Exclude
                                      };
                        dc.DrawGeometry(new SolidColorBrush(line.FillColor), pen, geo);
                    }
                    else
                        dc.DrawGeometry(null, pen, myPathGeometry);

                    if (line.CanEdit)
                    {
                        // Draw fat circles on each data point
                        var brush = new SolidColorBrush(line.LineColor);
                        var pointRadius = IsInteractive
                            ? InteractivePointRadius
                            : line.LineThickness + 2;
                        foreach (var pt in pts)
                            dc.DrawEllipse(brush, pen, pt, pointRadius, pointRadius);
                    }
                }
            }
        }

        /// <summary>
        /// Draw background (with hole)
        /// </summary>
        /// <param name="dc"></param>
        private void DrawBackground(DrawingContext dc)
        {
            var rec = new RectangleGeometry(new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            var hole = new EllipseGeometry(center, HoleRadius, HoleRadius);
            var geo = new CombinedGeometry(rec, hole) {
                                                         GeometryCombineMode = GeometryCombineMode.Exclude
                                                     };
            dc.DrawGeometry(new SolidColorBrush(BackgroundColor), null, geo);
        }

        private const double Degree2Rad = Math.PI/180D;
        private static Point GetPoint(Point center, double radius, double angle)
        {
            var radAngle = angle * Degree2Rad;
            var x = center.X + radius * Math.Sin(radAngle);
            var y = center.Y - radius * Math.Cos(radAngle);
            return new Point(x, y);
        }

        ///// <summary>
        ///// Project a point onto a line
        ///// </summary>
        ///// <param name="line1">Point 1 on line</param>
        ///// <param name="line2">Point 1 on line</param>
        ///// <param name="toProject">Point to project</param>
        ///// <returns>Point projected perpendicular onto line</returns>
        //private static Point Project(Point line1, Point line2, Point toProject)
        //{
        //    var m = (line2.Y - line1.Y) / Math.Max(1, line2.X - line1.X);
        //    var b = line1.Y - (m * line1.X);

        //    var x = (m * toProject.Y + toProject.X - m * b) / (m * m + 1);
        //    var y = (m * m * toProject.Y + m * toProject.X + b) / (m * m + 1);

        //    return new Point((int)x, (int)y);
        //}

        /// <summary>
        /// Computes the Euclidean distance between pt1 and pt2
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        private static double Distance(Point pt1, Point pt2)
        {
            return Math.Sqrt((pt1.X - pt2.X)*(pt1.X - pt2.X) + (pt1.Y - pt2.Y)*(pt1.Y - pt2.Y));
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (UIElement elem in Children)
            {
                //Give Infinite size as the avaiable size for all the children
                elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            InvalidateVisual();
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        ///   Arrange all children based on the geometric equations for the circle.
        /// </summary>
        /// <param name="finalSize"> </param>
        /// <returns> </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0 || (RenderSize.Width <= 0 && RenderSize.Height <= 0))
                return finalSize;

            double angle = 0;

            //Degrees converted to Radian by multiplying with PI/180
            var incrementalAngularSpace = (360.0 / Children.Count) * (Math.PI / 180);
            //An approximate radii based on the avialable size , obviusly a better approach is needed here.
            const double d = 2.15;
            var minSide = Math.Min(RenderSize.Width, RenderSize.Height);
            var radiusX = minSide / d;
            var radiusY = minSide / d;

            foreach (UIElement elem in Children)
            {
                //Calculate the point on the circle for the element
                var childPoint = new Point(Math.Sin(angle) * radiusX, -Math.Cos(angle) * radiusY);

                //Offsetting the point to the Avalable rectangular area which is FinalSize.
                var actualChildPoint = new Point(finalSize.Width / 2 + childPoint.X - elem.DesiredSize.Width / 2, finalSize.Height / 2 + childPoint.Y - elem.DesiredSize.Height / 2);

                //Call Arrange method on the child element by giving the calculated point as the placementPoint.
                elem.Arrange(new Rect(actualChildPoint.X, actualChildPoint.Y, elem.DesiredSize.Width, elem.DesiredSize.Height));

                //Calculate the new _angle for the next element
                angle += incrementalAngularSpace;
            }

            return finalSize;
        }
    }
}