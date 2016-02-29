using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using DataServer;
using ESRI.ArcGIS.Client;
using csShared;

namespace csDataServerPlugin
{
    /// <summary>
    /// Interaction logic for SvnMapFeature.xaml
    /// </summary>
    public partial class ucPoiPreview
    {
        public bool ShowTitle
        {
            get { return (bool)GetValue(ShowTitleProperty); }
            set { SetValue(ShowTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register("ShowTitle", typeof(bool), typeof(ucPoiPreview), new UIPropertyMetadata(false));

        // Using a DependencyProperty as the backing store for Plan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PoIProperty =
            DependencyProperty.Register("PoI", typeof(PoI), typeof(ucPoiPreview), new UIPropertyMetadata(null));

        private readonly List<InputDevice> _ignoredDeviceList = new List<InputDevice>();

        public Color InnerTextColor
        {
            get { return (Color)GetValue(InnerTextColorProperty); }
            set { SetValue(InnerTextColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InnerTextColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InnerTextColorProperty =
            DependencyProperty.Register("InnerTextColor", typeof(Color), typeof(ucPoiPreview), new PropertyMetadata(Colors.White));

        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Selected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(bool), typeof(ucPoiPreview), new UIPropertyMetadata(false));

        public ucPoiPreview()
        {
            InitializeComponent();
            this.Loaded += SystemFeature_Loaded;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        void Dispatcher_ShutdownStarted(object sender, System.EventArgs e)
        {
            AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
            if (PoI!=null) ((PoiService)PoI.Service).PoIs.CollectionChanged -= PoIs_CollectionChanged;
        }

        void SystemFeature_Loaded(object sender, RoutedEventArgs e)
        {
            AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
            if (this.DataContext is DataBinding)
            {
                //DataBinding g = (DataBinding)DataContext;

                //if (g.Attributes.ContainsKey("system"))
                //    SystemInstance = (string)g.Attributes["system"];
            }
            if (!(DataContext is PoI)) return;
            PoI = (PoI)DataContext;
                
            ((PoiService)PoI.Service).PoIs.CollectionChanged += PoIs_CollectionChanged;
            PoI.Service.Unsubscribed                         += Service_Unsubscribed;

            bMain.Background = null;
                
            switch (PoI.DrawingMode)
            {
                case DrawingModes.Point:
                    iImage.Visibility = Visibility.Visible;
                    bMain.Background = new SolidColorBrush(PoI.FillColor);
                    var width = PoI.NEffectiveStyle.IconWidth != null ? PoI.NEffectiveStyle.IconWidth.Value : 20;
                    var height = PoI.NEffectiveStyle.IconHeight != null ? PoI.NEffectiveStyle.IconHeight.Value : 20;
                    bMain.Width = width;
                    bMain.Height = height;
                    bMain.CornerRadius = new CornerRadius(width/2.0);
                    if (!string.IsNullOrEmpty(PoI.InnerText))
                    {
                        tbInnerText.FontWeight = FontWeights.ExtraBold;
                        tbInnerText.Text = PoI.InnerText;
                        tbInnerText.Foreground = new SolidColorBrush(PoI.NEffectiveStyle.InnerTextColor.Value);
                    }
                    break;
                case DrawingModes.Image:                        
                    iImage.Visibility = Visibility.Visible;
                    break;
                case DrawingModes.Polyline:
                    pPolyline.Visibility = Visibility.Visible;
                    break;
                case DrawingModes.Polygon:
                    pPolygon.Visibility = Visibility.Visible;
                    break;
                case DrawingModes.Freehand:
                    pFreehand.Visibility = Visibility.Visible;
                    break;
                case DrawingModes.Circle:
                    pCircle.Visibility = Visibility.Visible;
                    break;
                case DrawingModes.Rectangle:
                    pRectangle.Visibility = Visibility.Visible;
                    break;
            }
            UpdateIsEnabled();
        }

        void Service_Unsubscribed(object sender, System.EventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                ((PoiService)PoI.Service).PoIs.CollectionChanged -= PoIs_CollectionChanged;
                AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
            });
        }

        void PoIs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (PoI.Service.IsSubscribed) UpdateIsEnabled();
        }

        void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            UpdateIsEnabled();
        }

        public void UpdateIsEnabled()
        {
            if (PoI == null) return;
            if (PoI.MaxItems.HasValue && (!PoI.HasPosition))
            {
                var c = ((PoiService) PoI.Service).PoIs.Count(k => k.PoiTypeId == PoI.ContentId);
                if (c >= PoI.MaxItems.Value)
                {
                    PoI.IsVisible = false;
                    Opacity = 0.2;
                    return;
                }
            }
            PoI.CalculateVisible(AppStateSettings.Instance.ViewDef.MapControl.Resolution);            
            this.Opacity = (PoI.IsVisible) ? 1.0 : 0.2;
        }

        public PoI PoI
        {
            get { return (PoI)GetValue(PoIProperty); }
            set { SetValue(PoIProperty, value); }
        }
    }
}