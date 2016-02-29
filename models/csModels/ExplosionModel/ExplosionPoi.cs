using System.Globalization;
using System.Linq;
using System.Windows.Media;
using System.Xml;
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

namespace csModels.ExplosionModel
{

    public class ExplosionPoi : ModelPoiBase
    {
        private readonly WebMercator webMercator = new WebMercator();
        private Color defaultColor;
        private RadialGradientBrush radialGradientBrush;

        public GraphicsLayer ExplosionLayer { private get; set; }

        public override void Start()
        {
            base.Start();

            defaultColor = Model.Model.GetColor("Color", Colors.Red);
            radialGradientBrush = new RadialGradientBrush(defaultColor, Color.FromArgb(30, defaultColor.R, defaultColor.G, defaultColor.B));

            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => Poi.PositionChanged += ev, ev => Poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k => Calculate());

            var labelChanged = Observable.FromEventPattern<LabelChangedEventArgs>(ev => Poi.LabelChanged += ev, ev => Poi.LabelChanged -= ev);
            labelChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(k => LabelChanged(k.EventArgs.Label));

            Calculate();
        }

        private void LabelChanged(string label)
        {
            if (   string.Equals(label, Model.Id + ".TntEqInKg",   StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(label, Model.Id + ".SingleGlass", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(label, Model.Id + ".Lethality",   StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(label, Model.Id + ".FrontSize",   StringComparison.InvariantCultureIgnoreCase)
                )
                Calculate();
        }

        public override void Stop()
        {
            base.Stop();
            var circle = GetImpactCircle();
            if (circle != null) Execute.OnUIThread(() => ExplosionLayer.Graphics.Remove(circle));
        }

        private Graphic GetImpactCircle()
        {
            var id = Poi.Id.ToString();
            return ExplosionLayer.Graphics.FirstOrDefault(k => (string) k.Attributes["ID"] == Model.Id && (string) k.Attributes["UserId"] == id);
        }

        private bool isBusy;

        private void Calculate()
        {
            if (isBusy) return;
            isBusy = true;
            //var circle = GetCircle();
            //if (circle != null)
            //    Execute.OnUIThread(() =>
            //    {
            //        circle.Symbol. = (Poi.IsVisible) ? Visibility.Visible : Visibility.Collapsed;
            //    });

            // Use single or double glass in the calculations
            var option = Model.Id + ".SingleGlass";
            var singleGlass = true;
            if (!Poi.Labels.ContainsKey(option)) Poi.Labels[option] = true.ToString();
            else bool.TryParse(Poi.Labels[option], out singleGlass);

            // Compute the probability of lethality, or, if false, the probability of breaking glass.
            option = Model.Id + ".Lethality";
            var lethality = false;
            if (!Poi.Labels.ContainsKey(option)) Poi.Labels[option] = false.ToString();
            else bool.TryParse(Poi.Labels[option], out lethality);

            // Consider the effects at the front or backside of the building
            option = Model.Id + ".FrontSize";
            var frontSide = true;
            if (!Poi.Labels.ContainsKey(option)) Poi.Labels[option] = true.ToString();
            else bool.TryParse(Poi.Labels[option], out frontSide);
            var side = frontSide
                ? GlazingFailureModel.EFRONTBACKSIDE.FRONT
                : GlazingFailureModel.EFRONTBACKSIDE.BACK;

            option = Model.Id + ".TntEqInKg";
            var mass = 10.0;
            if (!Poi.Labels.ContainsKey(option)) Poi.Labels[option] = mass.ToString(CultureInfo.InvariantCulture);
            else double.TryParse(Poi.Labels[option], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out mass);

            var explosionModel = lethality
                ? (singleGlass
                    ? (GlazingFailureModel) new LethalitySingleGlazingModel {Side = side}
                    : new LethalityDoubleGlazingModel {Side = side})
                : (singleGlass
                    ? (GlazingFailureModel) new SingleGlazingFailureModel {Side = side}
                    : new DoubleGlazingFailureModel {Side = side});

            var distance = 0;
            //var stepSize = distance/2;
            const double threshold = 5;
            double probability;
            do
            {
                distance += 5;
                probability = explosionModel.CalculatePbreak(distance, mass);
            } while (probability > threshold);
            UpdateCircle(Model.Id, distance);
            var key = Model.Id + ".ImpactRadius";
            Poi.Labels[key] = distance.ToString(CultureInfo.InvariantCulture);
            Poi.OnLabelChanged(key, string.Empty, string.Empty);
            isBusy = false;
        }

        private void UpdateCircle(string label, double radius)
        {
            var inLat = Poi.Position.Latitude;
            var inLon = Poi.Position.Longitude;
            var coordinates = new PointCollection();
            for (var i = 0; i < 360; i += 10)
            {
                double lat, lon;
                CoordinateUtils.CalculatePointSphere2D(inLat, inLon, radius, i, out lat, out lon);
                coordinates.Add((MapPoint)webMercator.FromGeographic(new MapPoint(lon, lat)));
            }
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
                    ExplosionLayer.Graphics.Add(g);
                }
                else
                {
                    var polygon = new Polygon();
                    polygon.Rings.Add(coordinates);
                    circle.Geometry = polygon;
                    ExplosionLayer.Refresh();
                }
            });
        }

    }
}
