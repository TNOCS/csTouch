using Caliburn.Micro;
using csDataServerPlugin;
using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using System.Collections.ObjectModel;
using System.Threading;

namespace csModels.TrackModel
{
    public class TrackPoi : ModelPoiBase
    {
        AppStateSettings AppState { get { return AppStateSettings.Instance; } }
        public GraphicsLayer TrackLayer { get; set; }

        public Graphic Track { get; set; }

        public TrackPoi()
        {

        }

        Polyline p = new Polyline();

        public override void Start()
        {
            base.Start();
            //Track = new Graphic();

            //SimpleLineSymbol sls = new SimpleLineSymbol();
            //sls.Color = new SolidColorBrush(Colors.Blue);
            //sls.Style = SimpleLineSymbol.LineStyle.Solid;
            //sls.Width = 2;
            //UpdateTrackPath();

            ////p.SpatialReference = new SpatialReference(102100);
            ////Track.Geometry = p;
            //Track.Symbol = sls;

            //TrackLayer.Graphics.Add(Track);
            AppState.TimelineManager.FocusTimeThrottled += TimelineManager_FocusTimeThrottled;
        }

        // TODO Probably redundant, since the keys should be with capital L.
        void TimelineManager_FocusTimeThrottled(object sender, csShared.Interfaces.TimeEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (Poi.Sensors.ContainsKey("[lat]") && Poi.Sensors.ContainsKey("[lon]"))
                {
                    Execute.OnUIThread(() =>
                    {
                        Poi.Position.Latitude = Poi.Sensors["[lat]"].FocusValue;
                        Poi.Position.Longitude = Poi.Sensors["[lon]"].FocusValue;
                        Poi.TriggerPositionChanged();
                        //UpdateTrackPath();
                    });

                }
            });

        }

        private readonly WebMercator webMercator = new WebMercator();

        private void UpdateTrackPath()
        {
            var pointCollection = new ESRI.ArcGIS.Client.Geometry.PointCollection();
            var rings = new ObservableCollection<ESRI.ArcGIS.Client.Geometry.PointCollection>();

            foreach (var l in Poi.Sensors["[lat]"].Data)
            {
                if (Poi.Sensors["[lon]"].Data.ContainsKey(l.Key))
                {
                    var pos = new MapPoint(Poi.Sensors["[lon]"].Data[l.Key], Poi.Sensors["[lat]"].Data[l.Key]);
                    pointCollection.Add((MapPoint)webMercator.FromGeographic(pos));

                }
            }
            rings.Add(pointCollection);
            p.Paths = rings;

            Track.Geometry = p;

            //Track.Geometry = line;

        }


        public override void Stop()
        {
            base.Stop();
            AppState.TimelineManager.FocusTimeThrottled -= TimelineManager_FocusTimeThrottled;
            Execute.OnUIThread(() =>
            {
                if (TrackLayer.Graphics.Contains(Track)) TrackLayer.Graphics.Remove(Track);
            });
        }
    }
}
