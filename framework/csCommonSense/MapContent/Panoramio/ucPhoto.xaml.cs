using System.Windows;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Input;
using csShared;
using csShared.Documents;
using csShared.Geo;
using csShared.Interfaces;
using System.Windows.Input;

namespace csGeoLayers.Content.Panoramio
{
    /// <summary>f
    /// cdws\
    ///        // 
    /// Interaction logic for ucPlacemark.xaml
    /// </summary>
    public partial class UcPhoto 
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof (Document), typeof (UcPhoto), new UIPropertyMetadata(null));

       
        private bool _initialized;

        public UcPhoto()
        {
            InitializeComponent();
            Loaded += UcPlacemarkLoaded;
        }


        public Document Document
        {
            get { return (Document) GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        public FloatingCollection FloatingItems { get; set; }

        // Using a DependencyProperty as the backing store for Document.  This enables animation, styling, binding, etc...

        #region IMapControl Members

      
        public ITimelineManager Timeline { get; set; }


        

        public void Init(MapViewDef transform)
        {
            _initialized = true;
        }

        #endregion

        public PhotoFeature Feature { get; set; }

        private void UcPlacemarkLoaded(object sender, RoutedEventArgs e)
        {
            TouchDown += UcPhotoContactTap;
            var pf = (PhotoFeature) Feature;
            if (Document==null) Document = new Document { FileType = FileTypes.image, Location = pf.ImageUrl, OriginalUrl = pf.ImageUrl, Channel = "ConnectMedia" };
        }

        private void UcPhotoContactTap(object sender, TouchEventArgs e)
        {
            if (Feature is PhotoFeature)
            {
                var pf = (PhotoFeature) Feature;
                var fe = new FloatingElement
                             {
                                 Document = new Document {FileType = FileTypes.image, Location = pf.ImageUrl},
                                 OpacityDragging = 0.5,
                                 OpacityNormal = 1.0,
                                 CanMove = true,
                                 CanRotate = true,
                                 CanScale = true,
                                 StartOrientation = e.Device.GetOrientation(Application.Current.MainWindow) + 90,
                                 Background = Brushes.DarkOrange,
                                 MaxSize = new Size(500, (500.0/pf.Width)*pf.Height),
                                 StartPosition = e.TouchDevice.GetTouchPoint(Application.Current.MainWindow).Position,
                                 StartSize = new Size(200, (200.0/pf.Width)*pf.Height),
                                 MinSize = new Size(100, (100.0/pf.Width)*pf.Height),
                                 ShowsActivationEffects = false,
                                 RemoveOnEdge = true,
                                 Contained = true,
                                 Title = pf.Name,
                                 Foreground = Brushes.White,
                                 DockingStyle = DockingStyles.None,
                             };
                AppStateSettings.Instance.FloatingItems.Add(fe);
                //State.AddFloatingElement(new CoFile() { Location = pf.ImageUrl, Id = pf.Id, Name = pf.Name }, (pf.Width/2), (pf.Height/2));
            }

        }
    }
}