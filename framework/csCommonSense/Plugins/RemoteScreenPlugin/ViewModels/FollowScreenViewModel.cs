using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BaseWPFHelpers;
using Caliburn.Micro;
using csImb;
using csShared;
using csShared.Documents;
using IMB3;
using IMB3.ByteBuffers;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using SimzzDev;
using System.Windows.Input;
using System.Windows.Ink;
using System.Collections.Generic;
using Brush = System.Windows.Media.Brush;

namespace csRemoteScreenPlugin
{
    [Export(typeof (IFollowScreen))]
    public class FollowScreenViewModel : Screen, IFollowScreen
    {
        
        private TEventEntry _cameraStream;
        private TEventEntry _strokeStream;
        private bool _canConnect;        
        private TEventEntry _screenshot;

        private ScatterViewItem _svi;
        private FollowScreenView rsv;
        private WriteableBitmap wb;

        //private DrawingBoard drawBoard;

        private Dictionary<string, Stroke> _strokes = new Dictionary<string, Stroke>();

        public Brush AccentBrush { get { return AppState.AccentBrush; } }
        
        

        private bool _follow;

        public bool Follow
        {
            get { return _follow; }
            set
            {
                _follow = value; NotifyOfPropertyChange(() => Follow); NotifyOfPropertyChange(()=>CanScreenOff); NotifyOfPropertyChange(()=>CanScreenOn);
            }
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
        public FollowScreenViewModel()
        {
           
        }

        void Client_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
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


        #region IRemoteScreen Members

        public FloatingElement Fe { get; set; }
        public ImbClientStatus Client { get; set; }

        #endregion

        
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);            
            rsv = (FollowScreenView)view;
            Client.PropertyChanged += Client_PropertyChanged;

            _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(rsv, typeof(ScatterViewItem));


            if (rsv != null)
            {
                rsv.AllowDrop = true;
                SurfaceDragDrop.AddPreviewDropHandler(rsv, DoDrop);
                SurfaceDragDrop.AddPreviewDragEnterHandler(rsv, DoEnter);
                SurfaceDragDrop.AddPreviewDragLeaveHandler(rsv, DoLeave);
            }

            if (Fe != null)
            {
                Fe.DockedChanged += FeDockedChanged;
            }
            
            CanConnect = (Client.Type == "Display");

            //Subscribe();
            //ScreenOn();
            if (_svi!=null) _svi.SizeChanged += _svi_SizeChanged;
            if (Fe!=null) Fe.Closed += (e, s) =>
                                           {
                                               Stop();
                                           };
            //drawBoard = new DrawingBoard(rsv.inkStrokes);

        }

        public void Start()
        {
            ScreenOn();
            Subscribe();
        }

        public void Stop()
        {
            ScreenOff();
            Unsubscribe();
        }
    

