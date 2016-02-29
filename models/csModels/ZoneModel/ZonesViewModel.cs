using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Interfaces;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace csModels.ZoneModel
{
    public class ZoneList : BindableCollection<Zone>
    {
        public const string ZoneLabel = "zones";

        public override string ToString()
        {
            var res = string.Join("|", this.Select(k => k.ToString()));

            return res;
        }

        public void FromString(string s)
        {
            Clear();
            var zz = s.Split('|');
            foreach (var z in zz)
            {
                var zone = new Zone();
                zone.FromString(z);
                Add(zone);
            }
        }
    }

    public class Zone : PropertyChangedBase
    {
        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(() => Title); }
        }

        public List<Point> Points { get; set; }

        public Graphic Graphic { get; set; }

        private string color;

        public string Color
        {
            get { return color; }
            set { color = value; NotifyOfPropertyChange(() => Color); }
        }

        /// <summary>
        /// Returns the zone color as MediaColor object, with optional opacity.
        /// </summary>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public Color MediaColor(byte opacity = 0xFF) {
            var convertFromInvariantString = converter.ConvertFromInvariantString(color);
            if (convertFromInvariantString == null) return new Color();
            var c = (Color)convertFromInvariantString;
            return new Color { R = c.R, G = c.G, B = c.B, A = opacity};
        }

        private readonly ColorConverter converter = new ColorConverter();

        public override string ToString()
        {
            if (Points == null) return "";
            return Title + ":" + converter.ConvertToInvariantString(Color) + ":" + string.Join(" ", Points.Select(k => k.X.ToString(CultureInfo.InvariantCulture) + "," + k.Y.ToString(CultureInfo.InvariantCulture)));
        }

        internal void FromString(string z)
        {
            var cc = z.Split(':');
            if (cc.Length != 3) return;
            Title = cc[0];
            Color = cc[1];
            var pp = cc[2].Split(' ');
            Points = new List<Point>();
            foreach (var cp in pp.Select(p => p.Split(',')))
            {
                Points.Add(new Point(double.Parse(cp[0], NumberStyles.Any, CultureInfo.InvariantCulture), double.Parse(cp[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
            }
        }
    }

    [Export(typeof(IScreen))]
    public class ZonesViewModel : Screen, IEditableScreen
    {
        public override string DisplayName { get; set; }

        public DataServerBase DataServer { get; set; }

        private static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        public ZoneList Zones { get; set; }

        private PoI poI;

        public PoI Poi
        {
            get { return poI; }
            set { poI = value; NotifyOfPropertyChange(() => Poi); }
        }

        private string selectedColor = "Red";

        public string SelectedColor
        {
            get { return selectedColor; }
            set
            {
                selectedColor = value;
                NotifyOfPropertyChange(() => SelectedColor);
                NotifyOfPropertyChange(() => SelectedColorBrush);
            }
        }

        public SolidColorBrush SelectedColorBrush { get { return new BrushConverter().ConvertFromString(SelectedColor) as SolidColorBrush; } }

        public ITimelineManager TimelineManager;
        public IMB3.TEventEntry ImbEvent;
        private Draw draw;

        private Zone activeZone;

        private string zoneName;

        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; NotifyOfPropertyChange(() => ZoneName); }
        }

        public void AddZone()
        {
            if (string.IsNullOrEmpty(ZoneName) || Zones.Any(k => k.Title == ZoneName))
            {
                AppState.TriggerNotification("Zone name needs to be unique");
            }
            else
            {
                var nz = new Zone { Title = ZoneName };

                AppState.TriggerNotification("Start zone drawing");
                draw = new Draw(AppState.ViewDef.MapControl);
                //Draw.DrawBegin += MyDrawObject_DrawBegin;
                draw.DrawComplete += MyDrawObjectDrawComplete;
                draw.DrawMode = DrawMode.Polygon;
                draw.LineSymbol = new LineSymbol
                {
                    Width = 2,
                    Color = SelectedColorBrush
                };
                activeZone = nz;
                draw.IsEnabled = true;
                ZoneName = string.Empty;
            }
        }

        private void MyDrawObjectDrawComplete(object sender, DrawEventArgs e)
        {
            draw.IsEnabled = false;
            if (activeZone == null) return;
            var g = e.Geometry as Polygon;
            activeZone.Points = new List<Point>();
            if (g != null)
            {
                foreach (var p in g.Rings[0])
                    activeZone.Points.Add(new Point { X = p.X, Y = p.Y });
                // Close the zone.
                activeZone.Points.Add(activeZone.Points[0]);
            }
            activeZone.Color = SelectedColor;
            Zones.Add(activeZone);
            UpdateZonesLabel();
        }

        private void UpdateZonesLabel()
        {
            Poi.Labels["zones"] = Zones.ToString();
            Poi.TriggerLabelChanged("zones", "", "");
        }

        public void RemoveZone(Zone z)
        {
            if (Zones.Contains(z)) Zones.Remove(z);
            UpdateZonesLabel();
        }

        private bool canEdit;

        private static MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(10, 30),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        public void SelectColor(FrameworkElement el)
        {
            var m = GetMenu(el);
            m.AddMenuItems(new[] { "Red", "Yellow", "Black", "Green", "Blue", "Orange", "Purple", "Brown" });
            m.Selected += (s, f) =>
            {
                SelectedColor = f.Object.ToString();
            };
            AppState.Popups.Add(m);
        }

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

        public MapCallOutViewModel CallOut { get; set; }
    }
}