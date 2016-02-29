using Caliburn.Micro;
using csCommon.Types.Geometries;
using csDataServerPlugin;
using csShared.Utils;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;
using PointCollection = ESRI.ArcGIS.Client.Geometry.PointCollection;
using Polygon = ESRI.ArcGIS.Client.Geometry.Polygon;

namespace csModels.ZoneModel
{
    /// <summary>
    /// Provide the PoI with an option to draw named zones.
    /// 
    /// Input Labels:
    ///     * ModelId.IsAreaFilled
    /// 
    /// Output Labels:
    ///     * ZoneList.ZonesLabel
    /// </summary>
    public class ZonesPoi : ModelPoiBase
    {
        public GraphicsLayer ZoneLayer { get; set; }

        private const string DefaultIsAreaFilled = "false";

        /// <summary>
        /// If true, draw a filled polygon, otherwise a polyline.
        /// </summary>
        private bool IsAreaFilled { get; set; }

        private ZoneList zones = new ZoneList();

        public ZoneList Zones
        {
            get { return zones; }
            set { zones = value; }
        }

        public override void Start()
        {
            base.Start();
            // create layer
            if (ZoneLayer == null) return;
            Zones.CollectionChanged += Zones_CollectionChanged;
            var defaultColor = "Red";
            foreach (var parameter in Model.Model.Parameters.Where(parameter => string.Equals(parameter.Name.ToLower(), "color"))) {
                defaultColor = parameter.Value;
            }
            ViewModel = new ZonesViewModel { DisplayName = Model.Id, Zones = Zones, Poi = Poi, SelectedColor = defaultColor };
            if (Poi.Labels.ContainsKey(ZoneList.ZoneLabel))
            {
                Zones.FromString(Poi.Labels[ZoneList.ZoneLabel]);
            }
            UpdateInfoFromLabels();
            //Poi.LabelChanged += Poi_LabelChanged;
            //Poi.Changed += Poi_Changed;
        }

        /// <summary>
        /// Update the internal state based on the label values. 
        /// </summary>
        private void UpdateInfoFromLabels()
        {
            var label = Model.Id + ".IsAreaFilled";
            if (!Poi.Labels.ContainsKey(label)) Poi.Labels[label] = DefaultIsAreaFilled;
            IsAreaFilled = bool.Parse(Poi.Labels[label]);
        }

        private void Zones_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    UpdateGraphics();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var z in from Zone z in e.OldItems where ZoneLayer.Graphics.Contains(z.Graphic) select z)
                    {
                        ZoneLayer.Graphics.Remove(z.Graphic);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (Zone z in e.NewItems)
                    {
                        AddZone(z);
                    }
                    break;
            }
            //SaveLabel();
        }

        public void SaveLabel()
        {

        }

        public void RemoveGraphics()
        {
            foreach (var z in Zones.Where(z => z.Graphic != null && ZoneLayer.Graphics.Contains(z.Graphic)))
            {
                ZoneLayer.Graphics.Remove(z.Graphic);
            }
        }

        public void UpdateGraphics()
        {
            RemoveGraphics();
            foreach (var z in Zones)
            {
                AddZone(z);
            }
        }

        //private void UpdateAllZones()
        //{
        //    var zz = new ZoneList();
        //    if (!Poi.Labels.ContainsKey("zones")) return;
        //    zz.FromString(Poi.Labels["zones"]);
        //    foreach (var z in zz)
        //    {
        //        if (Zones.All(k => k.Title != z.Title))
        //        {
        //            Zones.Add(z);
        //        }
        //    }

        //    var tbd = Zones.Where(k => !zz.Select(v => v.Title).Contains(k.Title));
        //    foreach (var d in tbd) Zones.Remove(d);
        //}

        private void DeleteAllZones()
        {
            var tbd = (from g in ZoneLayer.Graphics
                       where g.Attributes.ContainsKey("poi") && (Guid)g.Attributes["poi"] == Poi.Id
                       select g).ToList();

            foreach (var g in tbd)
            {
                ZoneLayer.Graphics.Remove(g);
            }
            Zones.Clear();
        }

        private void AddZone(Zone z)
        {
            try
            {
                var tbd = (from g in ZoneLayer.Graphics
                           where g.Attributes.ContainsKey("poi") && (Guid)g.Attributes["poi"] == Poi.Id
                           where g.Attributes.ContainsKey("zone") && (string)g.Attributes["zone"] == z.Title
                           select g).ToList();

                if (tbd.Any()) return;

                Execute.OnUIThread(() =>
                {
                    z.Graphic = new Graphic();
                    z.Graphic.Attributes["poi"] = Poi.Id;
                    z.Graphic.Attributes["zone"] = z.Title;

                    if (IsAreaFilled)
                    {
                        if (z.Color != null)
                        {
                            z.Graphic.Symbol = new SimpleFillSymbol
                            {
                                BorderBrush = Brushes.Black,
                                BorderThickness = 2,
                                Fill = new SolidColorBrush(z.MediaColor(0x30))
                            };
                        }
                        var poly = new Polygon { Rings = new ObservableCollection<PointCollection> { z.Points.ToPointCollection() } };
                        z.Graphic.Geometry = poly;
                    }
                    else
                    {
                        if (z.Color != null)
                        {
                            z.Graphic.Symbol = new LineSymbol
                            {
                                Width = 2,
                                Color = new SolidColorBrush(z.MediaColor())
                            };
                        }
                        var poly = new Polyline { Paths = new ObservableCollection<PointCollection> { z.Points.ToPointCollection() } };
                        z.Graphic.Geometry = poly;
                    }
                    ZoneLayer.Graphics.Add(z.Graphic);
                });
            }
            catch (Exception e)
            {
                Logger.Log("Zone Model", "Error adding zone", e.Message, Logger.Level.Error, true);
            }
        }





        public override void Stop()
        {
            base.Stop();

            DeleteAllZones();
            //todo remove graphics
            //if (ZoneLayer.Children.Contains(image)) ZoneLayer.Children.Remove(image);
        }
    }
}