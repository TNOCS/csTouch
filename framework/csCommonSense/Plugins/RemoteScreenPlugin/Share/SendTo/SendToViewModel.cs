#region

using System.ComponentModel.Composition;
using System.Windows.Media;
using Caliburn.Micro;
using csShared;
using EndPoint = csShared.FloatingElements.Classes.EndPoint;

#endregion

namespace csRemoteScreenPlugin.Share.SendTo
{ 
    public class SendToViewModel : Screen
    {
        
        [ImportingConstructor]
        public SendToViewModel()
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
            set { endPoint = value; }
        }

        public FloatingElement Element { get; set; }
    }
}