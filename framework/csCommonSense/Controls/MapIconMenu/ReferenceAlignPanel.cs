using System;
using System.Windows;
using System.Windows.Controls;

namespace csCommon.csMapCustomControls.MapIconMenu
{
    public class ReferenceAlignPanel : Panel, ICustomAlignedControl
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            AdjustAlignReferencePoint(new Size());

            if (!Double.IsNaN(InitialAlignReferencePoint.X) || !Double.IsNaN(InitialAlignReferencePoint.Y))
                AllowRealign = false;
        }

        #region Dependency Properties
        public static readonly DependencyProperty VerticalReferencePointAlignmentProperty =
            DependencyProperty.Register(
                "VerticalReferencePointAlignment",
                typeof(VerticalAlignment),
                typeof(ReferenceAlignPanel), 
                new FrameworkPropertyMetadata
                    (VerticalAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsArrange));

        public VerticalAlignment VerticalReferencePointAlignment
        {
            get
            {
                return (VerticalAlignment)GetValue(VerticalReferencePointAlignmentProperty);
            }
            set
            {
                SetValue(VerticalReferencePointAlignmentProperty, value);
            }
        }

        public static readonly DependencyProperty HorizontalReferencePointAlignmentProperty =
            DependencyProperty.Register(
                "HorizontalReferencePointAlignment",
                typeof(HorizontalAlignment),
                typeof(ReferenceAlignPanel), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsArrange));

        public HorizontalAlignment HorizontalReferencePointAlignment 
        {
            get
            {
                return (HorizontalAlignment)GetValue(HorizontalReferencePointAlignmentProperty);
            }
            set
            {
                SetValue(HorizontalReferencePointAlignmentProperty, value);
            }
        }

        public static readonly DependencyProperty InitialAlignReferencePointProperty =
            DependencyProperty.Register(
                "InitialAlignReferencePoint",
                typeof(Point),
                typeof(ReferenceAlignPanel), new FrameworkPropertyMetadata(new Point(Double.NaN, Double.NaN), FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public Point InitialAlignReferencePoint
        {
            get
            {
                return (Point)GetValue(InitialAlignReferencePointProperty);
            }
            set
            {
                SetValue(InitialAlignReferencePointProperty, value);
            }
        }

        public static readonly DependencyProperty AllowRealignProperty =
            DependencyProperty.Register(
                "AllowRealign",
                typeof(bool),
                typeof(ReferenceAlignPanel), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool AllowRealign
        {
            get
            {
                return (bool)GetValue(AllowRealignProperty);
            }
            set
            {
                SetValue(AllowRealignProperty, value);
            }
        }
        #endregion

        private Point alignReferencePoint;

        public Point AlignReferencePoint
        {
            get { return alignReferencePoint; }
            set { alignReferencePoint = value; }
        }

        public delegate void OnAlignReferencePointChanged(object sender);

        public event OnAlignReferencePointChanged AlignReferencePointChanged;


        private void AdjustAlignReferencePoint(Size bounds)
        {
            alignReferencePoint = InitialAlignReferencePoint;

            // This is not very straightforward. Also, putting the align reference point on a border is
            // never a good idea.
            switch (HorizontalReferencePointAlignment)
            {
                case HorizontalAlignment.Center:
                    alignReferencePoint.X = bounds.Width * 0.5;
                    break;

                case HorizontalAlignment.Left:
                    alignReferencePoint.X = 0;
                    break;

                case HorizontalAlignment.Right:
                    alignReferencePoint.X = bounds.Width;
                    break;
            }

            switch (VerticalReferencePointAlignment)
            {
                case VerticalAlignment.Center:
                    alignReferencePoint.Y = bounds.Height * 0.5;
                    break;

                case VerticalAlignment.Top:
                    alignReferencePoint.Y = 0;
                    break;

                case VerticalAlignment.Bottom:
                    alignReferencePoint.Y = bounds.Height;
                    break;
            }

            if (Double.IsNaN(alignReferencePoint.X) || Double.IsInfinity(alignReferencePoint.X))
                alignReferencePoint.X = 0;

            if (Double.IsNaN(alignReferencePoint.Y) || Double.IsInfinity(alignReferencePoint.Y))
                alignReferencePoint.Y = 0;

            if (null != AlignReferencePointChanged)
                AlignReferencePointChanged(this);
        }

        private Vector GetChildOffset(UIElement child)
        {
            Vector childDesiredOffset;

            var control = child as ICustomAlignedControl;
            if (control != null)
            {
                childDesiredOffset = AlignReferencePoint - control.AlignReferencePoint;
            }
            else
            {
                // TODO: Honor the children's align properties
                childDesiredOffset = new Vector();

                childDesiredOffset.X = alignReferencePoint.X - child.DesiredSize.Width * 0.5;
                childDesiredOffset.Y = alignReferencePoint.Y - child.DesiredSize.Height * 0.5;
            }

            if (!AllowRealign) return childDesiredOffset;
            // If this happens, we have to re-measure all items!
            if (childDesiredOffset.X < 0)
            {
                alignReferencePoint.X -= childDesiredOffset.X;
                childDesiredOffset.X = 0;
                realignRequired = true;
            }

            if (!(childDesiredOffset.Y < 0)) return childDesiredOffset;
            alignReferencePoint.Y -= childDesiredOffset.Y;
            childDesiredOffset.Y = 0;
            realignRequired = true;

            return childDesiredOffset;
        }

        bool realignRequired;

        protected override Size ArrangeOverride(Size finalSize)
        {
            var arrangeCount = 0;

            // realign should not be required, but you can't bet on it. Protect from infinite iteration.
            do
            {
                foreach (UIElement child in Children)
                {
                    if (!child.IsVisible) continue;
                    var childOffset = GetChildOffset(child);
                    try
                    {
                        child.Arrange(new Rect(childOffset.X, childOffset.Y, child.DesiredSize.Width, child.DesiredSize.Height));
                    }
                    catch (Exception)
                    {
                        //throw;
                    }
                }

                ++arrangeCount;
            } while (realignRequired && arrangeCount < 2);

            return finalSize;
        }
        

        protected override Size MeasureOverride(Size availableSize)
        {
            var inifiniteSize = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            AdjustAlignReferencePoint(availableSize);

            var bMeasureNecessary = true;
            const int iMaxRemeasureCount = 4;

            var neededSize = new Size();

            // ugly and convoluted
            // A high number of remeasures will not happen - except for the vs designer, it seems. Removing 
            // this check will crash VS
            // FIXME TODO: Clean this up or use different behaviour in design mode!
            for (var i = 0; bMeasureNecessary && i < iMaxRemeasureCount; ++i )
            {
                neededSize.Width = neededSize.Height = 0;

                bMeasureNecessary = false; // Assume remeasure is not needed

                var MinimumChildOffset = new Vector(Double.MaxValue, Double.MaxValue);

                foreach (UIElement child in Children)
                {
                    try
                    {
                        child.Measure(inifiniteSize);

                    }
                    catch (Exception)
                    {
                    }
                    
                    if (child.IsVisible == false)
                    {
                        continue;
                    }

                    var childDesiredOffset = GetChildOffset(child);

                    MinimumChildOffset.X = Math.Min(MinimumChildOffset.X, childDesiredOffset.X);
                    MinimumChildOffset.Y = Math.Min(MinimumChildOffset.Y, childDesiredOffset.Y);

                    neededSize.Width = Math.Max(neededSize.Width, childDesiredOffset.X + child.DesiredSize.Width);
                    neededSize.Height = Math.Max(neededSize.Height, childDesiredOffset.Y + child.DesiredSize.Height);
                }

                if (!AllowRealign) continue;
                if (MinimumChildOffset.X > 0)
                    alignReferencePoint.X -= MinimumChildOffset.X;

                if (MinimumChildOffset.Y > 0)
                    alignReferencePoint.Y -= MinimumChildOffset.Y;

                if (MinimumChildOffset.X > 0 || MinimumChildOffset.Y > 0)
                {
                    bMeasureNecessary = true;
                }
            }

            if (null != AlignReferencePointChanged)
            {
                AlignReferencePointChanged(this);
            }

            return neededSize;
        }


#if DEBUG
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);


            //Rect r = new Rect(0, 0, DesiredSize.Width - Margin.Left - Margin.Right, DesiredSize.Height - Margin.Top - Margin.Bottom);
            //drawingContext.DrawRectangle(null, new Pen(Brushes.Tomato, 2.0), r);

            // show the visual center
            //drawingContext.DrawEllipse(Brushes.SeaGreen, null, _alignReferencePoint, 3, 3);
        }
#endif
    }
}