        void _svi_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            //ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(rsv, typeof(ScatterViewItem));
            if (_svi != null)
            {
                _svi.Height = e.NewSize.Height;
                _svi.Width = e.NewSize.Height * ((double)rsv.bs.ActualWidth / (double)rsv.bs.ActualHeight);
            }

        }

        private void _cameraStream_OnBuffer(TEventEntry aEvent, int aTick, int aBufferId, TByteBuffer aBuffer)
        {
            rsv.Dispatcher.Invoke(delegate
            {
                //string fotoStr = aPayload.ReadString();
                byte[] fotoBa = aBuffer.Buffer;
                BitmapImage bmp = GetBitmapImage(fotoBa);
                wb = new WriteableBitmap(bmp);
                rsv.bs.Source = wb;
                rsv.bs.InvalidateVisual();
                rsv.inkStrokes.Width = wb.Width;
                rsv.inkStrokes.Height = wb.Height;
            });
        }
  

        public static BitmapImage GetBitmapImage(byte[] imageBytes)
        {
            try
            {
                //File.WriteAllBytes("test.jpg",imageBytes);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(imageBytes);
                bitmapImage.EndInit();
                return bitmapImage;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void _screenshot_OnBuffer(TEventEntry aEvent, int aTick, int aBufferId, TByteBuffer aBuffer)
        {
            rsv.Dispatcher.Invoke(
                delegate
                {
                    //rsv.bs.Source = GetBitmapImage(aBuffer.Buffer);
                    //Console.WriteLine(rsv.bs.Source.ToString());
                    var bsSrc =  GetImageFromJpegByteArray(aBuffer.Buffer);
                    if(bsSrc != null)
                        rsv.bs.Source = bsSrc;
                    //rsv.bs.Source = GetImageFromByteArray(aBuffer.Buffer, Client.ResolutionY / Client.Quality,Client.ResolutionX / Client.Quality);
                });
        }

        private void FeDockedChanged(object sender, EventArgs e)
        {
            if (Fe.Large)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        public void Unsubscribe()
        {
  
            if (_screenshot != null)
            {
                AppState.Imb.Imb.UnSubscribe(Client.Id + ".Screenshot");
                _screenshot = null;
            }

            if (_cameraStream != null)
            {
                AppState.Imb.Imb.UnSubscribe(Client.Id + ".CameraStream");
                _cameraStream = null;
            }
        }

        public void Subscribe()
        {
          

            _screenshot = AppState.Imb.Imb.Subscribe(Client.Id + ".Screenshot");
            _screenshot.OnBuffer += _screenshot_OnBuffer;

            if (Client.Type == "Phone")
            {
                _cameraStream = AppState.Imb.Imb.Subscribe(Client.Id + ".CameraStream");
                _cameraStream.OnBuffer += _cameraStream_OnBuffer;
                _strokeStream = AppState.Imb.Imb.Subscribe(Client.Id + ".StrokeStream");
                _strokeStream.OnNormalEvent += _strokeStream_OnNormalEvent;
                _cameraStream.OnStreamCreate += _cameraStream_OnStreamCreate;
                _cameraStream.OnStreamEnd += _cameraStream_OnStreamEnd;
            }
        }

        void _cameraStream_OnStreamEnd(TEventEntry aEvent, ref Stream aStream, string aStreamName)
        {
            // Create image from stream
            long strLen = aStream.Length;
        }

        private Stream strm = null;

        Stream _cameraStream_OnStreamCreate(TEventEntry aEvent, string aStreamName)
        {
            //throw new NotImplementedException();
            strm = new MemoryStream();
            return strm;
        }

        void _strokeStream_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            rsv.Dispatcher.Invoke(
                delegate
                {   
                    string strokesStr = aPayload.ReadString();
                    if (String.IsNullOrEmpty(strokesStr))
                        return;
                    if (strokesStr == "clear")
                    {
                        _strokes = new Dictionary<string, Stroke>();
                        rsv.inkStrokes.Strokes.Clear();
                        rsv.inkStrokes.InvalidateVisual();
                        rsv.gMain.InvalidateVisual();
                        return;
                    }
                    string[] strokesArr = strokesStr.Split(';');
                    _strokes = new Dictionary<string, Stroke>();
                    rsv.inkStrokes.Strokes.Clear();
                    foreach (string strokeStr in strokesArr)
                    {
                        string[] strokeDef = strokeStr.Split('/');
                        if (strokeDef.Length != 2)
                            break;
                        string guid = strokeDef[0];
                        if (_strokes.ContainsKey(guid))
                            continue;
                            
                        string[] pointsArr = strokeDef[1].Split('|');
                        StylusPointCollection points = new StylusPointCollection();
                        try
                        {
                            foreach (string pointStr in pointsArr)
                            {

                                string[] pointParts = pointStr.Split(',');

                                if (pointParts.Length == 2 && wb != null)
                                {
                                    StylusPoint point = new StylusPoint(wb.Width / 640d * double.Parse(pointParts[1]), wb.Height - (wb.Height / 480d * double.Parse(pointParts[0])));
                                    //StylusPoint point = new StylusPoint(double.Parse(pointParts[1]), wb.Height - double.Parse(pointParts[0]));
                                    points.Add(point);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            string errMsg = err.Message;
                        }
                        try
                        {
                            Stroke stroke = new Stroke(points);
                            stroke.DrawingAttributes.Color = Colors.Red;
                            stroke.DrawingAttributes.Height = 3;
                            stroke.DrawingAttributes.Width = 3;
                            //rsv.inkStrokes.Width = rsv.bs.ActualWidth;
                            rsv.inkStrokes.Strokes.Add(stroke);
                                
                            _strokes.Add(guid, stroke);
                               
                        }
                        catch { }
                    }
                    rsv.inkStrokes.InvalidateVisual();
                    rsv.gMain.InvalidateVisual();
                });
        }

        public void BConnect()
        {
            AppState.Imb.SendCommand(Client.Id, "Connect");
        }

        public void ScreenOn()
        {
            if (!Follow)
            {
                AppState.Imb.SendCommand(Client.Id, RemoteScreenPlugin.ScreenOnCommand);
                Follow = true;
            }
            else
            {
                AppState.Imb.SendCommand(Client.Id, RemoteScreenPlugin.ScreenOffCommand);
                Follow = false;    
            }
        }

        public void ScreenOff()
        {

            AppState.Imb.SendCommand(Client.Id, RemoteScreenPlugin.ScreenOffCommand);
            Follow = false;
            
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
            rsv.Opacity = 1;
            if (e.Cursor.Data is Document)
            {
                var d = (Document) e.Cursor.Data;

                AppState.SendDocument(Client, d);
            }
        }

        private void DoEnter(object sender, SurfaceDragDropEventArgs e)
        {
            rsv.Opacity = 0.5;
        }

        private void DoLeave(object sender, SurfaceDragDropEventArgs e)
        {
            rsv.Opacity = 1;
        }

        private System.Drawing.Bitmap buffImage = null;

        private BitmapSource GetImageFromJpegByteArray(byte[] pixelInfo)
        {
            
            MemoryStream ms = new MemoryStream(pixelInfo);
            var returnImage = System.Drawing.Image.FromStream(ms);
            var prop = returnImage.PropertyItems.FirstOrDefault(p => p.Id == 40094);
            int dimX = 0, dimY = 0, cX = 0, cY = 0, finalFrame = 0;
            if(prop != null)
            {
                // Decode properties
                var propStrParts = System.Text.Encoding.Default.GetString(prop.Value).Replace("\0","").Split('|');
                dimX = int.Parse(propStrParts[0]);
                dimY = int.Parse(propStrParts[1]);
                cX = int.Parse(propStrParts[2]);
                cY = int.Parse(propStrParts[3]);
                finalFrame = int.Parse(propStrParts[4]);
            }
            if (buffImage == null || (buffImage.Width != dimX * returnImage.Width) || (buffImage.Height != dimY * returnImage.Height))
            {
                buffImage = new Bitmap(dimX * returnImage.Width, dimY * returnImage.Height);
            }
            Graphics g = Graphics.FromImage((Image)buffImage);
            //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            //g.DrawImage(imgToDivide, xSize * x, ySize * y, xSize, ySize);
            g.DrawImage(returnImage, returnImage.Width * cX, returnImage.Height * cY);
            g.Dispose();

            if (finalFrame == 1)
            {
                var bms = Imaging.CreateBitmapSourceFromBitmap((System.Drawing.Bitmap) buffImage);

                return bms;
            }
            return null;
        }


        private BitmapSource GetImageFromByteArray(byte[] pixelInfo, int height, int width)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                //System.Drawing.Image i = new System.Drawing.Image();
                BitmapSource i = new BitmapImage();
                //i.
                //i.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                
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

    public interface IFollowScreen
    {
        FloatingElement Fe { get; set; }
        ImbClientStatus Client { get; set; }

        void Unsubscribe();
    }

    public static class Imaging
    {
        public static BitmapSource CreateBitmapSourceFromBitmap(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            if (Application.Current.Dispatcher == null)
                return null; // Is it possible?

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // You need to specify the image format to fill the stream. 
                    // I'm assuming it is PNG
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    // Make sure to create the bitmap in the UI thread
                    if (InvokeRequired)
                        return (BitmapSource)Application.Current.Dispatcher.Invoke(
                            new Func<Stream, BitmapSource>(CreateBitmapSourceFromBitmap),
                            System.Windows.Threading.DispatcherPriority.Normal,
                            memoryStream);

                    return CreateBitmapSourceFromBitmap(memoryStream);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool InvokeRequired
        {
            get { return System.Windows.Threading.Dispatcher.CurrentDispatcher != Application.Current.Dispatcher; }
        }

        private static BitmapSource CreateBitmapSourceFromBitmap(Stream stream)
        {
            BitmapDecoder bitmapDecoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            // This will disconnect the stream from the image completely...
            WriteableBitmap writable = new WriteableBitmap(bitmapDecoder.Frames.Single());
            writable.Freeze();

            return writable;
        }
    } 
}