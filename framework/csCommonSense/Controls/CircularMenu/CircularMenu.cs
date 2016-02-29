#region

using csCommon.Controls;
using Microsoft.Expression.Media;
using Microsoft.Expression.Shapes;
using Microsoft.Surface.Presentation.Controls;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

#endregion

namespace csCommon.csMapCustomControls.CircularMenu
{
    public class CircularMenu : ContentControl
    {
        // Using a DependencyProperty as the backing store for BorderSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderSizeProperty =
            DependencyProperty.Register("BorderSize", typeof (double), typeof (CircularMenu),
                new PropertyMetadata(235.0));

        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof (Brush), typeof (CircularMenu),
                new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof (CircularMenuItem), typeof (CircularMenu),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ArrowArcSizeProperty =
            DependencyProperty.Register("ArrowArcSize", typeof (double), typeof (CircularMenu),
                new PropertyMetadata(15.0));

        public static readonly DependencyProperty CenterSizeProperty =
            DependencyProperty.Register("CenterSize", typeof (double), typeof (CircularMenu), new PropertyMetadata(30.0));

        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register("ItemSize", typeof (double), typeof (CircularMenu), new PropertyMetadata(35.0));

        public static readonly DependencyProperty SegmentsProperty =
            DependencyProperty.Register("Segments", typeof (int), typeof (CircularMenu), new PropertyMetadata(8));

        public static readonly DependencyProperty MenuCenterSizeProperty =
            DependencyProperty.Register("MenuCenterSize", typeof (double), typeof (CircularMenu),
                new PropertyMetadata(70.0));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof (double), typeof (CircularMenu), new PropertyMetadata(100.0));

        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register("AccentBrush", typeof (Brush), typeof (CircularMenu),
                new PropertyMetadata(Brushes.Blue));

        public static readonly DependencyProperty SecondAccentBrushProperty =
            DependencyProperty.Register("SecondAccentBrush", typeof (Brush), typeof (CircularMenu),
                new PropertyMetadata(Brushes.LightBlue));

        public static readonly DependencyProperty OpenProperty =
            DependencyProperty.Register("Open", typeof (bool), typeof (CircularMenu),
                new FrameworkPropertyMetadata(false, OpenChanged));

