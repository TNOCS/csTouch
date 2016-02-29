using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BaseWPFHelpers;
using Caliburn.Micro;
using csImb;
using csShared;
using csShared.Documents;
using IMB3;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using csShared.FloatingElements;
using SimzzDev;
using System.Windows.Ink;
using System.Collections.Generic;
using System.Linq;

namespace csRemoteScreenPlugin
{
    [Export(typeof (IRemoteScreen))]    
    public class RemoteScreenViewModel : Screen, IRemoteScreen
    {
        //private TEventEntry _cameraStream;
        //private TEventEntry _strokeStream;
        private bool _canConnect;        
        //private TEventEntry _screenshot;

        private ScatterViewItem _svi;
        private RemoteScreenView rsv;
        //private WriteableBitmap wb;

        //private DrawingBoard drawBoard;

        public RemoteScreenPlugin Plugin { get; set; }

        private Dictionary<string, Stroke> _strokes = new Dictionary<string, Stroke>();

        public Brush AccentBrush { get { return AppState.AccentBrush; } }
        
        public bool CanFollow2D
        {
            get { return Client.AllCapabilities.Contains(csImb.csImb.Capability2D); }            
        }

        public bool CanFollow3D
        {
            get { return Client.AllCapabilities.Contains(csImb.csImb.Capability3D); }            
        }
        
        public bool CanFollowScreen
        {
            get { return Client.AllCapabilities.Contains("Screenshots") || Client.Os == "WP7.1"; }
        }

        

        public bool CanExit
        {
            get
            {
                
                return Client.AllCapabilities.Contains(csImb.csImb.CapabilityExit);
            }
        }

        public bool CanFilterClient
        {
            get
            {
                return Client.AllCapabilities.Any(c => c.ToLower().StartsWith("canfilterclient"));
            }
        }

       

        public bool IsFiltered
        {
            get
            {
                return Client.AllCapabilities.Any(c => c.ToLower().StartsWith("canfilterclient:" + AppState.Imb.Id));
            }
            set
            {
                if (value)
                {
                    AppState.Imb.SendCommand(Client.Id, "FilterClient", AppState.Imb.Id.ToString());
                }
                else
                {
                    AppState.Imb.SendCommand(Client.Id, "FilterClient", "");
                    
                }
            }
        }
        

        private bool _follow;

        public bool Follow
        {
            get { return _follow; }
            set { _follow = value; NotifyOfPropertyChange(() => Follow); NotifyOfPropertyChange(()=>CanScreenOff); NotifyOfPropertyChange(()=>CanScreenOn); }
        }

        public bool CanScreenOn
        {
            get { return !Follow; }
        }

        public bool CanScreenOff
        {
            get { return Follow; }
        }

        [ImportingConstructor]
        public RemoteScreenViewModel()
        {
            AppState.Imb.CommandReceived += Imb_CommandReceived;
            AppState.Imb.ClientChanged += Imb_ClientChanged;
        }

        void Imb_ClientChanged(object sender, ImbClientStatus e)
        {
           UpdateStatus();
        }

        void Imb_CommandReceived(object sender, csImb.Command c)
        {
            /*if(c.CommandName.ToLower() == "skypecontacts")
            {
                // Got it!
            }*/
        }

        void Client_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            NotifyOfPropertyChange(() => CanFollow2D);
            NotifyOfPropertyChange(() => CanFollow3D);
            NotifyOfPropertyChange(() => CanFollowScreen);
            NotifyOfPropertyChange(() => CanFilterClient);
            NotifyOfPropertyChange(() => IsFiltered);
            NotifyOfPropertyChange(()=>CanExit);
        }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public BitmapSource Bs { get; set; }

        public bool CanConnect
        {
            get { return _canConnect; }
            set
            {
                _canConnect = value;
                NotifyOfPropertyChange(() => CanConnect);
            }
        }

        public void Exit(RoutedEventArgs e)
        {
            Plugin.ExitClient(Client.Id);         
        }

       
        public void Follow2D(RoutedEventArgs e )
        {
            /*AppState.Imb.SendCommand(Client.Id, "GetContacts");

            AppState.Imb.CommandReceived += (ef, s) =>
                                                {
                                                    if (s.CommandName=="GetContacts")
                                                    {
                                                        AppState.Imb.SendCommand(s.SenderId,"SkypeContacts","");
                                                    }
                                                };*/
            if (Plugin != null)
            {
                ToggleButton tb = (ToggleButton) e.Source;
                if (tb.IsChecked.HasValue && tb.IsChecked.Value)
                {
                    Plugin.Follow2D(Client.Id);
                }
                else
                {
                    Plugin.Follow2D(0);
                }
                
            }
        
    }

        public void FilterClient()
        {
            //AppState.Imb.SendCommand(Client.Id,"FilterClient",AppState.Imb.Id.ToString());
        }

        public void FollowScreen()
        {
            FollowScreenViewModel vm = new FollowScreenViewModel() {Client = this.Client};
            
            Size s = new Size(500.0, (500.0/this.Client.ResolutionX)*this.Client.ResolutionY); 
            var fe = FloatingHelpers.CreateFloatingElement("Follow Screen", new Point(300, 300), s, vm);
            vm.Fe = fe;
            fe.CanFullScreen = true;
            fe.CanScale = true;
            fe.MinSize = new Size(300,200);
            AppState.FloatingItems.AddFloatingElement(fe);
            vm.Start();
            
                        
        }

        #region IRemoteScreen Members

        public FloatingElement Fe { get; set; }
        public ImbClientStatus Client { get; set; }

        #endregion



        protected override void OnViewAttached(object view, object context)
        {

            base.OnViewAttached(view, context);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            
            rsv = (RemoteScreenView)view;
            Client.PropertyChanged += Client_PropertyChanged;


            _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(rsv, typeof(ScatterViewItem));


            if (_svi != null)
            {
                _svi.AllowDrop = true;
                SurfaceDragDrop.AddPreviewDropHandler(_svi, DoDrop);
                SurfaceDragDrop.AddPreviewDragEnterHandler(_svi, DoEnter);
                SurfaceDragDrop.AddPreviewDragLeaveHandler(_svi, DoLeave);
            }

            
            CanConnect = (Client.Type == "Display");

            //drawBoard = new DrawingBoard(rsv.inkStrokes);
            
        }

        
  

        

       
       

      



        public void BEarth()
        {
            AppState.Imb.SendCommand(Client.Id, "Earth");
        }

        public void BStreet()
        {
            AppState.Imb.SendCommand(Client.Id, "Street");
        }


        private void DoDrop(object sender, SurfaceDragDropEventArgs e)
        {
            _svi.Opacity = 1;
            if (e.Cursor.Data is Document)
            {
                var d = (Document) e.Cursor.Data;

                AppState.SendDocument(Client, d);
            }
        }

        private void DoEnter(object sender, SurfaceDragDropEventArgs e)
        {
            _svi.Opacity = 0.5;
        }

        private void DoLeave(object sender, SurfaceDragDropEventArgs e)
        {
            _svi.Opacity = 1;
        }


        private BitmapSource GetImageFromByteArray(byte[] pixelInfo, int height, int width)
        {
            try
            {
                PixelFormat pf = PixelFormats.Bgr32;
                int stride = (width*pf.BitsPerPixel + 7)/8;
                BitmapSource image = BitmapSource.Create(width, height, 96, 96, pf, null, pixelInfo, stride);
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public interface IRemoteScreen
    {
        FloatingElement Fe { get; set; }
        ImbClientStatus Client { get; set; }

    }
}