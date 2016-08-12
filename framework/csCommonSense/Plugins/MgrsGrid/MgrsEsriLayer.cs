using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace csCommon.Plugins.MgrsGrid
{
    public class MgrsEsriLayer : DynamicLayer, ISupportsDynamicImageByteRequests
    {
        
        private Envelope fullExtent = new Envelope();

   



        /// <summary>
        /// Initializes a new instance of the <see cref="HeatMapLayer"/> class.
        /// </summary>
        public MgrsEsriLayer(MgrsConfig pCfg)
        {
            Cfg = pCfg;
            this.DisplayName = "MGRS grid";
        }

        public MgrsConfig Cfg { get; private set; }

        /// <summary>
        /// The full extent of the layer.
        /// </summary>
        public override Envelope FullExtent
        {
            get
            {
                return fullExtent;
            }
            protected set { throw new NotSupportedException(); }
        }


       public double LastMetersPerPixel { get; private set;  }




        /// <summary>
        /// Gets the source image to display in the dynamic layer. Override this to generate
        /// or modify images.
        /// </summary>
        /// <param name="properties">The image export properties.</param>
        /// <param name="onComplete">The method to call when the image is ready.</param>
        /// <seealso cref="ESRI.ArcGIS.Client.DynamicLayer.OnProgress"/>
        protected override void GetSource(DynamicLayer.ImageParameters properties, DynamicLayer.OnImageComplete onComplete)
        {
            if (!IsInitialized)
            {
                onComplete(null, null);
                return;
            }
            Envelope extent = properties.Extent;
            var ex121 = AppStateSettings.Instance.ViewDef.MapControl.Extent;

            var vp = new MgrsViewport(Map); /* Calculate visisble UTM boxes */
            LastMetersPerPixel = vp.MetersPerPixel;
            if (!Cfg.IsEnabled)
            {
                onComplete(null, null);
                return;
            }
            var draw = new DrawMgrsRaster(Cfg, vp);
            


            int width = properties.Width;
            int height = properties.Height;
            var img1 = GenerateImage1(width, height, 96, dc => draw.Render(dc));


            onComplete(Convert(img1), new ImageResult(extent));
        }


        public static BitmapSource GenerateImage1(int pWidth, int pHeight, double pDpi, Action<DrawingContext> pRender)
        {
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                pRender(dc);
            }
            RenderTargetBitmap rt = new RenderTargetBitmap(pWidth, pHeight, pDpi, pDpi, PixelFormats.Default);
            rt.Render(dv);
            return rt;
        }

        public static BitmapImage Convert(BitmapSource pBS)
        {
            var imgEncoder = new PngBitmapEncoder();
            imgEncoder.Interlace = PngInterlaceOption.Off;
            imgEncoder.Frames.Add(BitmapFrame.Create(pBS));
            BitmapImage result = new BitmapImage();
            using (var stream = new MemoryStream())
            {
                imgEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);



                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                //  result.Freeze();

            }

            return result;
        }

        public static BitmapImage GenerateImage(FrameworkElement pFE, int pWidth, int pHeight)
        {;
            pFE.Width = pWidth;
            pFE.Height = pHeight;
            System.Windows.Size theTargetSize = new System.Windows.Size(pWidth, pHeight);
            pFE.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            pFE.Arrange(new Rect(theTargetSize));
            // to affect the changes in the UI, you must call this method at the end to apply the new changes

            pFE.UpdateLayout();
            pFE.InvalidateMeasure();
            pFE.InvalidateArrange();
            pFE.InvalidateVisual();
            var imgEncoder = new PngBitmapEncoder();
            imgEncoder.Interlace = PngInterlaceOption.Off;
            RenderTargetBitmap bmpSource = new RenderTargetBitmap((int)pWidth, (int)pHeight, 96, 96, PixelFormats.Pbgra32);
            bmpSource.Render(pFE);
            imgEncoder.Frames.Add(BitmapFrame.Create(bmpSource));
            BitmapImage result = new BitmapImage();
            using (var stream = new MemoryStream())
            {
                imgEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                
                
                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
              //  result.Freeze();
                
            }

            return result;


        }





       



        void ISupportsDynamicImageByteRequests.GetImageData(DynamicLayer.ImageParameters properties, OnImageDataReceived onImageDataReceived)
        {
            OnImageComplete onImageComplete =
                (image, props) =>
                {
                    BitmapSource bitmapSource = image as BitmapSource;

                    MemoryStream stream = new MemoryStream();
                    if (bitmapSource != null)
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Interlace = PngInterlaceOption.Off;
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    onImageDataReceived(stream, props);
                };

            GetSource(properties, onImageComplete);
        }


    }
}