        static CircularMenu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (CircularMenu),
                new FrameworkPropertyMetadata(typeof (CircularMenu)));
        }

        public double BorderSize
        {
            get { return (double) GetValue(BorderSizeProperty); }
            set { SetValue(BorderSizeProperty, value); }
        }

        public string Id { get; set; }

        public Brush BackgroundBrush
        {
            get { return (Brush) GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundBrush.  This enables animation, styling, binding, etc...


        public CircularMenuItem SelectedItem
        {
            get { return (CircularMenuItem) GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...


        public double ArrowArcSize
        {
            get { return (double) GetValue(ArrowArcSizeProperty); }
            set { SetValue(ArrowArcSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArrowArcSize.  This enables animation, styling, binding, etc...


        public double CenterSize
        {
            get { return (double) GetValue(CenterSizeProperty); }
            set { SetValue(CenterSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterSize.  This enables animation, styling, binding, etc...


        public double ItemSize
        {
            get { return (double) GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemSize.  This enables animation, styling, binding, etc...


        public int Segments
        {
            get { return (int) GetValue(SegmentsProperty); }
            set { SetValue(SegmentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Segments.  This enables animation, styling, binding, etc...


        public double MenuCenterSize
        {
            get { return (double) GetValue(MenuCenterSizeProperty); }
            set { SetValue(MenuCenterSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MenuCenterSize.  This enables animation, styling, binding, etc...


        public double Size
        {
            get { return (double) GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Size.  This enables animation, styling, binding, etc...


        public Brush AccentBrush
        {
            get { return (Brush) GetValue(AccentBrushProperty); }
            set { SetValue(AccentBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AccentBrush.  This enables animation, styling, binding, etc...


        public Brush SecondAccentBrush
        {
            get { return (Brush) GetValue(SecondAccentBrushProperty); }
            set { SetValue(SecondAccentBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SecondAccentBrush.  This enables animation, styling, binding, etc...


        public bool Open
        {
            get { return (bool) GetValue(OpenProperty); }
            set
            {
                SetValue(OpenProperty, value);
                this.RootItem.IsOpen = value;
            }
        }

        // Using a DependencyProperty as the backing store for Open.  This enables animation, styling, binding, etc...


        public Grid Main { get; set; }
        public Image iCenterIcon { get; set; }
        public CircularMenuItem RootItem { get; set; }
        public Grid gArcs { get; set; }
        public SurfaceButton Center { get; set; }
        public Path pBack { get; set; }
        public ClippingBorder cbCenter { get; set; }
        public ContentControl ccCenter { get; set; }

        public static void OpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cm = d as CircularMenu;
            if (cm == null) return;
            cm.Draw();
            if (!cm.Open) cm.SelectedItem = cm.RootItem;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Main = GetTemplateChild("Main") as Grid;
            iCenterIcon = GetTemplateChild("iCenterIcon") as Image;
            gArcs = GetTemplateChild("gArcs") as Grid;
            Center = GetTemplateChild("Center") as SurfaceButton;
            pBack = GetTemplateChild("pBack") as Path;
            cbCenter = GetTemplateChild("cbCenter") as ClippingBorder;
            //ccCenter = GetTemplateChild("ccCenter") as ContentControl;
            if (cbCenter != null)
            {
                cbCenter.Width = CenterSize;
                cbCenter.Height = CenterSize;
                cbCenter.CornerRadius = new CornerRadius(CenterSize/2);

                Center.Click += (e, f) => { Back(); };
            }

            //Center.TouchDown += (e, si) =>
            //{
            //    if (SelectedItem == RootItem) Open = !Open;
            //    if (SelectedItem.Parent != null) SelectItem(SelectedItem.Parent);
            //};

            RootItem = Content as CircularMenuItem;

            if (RootItem != null) SelectItem(RootItem);
        }

        public void RootItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Icon")
                Draw();
        }

        public void Back()
        {
            if (SelectedItem == RootItem && (RootItem.Items == null || !RootItem.Items.Any()))
            {
                Open = false;
                SelectedItem.TriggerSelected(this, Center);
            }
            else
            {
                if (SelectedItem == RootItem)
                    Open = !Open;
                if (SelectedItem != null && SelectedItem.Parent != null)
                    SelectItem(SelectedItem.Parent);
            }
        }

        

        public void Draw()
        {
            if (SelectedItem == null) return;
            if (gArcs == null) return;
            gArcs.Children.Clear();
            var a = 360.0/Segments;

            if (SelectedItem.Element != null)
            {
                ccCenter.Content = SelectedItem.Element;
                pBack.Visibility = Visibility.Collapsed;
                //ccCenter.Visibility = Visibility.Visible;
            }
            else if (SelectedItem.Icon != null && SelectedItem == RootItem)
            {
                iCenterIcon.Source = new BitmapImage(new Uri(SelectedItem.Icon, UriKind.RelativeOrAbsolute));
                iCenterIcon.Visibility = Visibility.Visible;
                pBack.Visibility = Visibility.Collapsed;
            }
            else
            {
                iCenterIcon.Visibility = Visibility.Collapsed;
                pBack.Visibility = Visibility.Visible;
                //ccCenter.Visibility = Visibility.Collapsed;
            }

            if (!Open)
            {
                BackgroundBrush = null;
                return;
            }
            BackgroundBrush = Brushes.White;

            for (var i = 0; i < Segments; i++)
            {
                var mi = SelectedItem.Items.FirstOrDefault(k => k.Position == i);

                var s = new Arc
                {
                    Width = Size,
                    Height = Size,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.None,
                    StartAngle = i*a,
                    StrokeThickness = 0,
                    Stroke = null,
                    EndAngle = (i + 1)*a - 1
                };
                if (mi != null && mi.Items != null && mi.Items.Count > 0)
                {
                    s.Fill = AccentBrush;
                    s.MouseDown += (e, si) => SelectItem(mi);
                    s.TouchDown += (e, si) => SelectItem(mi);
                }
                else
                {
                    s.Fill = SecondAccentBrush;
                }
                s.ArcThickness = 0;
                s.ArcThicknessUnit = UnitType.Pixel;

                gArcs.Children.Add(s);
                s.BeginAnimation(Arc.ArcThicknessProperty,
                    new DoubleAnimation(ArrowArcSize, new Duration(new TimeSpan(0, 0, 0, 0, 200))));
                const double dDegToRad = Math.PI/180.0;

                if (mi == null) continue;
                var f = new Arc
                {
                    Width = Size - (ArrowArcSize*2) - 3,
                    Height = Size - (ArrowArcSize*2) - 3,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.None,
                    StartAngle = i*a,
                    StrokeThickness = 0,
                    Stroke = null,
                    EndAngle = (i + 1)*a - 1,
                    Tag =  i
                };
                if (mi.Fill == null) mi.Fill = Brushes.Transparent;
                f.Fill = mi.Fill;
                f.ArcThickness = ItemSize;
                f.ArcThicknessUnit = UnitType.Pixel;
                f.MouseDown += (sender, e) => SelectItem(mi);

                //var eventAsObservable = Observable.FromEventPattern<TouchEventArgs>(f, "TouchDown");
                //eventAsObservable.Throttle(TimeSpan.FromMilliseconds(200)).Subscribe(k => Execute.OnUIThread(() => SelectItem(mi)));

                // Only subscribe to TouchDown on Windows 7: On Windows 8, it causes 
                var win8Version = new Version(6, 2, 9200, 0);
                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Environment.OSVersion.Version < win8Version)
                {
                    f.TouchDown += (e, si) => SelectItem(mi);
                }
                //f.TouchDown += (sender, e) => SelectItem(mi);

                gArcs.Children.Add(f);
                f.UpdateLayout();


                var sp = new StackPanel {Opacity = 0, IsHitTestVisible = false};
                if (mi.Element != null)
                {
                    var vb = new Viewbox {Width = 20, Height = 20, Stretch = Stretch.Uniform};
                    var pa = new Path {Data = Geometry.Parse(mi.Element), Fill = Brushes.Black};
                    vb.Child = pa;

                    sp.Children.Add(vb);
                }
                else if (!string.IsNullOrEmpty(mi.Icon))
                {
                    var img = new Image {Width = 20, Height = 20, Stretch = Stretch.UniformToFill};
                    var binding = new Binding {Source = mi, Path = new PropertyPath("Icon"), Mode = BindingMode.OneWay};
                    //img.Source = new BitmapImage(new Uri(mi.Icon, UriKind.RelativeOrAbsolute));
                    img.SetBinding(Image.SourceProperty, binding);
                    sp.Children.Add(img);
                }
                else
                {
                    var b = new Border {Background = null, Width = 20, Height = 20};
                    sp.Children.Add(b);
                }
                var tb = new TextBlock {Text = mi.Title};
                var bind = new Binding {Source = mi, Path = new PropertyPath("Title"), Mode = BindingMode.OneWay};
                tb.SetBinding(TextBlock.TextProperty, bind);
                var r = MenuCenterSize/2 + ItemSize - 10;
                var dX = r*Math.Sin((i + 0.5)*a*dDegToRad);

                // We invert the Y coordinate, because the origin in controls 
                // is the upper left corner, rather than the lower left
                var dY = -r*Math.Cos((i + 0.5)*a*dDegToRad);
                tb.Foreground = Brushes.Black;
                tb.FontSize = 9;
                f.TranslatePoint(new Point(ItemSize/2, ItemSize/2), gArcs);
                tb.TextAlignment = TextAlignment.Center;
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                sp.RenderTransform = new TranslateTransform(dX, dY + Size/2 - 15);
                sp.Tag = i;
                sp.Children.Add(tb);
                gArcs.Children.Add(sp);

                sp.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(1.0, new Duration(new TimeSpan(0, 0, 0, 0, 200))));


                if (mi.Items == null || mi.Items.Count <= 0) continue;
                var rp = new RegularPolygon
                {
                    IsHitTestVisible = false,
                    PointCount = 3,
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Margin = new Thickness(-4, 0, 0, 0)
                };
                var r2 = Size/2 - (ArrowArcSize/2) + (rp.Width/2);
                rp.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new RotateTransform((i + 0.5)*a),
                        new TranslateTransform(
                            r2*Math.Sin((i + 0.5)*a*dDegToRad) + 5,
                            -r2*Math.Cos((i + 0.5)*a*dDegToRad) + 5
                            )
                    }
                };
                gArcs.Children.Add(rp);
            }

            //if (SelectedItem.Items == null || SelectedItem.Items.Count <= 0) return;
            //foreach (var i in SelectedItem.Items)
            //    if (i.Fill != null)
            //    {
            //    }
        }

        public void Update()
        {
            if (SelectedItem == null) return;
            if (gArcs == null) return;
            //gArcs.Children.Clear();

            if (SelectedItem.Element != null)
            {
                ccCenter.Content = SelectedItem.Element;
                pBack.Visibility = Visibility.Collapsed;
                //ccCenter.Visibility = Visibility.Visible;
            }
            else if (SelectedItem.Icon != null && SelectedItem == RootItem)
            {
                iCenterIcon.Source = new BitmapImage(new Uri(SelectedItem.Icon, UriKind.RelativeOrAbsolute));
                iCenterIcon.Visibility = Visibility.Visible;
                pBack.Visibility = Visibility.Collapsed;
            }
            else
            {
                iCenterIcon.Visibility = Visibility.Collapsed;
                pBack.Visibility = Visibility.Visible;
                //ccCenter.Visibility = Visibility.Collapsed;
            }

            if (!Open)
            {
                BackgroundBrush = null;
                return;
            }

            for (var i = 0; i < Segments; i++)
            {
                var mi = SelectedItem.Items.FirstOrDefault(k => k.Position == i);
                if (mi == null) continue;
                var arc = gArcs.Children.OfType<Arc>().FirstOrDefault(k => k.Tag != null && (int) k.Tag == i);
                if (arc != null && mi.Fill!=null)
                    arc.Fill = mi.Fill;
                var sp = gArcs.Children.OfType<StackPanel>().FirstOrDefault(k => k.Tag != null && (int)k.Tag ==  i);
                if (sp != null)
                {
                    foreach (var tb in sp.Children)
                    {
                        var textblock = tb as TextBlock;
                        if (textblock != null)
                        {
                            textblock.Text = mi.Title;
                        }
                    }
                }
            }
            return;
        }


        public void SelectItem(CircularMenuItem mi)
        {
            if (mi == null) return;
            mi.Menu = this;
            if (mi.Items == null || mi.Items.Count == 0)
            {
                mi.TriggerSelected(this);
                if (SelectedItem != null) SelectedItem.TriggerItemSelected(mi, this);
                Update();
            }
            else
            {
                if (mi != RootItem && mi.Parent == null) mi.Parent = SelectedItem;
                SelectedItem = mi;

                if (SelectedItem.Parent != null)
                {
                    pBack.Visibility = Visibility.Visible;
                    iCenterIcon.Visibility = Visibility.Collapsed;
                }
                else
                {
                    pBack.Visibility = Visibility.Collapsed;
                    iCenterIcon.Visibility = Visibility.Visible;
                    iCenterIcon.Source = mi.Icon != null
                        ? new BitmapImage(new Uri(mi.Icon, UriKind.RelativeOrAbsolute))
                        : null;
                }
                Draw();
            }
        }
    }

    public class BorderCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}