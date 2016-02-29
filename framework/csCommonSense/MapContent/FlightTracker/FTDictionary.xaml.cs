using System;
using System.Collections.Generic;
using System.Windows;
using ESRI.ArcGIS.Client;
using System.Linq;
using csShared;
using csShared.Documents;
using csShared.FloatingElements;


namespace csGeoLayers.FlightTracker
{
    /// <summary>
    /// Interaction logic for RCDictionary.xaml
    /// </summary>
    public partial class FTDDictionary : ResourceDictionary
    {
        public FTDDictionary()
        {
            
        }

        private void MapMenuItem_Tap(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = (FrameworkElement) sender;
            DataBinding g = (DataBinding) fe.DataContext;
            Plane p = (Plane) g.Attributes["plane"];
            GZipWebClient wc = new GZipWebClient();
            wc.DownloadStringCompleted += wc_DownloadStringCompleted;
            wc.DownloadStringAsync(new Uri("http://www.planepictures.net/netsearch4.cgi?srch=" + p.Registration + "&srng=2&stype=reg"),fe);
        }

        void wc_DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            
            if (e.Error == null)
            {
                int p = 0;
                int table = e.Result.IndexOf("</TD></TR></TABLE>", 0,StringComparison.Ordinal)-1000;
                List<string> images = new List<string>();
                while (p < table)
                {
                    int t = e.Result.IndexOf("<TD", p);
                    int s = e.Result.IndexOf(@"<img src=", t);
                    int se = e.Result.IndexOf(@"""", s + 10);
                    string i = e.Result.Substring(s + 10, se - s - 10);
                    if (i!="email.gif")
                    {
                        if (!i.StartsWith("http")) i = "http://www.planepictures.net" + i;
                        
                        
                        images.Add(i);
                    }
                    p = se + 20;
                }
                int px = 200;
                foreach (string i in images.Take(5))
                {
                    Document d = new Document()
                                     {FileType = FileTypes.image, OriginalUrl = i, Location = i, IsCachable = true};
                    FloatingElement fe = FloatingHelpers.CreateFloatingElement(d);
                    fe.StartPosition = new Point(px,200);                    
                    px += 200;
                    fe.StartPosition = ((FrameworkElement) e.UserState).PointToScreen(new Point(0, 0));
                    fe.OriginSize = new Size(0,0);
                    AppStateSettings.Instance.FloatingItems.AddFloatingElement(fe);
                    
                }

            }
        }

        
    }
}
