using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csModels.GridModel
{
    [Export(typeof(IScreen))]
    public class GridViewModel : Screen, IEditableScreen
    {
        const int StrokeWidth = 2;

        private bool canEdit;
        private int cellSize;
        private int columns;
        private PoI poI;
        private int rows;
        private string selectedColor = "Blue";

        public GridViewModel(string displayName, PoI poi, GraphicsLayer gridLayer)
        {
            DisplayName = displayName;
            Poi = poi;
            GridLayer = gridLayer;
            ReadLabels();
            RedrawGrid();

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => Poi.PositionChanged += ev, ev => Poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(k => RedrawGrid());
        }

        public MapCallOutViewModel CallOut { get; set; }

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public int CellSize
        {
            get { return cellSize; }
            set
            {
                if (cellSize == value) return;
                cellSize = value;
                Update();
                NotifyOfPropertyChange(() => CellSize);
            }
        }

        public int Columns
        {
            get { return columns; }
            set
            {
                if (columns == value) return;
                columns = value;
                Update();
                NotifyOfPropertyChange(() => Columns);
            }
        }

        public DataServerBase DataServer { get; set; }

        public override string DisplayName { get; set; }

        public GraphicsLayer GridLayer { get; set; }

        public PoI Poi
        {
            get { return poI; }
            set { poI = value; NotifyOfPropertyChange(() => Poi); }
        }

        public int Rows
        {
            get { return rows; }
            set
            {
                if (rows == value) return;
                rows = value;
                Update();
                NotifyOfPropertyChange(() => Rows);
            }
        }

        public string SelectedColor
        {
            get { return selectedColor; }
            set
            {
                selectedColor = value;
                SaveLabels();
                NotifyOfPropertyChange(() => SelectedColor);
                NotifyOfPropertyChange(() => SelectedColorBrush);
            }
        }

        public SolidColorBrush SelectedColorBrush { get { return new BrushConverter().ConvertFromString(SelectedColor) as SolidColorBrush; } }

        private static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        public void SelectColor(FrameworkElement el)
        {
            var m = GetMenu(el);
            m.AddMenuItems(new[] { "Red", "Yellow", "Black", "Green", "Blue", "Orange", "Purple", "Brown" });
            m.Selected += (s, f) =>
            {
                SelectedColor = f.Object.ToString();
                RedrawGrid();
            };
            AppState.Popups.Add(m);
        }

        private static MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(10, 30),
                TimeOut = new TimeSpan(0, 0, 0, 30),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        private void ClearGraphics()
        {
            var poiId = Poi.Id.ToString();
            for (int i = GridLayer.Graphics.Count - 1; i >= 0; i--)
            {
                var g = GridLayer.Graphics[i];
                if (g.Attributes.ContainsKey("ID") && string.Equals(g.Attributes["ID"].ToString(), poiId, StringComparison.CurrentCultureIgnoreCase)) GridLayer.Graphics.Remove(g);
            }
        }

        private void PoiPositionChanged(object sender, PositionEventArgs e)
        {

        }
        private void ReadLabels()
        {
            cellSize = TryGetValue("CellSize", 100);
            columns = TryGetValue("Columns", 20);
            rows = TryGetValue("Rows", 20);
            string color;
            if (Poi.Labels.TryGetValue(DisplayName + ".Color", out color))
            {
                SelectedColor = color;
            }
        }

        /// <summary>
        ///    Produce an array from A..ZZ
        /// </summary>
        /// <param name="i"></param>
        /// <returns>A..ZZ</returns>
        /// <see cref="http://stackoverflow.com/questions/5384554/in-c-how-can-i-build-up-array-from-a-to-zz-that-is-similar-to-the-way-that-exc"/>
        private string ToExcelColumnName(long i)
        {
            if (i == 0) return ""; i--;
            return ToExcelColumnName(i / 26) + (char)('A' + i % 26);
        }

        private void RedrawGrid()
        {
            if (cellSize == 0 || columns == 0 || rows == 0) return;

            var tlLat = Poi.Position.Latitude;
            var tlLon = Poi.Position.Longitude;

            var width = cellSize * columns;
            var height = cellSize * rows;

            double trLat, trLon;
            CoordinateUtils.CalculatePointSphere2D(tlLat, tlLon, width, 90, out trLat, out trLon);
            double blLat, blLon;
            CoordinateUtils.CalculatePointSphere2D(tlLat, tlLon, height, 180, out blLat, out blLon);

            // Convert to map points
            var p = new Point(tlLon, tlLat).ToMapPoint();
            tlLon = p.X; tlLat = p.Y;
            p = new Point(trLon, trLat).ToMapPoint();
            trLon = p.X; trLat = p.Y;
            p = new Point(blLon, blLat).ToMapPoint();
            blLon = p.X; blLat = p.Y;

            var deltaLat = Math.Abs(tlLat - blLat) / rows;
            var deltaLon = Math.Abs(trLon - tlLon) / columns;

            var labels = new List<string>();
            for (int i = 1; i <= rows; i++)
                labels.Add(i.ToString());
            labels.Add(""); // Empty label for last row
            for (int i = 1; i <= columns; i++)
                labels.Add(ToExcelColumnName(i));
            labels.Add(""); // Empty label for last column

            Execute.OnUIThread(() =>
            {
                var pts = new List<PointCollection>();
                PointCollection labelCoordinates = new PointCollection();
                PointCollection lineCoordinates;

                double fromLat = tlLat, fromLon = tlLon, toLat = tlLat, toLon = trLon;
                for (int i = 0; i <= rows; i++)
                {
                    lineCoordinates = new PointCollection();
                    lineCoordinates.Add(new MapPoint(tlLon, fromLat));
                    lineCoordinates.Add(new MapPoint(trLon, fromLat));
                    pts.Add(lineCoordinates);
                    labelCoordinates.Add(new MapPoint(tlLon, fromLat));
                    fromLat -= deltaLat;
                }
                for (int i = 0; i <= columns; i++)
                {
                    lineCoordinates = new PointCollection();
                    lineCoordinates.Add(new MapPoint(fromLon, tlLat));
                    lineCoordinates.Add(new MapPoint(fromLon, blLat));
                    pts.Add(lineCoordinates);
                    labelCoordinates.Add(new MapPoint(fromLon, tlLat));
                    fromLon += deltaLon;
                }

                var symbol = new SimpleLineSymbol
                {
                    Color = SelectedColorBrush,
                    Width = StrokeWidth,
                    Style = SimpleLineSymbol.LineStyle.Dot
                };
                var poiId = Poi.Id.ToString();

                var graphics = new List<Graphic>();

                var j = 0;
                foreach (var c in pts)
                {
                    var pl = new Polyline();
                    pl.Paths.Add(c);
                    var g = new Graphic
                    {
                        Symbol = symbol,
                        Geometry = pl
                    };
                    g.Attributes["ID"] = poiId;
                    g.Attributes["NAME"] = labels[j];

                    graphics.Add(g);

                    // Add label, except A or 1, as this intersects with the icon.
                    if (!(string.Equals(labels[j], "A", StringComparison.InvariantCultureIgnoreCase) 
                       || string.Equals(labels[j], "1", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var gt = new Graphic
                        {
                            Symbol = CreateTextSymbol(labels[j]),
                            Geometry = labelCoordinates[j]
                        };
                        gt.Attributes["ID"] = poiId;
                        graphics.Add(gt);
                    }
                    j++;
                }
                ClearGraphics();
                GridLayer.Graphics.AddRange(graphics);
            });
        }

        private Symbol CreateTextSymbol(string text)
        {
            var ts = new TextSymbol {
                Text = text,
                OffsetX = -5,
                OffsetY = -5,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            return ts;
        }

        private void SaveLabels()
        {
            Poi.Labels[DisplayName + "." + "CellSize"] = cellSize.ToString();
            Poi.Labels[DisplayName + "." + "Rows"    ] = rows.ToString();
            Poi.Labels[DisplayName + "." + "Columns" ] = columns.ToString();
            Poi.Labels[DisplayName + "." + "Color"   ] = selectedColor;
        }

        private int TryGetValue(string key, int defaultValue = 20)
        {
            string result;
            int val;
            if (Poi.Labels.TryGetValue(DisplayName + "." + key, out result) && int.TryParse(result, out val))
            {
                return val;
            }
            return defaultValue;
        }

        private void Update()
        {
            SaveLabels();
            RedrawGrid();
        }
    }
}
