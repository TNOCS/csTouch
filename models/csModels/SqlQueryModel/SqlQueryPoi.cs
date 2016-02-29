using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Caliburn.Micro;
using csDataServerPlugin;
using csGeoLayers;
using csShared.Utils;
using DataServer;
using DataServer.SqlProcessing;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;

namespace csModels.SqlQueryModel
{
    public enum SymbolStyle
    {
        Line, Polygon
    }

    // TODO Specify the circle color as an InputParameter
    public class SqlQueryPoi : ModelPoiBase
    {
        private readonly SqlQueries sqlQueries = new SqlQueries();
        private readonly WebMercator webMercator = new WebMercator();

        private SymbolStyle symbolStyle = SymbolStyle.Line;
        private SolidColorBrush strokeBrush = Brushes.Red;

        public GraphicsLayer QueryLayer { private get; set; }

        public override void Start()
        {
            var posChanged = Observable.FromEventPattern<PositionEventArgs>(ev => Poi.PositionChanged += ev, ev => Poi.PositionChanged -= ev);
            posChanged.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(k => Calculate(string.Empty));

            var labelChanged = Observable.FromEventPattern<LabelChangedEventArgs>(ev => Poi.LabelChanged += ev, ev => Poi.LabelChanged -= ev);
            labelChanged.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(k => Calculate(k.EventArgs.Label));

            LoadAllQueries(Poi.Service.Folder);
        }

        public override void Stop()
        {
            var circle = GetCircle();
            if (circle == null) return;
            QueryLayer.Graphics.Remove(circle);
        }

        private void Calculate(string changedLabel)
        {
            foreach (var sqlQuery in sqlQueries.OfType<SqlQuery>())
                DrawCircleAndExecuteQuery(sqlQuery, changedLabel);
        }

