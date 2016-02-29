using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using csGeoLayers;
using csShared;
using csShared.Utils;
using Caliburn.Micro;
using DotSpatial.Topology;
using DotSpatial.Topology.Utilities;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Tasks;
using Newtonsoft.Json.Linq;
using Geometry = ESRI.ArcGIS.Client.Geometry.Geometry;
using Point = DotSpatial.Topology.Point;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;
using Polygon = ESRI.ArcGIS.Client.Geometry.Polygon;
using csUSDomainPlugin.Effects.Views;
using csUSDomainPlugin.Effects.ViewModels;
using DotSpatial.Projections;
using Vector = DotSpatial.Topology.Vector;


namespace csUSDomainPlugin.Effects.Util
{
    public class DirectionTool : PropertyChangedBase
    {
        public GroupLayer Layer;
        public Graphic _start;
        public Graphic _finish;
        public Graphic Line;
        public GraphicCollection BaseEffectsModelShapes;
        public GraphicCollection EffectsModelShapes;

        public EffectsPoint Start;
        public EffectsPoint Finish;

        public GraphicsLayer MLayer;
        public GraphicsLayer ContoursLayer;

        public EffectsModelSettingsViewModel SettingsViewModel;

        private bool _firstMovement = true;

        private double _distance;

        public double Distance
        {
            get { return _distance; }
            set { _distance = value; NotifyOfPropertyChange(() => Distance); }
        }

        private string _windDirection;

        public string WindDirection
        {
            get { return _windDirection;}
            set
            {
                //if (_windDirection != value)
                {
                    _windDirection = value;
                    NotifyOfPropertyChange(()=>WindDirection);
                }
            }
        }

        public double GetDistance()
        {
            var w = new WebMercator();
            var p1 = w.ToGeographic(Start.Mp) as MapPoint;
            var p2 = w.ToGeographic(Finish.Mp) as MapPoint;
            var pLon1 = p1.X;
            var pLat1 = p1.Y;
            var pLon2 = p2.X;
            var pLat2 = p2.Y;

            var dist = csShared.Utils.CoordinateUtils.Distance(pLat1, pLon1, pLat2, pLon2, 'K');
            return dist;//Math.Sqrt((deltaX*deltaX) + (deltaY*deltaY));
        }

        public string GetWindDirection()
        {
            var w = new WebMercator();
            var p1 = w.ToGeographic(Start.Mp) as MapPoint;
            var p2 = w.ToGeographic(Finish.Mp) as MapPoint;
            var angle = GetAngle(p1, p2);

            if (angle < 22.5) return "E";
            if (angle < 67.5) return "NE";
            if (angle < 112.5) return "N";
            if (angle < 157.5) return "NW";
            if (angle < 202.5) return "W";
            if (angle < 247.5) return "SW";
            if (angle < 292.5) return "S";
            if (angle < 337.5) return "SE";
            return "E";
        }

        public double GetAngle(MapPoint p1, MapPoint p2)
        {
            return GetAngle(p1.X, p1.Y, p2.X, p2.Y);
        }

        public double GetAngle(double px1, double py1, double px2, double py2)
        {
            // Negate X and Y values
            double pxRes = px2 - px1;
            double pyRes = py2 - py1;
            double angle;
            // Calculate the angle
            if (pxRes == 0.0)
            {
                if (pxRes == 0.0)
                    angle = 0.0;
                else if (pyRes > 0.0) angle = Math.PI / 2.0;
                else
                    angle = Math.PI * 3.0 / 2.0;
            }
            else if (pyRes == 0.0)
            {
                if (pxRes > 0.0)
                    angle = 0.0;
                else
                    angle = Math.PI;
            }
            else
            {
                if (pxRes < 0.0)
                    angle = Math.Atan(pyRes / pxRes) + Math.PI;
                else if (pyRes < 0.0) angle = Math.Atan(pyRes / pxRes) + (2 * Math.PI);
                else
                    angle = Math.Atan(pyRes / pxRes);
            }
            // Convert to degrees
            angle = angle * 180 / Math.PI;
            return angle;
        }

