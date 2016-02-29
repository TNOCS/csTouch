using System;
using System.Collections.Generic;
using System.Windows;
using csGeoLayers.Kml;
using csShared.Geo;
using csShared.Interfaces;

namespace csGeoLayers.Wikipedia
{
    [Serializable]
    public class WikiFeature : IGeoFeature
    {
        public Rect Boundary;
        public Dictionary<string, Object> DbInfo = new Dictionary<string, object>();
        private bool _cluster;
        private int _height;
        private int _width;

        private string _url;

        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }


        private string _summary;

        public string Summary
        {
            get { return _summary; }
            set { _summary = value; }
        }


        public string Source { get; set; }

        public bool Selected { get; set; }

        public IGeoLayer Layer { get; set; }

        public bool Enabled { get; set; }

        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }


        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }


        public KmlPoint Point { get; set; }
        public KmlPoint Position { get; set; }

        #region IGeoFeature Members

        public bool Cluster
        {
            get { return _cluster; }
            set { _cluster = value; }
        }

        public int TimelinePriority { get; set; }

        public List<KmlPoint> Points
        {
            get
            {
                var result = new List<KmlPoint>();
                if (Point != null) result.Add(Point);
                return result;
            }
        }

        public bool InTime(ITimelineManager t, IGeoLayer l)
        {
            return true;
        }

        public string ImageUrl { get; set; }

        public object Tag { get; set; }

        public string Id { get; set; }

        public bool Visible { get; set; }

        public FrameworkElement Element { get; set; }

        public Rect BoundingBox { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public bool Listed { get; set; }


        public KmlPoint Center { get; set; }

        public DateTime Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }


        public bool InView(MapViewDef t)
        {
            return true;
            //return t.Contains(this.Point);
        }


        public Type MapControlType { get; set; }

        #endregion

        public event EventHandler Updated;

        public void ForceUpdate()
        {
            if (Updated != null) Updated(this, null);
        }

        public Rect Boundaries()
        {
            if (Boundary == new Rect())
            {
                var b = new Boundary();
                if (b.Leftup != null && b.Rightdown != null)
                {
                    Boundary = new Rect(new Point(b.Leftup.Latitude, b.Leftup.Longitude),
                                        new Point(b.Rightdown.Latitude, b.Rightdown.Longitude));
                }
            }
            return Boundary;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}