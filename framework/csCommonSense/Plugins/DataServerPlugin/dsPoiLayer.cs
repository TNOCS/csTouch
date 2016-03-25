using System.Windows.Controls;
using Caliburn.Micro;
using csDataServerPlugin.Extensions;
using csEvents;
using csShared;
using csShared.Geo;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;
using csCommon.Types.Geometries.AdvancedGeometry.Symbols;

namespace csDataServerPlugin
{
    public interface IServiceLayer
    {
        PoiService Service { get; set; }
        DataServerPlugin plugin { get; set; }
        EventList EventList { get; set; }
        void OpenPoiPopup(BaseContent p);
    }

    [DebuggerDisplay("Child of {parent.Service.Name} SERVICE Name: {Service.Name}, #PoIs: {Service.PoIs.Count}, #types: {Service.PoITypes.Count}")]
    public class dsPoiLayer : GraphicsLayer, IdsChildLayer
    {
        private readonly WebMercator mercator = new WebMercator();
        public IServiceLayer parent { get; set; }
        private DateTime lastTap;
        private MarkerSymbol markerSymbol;
        private static readonly MapViewDef mapViewDef = AppStateSettings.Instance.ViewDef;

        public dsPoiLayer(IServiceLayer layer)
        {
            parent = layer;
        }

        public override void Initialize()
        {
            base.Initialize();
            var pd = new ResourceDictionary
            {
                Source = new Uri("csCommon;component/Resources/Styles/PDictionary.xaml", UriKind.Relative)
            };

            markerSymbol = pd["ImageSymbol"] as MarkerSymbol;
            mapViewDef.MapControl.MapGesture -= MapControlMapGesture;
            mapViewDef.MapControl.MapGesture += MapControlMapGesture;
        }

        protected override void Cancel()
        {
            base.Cancel();
            mapViewDef.MapControl.MapGesture -= MapControlMapGesture;
        }

        private void MapControlMapGesture(object sender, Map.MapGestureEventArgs e)
        {
            if (e.Gesture != GestureType.Tap) return;
            if (lastTap.AddMilliseconds(500) >= DateTime.Now) return;
            lastTap = DateTime.Now;
            var gg = FindGraphicsInHostCoordinates(e.GetPosition(mapViewDef.MapControl));

            foreach (var g in gg.Cast<PoiGraphic>())
                g.Tapped(e.MapPoint);
        }

        public void RemovePoi(PoI p)
        {
            Execute.OnUIThread(() =>
            {
                if (!p.Data.ContainsKey("layer")) return;
                var gl = p.Data["layer"] as GraphicsLayer;
                if (gl == null) return;
                if (p.Data.ContainsKey("graphic")) p.Data.Remove("graphic");
                //if (p.Data.ContainsKey("layer")) p.Data.Remove("layer");
                var gg = gl.Where(k => k is PoiGraphic && ((PoiGraphic)k).Poi == p).ToList();
                if (p.Data.ContainsKey("events") && p.Data["events"] is List<IEvent>)
                {
                    while (parent.EventList.Count>0) parent.EventList.RemoveAt(0);
                }

                foreach (var g in gg) gl.Graphics.Remove(g);
            });
        }

