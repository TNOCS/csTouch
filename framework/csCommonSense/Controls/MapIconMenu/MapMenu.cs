using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace csCommon.csMapCustomControls.MapIconMenu
{
    /// <summary>
    /// </summary>
    public partial class MapMenu : ContentControl, ICustomAlignedControl
    {
        static MapMenu()    
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MapMenu),
                new FrameworkPropertyMetadata(typeof(MapMenu)));
        }

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Radius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(double), typeof(MapMenu), new UIPropertyMetadata(30.0));

        

        void HandleMouseHover(object sender, RoutedEventArgs rea)
        {
            if (null != rea)
            {
                MapMenuItem mki = rea.OriginalSource as MapMenuItem;

                if (null != mki)
                {
                    HoverToolTip = mki.RootToolTip;
                }
            }
        }



        public bool MenuEnabled
        {
            get { return (bool)GetValue(MenuEnabledProperty); }
            set { SetValue(MenuEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MenuEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuEnabledProperty =
            DependencyProperty.Register("MenuEnabled", typeof(bool), typeof(MapMenu), new UIPropertyMetadata(true));

        
        

        public int Timeout
        {
            get { return (int)GetValue(TimeoutProperty); }
            set { SetValue(TimeoutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Timeout.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimeoutProperty =
            DependencyProperty.Register("Timeout", typeof(int), typeof(MapMenu), new UIPropertyMetadata(2000));

        

        [Category("Content")]
        [Description("Provides a rich tooltip for the menu killer item which is under the mouse currently")]
        public static readonly DependencyProperty HoverToolTipProperty =
            DependencyProperty.Register(
                "HoverToolTip",
                typeof(object),
                typeof(MapMenu),
                new PropertyMetadata(null));

        public object HoverToolTip
        {
            get
            {
                return (object)GetValue(HoverToolTipProperty);
            }
            set
            {
                SetValue(HoverToolTipProperty, value);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //AddHandler(MapMenuItem.MouseHoverEvent, new RoutedEventHandler(HandleMouseHover));
            
           
        }

     

        /*
        static readonly DependencyProperty AlignReferencePointProperty =
            DependencyProperty.Register("AlignReferencePoint", typeof(Point), typeof(MapMenu),
            new FrameworkPropertyMetadata(new Point(), FrameworkPropertyMetadataOptions.AffectsArrange, new PropertyChangedCallback(AlignReferencePointDPChanged)));

        void AlignReferencePointDPChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            // dependencyObject.
        }*/

        #region ICustomAlignedControl Members
        public Point AlignReferencePoint
        {
            get
            {
                MapMenuItem rootItem = this.Content as MapMenuItem;
                if (null != rootItem)
                {
                    return rootItem.AlignReferencePoint;
                }

                return new Point();
            }
        }
        #endregion
    }
}
