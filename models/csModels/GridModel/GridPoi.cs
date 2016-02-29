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

namespace csModels.GridModel
{
    class GridPoi : ModelPoiBase
    {
        public GraphicsLayer GridLayer { get; set; }

        private const string DefaultIsAreaFilled = "false";

        public override void Start()
        {
            base.Start();
            // create layer
            if (GridLayer == null) return;
            ViewModel = new GridViewModel(Model.Id, Poi, GridLayer);
            //if (Poi.Labels.ContainsKey(ZoneList.ZoneLabel))
            //{
            //    Zones.FromString(Poi.Labels[ZoneList.ZoneLabel]);
            //}
            //UpdateInfoFromLabels();
            //Poi.LabelChanged += Poi_LabelChanged;
            //Poi.Changed += Poi_Changed;
        }

        public void RemoveGraphics()
        {
            //foreach (var z in Zones.Where(z => z.Graphic != null && ZoneLayer.Graphics.Contains(z.Graphic)))
            //{
            //    ZoneLayer.Graphics.Remove(z.Graphic);
            //}
        }

        public void UpdateGraphics()
        {
            RemoveGraphics();
        }

        public override void Stop()
        {
            base.Stop();

            //DeleteAllZones();
            //todo remove graphics
            //if (ZoneLayer.Children.Contains(image)) ZoneLayer.Children.Remove(image);
        }
    }
}