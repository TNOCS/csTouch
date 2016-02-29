using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using csCommon.Types.DataServer.PoI;
using csShared;
using csShared.Interfaces;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.FeatureService.Symbols;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace MSA.Plugins.SketchPlugin
{
    [Export(typeof (IPlugin))]
    public class SketchPlugin : PropertyChangedBase, IPlugin
    {
        private PoI selectedPoiType;
        public SaveService SketchService;
        private Draw draw;
        private CircularMenuItem circularMenuItem;
        // private MenuItem config; // Never used.
        private NotificationEventArgs drawingNotification;
        private ColorCircularMenuItem fillColor;
        private bool hideFromSettings;
        private bool isRunning;
        private ColorCircularMenuItem lineColor;
        private IPluginScreen screen;
        private ISettingsScreen settings;
        private CircularMenuItem sketchMenuItem;
        public FloatingElement Element { get; set; }

        public Draw Draw
        {
            get { return draw; }
            set
            {
                draw = value;
                NotifyOfPropertyChange(() => Draw);
            }
        }

        public bool CanStop
        {
            get { return true; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/PenWhite.png"; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }


        public string Name
        {
            get { return "SketchPlugin"; }
        }

        public void Init()
        {
        }

        public void Start()
        {
            IsRunning = true;
            SketchService = CreateService();
            circularMenuItem = new CircularMenuItem {Title = "Start Sketch"};
            circularMenuItem.Selected += circularMenuItem_Selected;

            circularMenuItem.Id = Guid.NewGuid().ToString();
            circularMenuItem.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/Pen.png";
            AppState.AddCircularMenu(circularMenuItem);

            circularMenuItem.OpenChanged += (e, f) =>
            {
                if (circularMenuItem.IsOpen)
                {
                    CreateService();
                    SketchService.Layer.Visible = true;
                    SketchService.Layer.Opacity = 1;
                }
                else
                {
                    if (SketchService != null)
                    {
                        SketchService.Layer.Opacity = 0;
                    }
                    if (Draw != null && Draw.IsEnabled) Draw.IsEnabled = false;
                    DisableSelections();
                }
            };

            UpdateMenu();
            Execute.OnUIThread(() => AppState.DataServer.Services.Add(SketchService));
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Stop()
        {
            IsRunning = false;
            AppState.RemoveCircularMenu(circularMenuItem.Id);
        }

        private void DisableSelections()
        {
            sketchMenuItem.Fill = Brushes.White;
        }

        private void UpdateMenu()
        {
            circularMenuItem.Items.Clear();
            lineColor = ColorCircularMenuItem.CreateColorMenu("Line", 5);
            lineColor.Color = Colors.Black;
            circularMenuItem.Items.Add(lineColor);

            fillColor = ColorCircularMenuItem.CreateColorMenu("Fill", 6);
            fillColor.Color = Colors.Transparent;
            //circularMenuItem.Items.Add(fillColor);

            sketchMenuItem = new CircularMenuItem
            {
                Title = "Draw",
                Id = "Draw",
                CanCheck = true,
                Fill = Brushes.White,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/freehand.png"
            };

            Draw = new Draw(AppState.ViewDef.MapControl)
            {
                DrawMode = DrawMode.Freehand,
                LineSymbol = new SimpleLineSymbol {Color = Brushes.Black, Width = 3}
            };
            Draw.DrawBegin += MyDrawObject_DrawBegin;
            Draw.DrawComplete += MyDrawObjectDrawComplete;
            sketchMenuItem.Selected += (s, f) =>
            {
                selectedPoiType = new PoI
                {
                    Service = SketchService,
                    DrawingMode = DrawingModes.Polyline,
                    StrokeColor = lineColor.Color,
                    FillColor = Colors.Transparent,
                    StrokeWidth = 3,
                    
                };
                Draw.LineSymbol = new SimpleLineSymbol {Color = new SolidColorBrush(lineColor.Color), Width = 3};
                //sketchMenuItem.Fill = AppState.AccentBrush;
                Draw.IsEnabled = true;
                drawingNotification = new NotificationEventArgs
                {
                    Id = Guid.NewGuid(),
                    Style = NotificationStyle.Popup,
                    Duration = TimeSpan.FromHours(1),
                    Header = "Drawing activated",
                    Foreground = Brushes.White,
                    Background = AppState.AccentBrush,
                    Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/Pen.png"))
                };

                AppState.TriggerNotification(drawingNotification);
            };

            var clear = new CircularMenuItem
            {
                Title = "Clear",
                Id = "Clear",
                CanCheck = true,
                Position = 3,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/appbar.delete.png"
            };
            clear.Selected += (s, f) => { while (SketchService.PoIs.Any()) SketchService.PoIs.RemoveAt(0); };

            circularMenuItem.Items.Add(sketchMenuItem);
            circularMenuItem.Items.Add(clear);
        }

        private void MyDrawObjectDrawComplete(object sender, DrawEventArgs e)
        {
            AppState.TriggerDeleteNotification(drawingNotification);
            Draw.IsEnabled = false;

            var wm = new WebMercator();
            PoI newPoi = selectedPoiType.GetInstance();
            newPoi.Points = new ObservableCollection<Point>();
            newPoi.Style = new PoIStyle {StrokeColor = lineColor.Color, StrokeWidth = 3, CanDelete = true};
            //Logger.Stat("Drawing.Completed." + ((Custom) ? "Custom." + e.DrawMode : "Template." + ActiveMode.Name));
            switch (e.DrawMode)
            {
                case DrawMode.Freehand:
                case DrawMode.Polyline:
                    newPoi.DrawingMode = DrawingModes.Polyline;

                    //var polygon = e.Geometry is Polygon
                    //                      ? e.Geometry as Polygon
                    //                      : new Polygon();
                    if (e.Geometry is Polyline)
                    {
                        var source = e.Geometry as Polyline;
                        foreach (PointCollection path in source.Paths)
                        {
                            foreach (MapPoint po in path)
                            {
                                var r = wm.ToGeographic(po) as MapPoint;
                                if (r == null) continue;
                                newPoi.Points.Add(new Point(r.X, r.Y));
                                newPoi.Position = new Position(r.X, r.Y);
                            }
                        }
                    }
                    SketchService.PoIs.Add(newPoi);
                    break;
                case DrawMode.Circle:
                case DrawMode.Polygon:
                    if (e.Geometry is Polygon)
                    {
                        var source = e.Geometry as Polygon;
                        foreach (var path in source.Rings)
                        {
                            foreach (var r in path.Select(wm.ToGeographic).OfType<MapPoint>())
                            {
                                newPoi.Points.Add(new Point(r.X, r.Y));
                                newPoi.Position = new Position(r.X, r.Y);
                            }
                        }
                    }
                    SketchService.PoIs.Add(newPoi);
                    break;
            }
            selectedPoiType = null;
            DisableSelections();
            //UpdateMenu();
            //ActiveLayer.Graphics.Add(g);
        }

        private void MyDrawObject_DrawBegin(object sender, EventArgs e)
        {
        }


        private void circularMenuItem_Selected(object sender, MenuItemEventArgs e)
        {
        }

        public SaveService CreateService()
        {
            AppState.ViewDef.FolderIcons[@"Layers\Sketch Layer"] = "pack://application:,,,/csCommon;component/Resources/Icons/sketch.png";

            var ss = new SaveService
            {
                IsLocal = true,
                Name = "Sketch " + AppState.Imb.Status.Name,
                Id = Guid.NewGuid(),
                IsFileBased = false,
                StaticService = false,
                IsVisible = false,
                RelativeFolder = "Sketch Layer"
            };


            ss.Init(Mode.client, AppState.DataServer);
            ss.Folder = Directory.GetCurrentDirectory() + @"\PoiLayers\Sketch Layer";
            ss.InitPoiService();
            
            ss.SettingsList = new ContentList
            {
                Service = ss,
                ContentType = typeof (ServiceSettings),
                Id = "settings",
                IsRessetable = false
            };
            ss.SettingsList.Add(new ServiceSettings());
            ss.AllContent.Add(ss.SettingsList);
            ss.Settings.OpenTab = false;
            ss.Settings.TabBarVisible = false;
            ss.Settings.Icon = "layer.png";
            ss.AutoStart = true;
            //var result = new PoI
            //{
            //    ContentId = "Brug",
            //    Style = new PoIStyle
            //    {
            //        DrawingMode = DrawingModes.Point,
            //        FillColor = Colors.Red,
            //        IconWidth = 30,
            //        IconHeight = 30,
            //        TitleMode = TitleModes.Bottom
            //    }
            //};

            //result.AddMetaInfo("name", "name");
            ////result.AddMetaInfo("height", "height", MetaTypes.number);
            ////result.AddMetaInfo("width", "width", MetaTypes.number);
            ////result.AddMetaInfo("description", "description");
            ////result.AddMetaInfo("image", "image", MetaTypes.image);
            //ss.PoITypes.Add(result);
            return ss;
            
        }
    }
}