using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using csDataServerPlugin;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using GlazingFailureModelLibrary;
using System;
using System.Reactive.Linq;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csModels.CircleModel
{
    public class CirclePoi : ModelPoiBase
    {
        private static readonly WebMercator webMercator = new WebMercator();
        private RadialGradientBrush radialGradientBrush;

        public GraphicsLayer CircleLayer { private get; set; }

        public override void Start()
        {
            base.Start();

            var startColor = Model.Model.GetColor("StartColor", Colors.Blue);
            var endColor   = Model.Model.GetColor("EndColor", Colors.Blue);
            radialGradientBrush = new RadialGradientBrush(startColor, Color.FromArgb(30, endColor.R, endColor.G, endColor.B));

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => Poi.PositionChanged += ev, ev => Poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k => Calculate());

            var labelChanged = Observable.FromEventPattern<LabelChangedEventArgs>(ev => Poi.LabelChanged += ev, ev => Poi.LabelChanged -= ev);
            labelChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k => LabelChanged(k.EventArgs.Label));

            Calculate();
        }

        private void LabelChanged(string label)
        {
            if (label.StartsWith(Model.Id + ".", StringComparison.InvariantCultureIgnoreCase))
                Calculate();
        }

        public override void Stop()
        {
            base.Stop();
            var circle = GetImpactCircle();
            if (circle != null) Execute.OnUIThread(() => CircleLayer.Graphics.Remove(circle));
        }

        private Graphic GetImpactCircle()
        {
            var id = Poi.Id.ToString();
            return CircleLayer.Graphics.FirstOrDefault(k => (string)k.Attributes["ID"] == Model.Id && (string)k.Attributes["UserId"] == id);
        }

        private bool isBusy;

        private void Calculate()
        {
            if (isBusy) return;
            isBusy = true;

            string labelValue;
            double startAngle;
            if (!(Poi.Labels.TryGetValue(Model.Id + ".StartAngle", out labelValue) && double.TryParse(labelValue, out startAngle)))
                startAngle = 0;
            double endAngle;
            if (!(Poi.Labels.TryGetValue(Model.Id + ".EndAngle", out labelValue) && double.TryParse(labelValue, out endAngle)))
                endAngle = 360;
            double distanceInMeters;
            if (Poi.Labels.TryGetValue(Model.Id + ".CircleRadius", out labelValue) && double.TryParse(labelValue, out distanceInMeters))
                UpdateCircle(Model.Id, distanceInMeters, startAngle, endAngle < startAngle ? 360 + endAngle : endAngle);
            isBusy = false;
        }

        private void UpdateCircle(string label, double radius, double startAngle, double endAngle)
        {
            var inLat = Poi.Position.Latitude;
            var inLon = Poi.Position.Longitude;
            var coordinates = new PointCollection();
            for (var i = startAngle; i <= endAngle; i += 5)
            {
                double lat, lon;
                CoordinateUtils.CalculatePointSphere2D(inLat, inLon, radius, i, out lat, out lon);
                coordinates.Add((MapPoint)webMercator.FromGeographic(new MapPoint(lon, lat)));
            }
            if (endAngle - startAngle < 360) coordinates.Add((MapPoint)webMercator.FromGeographic(new MapPoint(inLon, inLat))); // Not a full circle, so add center too.
            coordinates.Add(coordinates[0]); // Add first point to close the circle

            Execute.OnUIThread(() =>
            {
                var circle = GetImpactCircle();
                if (circle == null)
                {
                    var polygon = new Polygon();
                    polygon.Rings.Add(coordinates);
                    var g = new Graphic
                    {
                        Symbol = new SimpleFillSymbol
                        {
                            BorderBrush = Brushes.Transparent,
                            BorderThickness = 1,
                            Fill = radialGradientBrush
                        },
                        Geometry = polygon
                    };
                    g.Attributes["ID"] = label;
                    g.Attributes["UserId"] = Poi.Id.ToString(); // To distinguish it from other models
                    CircleLayer.Graphics.Add(g);
                }
                else
                {
                    var polygon = new Polygon();
                    polygon.Rings.Add(coordinates);
                    circle.Geometry = polygon;
                    CircleLayer.Refresh();
                }
            });
        }
    }
}