        public void Init(GroupLayer gl, MapPoint start, MapPoint finish, ResourceDictionary rd)
        {
            Start = new EffectsPoint() { Mp = start };
            Finish = new EffectsPoint() { Mp = finish };
            MLayer = new GraphicsLayer() { ID = Guid.NewGuid().ToString() };
            ContoursLayer = new GraphicsLayer() { ID = Guid.NewGuid().ToString() };
            _start = new Graphic();
            _finish = new Graphic();
            Line = new Graphic();
            BaseEffectsModelShapes = new GraphicCollection();
            EffectsModelShapes = new GraphicCollection();

            
            
//            var testJson = new FileInfo(@"Plugins\USDomainPlugin\Effects\Data\testshape.json");
//            JObject geoJson = null;
//            using (var reader = testJson.OpenText())
//            {
//                var strJson = reader.ReadToEnd();
//                geoJson = JObject.Parse(strJson);
//                reader.Close();
//            }
//            var strWkt = @"POLYGON ((281.4968022018320000 0,281.3579005227060000 8.8420282352252900,280.9413325645150000 17.6753304560934000,280.2475094295410000 26.4911892597699000,279.2771158374510000 35.2809044579670000,278.0311094495650000 44.0358016629793000,276.5107199237550000 52.7472408482570000,274.7174477009240000 61.4066248750691000,272.6530625242520000 70.0054079768411000,270.3196016926710000 78.5351041927953000,267.7193680503000000 86.9872957425693000,264.8549277138150000 95.3536413335488000,261.7291075400000000 103.6258843927170000,258.3449923359800000 111.7958612148940000,254.7059218148940000 119.8555090193290000,250.8154872999950000 127.7968739066940000,246.6775281804520000 135.6121187086160000,242.2961281223330000 143.2935307220230000,237.6756110385240000 150.8335293206440000,232.8205368215440000 158.2246734361790000,227.7356968434850000 165.4596689017280000,222.4261092275060000 172.5313756502600000,216.8970138955490000 179.4328147609900000,211.1538673971700000 186.1571753467370000,205.2023375245860000 192.6978212754450000,199.0482977192440000 199.0482977192440000,192.6978212754450000 205.2023375245860000,186.1571753467370000 211.1538673971700000,179.4328147609900000 216.8970138955490000,172.5313756502600000 222.4261092275060000,165.4596689017280000 227.7356968434850000,158.2246734361790000 232.8205368215440000,150.8335293206440000 237.6756110385240000,143.2935307220230000 242.2961281223330000,135.6121187086160000 246.6775281804520000,127.7968739066940000 250.8154872999950000,119.8555090193290000 254.7059218148940000,111.7958612148940000 258.3449923359800000,103.6258843927170000 261.7291075400000000,95.3536413335487000 264.8549277138150000,86.9872957425692000 267.7193680503000000,78.5351041927953000 270.3196016926710000,70.0054079768411000 272.6530625242520000,61.4066248750690000 274.7174477009240000,52.7472408482570000 276.5107199237550000,44.0358016629793000 278.0311094495650000,35.2809044579670000 279.2771158374510000,26.4911892597699000 280.2475094295410000,17.6753304560934000 280.9413325645150000,8.8420282352253100 281.3579005227060000,0.0000000000000172 281.4968022018320000,-8.8420282352252800 281.3579005227060000,-17.6753304560934000 280.9413325645150000,-26.4911892597699000 280.2475094295410000,-35.2809044579670000 279.2771158374510000,-44.0358016629793000 278.0311094495650000,-52.7472408482571000 276.5107199237550000,-61.4066248750690000 274.7174477009240000,-70.0054079768410000 272.6530625242520000,-78.5351041927953000 270.3196016926710000,-86.9872957425692000 267.7193680503000000,-95.3536413335488000 264.8549277138150000,-103.6258843927170000 261.7291075400000000,-111.7958612148940000 258.3449923359800000,-119.8555090193300000 254.7059218148940000,-127.7968739066940000 250.8154872999950000,-135.6121187086170000 246.6775281804520000,-143.2935307220230000 242.2961281223330000,-150.8335293206440000 237.6756110385240000,-158.2246734361790000 232.8205368215440000,-165.4596689017280000 227.7356968434850000,-172.5313756502600000 222.4261092275060000,-179.4328147609900000 216.8970138955490000,-186.1571753467370000 211.1538673971700000,-192.6978212754450000 205.2023375245860000,-199.0482977192440000 199.0482977192440000,-205.2023375245860000 192.6978212754450000,-211.1538673971700000 186.1571753467370000,-216.8970138955490000 179.4328147609900000,-222.4261092275060000 172.5313756502600000,-227.7356968434850000 165.4596689017280000,-232.8205368215440000 158.2246734361790000,-237.6756110385240000 150.8335293206440000,-242.2961281223330000 143.2935307220230000,-246.6775281804520000 135.6121187086160000,-250.8154872999950000 127.7968739066940000,-254.7059218148940000 119.8555090193290000,-258.3449923359800000 111.7958612148940000,-261.7291075400000000 103.6258843927170000,-264.8549277138150000 95.3536413335488000,-267.7193680503000000 86.9872957425693000,-270.3196016926710000 78.5351041927953000,-272.6530625242520000 70.0054079768411000,-274.7174477009240000 61.4066248750690000,-276.5107199237550000 52.7472408482571000,-278.0311094495650000 44.0358016629793000,-279.2771158374510000 35.2809044579671000,-280.2475094295410000 26.4911892597699000,-280.9413325645150000 17.6753304560935000,-281.3579005227060000 8.8420282352252700,-281.4968022018320000 0.0000000000000345,-281.3579005227060000 -8.8420282352253200,-280.9413325645150000 -17.6753304560934000,-280.2475094295410000 -26.4911892597699000,-279.2771158374510000 -35.2809044579670000,-278.0311094495650000 -44.0358016629794000,-276.5107199237550000 -52.7472408482570000,-274.7174477009240000 -61.4066248750692000,-272.6530625242520000 -70.0054079768412000,-270.3196016926710000 -78.5351041927953000,-267.7193680503000000 -86.9872957425693000,-264.8549277138150000 -95.3536413335488000,-261.7291075400000000 -103.6258843927170000,-258.3449923359800000 -111.7958612148940000,-254.7059218148940000 -119.8555090193290000,-250.8154872999950000 -127.7968739066940000,-246.6775281804520000 -135.6121187086160000,-242.2961281223330000 -143.2935307220230000,-237.6756110385240000 -150.8335293206440000,-232.8205368215440000 -158.2246734361790000,-227.7356968434850000 -165.4596689017280000,-222.4261092275060000 -172.5313756502600000,-216.8970138955490000 -179.4328147609900000,-211.1538673971700000 -186.1571753467370000,-205.2023375245860000 -192.6978212754450000,-199.0482977192440000 -199.0482977192440000,-192.6978212754450000 -205.2023375245860000,-186.1571753467370000 -211.1538673971700000,-179.4328147609900000 -216.8970138955490000,-172.5313756502600000 -222.4261092275060000,-165.4596689017280000 -227.7356968434850000,-158.2246734361790000 -232.8205368215440000,-150.8335293206440000 -237.6756110385240000,-143.2935307220230000 -242.2961281223330000,-135.6121187086160000 -246.6775281804520000,-127.7968739066940000 -250.8154872999950000,-119.8555090193290000 -254.7059218148940000,-111.7958612148940000 -258.3449923359800000,-103.6258843927170000 -261.7291075400000000,-95.3536413335488000 -264.8549277138150000,-86.9872957425693000 -267.7193680503000000,-78.5351041927954000 -270.3196016926710000,-70.0054079768412000 -272.6530625242520000,-61.4066248750690000 -274.7174477009240000,-52.7472408482570000 -276.5107199237550000,-44.0358016629793000 -278.0311094495650000,-35.2809044579671000 -279.2771158374510000,-26.4911892597698000 -280.2475094295410000,-17.6753304560934000 -280.9413325645150000,-8.8420282352252900 -281.3579005227060000,-0.0000000000000517 -281.4968022018320000,8.8420282352251800 -281.3579005227060000,17.6753304560935000 -280.9413325645150000,26.4911892597699000 -280.2475094295410000,35.2809044579670000 -279.2771158374510000,44.0358016629792000 -278.0311094495650000,52.7472408482572000 -276.5107199237550000,61.4066248750692000 -274.7174477009240000,70.0054079768411000 -272.6530625242520000,78.5351041927953000 -270.3196016926710000,86.9872957425694000 -267.7193680503000000,95.3536413335489000 -264.8549277138150000,103.6258843927170000 -261.7291075400000000,111.7958612148940000 -258.3449923359800000,119.8555090193290000 -254.7059218148940000,127.7968739066940000 -250.8154872999950000,135.6121187086160000 -246.6775281804520000,143.2935307220230000 -242.2961281223330000,150.8335293206440000 -237.6756110385240000,158.2246734361790000 -232.8205368215440000,165.4596689017280000 -227.7356968434850000,172.5313756502600000 -222.4261092275060000,179.4328147609900000 -216.8970138955490000,186.1571753467370000 -211.1538673971700000,192.6978212754450000 -205.2023375245860000,199.0482977192440000 -199.0482977192440000,205.2023375245860000 -192.6978212754450000,211.1538673971700000 -186.1571753467370000,216.8970138955490000 -179.4328147609900000,222.4261092275060000 -172.5313756502600000,227.7356968434850000 -165.4596689017280000,232.8205368215440000 -158.2246734361790000,237.6756110385240000 -150.8335293206440000,242.2961281223330000 -143.2935307220230000,246.6775281804520000 -135.6121187086160000,250.8154872999950000 -127.7968739066940000,254.7059218148940000 -119.8555090193290000,258.3449923359800000 -111.7958612148940000,261.7291075400000000 -103.6258843927170000,264.8549277138150000 -95.3536413335488000,267.7193680503000000 -86.9872957425693000,270.3196016926710000 -78.5351041927954000,272.6530625242520000 -70.0054079768413000,274.7174477009240000 -61.4066248750690000,276.5107199237550000 -52.7472408482570000,278.0311094495650000 -44.0358016629794000,279.2771158374510000 -35.2809044579671000,280.2475094295410000 -26.4911892597698000,280.9413325645150000 -17.6753304560934000,281.3579005227060000 -8.8420282352253000,281.4968022018320000 -0.0000000000000689))";

//            BaseEffectsModelShapes.AddRange(geoJson.ToGraphic(new SpatialReference(28992)));
//            BaseEffectsModelShapes.AddRange(strWkt.ToGraphic(new SpatialReference(28992)));
//            BaseEffectsModelShapes.ForEach(shape =>
//            {
//                var clone = new Graphic();
//                clone.Geometry = ((Polygon) shape.Geometry).Clone();
//                clone.Symbol = new SimpleFillSymbol
//                {
//                    BorderBrush = new SolidColorBrush(Colors.Black),
//                    BorderThickness = 4,
//                    Fill = new SolidColorBrush(Colors.Red) { Opacity = 0.8f }
//                };
//                clone.Attributes.Add("base", shape);
//                EffectsModelShapes.Add(clone);
//            });
//            ContoursLayer.Graphics.AddRange(EffectsModelShapes);
//            ContoursLayer.Graphics.AddRange(BaseEffectsModelShapes);
            Layer.ChildLayers.Add(ContoursLayer);
            Layer.ChildLayers.Add(MLayer);

            LineSymbol ls = new LineSymbol() { Color = Brushes.Black, Width = 4 };
            Line.Symbol = ls;
            UpdateLine();

            MLayer.Graphics.Add(Line);
            _start.Geometry = start;
            _start.Attributes["position"] = start;

            _start.Symbol = rd["Start"] as Symbol;
            _start.Attributes["finish"] = _finish;
            _start.Attributes["start"] = _start;
            _start.Attributes["line"] = Line;
            _start.Attributes["state"] = "start";
            _start.Attributes["direction"] = this;
            _start.Attributes["menuenabled"] = true;

            MLayer.Graphics.Add(_start);

            _finish.Geometry = finish;
            _finish.Attributes["position"] = finish;
            _finish.Symbol = rd["Finish"] as Symbol;
            _finish.Attributes["finish"] = _finish;
            _finish.Attributes["start"] = _start;
            _finish.Attributes["line"] = Line;
            _finish.Attributes["direction"] = this;
            _finish.Attributes["state"] = "finish";
            MLayer.Graphics.Add(_finish);


            MLayer.Initialize();

            _start.SetZIndex(10);
            _finish.SetZIndex(10);
            Line.SetZIndex(10);

            SettingsViewModel = new EffectsModelSettingsViewModel();
            SettingsViewModel.PropertyChanged += SettingsViewModelOnPropertyChanged;
            SettingsViewModel.Initialize();

            AppStateSettings.Instance.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

        }

