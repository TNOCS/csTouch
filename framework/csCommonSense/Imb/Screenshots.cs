#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using csShared.Utils;
using IMB3;
using Image = System.Drawing.Image;
using Point = System.Windows.Point;
using Size = System.Drawing.Size;

#endregion

namespace csImb
{
    public class Screenshots
    {
        private DispatcherTimer timer = new DispatcherTimer();
        private readonly Dictionary<string, byte[]> oldParts = new Dictionary<string, byte[]>();
        public TEventEntry Screenshot;
        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
            }
        }

        public ImbClientStatus ImbClientStatus { get; set; }
        public csImb Imb { get; set; }
        public FrameworkElement Target { get; set; }

        /// <summary>
        ///     Only do something when someone is interested in a screenshot.
        /// </summary>
        public bool HasListeners
        {
            get { return InitChannel() && Screenshot.Subscribers; }
        }


        // get raw bytes from BitmapImage using BitmapImage.CopyPixels
        private byte[] GetImageByteArray(BitmapSource bi)
        {
            var rawStride = (bi.PixelWidth * bi.Format.BitsPerPixel + 7) / 8;
            var result = new byte[rawStride * bi.PixelHeight];
            bi.CopyPixels(result, rawStride, 0);
            return result;
        }

        private BitmapSource GetImageFromByteArray(byte[] pixelInfo, int height, int width)
        {
            var pf = PixelFormats.Bgr32;
            var stride = (width * pf.BitsPerPixel + 7) / 8;
            var image = BitmapSource.Create(width, height, 96, 96, pf, null, pixelInfo, stride);
            return image;
        }

        /*public Stream GetJpgImage2(Image source)
        {
            var encoder = new JpegBitmapEncoder();
            //Guid photoID = System.Guid.NewGuid();
            //String photolocation = photoID.ToString() + ".jpg";  //file name 

            encoder.Frames.Add(BitmapFrame.Create());
            var memStr = new MemoryStream();
            
            encoder.Save(memStr);
            //var strRead = new StreamReader(memStr);
            return memStr;
            //using (var filestream = new FileStream(photolocation, FileMode.Create))
            //    encoder.Save(filestream);

        }*/

        public byte[] GetJpgImage(FrameworkElement source, double scale, int quality)
        {
            try
            {
                var actualHeight = source.ActualHeight;
                var actualWidth = source.ActualWidth;

                var renderHeight = actualHeight * scale;
                var renderWidth = actualWidth * scale;

                var renderTarget = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96,
                                                          PixelFormats.Pbgra32);
                var sourceBrush = new VisualBrush(source);

                var drawingVisual = new DrawingVisual();
                var drawingContext = drawingVisual.RenderOpen();

                using (drawingContext)
                {
                    drawingContext.PushTransform(new ScaleTransform(scale, scale));
                    drawingContext.DrawRectangle(sourceBrush, null,
                                                 new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
                }
                renderTarget.Render(drawingVisual);

                var jpgEncoder = new JpegBitmapEncoder { QualityLevel = quality };
                jpgEncoder.Frames.Add(BitmapFrame.Create(renderTarget));

                Byte[] imageArray;

                using (var outputStream = new MemoryStream())
                {
                    jpgEncoder.Save(outputStream);
                    imageArray = outputStream.ToArray();
                }
                return imageArray;
            }
            catch (Exception)
            {
                return null;
            }
        }

        ///// <summary>
        ///// Get screen from Window (uses WindowInteropHelper to get the handle from a Window).
        ///// </summary>
        ///// <param name="window"></param>
        ///// <returns></returns>
        //private static System.Windows.Forms.Screen GetScreen(Window window)
        //{
        //    return System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
        //}

        static Point WpfToRealPixels(Window w, Point p)
        {
            var presentationSource = PresentationSource.FromVisual(w);
            if (presentationSource != null)
                if (presentationSource.CompositionTarget != null)
                    return presentationSource.CompositionTarget.TransformToDevice.Transform(p);
            return new Point(0, 0);
        }

        /// <summary>
        ///     Take a screenshot and send it over the bus.
        /// </summary>
        private void TakeScreenshot()
        {
            if (Target == null) return;

            if (!InitChannel() || !Screenshot.Subscribers) return;
            try
            {
                var mw = Application.Current.MainWindow;
                // get complete screen
                // Get potentially partial screen:
                var topLeft = WpfToRealPixels(mw, new Point(mw.Left, mw.Top));
                var bottomRight = WpfToRealPixels(mw, new Point(mw.Left + mw.ActualWidth, mw.Top + mw.ActualHeight));
                var bounds = new Rect(topLeft, bottomRight);
                var screen = new Bitmap((int)bounds.Width, (int)bounds.Height);
                var g = Graphics.FromImage(screen);

                g.CopyFromScreen((int)bounds.Left, (int)bounds.Top, 0, 0, screen.Size);
                g.Dispose();

                // resize image

                //var bm = resizeImage(screen, new Size(Imb.Status.ResolutionX / Imb.Status.Quality, Imb.Status.ResolutionY / Imb.Status.Quality));
                //var bm2 = resizeImage(screen, new Size(Imb.Status.ResolutionX, Imb.Status.ResolutionY ));

                const int dimX = 4;
                const int dimY = 4;
                for (var cX = 0; cX < dimX; cX++)
                {
                    for (var cY = 0; cY < dimY; cY++)
                    {
                        var bm = GetSubRectangle(screen, dimX, dimY, cX, cY);

                        if (bm == null) continue;
                        var ms = new MemoryStream();
                        bm.Save(ms, ImageFormat.Jpeg);
                        ms.Position = 0;
                        var finalFrame = 0;
                        if (cX == dimX - 1 && cY == dimY - 1)
                            finalFrame = 1;
                        var metaString = String.Format("{0}|{1}|{2}|{3}|{4}", dimX, dimY, cX, cY, finalFrame);
                        var newStream = (MemoryStream)AddImageComment(ms, metaString, 40094);
                        ms.Dispose();
                        ms = newStream;
                        // get byte array
                        //BitmapSource bs = loadBitmap((Bitmap) bm);
                        //byte[] ScreenshotBytes = GetImageByteArray(bs);
                        ms.Position = 0;
                        var ssbytes = new byte[ms.Length];
                        ms.Read(ssbytes, 0, Convert.ToInt32(ms.Length));

                        var sendPart = true;
                        var partKey = cX.ToString(CultureInfo.InvariantCulture) + "|" +
                                      cY.ToString(CultureInfo.InvariantCulture);
                        if (oldParts.ContainsKey(partKey))
                        {
                            // Compare the byte[]s
                            // Test ; only check length
                            if (oldParts[partKey].Length == ssbytes.Length)
                                sendPart = false;
                            else
                                oldParts[partKey] = ssbytes;
                        }
                        else
                        {
                            oldParts.Add(partKey, ssbytes);
                        }

                        // Always send last image to make sure client will update image
                        if (finalFrame == 1)
                            sendPart = true;
                        /*var ssbytesEnh = new byte[ssbytes.Length + 4];
                                ssbytes.CopyTo(ssbytesEnh, 4);
                                ssbytesEnh[0] = Convert.ToByte(dimX);
                                ssbytesEnh[1] = Convert.ToByte(dimY);
                                ssbytesEnh[2] = Convert.ToByte(cX);
                                ssbytesEnh[3] = Convert.ToByte(cY);*/
                        if (sendPart)
                            Screenshot.SignalBuffer(0, ssbytes); //Enh);
                        ms.Dispose();
                        //Screenshot.SignalStream("jpgimage", ms);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("");
            }
            // c.MessageBroker.SendMessage(c, new Header(), screen);

            //f.MessageBroker.mConnection.SignalBuffer(_channelId, 0, Screenshot, 0);
        }

        /// <summary>
        ///     Save an image of a control
        /// </summary>
        /// <param name="controlToConvert"> The control to convert to an ImageSource </param>
        /// ///
        /// <param name="fileName"> The location to save the image to </param>
        /// <returns> The returned ImageSource of the controlToConvert </returns>
        public static ImageSource SaveImageOfControl(Control controlToConvert, string fileName)
        {
            try
            {
                // create a file stream for saving image
                using (var outStream = new FileStream(fileName, FileMode.Create))
                {
                    var r = GetImageFromControl(controlToConvert);
                    r.Save(outStream);
                    return r.Frames[0];
                } // save encoded data to stream
            }
            catch (Exception e)
            {
                Logger.Log("SaveImageOfControl", string.Format("Exception caught saving stream: {0}", e.Message), "Error saving image", Logger.Level.Error, true, true);
                return null;
            }
        }

        /// <summary>
        ///     Get an ImageSource of a control
        /// </summary>
        /// <param name="controlToConvert"> The control to convert to an ImageSource </param>
        /// <returns> The returned ImageSource of the controlToConvert </returns>
        public static ImageSource GetImageOfControl(Control controlToConvert)
        {
            // return first frame of image 
            return GetImageFromControl(controlToConvert).Frames[0];
        }

        /// <summary>
        ///     Convert any control to a PngBitmapEncoder
        /// </summary>
        /// <param name="controlToConvert"> The control to convert to an ImageSource </param>
        /// <returns> The returned ImageSource of the controlToConvert </returns>
        /// <see cref="http://www.dreamincode.net/code/snippet4326.htm" />
        public static PngBitmapEncoder GetImageFromControl(FrameworkElement controlToConvert)
        {
            // get size of control
            var sizeOfControl = new System.Windows.Size(controlToConvert.ActualWidth, controlToConvert.ActualHeight);
            // measure and arrange the control
            controlToConvert.Measure(sizeOfControl);
            // arrange the surface
            controlToConvert.Arrange(new Rect(sizeOfControl));

            // craete and render surface and push bitmap to it
            var renderBitmap = new RenderTargetBitmap((Int32)sizeOfControl.Width, (Int32)sizeOfControl.Height, 96d, 96d, PixelFormats.Pbgra32);
            // now render surface to bitmap
            renderBitmap.Render(controlToConvert);
            // encode png data
            var pngEncoder = new PngBitmapEncoder();
            // puch rendered bitmap into it
            pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            //pngEncoder.Metadata = new BitmapMetadata("png") {
            //                                                    ApplicationName = "Marvelous",
            //                                                    DateTaken = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            //                                                    Subject = "Casual Loop Analysis using Marvel",
            //                                                    Title = "Marvelous screenshot",
            //                                                    Author = new ReadOnlyCollection<string>(new List<string> {
            //                                                                                                                 Properties.Settings.Default.UserName
            //                                                                                                             })
            //                                                };

            // return encoder
            return pngEncoder;
        }

        /// <summary>
        ///     Send a screenshot over the bus. Identical to TakeScreenshot, except that this method does not capture the screenshot itself.
        /// </summary>
        /// <param name="screenshot">Original image with 96x96 DPI</param>
        public void ProcessScreenshot(Image screenshot)
        {
            if (screenshot == null) return;

            if (!HasListeners) return;
            try
            {
                // Split the image in 4x4 blocks, compress each block and only sent the blocks that have changed since the last time
                const int dimX = 4;
                const int dimY = 4;
                // TODO Use a Parallel.For loop to process these blocks in parallel? Or is the receiving order important?

                for (var cX = 0; cX < dimX; cX++)
                {
                    for (var cY = 0; cY < dimY; cY++)
                    {
                        var bm = GetSubRectangle(screenshot, dimX, dimY, cX, cY);

                        if (bm == null) continue;
                        using (var ms = new MemoryStream())
                        {
                            bm.Save(ms, ImageFormat.Jpeg);
                            ms.Position = 0;
                            var finalFrame = 0;
                            if (cX == dimX - 1 && cY == dimY - 1)
                                finalFrame = 1;
                            var metaString = String.Format("{0}|{1}|{2}|{3}|{4}", dimX, dimY, cX, cY, finalFrame);
                            using (var newStream = (MemoryStream)AddImageComment(ms, metaString, 40094))
                            {
                                newStream.Position = 0;
                                // get byte array
                                //BitmapSource bs = loadBitmap((Bitmap) bm);
                                //byte[] ScreenshotBytes = GetImageByteArray(bs);
                                //ms.Position = 0;
                                var ssbytes = new byte[newStream.Length];
                                newStream.Read(ssbytes, 0, Convert.ToInt32(newStream.Length));

                                var sendPart = true;
                                var partKey = cX.ToString(CultureInfo.InvariantCulture) + "|" +
                                              cY.ToString(CultureInfo.InvariantCulture);
                                if (oldParts.ContainsKey(partKey))
                                {
                                    // Compare the byte[]s
                                    // Test ; only check length
                                    if (oldParts[partKey].Length == ssbytes.Length)
                                        sendPart = false;
                                    else
                                        oldParts[partKey] = ssbytes;
                                }
                                else
                                {
                                    oldParts.Add(partKey, ssbytes);
                                }

                                // Always send last image to make sure client will update image
                                if (sendPart || finalFrame == 1)
                                    Screenshot.SignalBuffer(0, ssbytes); //Enh);
                                //Screenshot.SignalStream("jpgimage", ms);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("");
            }
            // c.MessageBroker.SendMessage(c, new Header(), screen);

            //f.MessageBroker.mConnection.SignalBuffer(_channelId, 0, Screenshot, 0);
        }

        private static Stream AddImageComment(Stream jpegStreamIn, string propValue, int propId)
        {
            //string jpegDirectory = Path.GetDirectoryName(imageFlePath);
            //string jpegFileName = Path.GetFileNameWithoutExtension(imageFlePath);


            var decoder = new JpegBitmapDecoder(jpegStreamIn, BitmapCreateOptions.PreservePixelFormat,
                                                BitmapCacheOption.OnLoad);
            var bitmapFrame = decoder.Frames[0];

            var metaData = (BitmapMetadata)bitmapFrame.Metadata.Clone();

            // modify the metadata   
            metaData.SetQuery("/app1/ifd/exif:{uint=" + propId.ToString(CultureInfo.InvariantCulture) + "}", propValue);

            // get an encoder to create a new jpg file with the new metadata.      
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapFrame, bitmapFrame.Thumbnail, metaData,
                                                  bitmapFrame.ColorContexts));

            // Save the new image 
            var ms = new MemoryStream();

            encoder.Save(ms);
            return ms;
        }

        private static Image GetSubRectangle(Image imgToDivide, int totalX, int totalY, int x, int y)
        {
            var xSize = imgToDivide.Width / totalX;
            var ySize = imgToDivide.Height / totalY;

            var b = new Bitmap(xSize, ySize); //, imgToDivide.PixelFormat);
            //b.SetResolution(imgToDivide.HorizontalResolution, imgToDivide.VerticalResolution);
            var g = Graphics.FromImage(b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            //g.DrawImage(imgToDivide, xSize * x, ySize * y, xSize, ySize);
            g.DrawImage(imgToDivide, 0, 0, new Rectangle(xSize * x, ySize * y, xSize, ySize), GraphicsUnit.Pixel);
            g.Dispose();

            return b;
        }

        private static Image CropImage(Image img, Rectangle cropArea)
        {
            var b = new Bitmap(img);
            var bmpCrop = b.Clone(cropArea, b.PixelFormat);
            return bmpCrop;
        }

        private static Image resizeImage(Image imgToResize, Size size)
        {
            var sourceWidth = imgToResize.Width;
            var sourceHeight = imgToResize.Height;

            var nPercentW = (size.Width / (float)sourceWidth);
            var nPercentH = (size.Height / (float)sourceHeight);

            //if (nPercentH < nPercentW)
            //    ;
            //else
            //    ;

            var destWidth = size.Width; // (int)(sourceWidth * nPercent);
            var destHeight = size.Height; // (int)(sourceHeight * nPercent);

            if (destWidth > 0 && destHeight > 0)
            {
                var b = new Bitmap(destWidth, destHeight);
                var g = Graphics.FromImage(b);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
                g.Dispose();

                return b;
            }
            return null;
        }

        public static BitmapSource LoadBitmap(Bitmap source)
        {
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                                                             BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool InitChannel()
        {
            if (Screenshot == null)
            {
                if (Imb != null && Imb.IsConnected)
                {
                    Screenshot = Imb.Imb.Publish(Imb.Imb.ClientHandle + ".Screenshot");
                }
                else return false;
            }
            return true;
        }

        public void Start(int interval)
        {
            timer.Interval = new TimeSpan(0, 0, 0, 0, interval);
            timer.Tick -= TimerTick;
            timer.Tick += TimerTick;
            timer.Start();
            IsRunning = true;
        }

        [DebuggerStepThrough]
        void TimerTick(object sender, EventArgs e)
        {
            if (IsRunning) TakeScreenshot();
        }

        public void Stop()
        {
            if (timer == null) return;
            IsRunning = false;
            timer.Stop();
            timer.IsEnabled = false;
        }
    }
}