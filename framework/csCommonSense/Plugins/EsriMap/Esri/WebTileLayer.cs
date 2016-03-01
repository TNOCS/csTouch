// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Concurrent;
using Caliburn.Micro;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
using TileInfo = ESRI.ArcGIS.Client.TileInfo;

namespace csShared.Geo.Esri
{
    public class MBTileCache
    {
        private SQLiteConnection connection;
        private readonly Dictionary<string, byte[]> memoryCache = new Dictionary<string, byte[]>();

        private readonly string tileCacheFolder = Path.Combine(AppStateSettings.CacheFolder, "webtiles\\");

        public void InitFile(string filename)
        {
            if (!File.Exists(filename)) return;
            var connectionString = string.Format("Data Source={0}; FailIfMissing=False", filename);

            OpenSqlConnection(connectionString);
        }

        private void OpenSqlConnection(string connectionString)
        {
            connection = new SQLiteConnection(connectionString);
            connection.Open();
        }

        public void Init(string filename)
        {
            var fullPath = Path.Combine(tileCacheFolder, filename); // REVIEW TODO: Used Path instead of String concat.
            var connectionString = string.Format("Data Source={0}; FailIfMissing=False", fullPath);

            if (!File.Exists(fullPath))
            {
                if (!Directory.Exists(tileCacheFolder)) Directory.CreateDirectory(tileCacheFolder);
                CreateFile(connectionString, null);
            }

            OpenSqlConnection(connectionString);
        }

        private static void CreateFile(string connectionString, IDictionary<string, string> metadata)
        {
            var csb = new SQLiteConnectionStringBuilder(connectionString);
            if (File.Exists(csb.DataSource))
                File.Delete(csb.DataSource);

            using (var cn = new SQLiteConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "CREATE TABLE metadata (name text, value text);"
                        + "CREATE TABLE tiles (zoom_level integer, tile_column integer, tile_row integer, tile_data blob);"
                        + "CREATE UNIQUE INDEX idx_tiles ON tiles (zoom_level, tile_column, tile_row);";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "INSERT INTO metadata VALUES (?, ?);";
                    var pName = new SQLiteParameter("PName", DbType.String); cmd.Parameters.Add(pName);
                    var pValue = new SQLiteParameter("PValue", DbType.String); cmd.Parameters.Add(pValue);

                    if (metadata == null || metadata.Count == 0)
                    {
                        metadata = new Dictionary<string, string>();
                    }
                    if (!metadata.ContainsKey("bounds"))
                        metadata.Add("bounds", "-180,-85,180,85");

                    //if (!metadata.ContainsKey("type")) metadata.Add("type", "baselayer");
                    //if (!metadata.ContainsKey("format")) metadata.Add("type","png");
                    //if (!metadata.ContainsKey("minzoom")) metadata.Add("minzoom", "1");
                    //if (!metadata.ContainsKey("maxzoom")) metadata.Add("maxzoom", "20");

                    foreach (var kvp in metadata)
                    {
                        pName.Value = kvp.Key;
                        pValue.Value = kvp.Value;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SaveImage(int level, int row, int col, byte[] image)
        {
            try
            {
                //DbCommand cmd = _connection.CreateCommand();
                //cmd.CommandText = String.Format("INSERT INTO {0} VALUES(@Level, @Col, @Row, @Image);","tiles");
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = string.Format(
                        CultureInfo.InvariantCulture,
                        @"SELECT [tile_data] FROM [tiles] WHERE zoom_level = {0} AND tile_column = {1} AND tile_row = {2};",
                        level, col, row);
                    var tileObj = command.ExecuteScalar();
                    if (tileObj != null)
                    {
                        return; // Tile already cached ...
                    }
                }
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO tiles VALUES(@Level, @Col, @Row, @Image);";
                    var par = cmd.CreateParameter();
                    par.DbType = DbType.Int32;
                    par.ParameterName = "Level";
                    par.Value = level;
                    cmd.Parameters.Add(par);

                    var gRow = (int)((Math.Pow(2, level) - 1) - row);

                    par = cmd.CreateParameter();
                    par.DbType = DbType.Int32;
                    par.ParameterName = "Row";
                    par.Value = gRow;
                    cmd.Parameters.Add(par);

                    par = cmd.CreateParameter();
                    par.DbType = DbType.Int32;
                    par.ParameterName = "Col";
                    par.Value = col;
                    cmd.Parameters.Add(par);

                    par = cmd.CreateParameter();
                    par.DbType = DbType.Binary;
                    par.ParameterName = "Image";
                    par.Value = image;
                    cmd.Parameters.Add(par);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SQLiteException e)
            {
                Logger.Log("MBTileCache", "Error adding tile", e.Message, Logger.Level.Error);
            }
            catch (Exception e)
            {
                Logger.Log("MBTileCache", "Error adding tile", e.Message, Logger.Level.Error);
            }
        }


        public byte[] GetImage(int level, int row, int col)
        {
            var gRow = (int)((Math.Pow(2, level) - 1) - row);
            var key  = string.Format("{0}-{1}-{2}", gRow, col, level);
            //var pos = gRow + "-" + col + "-" + level;
            if (memoryCache.ContainsKey(key)) return memoryCache[key];

            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = string.Format(CultureInfo.InvariantCulture, @"SELECT [tile_data] FROM [tiles] WHERE zoom_level = {0} AND tile_column = {1} AND tile_row = {2};", level, col, gRow);
                    //"SELECT [tile_data] FROM [tiles] WHERE zoom_level = " + level + " AND tile_column = " + col +
                    //" AND tile_row = " + gRow;

                //"SELECT [tile_data] FROM [tiles] WHERE zoom_level = @zoom AND tile_column = @col AND tile_row = @row";
                //command.Parameters.Add(new SQLiteParameter("zoom", level));
                //command.Parameters.Add(new SQLiteParameter("col", col));
                //command.Parameters.Add(new SQLiteParameter("row", gRow));
                //Console.WriteLine(level + " , " + col + " , " + gRow);
                var tileObj = command.ExecuteScalar();
                if (tileObj == null)
                {
                    return null;
                }
                memoryCache[key] = (byte[])tileObj;
                if (memoryCache.Count > 3000) memoryCache.Remove(memoryCache.Keys.First());
                return (byte[])tileObj;
                //BitmapImage image = new BitmapImage();
                //image.BeginInit();
                //image.CacheOption = BitmapCacheOption.OnLoad;
                //image.UriSource = null;
                //image.StreamSource = new MemoryStream((byte[])tileObj);
                //image.EndInit();
                //return image;
            }
        }

        internal void Close()
        {
            if (connection != null) connection.Close();
        }
    }

