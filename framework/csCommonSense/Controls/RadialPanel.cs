
  #region

  using System;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;

  #endregion

  namespace csShared.Controls
  {
    /// <summary>
    ///  A radial panel
    /// </summary>
    /// <see cref = "http://www.wintellect.com/CS/blogs/jprosise/archive/2009/05/04/radial-layout-in-silverlight.aspx" />
    public class RadialPanel : Panel
    {
      #region Radius
      /// <summary>
      /// Radius Dependency Property
      /// </summary>
      public static readonly DependencyProperty RadiusProperty =
        DependencyProperty.Register("Radius", typeof(double), typeof(RadialPanel),
          new FrameworkPropertyMetadata((double)0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange,
            OnRadiusChanged));

      /// <summary>
      /// Gets or sets the Radius property. This dependency property 
      /// indicates the radius of the radial panel.
      /// </summary>
      public double Radius
      {
        get { return (double)GetValue(RadiusProperty); }
        set { SetValue(RadiusProperty, value); }
      }

      /// <summary>
      /// Handles changes to the Radius property.
      /// </summary>
      private static void OnRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
        //var panel = (RadialPanel)d;
        //panel.Refresh(new Size(panel.Width, panel.Height));
        ((RadialPanel)d).OnRadiusChanged(e);
      }

      /// <summary>
      /// Provides derived classes an opportunity to handle changes to the Radius property.
      /// </summary>
      protected virtual void OnRadiusChanged(DependencyPropertyChangedEventArgs e)
      {
      }
      #endregion

      #region ItemAlignment

      /// <summary>
      /// ItemAlignment Dependency Property
      /// </summary>
      public static readonly DependencyProperty ItemAlignmentProperty =
        DependencyProperty.Register("ItemAlignment", typeof(ItemAlignmentOptions), typeof(RadialPanel),
          new FrameworkPropertyMetadata(ItemAlignmentOptions.Center, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
            OnItemAlignmentChanged));

      /// <summary>
      /// Gets or sets the ItemAlignment property. This dependency property 
      /// indicates the alignment of the items in the panel.
      /// </summary>
      public ItemAlignmentOptions ItemAlignment
      {
        get { return (ItemAlignmentOptions)GetValue(ItemAlignmentProperty); }
        set { SetValue(ItemAlignmentProperty, value); }
      }

      /// <summary>
      /// Handles changes to the ItemAlignment property.
      /// </summary>
      private static void OnItemAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
        //var panel = (RadialPanel)d;
        //panel.Refresh(new Size(panel.Width, panel.Height));
        ((RadialPanel)d).OnItemAlignmentChanged(e);
      }

      /// <summary>
      /// Provides derived classes an opportunity to handle changes to the ItemAlignment property.
      /// </summary>
      protected virtual void OnItemAlignmentChanged(DependencyPropertyChangedEventArgs e)
      {
      }

      #endregion

      #region ItemOrientation

      /// <summary>
      /// ItemOrientation Dependency Property
      /// </summary>
      public static readonly DependencyProperty ItemOrientationProperty =
        DependencyProperty.Register("ItemOrientation", typeof(ItemOrientationOptions), typeof(RadialPanel),
          new FrameworkPropertyMetadata(ItemOrientationOptions.Upright, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange,
            OnItemOrientationChanged));

      /// <summary>
      /// Gets or sets the ItemOrientation property. This dependency property 
      /// indicates the orientation of the items.
      /// </summary>
      public ItemOrientationOptions ItemOrientation
      {
        get { return (ItemOrientationOptions)GetValue(ItemOrientationProperty); }
        set { SetValue(ItemOrientationProperty, value); }
      }

      /// <summary>
      /// Handles changes to the ItemOrientation property.
      /// </summary>
      private static void OnItemOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
        //var panel = (RadialPanel)d;
        //panel.Refresh(new Size(panel.Width, panel.Height));
        ((RadialPanel)d).OnItemOrientationChanged(e);
      }

      /// <summary>
      /// Provides derived classes an opportunity to handle changes to the ItemOrientation property.
      /// </summary>
      protected virtual void OnItemOrientationChanged(DependencyPropertyChangedEventArgs e)
      {
      }

      #endregion

      protected override Size MeasureOverride(Size availableSize)
      {
        if (!double.IsPositiveInfinity(availableSize.Width) && !double.IsPositiveInfinity(availableSize.Height)) return availableSize;
        // Compute our own desired size, taking into account the fact
        // that availableSize could specify infinite widths and heights
        // (which are not valid return values)
        var max = new Size(0.0, 0.0);

        // Call Measure on each child and record the maximum
        // width and height of the child elements
        foreach (UIElement element in Children)
        {
          element.Measure(availableSize);
          max.Width = Math.Max(max.Width, element.DesiredSize.Width);
          max.Height = Math.Max(max.Height, element.DesiredSize.Height);
        }

        // Return our desired size
        var radius = DesiredRadius(max);
        var diameter = 2 * radius + Math.Max(max.Width, max.Height);
        return new Size(diameter, diameter);
      }

      protected override Size ArrangeOverride(Size finalSize)
      {
        // Size and position the child elements
        Refresh(finalSize);
        return finalSize;
      }

      private double DesiredRadius(Size max)
      {
        if (Radius != 0) return Radius;
        switch (ItemOrientation)
        {
          case ItemOrientationOptions.Radial:
            var desiredCircumference = max.Width * Children.Count;
            return desiredCircumference / (2 * Math.PI);
          default:
            var desiredCircumference2 = max.Height * Children.Count;
            return desiredCircumference2 / (2 * Math.PI);
        }
      }

      // Helper methods
      private void Refresh(Size size)
      {
        if (Children == null || Children.Count == 0) return;
        var i = 0;
        var inc = 360.0 / Children.Count;

        var radius = Radius;
        if (radius == 0)
        {
          var maxItemHeight = ItemOrientation == ItemOrientationOptions.Radial
            ? Children.Cast<FrameworkElement>().Max(child => child.DesiredSize.Height)
            : Children.Cast<FrameworkElement>().Max(child => child.DesiredSize.Width);
          var height = size.Height + Margin.Top + Margin.Bottom;
          var width = size.Width + Margin.Left + Margin.Right;
          radius = (Math.Min(width, height) - maxItemHeight) / 2;
        }
        foreach (FrameworkElement element in Children)
        {
          var width = 0.0;
          var height = 0.0;

          switch (ItemAlignment)
          {
            case ItemAlignmentOptions.Left:
              break;

            case ItemAlignmentOptions.Center:
              width = element.DesiredSize.Width / 2.0;
              height = element.DesiredSize.Height / 2.0;
              break;

            case ItemAlignmentOptions.Right:
              width = element.DesiredSize.Width;
              height = element.DesiredSize.Height;
              break;
          }

          var angle = inc * i++;

          switch (ItemOrientation)
          {
            case ItemOrientationOptions.Rotated:
              var transform = new RotateTransform { CenterX = width, CenterY = height, Angle = 90 + angle };
              element.RenderTransform = transform;
              break;
            case ItemOrientationOptions.Radial:
              var transform2 = new RotateTransform { CenterX = width, CenterY = height, Angle = angle };
              element.RenderTransform = transform2;
              break;
            default:
              break;
          }

          var x = radius * Math.Sin((Math.PI * angle) / 180.0);
          var y = radius * Math.Cos((Math.PI * angle) / 180.0);

          element.Arrange(new Rect((x + (size.Width / 2.0)) - width, (-y + (size.Height / 2.0)) - height, element.DesiredSize.Width,
                       element.DesiredSize.Height));
        }
      }
    }

    // Enums
    public enum ItemAlignmentOptions
    {
      Left,
      Center,
      Right
    }

    public enum ItemOrientationOptions
    {
      Upright,
      Radial,
      Rotated
    }
  }
