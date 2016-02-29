#region

using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Utils;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;

#endregion

namespace csCommon.MapPlugins.FindLocation
{
    [Export(typeof (IFindLocation))]
    public class FindLocationViewModel : Screen, IFindLocation
    {
        private string _keyword = "";
        private BindableCollection<GeoCodeSearchResult> _result = new BindableCollection<GeoCodeSearchResult>();
        private string selectedLocation;

        public FindLocationViewModel()
        {
            // Default caption
            Caption = "Find";
        }

        public string SelectedLocation
        {
            get { return selectedLocation; }
            set
            {
                selectedLocation = value;
                NotifyOfPropertyChange(() => SelectedLocation);
            }
        }


        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }


        public string Keyword
        {
            get { return _keyword; }
            set
            {
                _keyword = value;
                NotifyOfPropertyChange(() => Keyword);
                if (TextChanged != null) TextChanged(this, null);
            }
        }


        public string Caption { get; set; }

        public BindableCollection<GeoCodeSearchResult> Result
        {
            get { return _result; }
            set
            {
                _result = value;
                NotifyOfPropertyChange(() => Result);
            }
        }

        public event EventHandler<SearchEventArgs> TextChanged;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            IObservable<EventPattern<SearchEventArgs>> eventAsObservable = Observable.FromEventPattern
                <SearchEventArgs>(ev => TextChanged += ev, ev => TextChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(400)).Subscribe(k =>DoRequest());
        }


        public void Find(GeoCodeSearchResult item)
        {
            //var c = new Point(item.Location.Longitude, item.Location.Latitude);
            //double dis = Distance(item.Box.North, item.Box.East, item.Box.North, itemwm.Box.West, 'K');
            var wm = new WebMercator();

            var e = (Envelope) wm.FromGeographic(new Envelope(item.Box.West, item.Box.North, item.Box.East, item.Box.South));
            //var e = new Envelope(item.Box.West, item.Box.North, item.Box.East, item.Box.South);
            //e.SpatialReference = new SpatialReference(4326);

            AppState.ViewDef.NextEffect = true;
            AppState.ViewDef.MapControl.Extent = e;
            SelectedLocation = item.Name;
            //AppState.ViewDef.ZoomTo(new KmlPoint(c.X, c.Y), dis*1000);
        }

        public void SetKeywordFocus(TouchEventArgs e)
        {
            var textBox = (TextBox) e.Source;
            textBox.Focus();
        }


        public void DoRequest()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    var wc = new WebClient();
                    string uri =
                        "https://maps.google.com/maps/api/geocode/xml?address=" +
                        Keyword + "&sensor=false";
                    string res =
                        wc.DownloadString(uri);
                    // "http://maps.google.com/maps/geo?output=xml&q=" + Keyword + "&key=ABQIAAAA61Zfghqchn1_2tR1VCcfbxRDK-cVyu9nXPtoS5FDSKel16cd2BS816uIQiq1y6oHZZ5qRowceF4eNA&oe=utf8&gl=nl"

                    Application.Current.Dispatcher.Invoke(
                        delegate { ParseResult(res); });
                }
                catch (Exception e)
                {
                    Logger.Log("Geocoding", "Error finding location", e.Message,
                        Logger.Level.Error);
                }
            });
        }

        public void ParseResult(string res)
        {
            if (res != string.Empty)
            {
                try
                {
                    Result.Clear();
                    XDocument doc = XDocument.Parse(res);
                    XElement kml = doc.Elements().First();
                    string status = kml.Element("status").Value;
                    if ( status == "OK")
                    {
                        foreach (XElement xe in kml.Elements("result"))
                        {
                            var gcsr = new GeoCodeSearchResult
                                    {
                                        Name = xe.GetFirstElement("formatted_address").Value
                                    };
                            XElement ed = xe.GetFirstElement("geometry");
                            if ( ed != null)
                            {
                                XElement llb = ed.GetFirstElement("viewport");
                                XElement sw = llb.Element("southwest");
                                XElement ne = llb.Element("northeast");
                                
                                {
                                    double north = Convert.ToDouble(ne.Element("lat").Value,CultureInfo.InvariantCulture);
                                    double south = Convert.ToDouble(sw.Element("lat").Value,CultureInfo.InvariantCulture);
                                    double west = Convert.ToDouble(sw.Element("lng").Value,CultureInfo.InvariantCulture);
                                    double east = Convert.ToDouble(ne.Element("lng").Value,CultureInfo.InvariantCulture);
                                    gcsr.Box=new LatLonBox(north,south,east,west);
                                    Result.Add(gcsr);
                                }
                            }
                            //string [] locs = xe.GetFirstElement("Image").GetFirstElement("coordinates").Value.Split(',');
                            //if (locs.Count() ==3)
                            //{
                            //    gcsr.Location = new KmlPoint { Latitude=(float)Convert.ToDouble(locs[1],CultureInfo.InvariantCulture),
                            //                  Longitude=(float)Convert.ToDouble(locs[0],CultureInfo.InvariantCulture),};
                            //    Result.Add(gcsr);
                            //}
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Geocoding", "Error doing geocoding request : " + e.Message, "", Logger.Level.Error);
                }
                NotifyOfPropertyChange(() => Result);
            }
        }

        private double Distance(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1))*Math.Sin(deg2rad(lat2)) +
                          Math.Cos(deg2rad(lat1))*Math.Cos(deg2rad(lat2))*Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist*60*1.1515;
            if (unit == 'K') dist = dist*1.609344;
            else if (unit == 'N') dist = dist*0.8684;
            return (dist);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts decimal degrees to radians             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double deg2rad(double deg)
        {
            return (deg*Math.PI/180.0);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts radians to decimal degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double rad2deg(double rad)
        {
            return (rad/Math.PI*180.0);
        }

        public class SearchEventArgs : EventArgs
        {
        }
    }
}