        private void SettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            switch (propertyChangedEventArgs.PropertyName)
            {
                case "Contours":
                    
                    foreach (var graphic in EffectsModelShapes)
                    {
                        ContoursLayer.Graphics.Remove(graphic);
                    }

                    foreach (var graphic in BaseEffectsModelShapes)
                    {
                        ContoursLayer.Graphics.Remove(graphic);
                    }

                    BaseEffectsModelShapes.Clear();
                    BaseEffectsModelShapes.AddRange(SettingsViewModel.Contours);

                    EffectsModelShapes.Clear();

                    BaseEffectsModelShapes.ForEach(shape =>
                    {
                        var clone = new Graphic();
                        clone.Geometry = ((Polygon) shape.Geometry).Clone();
                        clone.Symbol = new SimpleFillSymbol
                        {
                            BorderBrush = new SolidColorBrush(Colors.Black),
                            BorderThickness = 1,
                            Fill = new SolidColorBrush(Colors.Red) { Opacity = 0.8f }
                        };
                        clone.Attributes.Add("base", shape);
                        EffectsModelShapes.Add(clone);

//                        MLayer.Graphics.Insert(0, shape);
//                        MLayer.Graphics.Insert(0, clone);
                    });

                    ContoursLayer.Graphics.AddRange(BaseEffectsModelShapes);
                    ContoursLayer.Graphics.AddRange(EffectsModelShapes);

                    UpdateModelShape();
//
//                    var rdCenter = new MapPoint(5.387206, 52.155174);
//                    var start = new WebMercator().ToGeographic(Start.Mp) as MapPoint;
//                    //rdCenter = new WebMercator().FromGeographic(rdCenter) as MapPoint;
//
//                    foreach (var graphic in contours)
//                    {
//                        var baseGraphic = graphic.Attributes["base"] as Graphic;
//                        var basePoly = baseGraphic.Geometry as Polygon;
//                        var clone = basePoly.Clone();
//                
//                        clone.Offset(start.X - rdCenter.X, start.Y - rdCenter.Y);
//
//                        graphic.Geometry = clone;
//                    }
//
//                    ContoursLayer.ClearGraphics();
//                    ContoursLayer.Graphics.AddRange(contours);
//
                    break;
            }
        }

