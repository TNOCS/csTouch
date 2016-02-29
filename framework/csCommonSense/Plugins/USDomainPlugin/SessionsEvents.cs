using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMB3;
using IMB3.ByteBuffers;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;


namespace SessionsEvents
{
    public class Sessions
    {
        public const string SessionsEventName = "Sessions";
        public const string DomainsEventNamePostFix = "Domains";

        public const Int32 scSession = 101;
        public const Int32 scRequestSessions = 102;

        public const Int32 scDomain = 111;
        public const Int32 scRequestDomains = 112;

        public const Int32 scLayer = 121;
        public const Int32 scRequestLayer = 122;

        public const Int32 scSelectID = 131;
        public const Int32 scSelectTime = 132;
        public const Int32 scPreSelectTime = 133;

        public const Int32 actionNew= 0;
        public const Int32 actionDelete = 1;
        public const Int32 actionChange = 2;

        public static void SignalRequestSessions(TEventEntry signalEvent, TEventEntry returnEvent)
        {
            TByteBuffer Payload = new TByteBuffer();
            Payload.Prepare(scRequestSessions);
            Payload.Prepare(returnEvent.EventName);
            Payload.PrepareApply();
            Payload.QWrite(scRequestSessions);
            Payload.QWrite(returnEvent.EventName);
            signalEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
        }

        public static void SignalRequestDomains(string signalEventName, TEventEntry returnEvent)
        {
            TByteBuffer Payload = new TByteBuffer();
            Payload.Prepare(scRequestDomains);
            Payload.Prepare(returnEvent.EventName);
            Payload.PrepareApply();
            Payload.QWrite(scRequestDomains);
            Payload.QWrite(returnEvent.EventName);
            returnEvent.connection.SignalEvent(signalEventName, TEventEntry.TEventKind.ekNormalEvent, Payload, false);
            returnEvent.connection.UnPublish(signalEventName, false);
        }

        public static void SignalRequestLayer(TEventEntry layerEvent, TEventEntry returnEvent)
        {
            TByteBuffer Payload = new TByteBuffer();
            Payload.Prepare(scRequestLayer);
            Payload.Prepare(returnEvent.EventName);
            Payload.PrepareApply();
            Payload.QWrite(scRequestLayer);
            Payload.QWrite(returnEvent.EventName);
            layerEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
        }
    }

    public class SessionPngImage
    {
        public SessionPngImage()
        {
            PngImage = new byte[0];
        }

        public SessionPngImage(TByteBuffer aPayload)
        {
            Int32 len;
            aPayload.Read(out len);
            PngImage = aPayload.ReadBytes((ulong)len);
        }
        
        public bool Empty { get { return PngImage.Length == 0; } }
        
        public BitmapSource GetImageSource()
        {
            if (!Empty)
            {
                MemoryStream ms = new MemoryStream(PngImage);
                PngBitmapDecoder decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bms = decoder.Frames[0];
                return bms;
            }
            else
                return null;
        }

        private byte[] PngImage;
    }

    public class DynamicImage
    {
        private Control fControl;
        private TEventEntry fImageSourceEvent;
        private TEventEntry fPrivateEvent;
        private WriteableBitmap fImageSrc;

        public TEventEntry ImageSourceEvent { get { return fImageSourceEvent; } }
        public TEventEntry PrivateEvent { get { return fPrivateEvent; } }
        
        public DynamicImage(Control aControl, TEventEntry aImageSourceEvent, TEventEntry aPrivateEvent, int aWidth, int aHeight)
        {
            fControl = aControl;
            fImageSourceEvent = aImageSourceEvent;
            fPrivateEvent = aPrivateEvent;
            if (aWidth > 0 && aHeight > 0)
            {
                try
                {
                    fImageSrc = new WriteableBitmap(aWidth, aHeight, 96, 96, PixelFormats.Pbgra32, null);
                }
                catch
                {
                    fImageSrc = null; // TODO: out of memory?  
                }
            }
            else
                fImageSrc = null; // TODO: should never happen!
            fImageSourceEvent.OnNormalEvent += new TEventEntry.TOnNormalEvent(fEvent_OnNormalEvent);
            fPrivateEvent.OnNormalEvent += new TEventEntry.TOnNormalEvent(fEvent_OnNormalEvent);
            Sessions.SignalRequestLayer(fImageSourceEvent, fPrivateEvent);
        }

        ~DynamicImage()
        {
            // remove unsubscribe because of call in garbage collector 
            // can be a lot later and already subscribed via other domain
        }

        public void UnSubscribe()
        {
            fImageSourceEvent.OnNormalEvent -= fEvent_OnNormalEvent;
            fImageSourceEvent.UnSubscribe();
            fPrivateEvent.OnNormalEvent -= fEvent_OnNormalEvent;
            fPrivateEvent.UnSubscribe();
        }

        delegate void TUpdateLayer(int aX, int aY, int aWidth, int aHeight, int[] aPixels);

        private void UpdateLayer(int aX, int aY, int aWidth, int aHeight, int[] aPixels)
        {
            lock (fImageSrc)
            {
                Int32Rect rect = new Int32Rect(0, 0, aWidth, aHeight);
                fImageSrc.WritePixels(rect, aPixels, aWidth * sizeof(Int32), aX, aY);
            }
        }