        private void LoadAllQueries(string sqlFolder)
        {
            if (sqlQueries.Any())
            {
                sqlQueries.ForEach(s =>
                {
                    var sqlQuery = s as SqlQuery;
                    if (sqlQuery != null)
                        DrawCircle(sqlQuery.InputParameters.FirstOrDefault(p => p.Type == SqlParameterTypes.RadiusInMeter));
                });
                return;
            }
            foreach (var parameter in Model.Model.Parameters)
            {
                switch (parameter.Name)
                {
                    case "SqlQueries":
                        var set = new XmlReaderSettings {ConformanceLevel = ConformanceLevel.Fragment};
                        var query = parameter.Value;
                        try
                        {
                            var xs = XDocument.Load(XmlReader.Create(new StringReader(query), set));

                            var c = xs.Root;
                            if (c != null)
                                foreach (var xcl in c.Elements())
                                {
                                    var sqlQuery = new SqlQuery();
                                    sqlQuery.FromXml(xcl, sqlFolder);
                                    sqlQueries.Add(sqlQuery);

                                    DrawCircleAndExecuteQuery(sqlQuery);
                                }
                        }
                        catch (SystemException e)
                        {
                            Logger.Log("SqlQueryModel", "Cannot load SQL query", e.Message, Logger.Level.Error, true);
                        }
                        break;
                    case "SymbolStyle":
                        var symbolString = parameter.Source == ModelParameterSource.direct
                            ? parameter.Value
                            : Poi.Labels.ContainsKey(parameter.Value)
                                ? Poi.Labels[parameter.Value] 
                                : Poi.Service.Settings.Labels.ContainsKey(parameter.Value)
                                    ? Poi.Service.Settings.Labels[parameter.Value]
                                    : SymbolStyle.Line.ToString();
                        Enum.TryParse(symbolString, true, out symbolStyle);
                        if (parameter.Source == ModelParameterSource.label && Poi.Labels.ContainsKey(parameter.Value))
                        {
                            var p = parameter;
                            Poi.LabelChanged += (sender, args) =>
                            {
                                if (!string.Equals(args.Label, p.Value)) return;
                                QueryLayer.Graphics.Remove(GetCircle());
                                foreach (var sqlQuery in sqlQueries.OfType<SqlQuery>())
                                    DrawCircle(sqlQuery.InputParameters.FirstOrDefault(p1 => p1.Type == SqlParameterTypes.RadiusInMeter));
                            };
                        }
                        break;
                    case "StrokeColor":
                        var colorString = parameter.Source == ModelParameterSource.direct
                            ? parameter.Value
                            : Poi.Labels.ContainsKey(parameter.Value)
                                ? Poi.Labels[parameter.Value]
                                : Poi.Service.Settings.Labels.ContainsKey(parameter.Value)
                                    ? Poi.Service.Settings.Labels[parameter.Value] 
                                    : "Red";
                        var color = ColorConverter.ConvertFromString(colorString);
                        if (color != null) strokeBrush = new SolidColorBrush((Color) color);
                        if (parameter.Source == ModelParameterSource.label && Poi.Labels.ContainsKey(parameter.Value))
                        {
                            var p = parameter;
                            Poi.LabelChanged += (sender, args) =>
                            {
                                if (!string.Equals(args.Label, p.Value)) return;
                                color = ColorConverter.ConvertFromString(Poi.Labels[p.Value]);
                                if (color != null) strokeBrush = new SolidColorBrush((Color) color);
                                foreach (var sqlQuery in sqlQueries.OfType<SqlQuery>())
                                    DrawCircle(sqlQuery.InputParameters.FirstOrDefault(p1 => p1.Type == SqlParameterTypes.RadiusInMeter));
                            };
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Draw the circle and, optionally, execute the query again.
        /// The query is executed when the changedLabel == string.Empty (forced), or when there is a label-based
        /// input parameter that has changed.
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <param name="changedLabel"></param>
        private void DrawCircleAndExecuteQuery(SqlQuery sqlQuery, string changedLabel = "")
        {
            DrawCircle(sqlQuery.InputParameters.FirstOrDefault(p => p.Type == SqlParameterTypes.RadiusInMeter));

            if (string.IsNullOrEmpty(changedLabel) || sqlQuery.InputParameters.Any(p => string.Equals(p.LabelName, changedLabel)))
            {
                sqlQuery.Execute(Poi.Service as PoiService, Poi, null, true);
            }
        }

        private void DrawCircle(SqlInputParameter inputParameter)
        {
            if (inputParameter == null) return;
            var radius = Poi.LabelToDouble(inputParameter.LabelName);
            if (radius > 0)
                //double.TryParse(Poi.Labels[inputParameter.LabelName], NumberStyles.Number, CultureInfo.InvariantCulture, out radius))
                UpdateCircle(radius);
        }

        private void UpdateCircle(double radius)
        {
            var inLat = Poi.Position.Latitude;
            var inLon = Poi.Position.Longitude;
            var coordinates = new PointCollection();
            for (var i = 0; i < 360; i += 10)
            {
                double lat;
                double lon;
                CoordinateUtils.CalculatePointSphere2D(inLat, inLon, radius, i, out lat, out lon);
                coordinates.Add((MapPoint)webMercator.FromGeographic(new MapPoint(lon, lat)));
            }
            Execute.OnUIThread(() =>
            {
                coordinates.Add(coordinates[0]); // Add first Point to close the circle
                var circle = GetCircle();
                if (circle == null)
                {
                    var g = new Graphic();
                    g.Attributes["ID"] = Poi.Id.ToString();
                    switch (symbolStyle)
                    {
                        case SymbolStyle.Line:
                            var pl = new Polyline();
                            pl.Paths.Add(coordinates);
                            g.Symbol = new SimpleLineSymbol
                            {
                                Color = strokeBrush,
                                Width = 3,
                                Style = SimpleLineSymbol.LineStyle.Solid
                            };
                            g.Geometry = pl;
                         break;
                        case SymbolStyle.Polygon:
                            var polygon = new Polygon();
                            polygon.Rings.Add(coordinates);
                            var color = new Color {A = 0x30, R = strokeBrush.Color.R, G = strokeBrush.Color.G, B = strokeBrush.Color.B};
                            var brush = new SolidColorBrush(color);
                            g.Symbol = new SimpleFillSymbol
                            {
                                BorderBrush = strokeBrush,
                                BorderThickness = 3,
                                Fill = brush
                            };
                            g.Geometry = polygon;
                            break;
                    }
                    QueryLayer.Graphics.Add(g);
                }
                else
                {
                    switch (symbolStyle)
                    {
                        case SymbolStyle.Line:
                            var polyline = new Polyline();
                            polyline.Paths.Add(coordinates);
                            circle.Geometry = polyline;
                            var simpleLineSymbol = circle.Symbol as SimpleLineSymbol;
                            if (simpleLineSymbol != null) simpleLineSymbol.Color = strokeBrush;
                            break;
                        case SymbolStyle.Polygon:
                            var polygon = new Polygon();
                            polygon.Rings.Add(coordinates);
                            circle.Geometry = polygon;
                            var fillSymbol = circle.Symbol as SimpleFillSymbol;
                            if (fillSymbol != null)
                            {
                                var color = new Color { A = 0x30, R = strokeBrush.Color.R, G = strokeBrush.Color.G, B = strokeBrush.Color.B };
                                var brush = new SolidColorBrush(color);
                                fillSymbol.BorderBrush = strokeBrush;
                                fillSymbol.Fill = brush;
                            }
                            break;
                    }
                    QueryLayer.Refresh();
                }
            });
        }

        private Graphic GetCircle()
        {
            var guid = Poi.Id.ToString();
            return QueryLayer.Graphics.FirstOrDefault(k => string.Equals(k.Attributes["ID"].ToString(), guid));
        }
    }
}