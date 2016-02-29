using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace csShared.Controls
{
  public class OrientedRadialPanel : Canvas
  {
    // Measure each children and give as much room as they want 



    public double StartAngle
    {
      get { return (double)GetValue(StartAngleProperty); }
      set { SetValue(StartAngleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for StartAngle. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty StartAngleProperty =
      DependencyProperty.Register("StartAngle", typeof(double), typeof(OrientedRadialPanel), new UIPropertyMetadata(0.0));



    public double MaxAngle
    {
      get { return (double)GetValue(MaxAngleProperty); }
      set { SetValue(MaxAngleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MaxAngle. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MaxAngleProperty =
      DependencyProperty.Register("MaxAngle", typeof(double), typeof(OrientedRadialPanel), new UIPropertyMetadata(0.0));

    


    public double FixedAngle
    {
      get { return (double)GetValue(FixedAngleProperty); }
      set { SetValue(FixedAngleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for FixedAngle. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FixedAngleProperty =
      DependencyProperty.Register("FixedAngle", typeof(double), typeof(OrientedRadialPanel), new UIPropertyMetadata(0.0));



    public static readonly DependencyProperty AngleProperty = DependencyProperty.RegisterAttached(
      "Angle",
      typeof(double),
      typeof(OrientedRadialPanel),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static double GetAngle(DependencyObject obj)
    {
      
      return (double)obj.GetValue(AngleProperty);
    }

    public static void SetAngle(DependencyObject obj, double value)
    {
      obj.SetValue(AngleProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {

      foreach (UIElement elem in Children)
      {

        //Give Infinite size as the avaiable size for all the children
        try
        {
          elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }
        catch (Exception)
        {
          
          //throw;
        }
        

      }

      return base.MeasureOverride(availableSize);

    }



    //Arrange all children based on the geometric equations for the circle.

    protected override Size ArrangeOverride(Size finalSize)
    {

      if (Children.Count == 0)

        return finalSize;



      double _angle = StartAngle * (Math.PI/180);



      //Degrees converted to Radian by multiplying with PI/180

      

      double _incrementalAngularSpace = (360.0 / Children.Count) * (Math.PI / 180);
      if (MaxAngle > 0) _incrementalAngularSpace = Math.Min(MaxAngle * (Math.PI / 180), _incrementalAngularSpace);
      if (FixedAngle > 0) _incrementalAngularSpace = FixedAngle;

      



      //An approximate radii based on the avialable size , obviusly a better approach is needed here.

      double radiusX = finalSize.Width / 0.4;

      double radiusY = finalSize.Height / 0.4;



      foreach (UIElement elem in Children)
      {

        //Calculate the point on the circle for the element



        Point childPoint = new Point(Math.Cos(_angle) * radiusX, -Math.Sin(_angle) * radiusY);

        //Offsetting the point to the Avalable rectangular area which is FinalSize.

        Point actualChildPoint = new Point(finalSize.Width / 2 , finalSize.Height / 2 );



        //Call Arrange method on the child element by giving the calculated point as the placementPoint.

        elem.Arrange(new Rect(actualChildPoint.X, actualChildPoint.Y, elem.DesiredSize.Width, elem.DesiredSize.Height));
        double a = _angle / (Math.PI / 180);
        SetAngle(elem, a);
        elem.RenderTransform = new RotateTransform(a);


        //Calculate the new _angle for the next element

        _angle += _incrementalAngularSpace;



      }



      return finalSize;

    }
  }
}