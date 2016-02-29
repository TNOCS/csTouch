using Caliburn.Micro;
using csGeoLayers.MapTools.MeasureTool;
using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;
using Polyline = ESRI.ArcGIS.Client.Geometry.Polyline;

namespace csCommon.MapPlugins.MapTools.MeasureTool
{
    public class Measure : PropertyChangedBase
    {
        public GroupLayer Layer;
        public Graphic _start;
        public Graphic _finish;
        public Graphic Line;

        public csPoint Start;
        public csPoint Finish;

        public GraphicsLayer MLayer;

        private bool _firstMovement = true;

        private double _distance;

        public double Distance
        {
            get { return _distance; }
            set { _distance = value; NotifyOfPropertyChange(() => Distance); }
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

        public void Init(GroupLayer gl, MapPoint start, MapPoint finish, ResourceDictionary rd)
        {
            Start = new csPoint() { Mp = start };
            Finish = new csPoint() { Mp = finish };
            MLayer = new GraphicsLayer() { ID = Guid.NewGuid().ToString() };
            _start = new Graphic();
            _finish = new Graphic();
            Line = new Graphic();

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
            _start.Attributes["measure"] = this;
            _start.Attributes["menuenabled"] = true;
            MLayer.Graphics.Add(_start);

            _finish.Geometry = finish;
            _finish.Attributes["position"] = finish;
            _finish.Symbol = rd["Finish"] as Symbol;
            _finish.Attributes["finish"] = _finish;
            _finish.Attributes["start"] = _start;
            _finish.Attributes["line"] = Line;
            _finish.Attributes["measure"] = this;
            _finish.Attributes["state"] = "finish";
            MLayer.Graphics.Add(_finish);



            Layer.ChildLayers.Add(MLayer);
            MLayer.Initialize();

            AppStateSettings.Instance.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

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

        internal void Remove()
        {
            Layer.ChildLayers.Remove(MLayer);
        }

        internal void UpdatePoint(string state, MapPoint geometry)
        {
            switch (state)
            {
                case "start":

                    if (_firstMovement)
                    {
                        Finish.Mp.X += geometry.X - Start.Mp.X;
                        Finish.Mp.Y += geometry.Y - Start.Mp.Y;
                    }
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
            Distance = GetDistance();
        }
    }
}