        void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            UpdateLine();
        }

        internal void UpdateLine()
        {
            Polyline pl = new Polyline();
            pl.Paths = new ObservableCollection<PointCollection>();
            PointCollection pc = new PointCollection();
            pc.Add(Start.Mp);
            pc.Add(Finish.Mp);

            pl.Paths.Add(pc);
            Line.Geometry = pl;


        }

        private void UpdateModelShape()
        {
            var rdCenter = new MapPoint(5.387206, 52.155174);
//            var rdCenter = new MapPoint(155000,  463000);
            var start = new WebMercator().ToGeographic(Start.Mp) as MapPoint;
//            rdCenter = new WebMercator().FromGeographic(rdCenter) as MapPoint;

            var srcSr = new SpatialReference(28992);
            var targetSr = new SpatialReference(4326);
            var src = ProjectionInfo.FromEpsgCode(srcSr.WKID);
            var dest = ProjectionInfo.FromEpsgCode(targetSr.WKID);
            var radAngle = (float)(GetAngle(Start.Mp, Finish.Mp) * (Math.PI / 180f));
            
            foreach (var graphic in EffectsModelShapes)
            {
                graphic.SetZIndex(0);
                var baseGraphic = graphic.Attributes["base"] as Graphic;
                var basePoly = baseGraphic.Geometry as Polygon;
                var clone = basePoly.Clone();
                clone.SpatialReference = targetSr;

                List<PointCollection> newRings = new List<PointCollection>();
                foreach (var linestring in clone.Rings)
                {

                    foreach (var point in linestring)
                    {
//                        RotatePoint(point, rdCenter, radAngle);
                        RotatePoint(point, new MapPoint(0,0), radAngle);
                    }

                    var projectedPoints = linestring.SelectMany(c => new double[2] { c.X + 155000, c.Y + 463000 }).ToArray();
                    Reproject.ReprojectPoints(projectedPoints, null, src, dest, 0, projectedPoints.Length / 2);
                    var projectedLineString = new PointCollection(Enumerable.Range(0, projectedPoints.Length / 2).Select(i => new MapPoint(projectedPoints[i * 2], projectedPoints[(i * 2) + 1] , targetSr)));
                    newRings.Add(projectedLineString);
                }
                clone.Rings = new ObservableCollection<PointCollection>(newRings);

                clone.Offset(start.X - rdCenter.X, start.Y - rdCenter.Y);

                graphic.Geometry = clone;
            }
        }

        internal void Remove()
        {
            Layer.ChildLayers.Remove(MLayer);
        }

        internal void UpdatePoint(string state, MapPoint geometry)
        {
            switch (state)
            {
                case "start":

                    Finish.Mp.X += geometry.X - Start.Mp.X;
                    Finish.Mp.Y += geometry.Y - Start.Mp.Y;
                    
                    Start.Mp = geometry;
                    _start.Geometry = geometry;
                    break;
                case "finish":
                    _firstMovement = false;
                    Finish.Mp = geometry;
                    _finish.Geometry = geometry;
                    break;
            }   

            UpdateLine();
            UpdateModelShape();
            WindDirection = GetWindDirection();
        }

        public MapPoint RotatePoint(MapPoint point, MapPoint origin, float angle)
        {
            MapPoint ret = new MapPoint();
//            ret.X = (float)(origin.X + ((point.X - origin.X) * Math.Cos((float)angle)) - ((point.Y - origin.Y) * Math.Sin((float)angle)));
//            ret.Y = (float)(origin.Y + ((point.X - origin.X) * Math.Sin((float)angle)) - ((point.Y - origin.Y) * Math.Cos((float)angle)));
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            ret.X = (point.X*cos) - (point.Y*sin);
            ret.Y = (point.X*sin) + (point.Y*cos);

            point.X = ret.X;
            point.Y = ret.Y;

            return point;
        }
    }
}
