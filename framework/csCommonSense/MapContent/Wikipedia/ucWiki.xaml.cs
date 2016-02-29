using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using csShared;
using csShared.Documents;
using csShared.Geo;
using csShared.Interfaces;

namespace csGeoLayers.Wikipedia
{
    /// <summary>
    /// Interaction logic for ucPlacemark.xaml
    /// </summary>
    public partial class UcWiki 
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(Document), typeof(UcWiki), new UIPropertyMetadata(null));

      
        private bool _initialized;

        public UcWiki()
        {
            InitializeComponent();
            Loaded += UcPlacemarkLoaded;
        }


        public Document Document
        {
            get { return (Document)GetValue(DocumentProperty); }
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

        private void UcPlacemarkLoaded(object sender, RoutedEventArgs e)
        {
            //var pf = (WikiFeature)Feature;
            //Document = new Document { FileType = FileTypes.web, IconUrl = "file://"  + System.IO.Directory.GetCurrentDirectory() +  @"\wikipedia\wiki.gif", Location = pf.Url, OriginalUrl = pf.Url, Channel = "ConnectMedia" };
        }

    }
}