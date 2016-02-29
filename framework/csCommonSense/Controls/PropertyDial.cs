using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace csCommon.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:NetMatch"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:NetMatch;assembly=NetMatch"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:PropertyDial/>
    ///
    /// </summary>
    public class PropertyDial : Control
    {

        public enum ValueModes
        {
            Relative,
            Absolute
        }



        public ValueModes ValueMode
        {
            get { return (ValueModes)GetValue(ValueModeProperty); }
            set { SetValue(ValueModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueModeProperty =
            DependencyProperty.Register("ValueMode", typeof(ValueModes), typeof(PropertyDial), new PropertyMetadata(ValueModes.Relative));

        

        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Min.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(double), typeof(PropertyDial), new PropertyMetadata(0.0));




        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Max.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max", typeof(double), typeof(PropertyDial), new PropertyMetadata(100.0));




        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(PropertyDial), new FrameworkPropertyMetadata(50.0, OnValuePropertyChanged));

        

        public double CircleSize
        {
            get { return (double)GetValue(CircleSizeProperty); }
            set { SetValue(CircleSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CircleSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CircleSizeProperty =
            DependencyProperty.Register("CircleSize", typeof(double), typeof(PropertyDial), new PropertyMetadata(20.0));




        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(PropertyDial), new FrameworkPropertyMetadata(90.0, OnValuePropertyChanged));


        public event EventHandler ValueChanged;


        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartAngle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(PropertyDial), new PropertyMetadata(-90.0));

        

        private static void OnValuePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            PropertyDial pd = (PropertyDial) source;
            pd.ValueString = pd.Value.ToString(pd.ValueFormat);
            if (pd.ValueChanged != null) pd.ValueChanged(pd, null);
        }

        public double CircleOffset
        {
            get { return (double)GetValue(CircleOffsetProperty); }
            set { SetValue(CircleOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CircleOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CircleOffsetProperty =
            DependencyProperty.Register("CircleOffset", typeof(double), typeof(PropertyDial), new PropertyMetadata(100.0));

        

        static PropertyDial()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyDial), new FrameworkPropertyMetadata(typeof(PropertyDial)));

        }

        private Border bBorder;

        public override void OnApplyTemplate()
        {


            base.OnApplyTemplate();

            bBorder = (Border)this.GetTemplateChild("bCircle");
            if (bBorder == null) return;
            bBorder.MouseDown += PropertyDial_MouseDown;
            bBorder.MouseMove += PropertyDial_MouseMove;
            bBorder.MouseUp += PropertyDial_MouseUp;

            bBorder.TouchMove += PropertyDial_TouchMove;
            bBorder.TouchDown += PropertyDial_TouchDown;
            bBorder.TouchUp += PropertyDial_TouchUp;
            gCircleBase = this.GetTemplateChild("gCircleBase") as Grid;
            if (gCircleBase!=null)
            {
                
            }
        }

        void PropertyDial_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bBorder.ReleaseMouseCapture();
            e.Handled = true;
        }

        void PropertyDial_TouchUp(object sender, TouchEventArgs e)
        {
            bBorder.ReleaseAllTouchCaptures();
            e.Handled = true;
        }

        void PropertyDial_TouchDown(object sender, TouchEventArgs e)
        {
            Point p = e.GetTouchPoint(Application.Current.MainWindow).Position;
            bBorder.CaptureTouch(e.TouchDevice);
            UpdateAngle(p);
            e.Handled = true;
        }

        void PropertyDial_TouchMove(object sender, TouchEventArgs e)
        {
            Point p = e.GetTouchPoint(Application.Current.MainWindow).Position;
            UpdateAngle(p);
            e.Handled = true;
        }

        void PropertyDial_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bBorder.CaptureMouse();
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(Application.Current.MainWindow);
                UpdateAngle(p);
            }
            e.Handled = true;
        }

        void PropertyDial_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(Application.Current.MainWindow);
                UpdateAngle(p);
            }
            e.Handled = true;
        }

        private double oldAngle;

        private void UpdateAngle(Point p)
        {
            //if (this.Name == "pdWind")
            //    Console.Write("bah");
            
            Point p2 =
                this.TranslatePoint(new Point(this.ActualWidth/2, this.ActualHeight/2),
                                    Application.Current.MainWindow);
            double angle = 360 - CAngle(p2, p);

            angle = angle%360;

            SetAngle(angle);
        }



        public string UnitString
        {
            get { return (string)GetValue(UnitStringProperty); }
            set { SetValue(UnitStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitStringProperty =
            DependencyProperty.Register("UnitString", typeof(string), typeof(PropertyDial), new PropertyMetadata(""));

        public double UnitSize
        {
            get { return (double)GetValue(UnitSizeProperty); }
            set { SetValue(UnitSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitSizeProperty =
            DependencyProperty.Register("UnitSize", typeof(double), typeof(PropertyDial), new PropertyMetadata(10.0));

        

        public double RelatieveTurns
        {
            get { return (double)GetValue(RelatieveTurnsProperty); }
            set { SetValue(RelatieveTurnsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RelatieveTurns.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RelatieveTurnsProperty =
            DependencyProperty.Register("RelatieveTurns", typeof(double), typeof(PropertyDial), new PropertyMetadata(2.0));

        

        public void SetAngle(double angle)
        {
            Angle = angle;
            var VAngle = (angle - StartAngle) % 360;

            switch (ValueMode)
            {
                case ValueModes.Absolute:
                    Value = ((Max - Min) / 360.0) * VAngle + Min;
                    break;
                case ValueModes.Relative:
                   if (Math.Abs(oldAngle-angle) < 30)
                   {
                       double f = (Max - Min)/(360*RelatieveTurns);
                        Value = Math.Min(Math.Max(Value + (Angle - oldAngle) * f,Min),Max);
                        
                    }
                   oldAngle = Angle;

                    break;
            }
        }


        public string ValueFormat
        {
            get { return (string)GetValue(ValueFormatProperty); }
            set { SetValue(ValueFormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueFormatProperty =
            DependencyProperty.Register("ValueFormat", typeof(string), typeof(PropertyDial), new PropertyMetadata("N0"));

        

        private Grid gCircleBase;


        private const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>
        /// Calculate angle in radians between line defined with two points and x-axis.
        /// </summary>
        private double CAngle(Point start, Point end)
        {
            return Math.Atan2(start.Y - end.Y, end.X - start.X) * Rad2Deg;
        }



        public string ValueString
        {
            get { return (string)GetValue(ValueStringProperty); }
            set { SetValue(ValueStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueStringProperty =
            DependencyProperty.Register("ValueString", typeof(string), typeof(PropertyDial), new PropertyMetadata(""));

        

    }


    

    public class CircleBorderRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double) return (double) value/2;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
