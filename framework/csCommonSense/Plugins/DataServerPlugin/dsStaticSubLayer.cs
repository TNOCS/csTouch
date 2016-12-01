using csShared;
using csShared.Geo;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace csDataServerPlugin
{
    public class dsStaticSubLayer : GraphicsLayer, ILayerWithParentLayer
    {
        private DateTime lastTap;

        public dsStaticSubLayer()
        {
            //RenderingMode = GraphicsLayerRenderingMode.Static;
            MouseLeftButtonDown += dsStaticSubLayer_MouseLeftButtonDown;

            //AppState.ViewDef.MapControl.MapGesture += MapControlMapGesture;
            SpatialReference = new SpatialReference(4326);
        }

        private readonly List<Graphic> selectedGraphicItems = new List<Graphic>();
        private void dsStaticSubLayer_MouseLeftButtonDown(object sender, GraphicMouseButtonEventArgs e)
        {
            // NOTE DO NOT REMOVE: ALTHOUGH EMPTY, WHEN NOT PRESENT, THE CLICK EVENT IS IGNORED!
            var g = e.Graphic;
            g.Select();
            foreach (var selectedGraphic in selectedGraphicItems.ToArray())
            {
                selectedGraphic.UnSelect();
            }
            selectedGraphicItems.Clear();
            selectedGraphicItems.Add(g);
        }


        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public GroupLayer Parent { get; set; }

        private void MapControlMapGesture(object sender, Map.MapGestureEventArgs e)
        {
            try
            {
                if (e.Gesture != GestureType.Tap) return;
                if (lastTap.AddMilliseconds(100) >= DateTime.Now) return;
                lastTap = DateTime.Now;

                var gg = FindGraphicsInHostCoordinates(e.GetPosition(AppState.ViewDef.MapControl));
                foreach (var g in gg)
                {
                    var graphic = g as StaticGraphic;
                    if (graphic != null)
                    {
                        var sg = graphic;
                        if (sg.Poi.NEffectiveStyle.TapMode != null && sg.Poi.NEffectiveStyle.TapMode.Value != TapMode.None)
                        {
                            sg.TappedByExternalMapControlMapGesture(e.MapPoint);
                        }
                    }
                    else if (g.Attributes.ContainsKey("staticgraphic"))
                    {
                        var sg = (StaticGraphic)g.Attributes["staticgraphic"];
                        if (sg.Poi.NEffectiveStyle.TapMode != null && sg.Poi.NEffectiveStyle.TapMode.Value != TapMode.None)
                        {
                            sg.TappedByExternalMapControlMapGesture(e.MapPoint);
                        }
                    }
                }
            }
            catch (Exception et)
            {
                Logger.Log("Static Layer", "Poi Click Error", et.Message, Logger.Level.Error);
            }
        }

        internal void Start()
        {
            AppState.ViewDef.MapControl.MapGesture += MapControlMapGesture;

        }

        internal void Stop()
        {
            AppState.ViewDef.MapControl.MapGesture -= MapControlMapGesture;

            foreach (PoiGraphic pg in Graphics.Where(k => k is PoiGraphic)) pg.Stop();
            ClearGraphics();
        }
    }
}