        public void AddPoi(PoI p)
        {
            //if (p.Data.ContainsKey("layer")) return;
            if (!string.IsNullOrEmpty(p.PoiTypeId) && p.PoiType == null)
                p.PoiType = parent.Service.PoITypes.FirstOrDefault(k => (k).ContentId == p.PoiTypeId) as PoI;

            if (String.IsNullOrEmpty(p.ContentId))
                p.ContentId = Guid.NewGuid().ToString();
            if (p.Data == null) p.Data = new Dictionary<string, object>();
            p.Data["layer"] = this;

            var g = new PoiGraphic { Service = parent.Service };
            var p1 = Graphics.FirstOrDefault(x => ((x.Attributes.ContainsKey("id") && (Guid)x.Attributes["id"] == p.Id)));
            bool doInsert = true;
            if (p1 != null)
            {
                g = p1 as PoiGraphic;
                g.Service = parent.Service;
                doInsert = false;
            }
            
            GetGraphic(p, ref g);
            g.Layer = this;
            g.Plugin = parent.plugin;
            g.GroupLayer = parent;
            g.Poi = p;
            p.Data["graphic"] = g;

            if (doInsert) Graphics.Add(g);
            p.PositionChanged += (e, f) => UpdatePosition(g, p);

            foreach (var m in p.EffectiveMetaInfo.Where(k=>k.Type == MetaTypes.datetime))
            {
                if (p.Labels.ContainsKey(m.Label))
                {
                    DateTime date;
                    if (DateTime.TryParse(p.Labels[m.Label], out date))
                    {
                        // create event
                        var eb = new EventBase { Date = date, Name = p.Name };
                        eb.Latitude = p.Position.Latitude;
                        eb.Longitude = p.Position.Longitude;
                        eb.Image = p.NEffectiveStyle.Picture;
                        eb.AlwaysShow = true;
                        eb.Id = p.Id;
                        parent.EventList.Add(eb);
                        
                    }

                }
            }
        }

        private void GetGraphic(PoI p, ref PoiGraphic g)
        {
            g.Attributes["id"]          = p.Id;
            //g.Attributes["feature"]   = pf;
            g.Attributes["visible"]     = true;
            g.Attributes["PoI"]         = p;
            g.Attributes["Orientation"] = p.Orientation;
            g.Attributes["Plugin"]      = parent.plugin;
            g.Attributes["Layer"]       = this;
            g.Attributes["Attachable"]  = true;
            g.Attributes["Image"]       = p.NEffectiveStyle.IconUri;
            g.Attributes["Graphic"]     = g;
            g.Poi                       = p;
            g.MapTip                    = new Label { Content = p.Labels.ContainsKey(p.NEffectiveStyle.NameLabel) ? p.Labels[p.NEffectiveStyle.NameLabel] : string.Empty };

            var pc = new PointCollection();
            switch (p.DrawingMode)
            {
                case DrawingModes.Point:
                case DrawingModes.Image:
                    if (p.Position != null)
                    {
                        var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                        g.SetGeometry(m);
                    }

//                    if (p.NEffectiveStyle.Picture != null)
//                    {
//                        g.Symbol = new PictureMarkerSymbol()
//                        {
//                            Source = p.NEffectiveStyle.Picture,
//                            Width = p.NEffectiveStyle.IconWidth.Value,
//                            Height = p.NEffectiveStyle.IconHeight.Value,
//                        };
//                    }
//                    else
                    {
                        g.Symbol = markerSymbol;
                    }
                    break;

                //case DrawingModes.ImageImage:
                //    // TODO Draw image underneath
                //    if (p.Position != null)
                //    {
                //        var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                //        g.SetGeometry(m);
                //    }
                //    g.Symbol = markerSymbol;
                //    break;
                case DrawingModes.Freehand:
                    if (p.FillColor.A == 0 || p.NEffectiveStyle.FillOpacity.Value <= 0.0)
                    {
                        CreateLineSymbol(p, g);
                        var pol = new Polyline();
                        pc = new PointCollection();
                        if (p.Points != null)
                        {
                            foreach (var po in p.Points)
                                pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                            pol.Paths.Add(pc);
                        }
                        g.SetGeometry(pol);
                    }
                    else
                    {
                        CreateFillSymbol(p, g);
                        var plgg = new Polygon();

                        foreach (var po in p.Points)
                            pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                        if (p.Points.Any())
                        {
                            pc.Add(mercator.FromGeographic(new MapPoint(p.Points.First().X, p.Points.First().Y)) as MapPoint);
                        }
                        plgg.Rings.Add(pc);
                        g.SetGeometry(plgg);
                    }
                    break;
                case DrawingModes.Polyline:
                    CreateLineSymbol(p, g);
                    var polyline = new Polyline();
                    foreach (var po in p.Points)
                        pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                    polyline.Paths.Add(pc);
                    g.SetGeometry(polyline);

                    break;

                case DrawingModes.AdvancedPolyline:
                    CreateAdvancedLineSymbol(p, g);
                    var advancedPolyline = new Polyline();
                    foreach (var po in p.Points)
                        pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                    advancedPolyline.Paths.Add(pc);
                    g.SetGeometry(advancedPolyline);

                    break;
                case DrawingModes.Polygon:
                    CreateFillSymbol(p, g);
                    //var plg = new Polygon();

                    //foreach (var po in p.Points)
                    //    pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                    //plg.Rings.Add(pc);
                    //g.SetGeometry(plg);
                    if (p.Geometry != null)
                        ConvertGeometryToPolygon(p, g);
                    else
                        ConvertPointsToPolygon(p, g);
                    break;
                case DrawingModes.Circle:
                    CreateFillSymbol(p, g);
                    var cc = new Polygon();
                    if (p.Points != null)
                    {
                        foreach (var po in p.Points)
                            pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                        cc.Rings.Add(pc);
                    }
                    g.SetGeometry(cc);
                    break;
            }

            g.SetZIndex(1000);
        }