        void fEvent_OnNormalEvent(TEventEntry aEvent, TByteBuffer aPayload)
        {
            Int32 code;
            Int32 x;
            Int32 y;
            Int32 width;
            Int32 height;
            int[] pixels;
            Int32 command;
            aPayload.Read(out command);
            switch (command)
            {
                case Sessions.scLayer:
                    lock (fImageSrc)
                    {
                        // read header
                        aPayload.Read(out code);
                        aPayload.Read(out x);
                        aPayload.Read(out y);
                        // load png image from payload
                        SessionPngImage pngImage = new SessionPngImage(aPayload);
                        if (!pngImage.Empty)
                        {
                            BitmapSource bmpSrc = pngImage.GetImageSource();
                            // extract pixels from image
                            width = bmpSrc.PixelWidth;
                            height = bmpSrc.PixelHeight;
                            pixels = new int[width * height];
                            bmpSrc.CopyPixels(pixels, width * sizeof(Int32), 0);
                        }
                        else
                        {
                            width = 0;
                            height = 0;
                            pixels = new int[0];
                        }
                    }
                    if (width != 0 && height != 0)
                    {
                        // put pixels in current bitmap
                        Object[] args = new Object[5] { x, y, width, height, pixels };
                        fControl.Dispatcher.BeginInvoke(new TUpdateLayer(UpdateLayer), args);
                    }
                    break;
            }
        }

        public WriteableBitmap ImageSrc { get { return fImageSrc; } }
    }

    public class SessionExtent
    {
        public double xMin;
        public double yMin;
        public double xMax;
        public double yMax;

        public SessionExtent()
        {
            xMin = 0;
            yMin = 0;
            xMax = 0;
            yMax = 0;
        }

        public SessionExtent(double axMin, double ayMin, double axMax, double ayMax)
        {
            xMin = axMin;
            yMin = ayMin;
            xMax = axMax;
            yMax = ayMax;
        }

        public SessionExtent(SessionExtent aSessionExtent)
        {
            xMin = aSessionExtent.xMin;
            yMin = aSessionExtent.yMin;
            xMax = aSessionExtent.xMax;
            yMax = aSessionExtent.yMax;
        }

        public SessionExtent(TByteBuffer aPayload)
        {
            aPayload.Read(out xMin);
            aPayload.Read(out yMin);
            aPayload.Read(out xMax);
            aPayload.Read(out yMax);
        }

        public double Width { get { return xMax - xMin; } set { xMax = xMin + value; } }
        public double Height { get { return yMax - yMin; } set { yMax = yMin + value; } }

        public SessionExtent Translate(double dx, double dy)
        {
            return new SessionExtent(xMin + dx, yMin + dy, xMax + dx, yMax + dy);
        }

        public SessionExtent Inflate(double aFactor)
        {
            SessionExtent res = new SessionExtent(this);
            // grow
            res.Width = Width * aFactor;
            res.Height = Height * aFactor;
            // reset center to same position
            res = res.Translate((Width - res.Width) / 2, (Height - res.Height) / 2);
            return res;
        }
    }

    public class SessionNewEvent
    {
        public Int32 sessionID;
        public string eventName;
        public string description;
        public SessionPngImage icon;
        public SessionExtent extent;

        public SessionNewEvent(TByteBuffer aPayload)
        {
            aPayload.Read(out sessionID);
            aPayload.Read(out eventName);
            aPayload.Read(out description);
            icon = new SessionPngImage(aPayload);
            extent = new SessionExtent(aPayload);
        }

        public object GenerateUI()
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.MinHeight = 32;
            if (!icon.Empty)
            {
                Image im = new Image();
                im.Source = icon.GetImageSource();
                im.Stretch = Stretch.None;
                //im.IsHitTestVisible = false;
                sp.Children.Add(im);
            }
            Label lbl = new Label();
            lbl.Content = description;
            //lbl.IsHitTestVisible = false;
            lbl.Foreground = Brushes.DarkGray;
            lbl.MinHeight = 32;
            sp.Children.Add(lbl);
            return sp;
        }
    }

    public class SessionChangeEvent
    {
        public Int32 sessionID;
        public string description;
        public SessionPngImage icon;
        public SessionExtent extent;

        public SessionChangeEvent(TByteBuffer aPayload)
        {
            aPayload.Read(out sessionID);
            aPayload.Read(out description);
            icon = new SessionPngImage(aPayload);
            extent = new SessionExtent(aPayload);
        }
    }

    public class SessionDeleteEvent
    {
        public Int32 sessionID;

        public SessionDeleteEvent(TByteBuffer aPayload)
        {
            aPayload.Read(out sessionID);
        }
    }

    public class SessionPaletteEntry
    {
        public Int32 color;
        public string description;

        public SessionPaletteEntry(TByteBuffer aPayload)
        {
            aPayload.Read(out color);
            aPayload.Read(out description);
        }
    }

    public class SessionPalette : List<SessionPaletteEntry>
    {
        public SessionPalette(TByteBuffer aPayload)
        {
            int cnt;
            aPayload.Read(out cnt);
            for (int pe = 0; pe<cnt;pe++)
                Add(new SessionPaletteEntry(aPayload));
        }
    }

    public class DomainNewEvent
    {
        public string domain;
        public string eventName;
        public SessionPngImage icon;
        public Int32 rows;
        public Int32 cols;
        public SessionPalette palette;
        public SessionExtent extent;
        public DynamicImage dynamicImage;

        public DomainNewEvent(TByteBuffer aPayload)
        {
            aPayload.Read(out domain);
            aPayload.Read(out eventName);
            icon = new SessionPngImage(aPayload);
            aPayload.Read(out rows);
            aPayload.Read(out cols);
            palette = new SessionPalette(aPayload);
            extent = new SessionExtent(aPayload);
        }
    }

    // DomainChangeEvent = DomainNewEvent 

    public class DomainDeleteEvent
    {
        public string domain;

        public DomainDeleteEvent(TByteBuffer aPayload)
        {
            aPayload.Read(out domain);
        }
    }
}
