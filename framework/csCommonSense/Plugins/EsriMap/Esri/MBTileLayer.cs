// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Toolkit.DataSources;

namespace csShared.Geo.Esri
{
    /// <summary>
    ///     OpenStreetMap tiled layer. Note, use of the OpenStreetMapLayer in your map application requires attribution. Please
    ///     read
    ///     the <a href="http://www.openstreetmap.org">usage guidelines</a> for using OpenStreetMap tile layers in your
    ///     application.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         To use an OpenStreetMap tile layer in your map, add the OpenStreetMapLayer, select a style, and add
    ///         attribution.
    ///     </para>
    ///     <code language="XAML">
    /// &lt;esri:Map x:Name="MyMap"&gt;
    ///   &lt;esri:OpenStreetMapLayer Style="Mapnik" /&gt;
    /// &lt;/esri:Map&gt;<br />&lt;esri:Attribution Layers="{Binding ElementName=MyMap, Path=Layers}" /&gt;
    /// </code>
    ///     <para>
    ///         When including the OpenStreetMapLayer in your map application, you must also include attribution. For the
    ///         latest information, please read the <a href="http://www.openstreetmap.org">usage guidelines</a> for using
    ///         OpenStreetMap tile layers in your application.
    ///     </para>
    ///     <para>OpenStreetMap is released under the Create Commons "Attribution-Share Alike 2.0 Generic" license.</para>
    /// </remarks>
    public class WebTileLayer2 : TiledMapServiceLayer, IAttribution
#if WINDOWS_PHONE
		, ITileCache
#endif
    {
        private ITileImageProvider tileProvider;

        private readonly Dictionary<string, Thread> activeThreads = new Dictionary<string, Thread>();
        private const int MaxActiveThreads = 50;
        private Thread threadPoller;
        private bool keepdownloading = true;

        public ITileImageProvider TileProvider
        {
            get { return tileProvider; }
            set
            {
                // Cancel all threads in activeThreads


                List<Thread> thrds =
                    activeThreads.Values.Where(
                        thr => thr.ThreadState == ThreadState.Running || thr.ThreadState == ThreadState.Unstarted).
                        ToList();
                try
                {
                    foreach (Thread thr in thrds)
                    {
                        try
                        {
                            thr.Abort();
                        }
                        catch
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }

                //_stp.Cancel(true);
                //_stp.MaxThreads = 50;
                tileProvider = value;
                if (!Directory.Exists(TileCacheFolder)) Directory.CreateDirectory(TileCacheFolder);
                if (TileProvider != null && !Directory.Exists(LocalFolder))
                    Directory.CreateDirectory(LocalFolder);
            }
        }

        public string TileCacheFolder = Path.Combine(AppStateSettings.CacheFolder, "webtiles\\");

        /// <summary>Available subdomains for tiles.</summary>
        private static readonly string[] subDomains = {"0", "1"};

        /// <summary>Simple constant used for full extent and tile origin specific to this projection.</summary>
        private const double cornerCoordinate = 20037508.3427892;

        /// <summary>ESRI Spatial Reference ID for Web Mercator.</summary>
        private const int WKID = 102100;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OpenStreetMapLayer" /> class.
        /// </summary>
        public WebTileLayer2()
        {
            SpatialReference = new SpatialReference(WKID);
            Application.Current.Exit += StopProcessing;
        }

        private void StopProcessing(object sender, ExitEventArgs e)
        {
            keepdownloading = false;
        }

        /// <summary>
        ///     Initializes the <see cref="OpenStreetMapLayer" /> class.
        /// </summary>
        static WebTileLayer2()
        {
            CreateAttributionTemplate();
        }

        private int lastLevel = -1;
        private int lastCol;
        private int lastRow;
        private bool locationChanged;

        /// <summary>
        ///     Initializes the resource.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Override this method if your resource requires asyncronous requests to initialize,
        ///         and call the base method when initialization is completed.
        ///     </para>
        ///     <para>
        ///         Upon completion of initialization, check the <see cref="ESRI.ArcGIS.Client.Layer.InitializationFailure" />
        ///         for any possible errors.
        ///     </para>
        /// </remarks>
        /// <seealso cref="ESRI.ArcGIS.Client.Layer.Initialized" />
        /// <seealso cref="ESRI.ArcGIS.Client.Layer.InitializationFailure" />
        public override void Initialize()
        {
            //Full extent fo the layer
            FullExtent = new Envelope(-cornerCoordinate, -cornerCoordinate, cornerCoordinate, cornerCoordinate)
            {
                SpatialReference = new SpatialReference(WKID)
            };
            //This layer's spatial reference
            //Set up tile information. Each tile is 256x256px, 19 levels.
            TileInfo = new TileInfo
            {
                Height = 256,
                Width = 256,
                Origin =
                    new MapPoint(-cornerCoordinate, cornerCoordinate) {SpatialReference = new SpatialReference(WKID)},
                Lods = new Lod[20]
            };
            //Set the resolutions for each level. Each level is half the resolution of the previous one.
            double resolution = cornerCoordinate*2/256;
            for (int i = 0; i < TileInfo.Lods.Length; i++)
            {
                TileInfo.Lods[i] = new Lod {Resolution = resolution};
                resolution /= 2;
            }
            //Call base initialize to raise the initialization event
            base.Initialize();

            if (threadPoller != null) return;
            threadPoller = new Thread(delegate()
            {
                while (keepdownloading)
                {
                    try
                    {
                        if (activeThreads == null || activeThreads.Count <= 0)
                        {
                            Thread.Sleep(20);
                            continue;
                        }
                        int cnt = activeThreads.Values.Count(t => t != null && t.ThreadState == ThreadState.Running);
                        if (cnt < MaxActiveThreads)
                        {
                            List<KeyValuePair<string, Thread>> threadsToStart =
                                activeThreads.Where(a => a.Value.ThreadState == ThreadState.Unstarted)
                                    .ToList()
                                    .Take(MaxActiveThreads - cnt)
                                    .ToList();
                            foreach (var tts in threadsToStart)
                            {
                                tts.Value.Start();
                            }
                        }

                        // Clean up stopped threads
                        List<KeyValuePair<string, Thread>> stoppedThreads =
                            activeThreads.Where(a => a.Value.ThreadState != ThreadState.Unstarted
                                                      && a.Value.ThreadState != ThreadState.Running)
                                .ToList();
                        foreach (var stoppedThread in stoppedThreads)
                            activeThreads.Remove(stoppedThread.Key);

                        // Add more threads that download other surrounding tiles when idle
                        if (activeThreads.Count != -1 || lastLevel == -1 || !locationChanged) continue;
                        // Add surrounding tiles to download queue
                        int level = lastLevel;
                        int row = lastRow;
                        int col = lastCol;
                        locationChanged = false;
                        //int self = int.Parse(keyParts[4]);

                        for (int nrow = row - 10; nrow <= row + 10; nrow++)
                        {
                            for (int ncol = col - 10; ncol <= col + 10; ncol++)
                            {
                                if (nrow <= 0 || ncol <= 0 || nrow == row || ncol == col) continue;
                                string localUrl = LocalFolder + @"\" + level + "-" + nrow + "-" + ncol + ".png";
                                string dictKey = TileProvider.CacheFolder + "|" +
                                                 level.ToString(CultureInfo.InvariantCulture) + "|" +
                                                 nrow.ToString(CultureInfo.InvariantCulture) + "|" +
                                                 ncol.ToString(CultureInfo.InvariantCulture);
                                if (!File.Exists(localUrl) && !activeThreads.Keys.Contains(dictKey))
                                {
                                    GetTileSourceInternal(level, nrow, ncol, null, true);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }) {Priority = ThreadPriority.BelowNormal};
            threadPoller.Start();
        }

        public byte[] ReadFully(Stream stream)
        {
            //thanks to: http://www.yoda.arachsys.com/csharp/readbinary.html
            var buffer = new byte[32768];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        return ms.ToArray();
                    }
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public string LocalFolder
        {
            get { return TileCacheFolder + TileProvider.CacheFolder; }
        }

        private void GetTileSourceInternal(int level, int row, int col, Action<ImageSource> onComplete,
            bool internalCall)
        {
            try
            {
                if (TileProvider == null) return;
                var image = new BitmapImage();

                string localUrl = LocalFolder + @"\" + level + "-" + row + "-" + col + ".png";
                bool local = false;
                if (!internalCall)
                {
                    if (lastLevel != level || lastCol != col || lastRow != row)
                        locationChanged = true;
                    lastCol = col;
                    lastLevel = level;
                    lastRow = row;
                }
                if (File.Exists(localUrl))
                {
                    if (internalCall)
                    {
                        return;
                    }
                    local = true;
                    if (TileProvider is WebTileProvider)
                    {
                        var f = new FileInfo(localUrl);
                        if ((f.CreationTime + ((WebTileProvider) TileProvider).CacheTimeout) < DateTime.Now)
                            local = false;
                    }
                }
                if (local)
                {
                    using (var fileStream = new FileStream(localUrl, FileMode.Open, FileAccess.Read))
                    {
                        byte[] img;
                        img = ReadFully(fileStream);
                        BindImage(image, img, onComplete);
                    }
                }
                else
                {
                    string str = GetTileUrl(level, row, col);
                    byte[] img;
                    string dictKey = TileProvider.CacheFolder + "|" + level.ToString(CultureInfo.InvariantCulture) + "|" +
                                     row.ToString(CultureInfo.InvariantCulture) + "|" +
                                     col.ToString(CultureInfo.InvariantCulture);
                    if (internalCall)
                        dictKey += "|" + 1;
                    else
                        dictKey += "|" + 0;
                    var thrd = new Thread(delegate()
                    {
                        if (!string.IsNullOrEmpty(str))
                        {
                            Guid g = AppStateSettings.Instance.AddDownload(str, "start");
                            try
                            {
                                string UserAgent = TileProvider.UserAgent;
                                string Referer = TileProvider.Refer;
                                var webRequest = (HttpWebRequest) WebRequest.Create(str);

                                webRequest.PreAuthenticate = true;
                                webRequest.Timeout = 5000;
                                webRequest.KeepAlive = false;

                                if (!String.IsNullOrEmpty(UserAgent))
                                    webRequest.UserAgent = UserAgent;
                                if (!String.IsNullOrEmpty(Referer)) webRequest.Referer = Referer;


                                WebResponse webResponse = webRequest.GetResponse();
                                if (webResponse.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (Stream responseStream = webResponse.GetResponseStream())
                                    {
                                        img = ReadFully(responseStream);
                                        if (!internalCall)
                                            BindImage(image, img, onComplete);

                                        if (!File.Exists(localUrl))
                                        {
                                            using (FileStream fileStream = File.Open(localUrl, FileMode.CreateNew))
                                            {
                                                fileStream.Write(img, 0, img.Length);
                                                fileStream.Flush();
                                                fileStream.Close();
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Logger.Log("ESRIMAP", "Error loading tile", TileProvider.Title, Logger.Level.Error);
                            }
                            finally
                            {
                                AppStateSettings.Instance.FinishDownload(g);
                            }
                        }
                    });
                    try
                    {
                        if (!activeThreads.Keys.Contains(dictKey))
                            activeThreads.Add(dictKey, thrd);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception es)
            {
                Logger.Log("Map", "Error getting tile", es.Message, Logger.Level.Error);
            }
        }

        protected override void GetTileSource(int level, int row, int col, Action<ImageSource> onComplete)
        {
            GetTileSourceInternal(level, row, col, onComplete, false);
        }

        /*protected override void GetTileSource(int level, int row, int col, Action<ImageSource> onComplete)
    {
      try
      {
        if (TileProvider == null) return;
        var image = new BitmapImage();
        
        string localUrl = LocalFolder + @"\" + level + "-" + row + "-" + col + ".png";
        bool local = false;
        if (File.Exists(localUrl))
        {
          local = true;
          if (TileProvider is WebTileProvider)
          {
            FileInfo f = new FileInfo(localUrl);
            if ((f.CreationTime + ((WebTileProvider) TileProvider).CacheTimeout) < DateTime.Now)
              local = false;
          }
        }
        if (local)
        {
          using (var fileStream = new FileStream(localUrl, FileMode.Open, FileAccess.Read))
          {
            byte[] img;
            img = ReadFully(fileStream);
            BindImage(image, img, onComplete);
          }
        }
        else
        {
          string str = GetTileUrl(level, row, col);
          byte[] img;
          _stp.QueueWorkItem(delegate
          {
            if (!string.IsNullOrEmpty(str))
            {
              Guid g = AppStateSettings.Instance.AddDownload(str, "start");
              try
              {


                string UserAgent = TileProvider.UserAgent;
                string Referer = TileProvider.Refer;
                var webRequest = (HttpWebRequest)WebRequest.Create(str);

                webRequest.PreAuthenticate = true;
                webRequest.Timeout = 100000;
                webRequest.KeepAlive = true;
                if (!String.IsNullOrEmpty(UserAgent))
                  webRequest.UserAgent = UserAgent;
                if (!String.IsNullOrEmpty(Referer)) webRequest.Referer = Referer;



                WebResponse webResponse = webRequest.GetResponse();
                if (webResponse.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                  using (StartStream responseStream = webResponse.GetResponseStream())
                  {
                    img = ReadFully(responseStream);
                    BindImage(image, img, onComplete);

                    if (!File.Exists(localUrl))
                    {

                      using (FileStream fileStream = File.Open(localUrl, FileMode.CreateNew))
                      {
                        fileStream.Write(img, 0, img.Length);
                        fileStream.Flush();
                        fileStream.Close();
                      }
                    }
                  }
                }

              }
              catch (Exception)
              {
                Logger.Log("ESRIMAP", "Error loading tile", TileProvider.Title, Logger.Level.Error);
              }
              finally
              {
                AppStateSettings.Instance.FinishDownload(g);  
              }
              
            }
            else
            {
              onComplete(null);
            }
          });
        } 
      }
      catch (Exception es)
      {        
        Logger.Log("Map","Error getting tile",es.Message,Logger.Level.Error);
      }
           
    }*/

        private void BindImage(BitmapImage image, byte[] img, Action<ImageSource> onComplete)
        {
            Dispatcher.Invoke(delegate
            {
                try
                {
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(img);
                    image.EndInit();
                    onComplete(image);
                }
                catch (Exception)
                {
                }
            });
        }

        /// <summary>
        ///     Returns a url to the specified tile
        /// </summary>
        /// <param name="level">Layer level</param>
        /// <param name="row">Tile row</param>
        /// <param name="col">Tile column</param>
        /// <returns>URL to the tile image</returns>
        public override string GetTileUrl(int level, int row, int col)
        {
            // Select a subdomain based on level/row/column so that it will always
            // be the same for a specific tile. Multiple subdomains allows the user
            // to load more tiles simultanously. To take advantage of the browser cache
            // the following expression also makes sure that a specific tile will always 
            // hit the same subdomain.
            string subdomain = subDomains[(level + col + row)%subDomains.Length];
            string r = string.Format(TileProvider.WebUrl(row, col, level)); //, subdomain, level, col, row);
            return r;
        }


        private static void OnStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (WebTileLayer) d;
            if (obj.IsInitialized)
                obj.Refresh();
        }

        #region IAttribution Members

        private const string template =
            @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
			<TextBlock Text=""Map data © OpenStreetMap contributors, CC-BY-SA"" TextWrapping=""Wrap""/></DataTemplate>";

        private static DataTemplate _attributionTemplate;

        private static void CreateAttributionTemplate()
        {
#if SILVERLIGHT
			_attributionTemplate = System.Windows.Markup.XamlReader.Load(template) as DataTemplate;
#else
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(template)))
            {
                _attributionTemplate = XamlReader.Load(stream) as DataTemplate;
            }
#endif
        }

        /// <summary>
        ///     Gets the attribution template of the layer.
        /// </summary>
        /// <value>The attribution template.</value>
        public DataTemplate AttributionTemplate
        {
            get { return _attributionTemplate; }
        }

        #endregion
    }
}