    public class WebTileLayer : TiledMapServiceLayer, IAttribution, IMenuLayer
#if WINDOWS_PHONE
		, ITileCache
#endif
    {
        private ITileImageProvider tileProvider;

        private readonly ConcurrentDictionary<string, Thread> activeThreads = new ConcurrentDictionary<string, Thread>();
        private const int MaxActiveThreads = 50;
        private Thread threadPoller;
        private bool keepDownloading = true;

        public ITileImageProvider TileProvider
        {
            get { return tileProvider; }
            set
            {
                // Cancel all threads in activeThreads
                if (cache != null) cache.Close();
                
                var thrds = activeThreads.Values.Where(
                    thr => thr.ThreadState == ThreadState.Running || thr.ThreadState == ThreadState.Unstarted).ToList();
                try
                {
                    foreach (var thr in thrds)
                    {
                        try
                        {
                            thr.Abort();
                        }
                        catch (Exception e)
                        {
                            Logger.Log("ESRIMAP", "Error aborting thread", e.Message, Logger.Level.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("ESRIMAP", "Error aborting threads", e.Message, Logger.Level.Error);
                }

                //tileProvider = value;
                InitializeTileProvider(value);
                //if (cache != null) cache.Close();
                //if (tileProvider == null) return;
                //cache = new MBTileCache();
                //if (!string.IsNullOrEmpty(TileProvider.MBTileFile))
                //{
                //    cache.InitFile(TileProvider.MBTileFile);
                //}
                //else
                //{
                //    cache.Init(TileProvider.CacheFolder + ".mbtiles");
                //}
                //cache = new MBTileCache();
                //cache.Init(tileProvider.CacheFolder + ".mbtiles");

                //if (!Directory.Exists(TileCacheFolder)) Directory.CreateDirectory(TileCacheFolder);
                //if (TileProvider != null && !Directory.Exists(LocalFolder))
                //    Directory.CreateDirectory(LocalFolder);
            }
        }

        /// <summary>Available subdomains for tiles.</summary>
        //private static readonly string[] SubDomains = { "0", "1" };

        /// <summary>Simple constant used for full extent and tile origin specific to this projection.</summary>
        private const double CornerCoordinate = 20037508.3427892;

        /// <summary>ESRI Spatial Reference ID for Web Mercator.</summary>
        private const int WKID = 102100;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OpenStreetMapLayer" /> class.
        /// </summary>
        public WebTileLayer()
        {
            SpatialReference = new SpatialReference(WKID);
            Application.Current.Exit += StopProcessingWebRequests;
        }

        private void StopProcessingWebRequests(object sender, ExitEventArgs e)
        {
            if (cache != null) cache.Close();
            keepDownloading = false;
            threadPoller = null;
        }

        private void PauseProcessingWebRequests(object sender, ExitEventArgs e)
        {
            keepDownloading = false;
            threadPoller = null;
        }

        /// <summary>
        ///     Initializes the <see cref="OpenStreetMapLayer" /> class.
        /// </summary>
        static WebTileLayer()
        {
            CreateAttributionTemplate();
        }

        private int lastLevel = -1;
        private int lastCol;
        private int lastRow;
        private bool locationChanged;
        private MBTileCache cache;

        public override void Initialize()
        {
            //Full extent fo the layer
            FullExtent = new Envelope(-CornerCoordinate, -CornerCoordinate, CornerCoordinate, CornerCoordinate)
            {
                SpatialReference = new SpatialReference(WKID)
            };

            //This layer's spatial reference
            //Set up tile information. Each tile is 256x256px, 19 levels.
            TileInfo = new TileInfo
            {
                Height = 256,
                Width  = 256,
                Origin = new MapPoint(-CornerCoordinate, CornerCoordinate) { SpatialReference = new SpatialReference(WKID) },
                Lods   = new Lod[20]
            };
            //Set the resolutions for each level. Each level is half the resolution of the previous one.
            var resolution = CornerCoordinate * 2 / 256;
            for (var i = 0; i < TileInfo.Lods.Length; i++)
            {
                TileInfo.Lods[i] = new Lod { Resolution = resolution };
                resolution /= 2;
            }
            //Call base initialize to raise the initialization event
            base.Initialize();

            StartProcessingWebRequest();
        }

        private void StartProcessingWebRequest()
        {
            keepDownloading = true;
            if (threadPoller != null) return;
            threadPoller = new Thread(delegate()
            {
                while (keepDownloading)
                {
                    try
                    {
                        if (activeThreads == null || activeThreads.Count <= 0)
                        {
                            PauseProcessingWebRequests(null, null);
                            return;
                            // FIXME TODO: Unreachable code
                            //Thread.Sleep(20);
                            //continue;
                        }
                        var cnt = activeThreads.Values.Count(t => t != null && t.ThreadState == ThreadState.Running);
                        if (cnt < MaxActiveThreads)
                        {
                            var threadsToStart = activeThreads.Where(a => a.Value != null && a.Value.ThreadState == ThreadState.Unstarted)
                                .ToList()
                                .Take(MaxActiveThreads - cnt)
                                .ToList();
                            foreach (var tts in threadsToStart)
                            {
                                tts.Value.Start();
                            }
                        }

                        // Clean up stopped threads
                        var stoppedThreads = activeThreads.Where(a => a.Value != null &&  a.Value.ThreadState != ThreadState.Unstarted
                                                                      && a.Value.ThreadState != ThreadState.Running)
                            .ToList();
                        Thread found; 
                        foreach (var stoppedThread in stoppedThreads)
                            activeThreads.TryRemove(stoppedThread.Key, out found);

                        // Add more threads that download other surrounding tiles when idle
                        if (activeThreads.Count != -1 || lastLevel == -1 || !locationChanged) continue;
                        // Add surrounding tiles to download queue
                        var level = lastLevel;
                        var row = lastRow;
                        var col = lastCol;
                        locationChanged = false;
                        //int self = int.Parse(keyParts[4]);

                        for (var nrow = row - 10; nrow <= row + 10; nrow++)
                        {
                            for (var ncol = col - 10; ncol <= col + 10; ncol++)
                            {
                                if (nrow <= 0 || ncol <= 0 || nrow == row || ncol == col) continue;
                                //string localUrl = LocalFolder + @"\" + level + "-" + nrow + "-" + ncol + ".png";
                                var dictKey = TileProvider.CacheFolder + "|" +
                                              level.ToString(CultureInfo.InvariantCulture) + "|" +
                                              nrow.ToString(CultureInfo.InvariantCulture) + "|" +
                                              ncol.ToString(CultureInfo.InvariantCulture);
                                cache.GetImage(level, row, col);
                                //var img = cache.GetImage(level, row, col);
                                if (!activeThreads.Keys.Contains(dictKey))
                                {
                                    GetTileSourceInternal(level, nrow, ncol, null, true);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e.Message);
                    }
                }
            }) {Priority = ThreadPriority.BelowNormal};
            threadPoller.Start();
        }

        private static byte[] ReadFully(Stream stream)
        {
            //thanks to: http://www.yoda.arachsys.com/csharp/readbinary.html
            var buffer = new byte[32768];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        return ms.ToArray();
                    }
                    ms.Write(buffer, 0, read);
                }
            }
        }

        //private readonly string tileCacheFolder = Path.Combine(AppStateSettings.CacheFolder, "webtiles\\");

        //public string LocalFolder
        //{
        //    get { return tileCacheFolder + TileProvider.CacheFolder; }
        //}

        private void GetTileSourceInternal(int level, int row, int col, Action<ImageSource> onComplete, bool internalCall)
        {
            try
            {
                //if (!InitializeTileProvider()) return;

                //string localUrl = LocalFolder + @"\" + level + "-" + row + "-" + col + ".png";
                //bool local = false;
                if (!internalCall)
                {
                    if (lastLevel != level || lastCol != col || lastRow != row)
                        locationChanged = true;

                    lastCol   = col;
                    lastLevel = level;
                    lastRow   = row;
                }
                byte[] mtimage = null;
                if (cache!=null) mtimage = cache.GetImage(level, row, col);
                //if (File.Exists(localUrl))
                //{
                //    if (internalCall)
                //    {
                //        return;
                //    }
                //    local = true;
                //    if (TileProvider is WebTileProvider)
                //    {
                //        var f = new FileInfo(localUrl);
                //        if ((f.CreationTime + ((WebTileProvider) TileProvider).CacheTimeout) < DateTime.Now)
                //            local = false;
                //    }
                //}
                //var image = new BitmapImage();
                if (mtimage != null)
                {
                    BindImage(mtimage, onComplete);
                    //image = mtimage;
                    //using (var fileStream = new FileStream(localUrl, FileMode.Open, FileAccess.Read))
                    //{
                    //    byte[] img;
                    //    img = ReadFully(fileStream);
                    //    BindImage(image, img, onComplete);
                    //}
                }
                else if (string.IsNullOrEmpty(TileProvider.MBTileFile))
                {
                    var str = GetTileUrl(level, row, col);
                    byte[] img;
                    var dictKey = DictionaryKey(level, row, col, internalCall);
                    if (activeThreads.Keys.Contains(dictKey)) return;
                    //var dictKey = TileProvider.CacheFolder + "|" + level.ToString(CultureInfo.InvariantCulture) + "|" +
                    //                 row.ToString(CultureInfo.InvariantCulture) + "|" +
                    //                 col.ToString(CultureInfo.InvariantCulture);
                    //if (internalCall)
                    //    dictKey += "|" + 1;
                    //else
                    //    dictKey += "|" + 0;
                    var thrd = new Thread(delegate()
                    {
                        if (string.IsNullOrEmpty(str)) return;
                        //Guid g = AppStateSettings.Instance.AddDownload(str, "start");
                        try
                        {
                            var webRequest = (HttpWebRequest)WebRequest.Create(str);

                            webRequest.PreAuthenticate = true;
                            webRequest.Timeout         = 5000;
                            webRequest.KeepAlive       = false;

                            if (!string.IsNullOrEmpty(TileProvider.UserAgent)) webRequest.UserAgent = TileProvider.UserAgent;
                            if (!string.IsNullOrEmpty(TileProvider.Refer)) webRequest.Referer = TileProvider.Refer;

                            var webResponse = webRequest.GetResponse();
                            if (!webResponse.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                                return;
                            using (var responseStream = webResponse.GetResponseStream())
                            {
                                img = ReadFully(responseStream);
                                if (!internalCall)
                                    BindImage(img, onComplete);
                                // TODO EV Check whether we already have it, BEFORE we initiate the webrequest!
                                cache.SaveImage(level, row, col, img);

                                //if (!File.Exists(localUrl))
                                //{
                                //    using (FileStream fileStream = File.Open(localUrl, FileMode.CreateNew))
                                //    {
                                //        fileStream.Write(img, 0, img.Length);
                                //        fileStream.Flush();
                                //        fileStream.Close();
                                //    }
                                //}
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log("ESRIMAP", "Error loading tile", e.Message, Logger.Level.Error);
                        }
                    });
                    try
                    {
                        if (activeThreads.Keys.Contains(dictKey)) return;
                        activeThreads.TryAdd(dictKey, thrd);
                        StartProcessingWebRequest();
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Map", "Error getting tile", e.Message, Logger.Level.Error);
                    }
                }
            }
            catch (Exception es)
            {
                Logger.Log("Map", "Error getting tile", es.Message, Logger.Level.Error);
            }
        }

        private void InitializeTileProvider(ITileImageProvider newTileProvider)
        {
            tileProvider = newTileProvider;
            if (cache != null) cache.Close();
            if (tileProvider == null) return;
            cache = new MBTileCache();
            if (!string.IsNullOrEmpty(TileProvider.MBTileFile))
            {
                cache.InitFile(TileProvider.MBTileFile);
            }
            else
            {
                cache.Init(TileProvider.CacheFolder + ".mbtiles");
            }
        }

        private string DictionaryKey(int level, int row, int col, bool internalCall)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}",
                TileProvider.CacheFolder, level, row, col, internalCall ? 1 : 0);
        }

        //private static SQLiteConnection MakeConnection(String datasource)
        //{
        //    var cn = new SQLiteConnection(string.Format("Data Source={0}", datasource));
        //    cn.Open();
        //    SQLiteCommand cmd = cn.CreateCommand();
        //    cmd.CommandText =
        //        "CREATE TABLE IF NOT EXISTS cache (level integer, row integer, col integer, size integer, image blob, primary key (level, row, col) on conflict replace);";
        //    cmd.ExecuteNonQuery();
        //    cn.Close();
        //    return cn;
        //}

        protected override void GetTileSource(int level, int row, int col, Action<ImageSource> onComplete)
        {
            GetTileSourceInternal(level, row, col, onComplete, false);
            //var connection = MakeConnection("test.db");
            //_cache = new MbTilesCache(connection);
        }

        private static void BindImage(byte[] img, Action<ImageSource> onComplete) {
            //Dispatcher.Invoke(delegate
            Execute.OnUIThread(() =>            
            {
                var image = new BitmapImage();
                try
                {
                    using (var mem = new MemoryStream(img))
                    {
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption   = BitmapCacheOption.OnLoad;
                        image.StreamSource  = mem;
                        image.EndInit();
                    }
                    //image.Freeze();
                    onComplete(image);
                }
                catch (Exception e)
                {
                    Logger.Log("Map", "Error binding image", e.Message, Logger.Level.Error);
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
            //var subdomain = SubDomains[(level + col + row) % SubDomains.Length];
            var r = string.Format(TileProvider.WebUrl(row, col, level)); //, subdomain, level, col, row);
            return r;
        }


        //private static void OnStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var obj = (WebTileLayer)d;
        //    if (obj.IsInitialized)
        //        obj.Refresh();
        //}

        #region IAttribution Members

        private const string Template =
            @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
			<TextBlock Text=""Map data © OpenStreetMap contributors, CC-BY-SA"" TextWrapping=""Wrap""/></DataTemplate>";

        private static DataTemplate attributionTemplate;

        private static void CreateAttributionTemplate()
        {
#if SILVERLIGHT
			attributionTemplate = System.Windows.Markup.XamlReader.Load(template) as DataTemplate;
#else
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Template)))
            {
                attributionTemplate = XamlReader.Load(stream) as DataTemplate;
            }
#endif
        }

        /// <summary>
        ///     Gets the attribution template of the layer.
        /// </summary>
        /// <value>The attribution template.</value>
        public DataTemplate AttributionTemplate
        {
            get { return attributionTemplate; }
        }

        #endregion

        internal void CloseCache()
        {
            cache.Close();
            cache = null;
        }

        public List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            var r = new List<System.Windows.Controls.MenuItem>();
            var deleteBaseLayer = MenuHelpers.CreateMenuItem("Remove base layer", MenuHelpers.DeleteIcon);
            deleteBaseLayer.Click += (e, f) =>
            {
                TileProvider.Activated = false;
                AppStateSettings.Instance.ViewDef.BaseLayers.ChildLayers.Remove(this);
            };
            return r;
        }
    }
}