        private void CreateFillSymbol(PoI p, Graphic g)
        {
            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("csCommon;component/Resources/Styles/PDictionary.xaml", UriKind.Relative)
            };
            var fillSymbol = resourceDictionary["TouchFillSymbol"] as FillSymbol;
            g.Symbol = fillSymbol;
            g.Attributes["Fill"] = new SolidColorBrush(p.FillColor) { Opacity = p.NEffectiveStyle.FillOpacity.Value };
            g.Attributes["BorderThickness"] = p.StrokeWidth;
            g.Attributes["BorderBrush"] = new SolidColorBrush(p.StrokeColor) { Opacity = p.NEffectiveStyle.StrokeOpacity.Value };
            //g.Attributes["PoI"] = p;
            g.Attributes["Layer"] = this;
            if (p.NEffectiveStyle.CanMove == true)
                g.MakeDraggable();
        }

        private void CreateAdvancedLineSymbol(PoI p, Graphic g)
        {
           
            // Uses ExtendedLine in csCommon;component/Types/Geometries/AdvancedGeometry/Symbols/SymbolTemplates.xaml as controltemplate
            g.Symbol = new ExtendedLineSymbol();
            g.Attributes["PoI"] = p;
            g.Attributes["Layer"] = this;
        }
        private void CreateLineSymbol(PoI p, Graphic g)
        {
            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("csCommon;component/Resources/Styles/PDictionary.xaml", UriKind.Relative)
            };
            var lineSymbol = resourceDictionary["TouchLineSymbol"] as LineSymbol;
            g.Symbol = lineSymbol;
            g.Attributes["BorderThickness"] = p.StrokeWidth;
            g.Attributes["BorderBrush"] = new SolidColorBrush(p.StrokeColor) { Opacity = p.NEffectiveStyle.StrokeOpacity.Value };
            //g.Attributes["PoI"] = p;
            g.Attributes["Layer"] = this;
            if (p.NEffectiveStyle.CanMove == true)
                g.MakeDraggable();
        }

        private void UpdatePosition(PoiGraphic g, PoI p)
        {
            Execute.OnUIThread(() =>
                        {
            g.Attributes["Orientation"] = p.Orientation;
            switch (p.NEffectiveStyle.DrawingMode.Value)
            {
                case DrawingModes.Point:
                case DrawingModes.Image:
                    if (p.Position != null)
                    {
                        
                            var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                            g.SetGeometry(m);
                            // parent.UpdatePoiExtensions(p);
                        
                    }
                    break;
                    case DrawingModes.Freehand:
                    if (p.FillColor.A == 0 || p.NEffectiveStyle.FillOpacity.Value <= 0.0)
                    {
                        ConvertPointsToPolyline(p,g);
                    }
                    else
                    {
                        ConvertPointsToPolygon(p,g);
                    }
                    break;
                    // FIXME TODO: Unreachable code
                    //break;
                case DrawingModes.Circle:
                   
                case DrawingModes.Polygon:
                     ConvertPointsToPolygon(p, g);

                    break;
                    
                case DrawingModes.Polyline:
                case DrawingModes.AdvancedPolyline:
                    ConvertPointsToPolyline(p,g);
                      
                    

                    break;
            }
                        });
        }

        public void UpdateGraphic(PoI p, PoiGraphic original, PoiGraphic updated)
        {
            foreach (var k in original.Attributes.Keys.ToList())
            {
                if (updated.Attributes.ContainsKey(k) &&
                    original.Attributes[k].ToString() != updated.Attributes[k].ToString())
                {
                    original.Attributes[k] = updated.Attributes[k];
                }
            }

            var pc = new PointCollection();
            switch (p.DrawingMode)
            {
                case DrawingModes.Point:
                case DrawingModes.Image:
                    if (p.Position != null)
                    {
                        var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                        //Todo.. Werkt niet!
                        if (original.Geometry as MapPoint != m)
                            original.SetGeometry(m);
                    }
                    //original.Symbol = markerSymbol;
                    break;

                //case DrawingModes.ImageImage:
                //    // TODO Draw image underneath
                //    if (p.Position != null)
                //    {
                //        var m = mercator.FromGeographic(p.Position.ToMapPoint()) as MapPoint;
                //        g.SetGeometry(m);
                //    }
                //    g.Symbol = markerSymbol;
                //    break;
                case DrawingModes.Freehand:
                    if (p.FillColor.A == 0 || p.NEffectiveStyle.FillOpacity.Value <= 0.0)
                    {
                        //CreateLineSymbol(p, g);
                        var pol = new Polyline();
                        pc = new PointCollection();
                        if (p.Points != null)
                        {
                            foreach (var po in p.Points)
                                pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                            pol.Paths.Add(pc);
                        }
                        //Todo: Werkt niet
                        if ((original.Geometry as Polyline) != pol)
                            original.SetGeometry(pol);
                    }
                    else
                    {
                        //CreateFillSymbol(p, g);
                        var plgg = new Polygon();

                        foreach (var po in p.Points)
                            pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                        if (p.Points.Any())
                        {
                            pc.Add(mercator.FromGeographic(new MapPoint(p.Points.First().X, p.Points.First().Y)) as MapPoint);
                        }
                        plgg.Rings.Add(pc);
                        original.SetGeometry(plgg);
                    }
                    break;
                case DrawingModes.Polyline:
                    //CreateLineSymbol(p, g);
                    //var polyline = new Polyline();
                    //foreach (var po in p.Points)
                    //    pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                    //polyline.Paths.Add(pc);
                    //original.SetGeometry(polyline);

                    break;
                case DrawingModes.Polygon:
                    //CreateFillSymbol(p, g);
                    //var plg = new Polygon();

                    //foreach (var po in p.Points)
                    //    pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                    //plg.Rings.Add(pc);
                    //g.SetGeometry(plg);
                    if (p.Geometry != null)
                        ConvertGeometryToPolygon(p, original);
                    else
                        ConvertPointsToPolygon(p, original);
                    break;
                case DrawingModes.Circle:
                    //CreateFillSymbol(p, g);
                    var cc = new Polygon();
                    if (p.Points != null)
                    {
                        foreach (var po in p.Points)
                            pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
                        cc.Rings.Add(pc);
                    }
                    original.SetGeometry(cc);
                    break;
            }

        }

        public void UpdatePoi(PoI p)
        {
            //Todo: Instead of update, refresh or copy new values...
            var graphic = new PoiGraphic();
            //var currgraphic = new PoiGraphic();
            //GetGraphic(p, ref graphic);
            
            //if (p.Data.ContainsKey("graphic"))
            //    currgraphic = p.Data["graphic"] as PoiGraphic;
            //UpdateGraphic(p, currgraphic, graphic);
            p.UpdateEffectiveStyle();
            //{
            //    var currgraphic = p.Data["graphic"] as PoiGraphic;
            //    UpdateGraphic(p, currgraphic, graphic);
            //}
            p.UpdateAnalysisStyle();
            RemovePoi(p);
            AddPoi(p);

            // TODO When turning on/off highlighters, it may be that this is called three times!
            
            //return;
            // FIXME TODO: Unreachable code
//            if (p.Data.ContainsKey("graphic"))
//            {
//                if (!p.IsVisible)
//                {
//                    RemovePoi(p);
//                }
//                else
//                {
//                    var g = p.Data["graphic"] as Graphic;
//                    g.Attributes["BorderThickness"] = p.StrokeWidth;
//                    g.Attributes["BorderBrush"] = new SolidColorBrush(p.StrokeColor) { Opacity = p.NEffectiveStyle.StrokeOpacity.Value };
//                    g.Attributes["Fill"] = new SolidColorBrush(p.FillColor) { Opacity = p.NEffectiveStyle.FillOpacity.Value };
//                }
//            }
//            else
//            {
//                if (p.IsVisible) AddPoi(p);
//            }
        }

        #region ConvertPoints helpers

        private PointCollection ConvertPointsToPointCollection(BaseContent p)
        {
            if (p.Points == null || p.Points.Count == 0) return null;
            var pc = new PointCollection();
            foreach (var po in p.Points)
            {
                pc.Add(mercator.FromGeographic(new MapPoint(po.X, po.Y)) as MapPoint);
            }
            return pc;
        }

        private void ConvertPointsToPolygon(BaseContent p, PoiGraphic g)
        {
            var pc = ConvertPointsToPointCollection(p);
            if (pc == null) return;
            var polygon = new Polygon();
            //pc.Add(pc[0]); // Close the loop
            polygon.Rings.Add(pc);
            g.SetGeometry(polygon);
        }

        private void ConvertGeometryToPolygon(PoI p, PoiGraphic g)
        {
            var polygon = new Polygon();
            if (p.Geometry is csCommon.Types.Geometries.Polygon)
            {
                var geom = p.Geometry as csCommon.Types.Geometries.Polygon;
                foreach (var ls in geom.LineStrings)
                {
                    var pc = new PointCollection();
                    foreach (var ps in ls.Line)
                    {
                        pc.Add(mercator.FromGeographic(new MapPoint(ps.X, ps.Y)) as MapPoint);
                    }
                    if (pc.First().X != pc.Last().X || pc.First().Y != pc.Last().Y)
                        pc.Add(pc.First());
                    polygon.Rings.Add(pc);
                }
                g.SetGeometry(polygon);
            }
            else return;
        }

        private void ConvertPolygonToGraphic(csCommon.Types.Geometries.Polygon p, PoiGraphic g)
        {
            var polygon = new Polygon();
            foreach (var ls in p.LineStrings)
            {
                var pc = new PointCollection();
                foreach (var ps in ls.Line)
                {
                    pc.Add(mercator.FromGeographic(new MapPoint(ps.X, ps.Y)) as MapPoint);
                }
                if (pc.First().X != pc.Last().X || pc.First().Y != pc.Last().Y)
                    pc.Add(pc.First());
                polygon.Rings.Add(pc);
            }
            g.SetGeometry(polygon);
        }


        private void ConvertPointsToPolyline(BaseContent p, PoiGraphic g)
        {
            var pc = ConvertPointsToPointCollection(p);
            if (pc == null) return;
            var pol = new Polyline();
            pol.Paths.Add(pc);
            g.SetGeometry(pol);
        }

        #endregion ConvertPoints helpers
    }

    public class ReverseBooleanVisiblityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}