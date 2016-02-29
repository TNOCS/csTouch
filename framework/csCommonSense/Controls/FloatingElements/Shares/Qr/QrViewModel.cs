#region

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using EndPoint = csShared.FloatingElements.Classes.EndPoint;

#endregion

namespace csShared
{
    public class QrViewModel : Screen
    {
        
        [ImportingConstructor]
        public QrViewModel()
        {
            
            
        }

        private string text = "hi";
        public string Text
        {
            get { return text; }
            set
            {
                if (text == value) return;
                text = value;
                NotifyOfPropertyChange(() => Text);
            }
        }

        private ImageSource qrImage;

        public ImageSource QrImage
        {
            get { return qrImage; }
            set { qrImage = value; NotifyOfPropertyChange(() => QrImage); }
        }
        

        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        private EndPoint endPoint;

        public EndPoint EndPoint {
            get { return endPoint; }
            set {
                endPoint = value;
                var q = new MessagingToolkit.QRCode.Codec.QRCodeEncoder();
                var bmp = q.Encode(endPoint.Value.ToString());
                QrImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bmp.GetHbitmap(),
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
            }
        }

        public FloatingElement Element { get; set; }
    }
}