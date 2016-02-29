using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csGeoLayers;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using csShared.Utils;
using DataServer;

namespace csCommon.Types.DataServer.PoI
{
    // TODO What is the distinction between the regular PoiService and this class? In other words, why are the methods below not in PoiService?
    public class ExtendedPoiService : PoiService
    {
        // SdJ Added so OpenFile can send an event whenever the file is opened.
        public event EventHandler FileOpenedEvent;

        protected static readonly AppStateSettings AppState = AppStateSettings.Instance;
        protected Color ShapeAccentColor = Colors.Red;

        protected static ExtendedPoiService CreateService(string name, Guid id, string folder = "", string relativeFolder = "")
        {
            var res = new ExtendedPoiService
            {
                IsLocal        = true,
                Name           = name,
                Id             = id,
                IsFileBased    = false,
                StaticService  = true,
                AutoStart      = false,
                HasSensorData  = true,
                Mode           = Mode.client,
                RelativeFolder = relativeFolder,
            };

            res.Init(Mode.client, AppState.DataServer);
            res.Folder = folder;
            res.InitPoiService();

            res.Settings.OpenTab = false;
            res.Settings.Icon = "layer.png";
            AppState.DataServer.Services.Add(res);
            return res;
        }

        private readonly List<String> availableColors = new List<string>
        {
            "Transparent", "Red", "Orange", "Yellow", "Black", "White", "Green", "Blue", "Purple", "Pink"
        };

        public string File { get; set; }

        public string DsdFile { get { return Path.ChangeExtension(File, "dsd"); } }

        private void SetShapeColor(string color)
        {
            var colorFromString = ColorConverter.ConvertFromString(color);
            if (colorFromString != null) ShapeAccentColor = (Color)colorFromString;
            foreach (var poi in PoITypes)
            {
                switch (poi.Style.DrawingMode)
                {
                    default:
                        poi.Style.FillColor = ShapeAccentColor;
                        break;
                    case DrawingModes.Polyline:
                        poi.Style.StrokeColor = ShapeAccentColor;
                        break;
                    case DrawingModes.Polygon:
                        poi.Style.FillColor = poi.Style.StrokeColor = ShapeAccentColor;
                        break;
                    case DrawingModes.MultiPolygon:
                        poi.Style.FillColor = poi.Style.StrokeColor = ShapeAccentColor;
                        break;
                }
                poi.UpdateEffectiveStyle();
            }
            SaveXml(DsdFile, true);
            //SaveXml(Path.ChangeExtension(File, "ds"));
            RestartService();
        }

               /// <summary>
        /// Stops and then starts the service again.
        /// </summary>
        private void RestartService()
        {
            var ls = Layer as IStartStopLayer;
            if (ls == null || !ls.IsStarted) return;
            ls.Stop();
            ls.Start();
        }

        public override void Subscribe(Mode serviceMode)
        {
            base.Subscribe(serviceMode);
            OpenFile();
        }

        public override List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            var r = base.GetMenuItems();
            if (!Layer.IsStarted) return r;
            var setcolor = MenuHelpers.CreateMenuItem("Set Color", MenuHelpers.ColorIcon);
            setcolor.Click += (e, f) => SelectColor();
            r.Add(setcolor);
            return r;
        }

        private void SelectColor()
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement   = Application.Current.MainWindow,
                RelativePosition  = new Point(200, 300),
                TimeOut           = new TimeSpan(0, 0, 0, 10),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty   = string.Empty
            };

            menu.AddMenuItems(availableColors.ToArray());

            menu.Selected += (e, f) => SetShapeColor(f.Object.ToString());

            AppState.Popups.Add(menu);
        }

        public void OpenFile(bool saveDsd = true)
        {
            Layer.IsLoading = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                Exception openFileException = OpenFileSync(saveDsd);
                FileOpenedEventArgs args = new FileOpenedEventArgs { OpenerService = this, Exception = openFileException };
                OnFileOpenedEvent(args); // SdJ added
            });
        }

        /// <summary>
        /// Open the file synchronously.
        /// </summary>
        /// <param name="saveDsd">Whether to save a data description file (dsd) too.</param>
        /// <returns>The exception that happened if opening the file did NOT work; null otherwise.</returns>
        public Exception OpenFileSync(bool saveDsd = true)
        {
            try
            {
                Reset();

                PoIs.StartBatch();
                var isLoaded = LoadOrCreateDataServiceDescription(saveDsd);
                IsLoading = true;

                Exception processFile = ProcessFile();
                if (processFile != null)
                {
                    Logger.Log("Exception", "ExtendedPoiService", processFile.Message, Logger.Level.Error);
                    return processFile;
                }

                PoIs.FinishBatch();
                IsLoading = false;

                ContentLoaded = true;

                Execute.OnUIThread(() => Layer.IsLoading = false);

                if (isLoaded || !saveDsd) return null;

                // Save DSD for editing styles and filters. We do it here after adding the metainfo.
                SaveXml(DsdFile, true);
                return null; // Nothing went wrong.
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public class FileOpenedEventArgs : EventArgs
        {
            public ExtendedPoiService OpenerService { get; set; }
            public Exception Exception { get; set; }
        }

        protected virtual void OnFileOpenedEvent(FileOpenedEventArgs e)
        {
            if (FileOpenedEvent != null)
            {
                FileOpenedEvent(this, e);
            }
        }

        /// <summary>
        /// Process the file. Returns an exception iff this did not work, and null otherwise. Perhaps a little ugly.
        /// </summary>
        /// <returns>An exception if this did not work.</returns>
        protected virtual Exception ProcessFile() { return null; }

        /// <summary>
        /// Initialize the ShapeService by either creating a new DSD, or by loading an existing one.
        /// </summary>
        /// <returns>True if the DSD was loaded, or False if it was created.</returns>
        private bool LoadOrCreateDataServiceDescription(bool create)
        {
            if (LoadDsd()) return true;
            if (! create) return false;
            CreateDsd(); 
            return false;
        }

        /// <summary>
        /// Create default PoI types in case we are opening a shape file, for example.
        /// </summary>
        private void CreateDsd()
        {
            AllContent.ForEach(c => c.Clear());
            AddPoiType("Point",    DrawingModes.Point,        ShapeAccentColor);
            AddPoiType("PolyLine", DrawingModes.Polyline,     stroke: ShapeAccentColor, strokeWidth: 2);
            AddPoiType("Polygon",  DrawingModes.Polygon,      stroke: ShapeAccentColor, strokeWidth: 2, fill: ShapeAccentColor);
            AddPoiType("WKT",      DrawingModes.MultiPolygon, stroke: ShapeAccentColor, strokeWidth: 2, fill: ShapeAccentColor);

            SaveXml(DsdFile, true);
            RestartService();
        }

        private bool LoadDsd()
        {
            if (!System.IO.File.Exists(DsdFile)) return false;
            AllContent.ForEach(c => c.Clear());
            try
            {
                FromXml(System.IO.File.ReadAllText(DsdFile), Folder); // UTF-8 is default.
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }

    }
}