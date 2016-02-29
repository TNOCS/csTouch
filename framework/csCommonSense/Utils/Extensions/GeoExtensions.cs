using System.Reactive;
using csDataServerPlugin;
using csGeoLayers;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using Microsoft.Surface.Presentation.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace csShared.Utils
{
    public static class GeometryExtensionMethods
    {
        public static readonly WebMercator MyWebMercator = new WebMercator();

        public static Position ToPosition(this MapPoint point)
        {
            var p = MyWebMercator.ToGeographic(point) as MapPoint;
            return p == null ? null : new Position(p.X, p.Y);
        }

        public static Point ToPoint(this MapPoint point)
        {
            var p = MyWebMercator.ToGeographic(point) as MapPoint;
            return p == null ? new Point() : new Point(p.X, p.Y);
        }

        public static MapPoint ToMapPoint(this Point point)
        {
            return MyWebMercator.FromGeographic(new MapPoint(point.X, point.Y)) as MapPoint;
        }

        public static void Offset(this Graphic graphic, double deltaX, double deltaY)
        {
            var geometry = graphic.Geometry;
            geometry.RequireArgument("geometry").NotNull();

            var poi = graphic.Attributes["PoI"] as PoI;
            if (poi == null) return;

            geometry.Offset(poi, deltaX, deltaY);
            poi.Updated = DateTime.Now;
        }

        public static void Offset(this Geometry geometry, PoI poi, double deltaX, double deltaY)
        {
            geometry.RequireArgument("geometry").NotNull();

            if (geometry is MapPoint)
            {
                var mapPoint = geometry as MapPoint;
                mapPoint.Offset(deltaX, deltaY);
                poi.Position = mapPoint.ToPosition();
            }
            else if (geometry is MultiPoint)
            {
                var multiPoint = geometry as MultiPoint;
                multiPoint.Offset(deltaX, deltaY);
                poi.Points.Clear();
                multiPoint.Points.ForEach(p => poi.Points.Add(p.ToPoint()));
            }
            else if (geometry is Polyline)
            {
                var polyline = geometry as Polyline;
                polyline.Offset(deltaX, deltaY);
                poi.Points.Clear();
                polyline.Paths.ForEach(pts => pts.ForEach(p => poi.Points.Add(p.ToPoint())));
            }
            else if (geometry is Polygon)
            {
                var polygon = geometry as Polygon;
                polygon.Offset(deltaX, deltaY);
                poi.Points.Clear();
                polygon.Rings.ForEach(pts => pts.ForEach(p => poi.Points.Add(p.ToPoint())));
            }
        }

        public static void Offset(this MapPoint point, double deltaX, double deltaY)
        {
            point.RequireArgument("point").NotNull();

            point.X = point.X + deltaX;
            point.Y = point.Y + deltaY;
        }

        public static void Offset(this MultiPoint multiPoint, double deltaX, double deltaY)
        {
            multiPoint.RequireArgument("multiPoint").NotNull();

            multiPoint.Points.ForEach(p => p.Offset(deltaX, deltaY));
        }

        public static void Offset(this Polyline polyline, double deltaX, double deltaY)
        {
            polyline.RequireArgument("polyline").NotNull();

            polyline.Paths.ForEach(pts => pts.ForEach(pt => pt.Offset(deltaX, deltaY)));
        }

        public static void Offset(this Polygon polygon, double deltaX, double deltaY)
        {
            polygon.RequireArgument("polygon").NotNull();

            polygon.Rings.ForEach(pts => pts.ForEach(pt => pt.Offset(deltaX, deltaY)));
        }
    }

    public class Disposables : IDisposable
    {
        public Disposables()
        {
            Subscriptions = new List<IDisposable>();
        }

        public List<IDisposable> Subscriptions { get; private set; }

        public void Dispose()
        {
            if (Subscriptions != null)
            {
                Subscriptions.ForEach(i => { if (i != null) i.Dispose(); });
            }
        }
    }

    public interface IPropertyValue<T>
    {
        string Name { get; set; }
        T Value { get; set; }
    }

    public class ArgumentPropertyValue<T> : IPropertyValue<T>, INotifyPropertyChanged
    {
        private string _name;
        private T _value;

        public ArgumentPropertyValue(string name, T val)
            : this()
        {
            _name = name;
            _value = val;
        }

        public ArgumentPropertyValue()
        {
        }

        #region IPropertyValue<T> Members

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public T Value
        {
            get { return _value; }
            set
            {
                if (!_value.Equals(value))
                {
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public static class GeoExtensions
    {
        /// <summary>
        ///     Convert the envelope to a list of four points, i.e. I don't close the surface
        ///     (last and first point are NOT the same).
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public static List<Point> ConvertToPoints(this Envelope envelope)
        {
            return new List<Point>
            {
                new Point(envelope.XMin, envelope.YMax), // Top left
                new Point(envelope.XMax, envelope.YMax), // Top right
                new Point(envelope.XMax, envelope.YMin), // Bottom right
                new Point(envelope.XMin, envelope.YMin) // Bottom left
            };
        }

        public static ArgumentPropertyValue<T> NotNull<T>(this ArgumentPropertyValue<T> item) where T : class
        {
            if (item.Value == null)
            {
                throw new ArgumentNullException(item.Name);
            }
            return item;
        }

        public static ArgumentPropertyValue<T> RequireArgument<T>(this T item, string argName)
        {
            return new ArgumentPropertyValue<T>(argName, item);
        }

        public static IDisposable MakeDraggable(this Graphic graphic)
        {
            var map = AppStateSettings.Instance.ViewDef.MapControl;
            graphic.RequireArgument("graphic").NotNull();
            map.RequireArgument("map").NotNull();

            var mapMouseMove = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                ev => map.MouseMove += ev,
                ev => map.MouseMove -= ev);

            var graphicLeftMouseButtonDown = Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                ev => graphic.MouseLeftButtonDown += ev,
                ev => graphic.MouseLeftButtonDown -= ev);

            var mapLeftMouseButtonUp = Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                ev => map.MouseLeftButtonUp += ev,
                ev => map.MouseLeftButtonUp -= ev);

            var mouseUp = mapLeftMouseButtonUp.Select(evt => map.ScreenToMap(evt.EventArgs.GetPosition(map)));
            var mouseDown = graphicLeftMouseButtonDown.Select(evt => map.ScreenToMap(evt.EventArgs.GetPosition(map)));
            var mouseMove = mapMouseMove.Select(evt => map.ScreenToMap(evt.EventArgs.GetPosition(map)));
            //Gets the delta of positions.
            var mouseMovements = mouseMove.Zip(mouseMove.Skip(1),
                (prev, curr) => new { X = curr.X - prev.X, Y = curr.Y - prev.Y });
            //Only streams when mouse is down
            var dragging = from md in mouseDown
                           from mm in mouseMovements.TakeUntil(mouseUp)
                           select mm;

            //var mapTouchDown = Observable.FromEventPattern<EventHandler<TouchEventArgs>, TouchEventArgs>(
            //    ev => map.TouchDown += ev,
            //    ev => map.TouchDown -= ev);

            //var mapTouchMove = Observable.FromEventPattern<EventHandler<TouchEventArgs>, TouchEventArgs>(
            //    ev => map.TouchMove += ev,
            //    ev => map.TouchMove -= ev);

            //var mapTouchLeave = Observable.FromEventPattern<EventHandler<TouchEventArgs>, TouchEventArgs>(
            //    ev => map.TouchLeave += ev,
            //    ev => map.TouchLeave -= ev);

            var removeHandlers = new Disposables();
            removeHandlers.Subscriptions.Add(graphicLeftMouseButtonDown.Subscribe(e => e.EventArgs.Handled = true));
            //removeHandlers.Subscriptions.Add(mapTouchDown.Subscribe(e => StartCapturingTouches(graphic, e, map)));
            //removeHandlers.Subscriptions.Add(mouseUp.Subscribe(mm =>
            //{
            //    var poiGraphic = graphic as PoiGraphic;
            //    if (poiGraphic != null) poiGraphic.Tapped(mm);
            //}));
            removeHandlers.Subscriptions.Add(dragging.Subscribe(mm => graphic.Offset(mm.X, mm.Y)));
            return removeHandlers;
        }

        public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            where TResult : class
        {
            return source.Select(selector)
                .Where(sequence => sequence != null)
                .SelectMany(x => x)
                .Where(item => item != null);
        }

        private static int counter = 0;
        private static MapPoint StartCapturingTouches(Graphic graphic, EventPattern<TouchEventArgs> evt, Map map)
        {
            var poiGraphic = graphic as PoiGraphic;
            if (poiGraphic == null) return null;
            var layer = poiGraphic.Layer;
            if (layer == null) return null;
            var point = evt.EventArgs.GetTouchPoint(map).Position;
            if (layer is GraphicsLayer)
            {
                var gg = ((GraphicsLayer)layer).FindGraphicsInHostCoordinates(point);
                if (!gg.Contains(graphic)) return null;
            }
            else return null;

            var touchDevice = evt.EventArgs.TouchDevice;
            if (touchDevice == null) return null;
            map.CaptureTouch(touchDevice);

            //touchDevice.Updated += touchDevice_Updated;

            var mapTouchUpdated = Observable.FromEventPattern<EventHandler, EventArgs>(
               ev => touchDevice.Updated += ev,
               ev => touchDevice.Updated -= ev);
            var mapTouchLeave = Observable.FromEventPattern<EventHandler<TouchEventArgs>, TouchEventArgs>(
                ev => map.TouchLeave += ev,
                ev => map.TouchLeave -= ev);

            var touchMove = mapTouchUpdated.Select(evt2 => ConvertTouchesToMapPoints(evt2.Sender, map));
            var touchUp = mapTouchLeave.Select(evt2 => StopCapturingTouches(evt2, map));

            //Gets the delta of positions.
            var touchMovements = touchMove.Zip(touchMove.Skip(1), (prev, curr) => prev == null || curr == null
                ? new {X = 0d, Y = 0d} 
                : new {X = curr.X - prev.X, Y = curr.Y - prev.Y});

            //Only streams when mouse is down
            var touchDragging = from mm in touchMovements
                                    //.Buffer(TimeSpan.FromMilliseconds(100))
                                    //.Select(m2 => new { X = m2.Select(m => m.X).Sum(), Y = m2.Select(m=>m.Y).Sum() })
                                    .TakeUntil(touchUp)
                                select mm;
            var removeHandlers = new Disposables();
            removeHandlers.Subscriptions.Add(touchDragging.Subscribe(mm => graphic.Offset(mm.X, mm.Y)));
            //removeHandlers.Subscriptions.Add(mapTouchLeave.Subscribe(mm => {
            //    evt.EventArgs.Handled = true;
            //    map.ReleaseTouchCapture(evt.EventArgs.TouchDevice);
            //}));

            evt.EventArgs.Handled = true;
            return map.ScreenToMap(point);
            //return new { TouchDevice = touchDevice, Point = map.ScreenToMap(point) };
        }

        private static MapPoint StopCapturingTouches(IEventPattern<object, TouchEventArgs> evt, Map map)
        {
            var touchDevice = evt.EventArgs.TouchDevice;
            if (!map.TouchesCaptured.Contains(touchDevice)) return null;
            var point = touchDevice.GetCenterPosition(map);
            evt.EventArgs.Handled = true;
            map.ReleaseTouchCapture(touchDevice);
            return map.ScreenToMap(point);
        }

        private static MapPoint ConvertTouchesToMapPoints(object sender, Map map)
        {
            var touchDevice = sender as TouchDevice;
            if (touchDevice == null) return null;
            if (!map.TouchesCaptured.Contains(touchDevice)) return null;
            Console.WriteLine(counter++);
            var point = touchDevice.GetCenterPosition(map);
            return map.ScreenToMap(point);
        }

    }
}