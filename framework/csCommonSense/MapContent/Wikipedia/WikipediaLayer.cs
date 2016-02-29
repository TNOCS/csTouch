using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using csShared.Geo;

namespace csGeoLayers.Wikipedia
{
    [Serializable]
    public class WikipediaLayer : BaseLayer
    {
        public event EventHandler Loaded;

        public override void Open()
        {
            Name = "Wikipedia";
            Description = "Photo-sharing community. Discover the world through photos";
            Thumbnail = "http://t1.gstatic.com/images?q=tbn:ANd9GcQFAlT9xesF00VzGyqeRddmw-rO-Kdjz5X8Z3hz5EW8Tx7NNE8s";

            Clickable = true;
            State = LayerState.Loaded;

            IsLoaded = true;

            MapControl = typeof(UcWiki);


            if (Loaded != null) Loaded(this, null);
            ActiveChanged += (e, s) => { if (Active) DownloadArticles(); };

            ViewDef.MapManipulationCompleted += ViewDefMapManipulationCompleted;
        }

        private void ViewDefMapManipulationCompleted(object sender, EventArgs e)
        {
            if (Active)
            {
                DownloadArticles();
            }
        }

        private void DownloadArticles()
        {
            var wc = new WebClient();
            wc.DownloadStringCompleted += WcDownloadStringCompleted;
            string url =
                "http://api.geonames.org/wikipediaBoundingBox?north=" +
               Convert.ToString(ViewDef.WorldExtent.MaxX, CultureInfo.InvariantCulture) + "&south=" +
               Convert.ToString(ViewDef.WorldExtent.MinX, CultureInfo.InvariantCulture) + "&east=" +
               Convert.ToString(ViewDef.WorldExtent.MaxY, CultureInfo.InvariantCulture) + "&west=" +
               Convert.ToString(ViewDef.WorldExtent.MinY, CultureInfo.InvariantCulture) + "&username=damylen";
            wc.DownloadStringAsync(new Uri(url));
        }


        public void UpdateLayer()
        {
            //currentFeatures = null;
            ViewDef.ForceRedraw();
            if (Updated != null) Updated(this, null);
        }


        public override event EventHandler Updated;

        public override void OpenAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate { Open(); });
        }

        public override XDocument KmlExport()
        {
            return new XDocument();
        }

        public override void Close()
        {
        }


        private void WcDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            
            if (e.Error == null)
            {
                foreach (WikiFeature pf in CurrentFeatures)
                {
                    pf.Enabled = false;
                }

                XDocument d = XDocument.Parse(e.Result);
                foreach (XElement el in d.Root.Elements())
                {
                    try
                    {
                        WikiFeature wf = new WikiFeature();
                        wf.Name = el.Element("title").Value;
                        wf.Point = new KmlPoint(Convert.ToDouble(el.Element("lng").Value, CultureInfo.InvariantCulture),
                            Convert.ToDouble(el.Element("lat").Value, CultureInfo.InvariantCulture));

                        wf.Summary = el.Element("summary").Value;
                        wf.Url = el.Element("wikipediaUrl").Value;
                        wf.Enabled = true;
                        CurrentFeatures.Add(wf);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error adding wikipedia article");
                    }

                    Console.Write(el.ToString());
                }


                CurrentFeatures.RemoveAll(k => !((WikiFeature)k).Enabled);


                UpdateLayer();
            }
        }

        public override List<IGeoFeature> AllFeatures()
        {
            return CurrentFeatures; // Options.Select(k => (IFeature)k).ToList();            
        }

        /// <summary>
        /// Obtiene el valor de una columna de la fila de resultados JSON
        /// </summary>
        /// <param name="input"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private string GetValueFromTag(string input, string tag)
        {
            try
            {
                int iEnd;
                int iStart = input.IndexOf(tag) + tag.Length + 3;
                if (input.Substring(iStart, 1) == "\"") //texto
                {
                    iStart++;
                    iEnd = input.IndexOf("\"", iStart);
                }
                else //num�rico
                {
                    iEnd = input.IndexOf(",", iStart);
                }
                return input.Substring(iStart, iEnd - iStart);
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}