using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;

namespace csDataServerPlugin
{
    public class dsOverlayLayer : ElementLayer, IdsChildLayer
    {

        public dsOverlayLayer(IServiceLayer layer)
        {
            parent = layer;
        }

        public IServiceLayer parent
        {
            get; set;            
        }

        public void AddPoi(PoI p)
        {
            UIElement uiElement;


            ImageSource imageSource = p.NEffectiveStyle.Picture;
    
            double opacity = p.NEffectiveStyle.FillOpacity.HasValue ? p.NEffectiveStyle.FillOpacity.Value : 1.0;

            uiElement = new PoiImageOverlay
            {
                Poi = p,
                Source = imageSource,
                Stretch = Stretch.Fill,
                Opacity = opacity,
                Name = "testImage",
                Tag = "testImage"
            };
            

           

            // Set the rotation
            if (p.Orientation != 0.0)
            {
                uiElement.RenderTransformOrigin = new Point(0.5, 0.5);
                uiElement.RenderTransform = new RotateTransform { Angle = p.Orientation }; // KML rotations are specified in a counterclockwise direction
            }

            // Set the envelope
            var elementLayerEnvelopeProperty = ElementLayer.EnvelopeProperty;
            if (p.Points.Count == 2)
            {
                var envelope = new Envelope(new MapPoint(p.Points[0].X, p.Points[0].Y),
                    new MapPoint(p.Points[1].X, p.Points[1].Y));
                var projection = new WebMercator();
                envelope = (Envelope) projection.FromGeographic(envelope) as Envelope;

                uiElement.SetValue(elementLayerEnvelopeProperty, envelope);
                p.Data["Image"] = uiElement;
                p.Data["layer"] = this;
                // Add element to element layer
                this.Children.Add(uiElement);
            }
        }

        public void UpdatePoi(PoI p)
        {
            RemovePoi(p);
            AddPoi(p);
        }

        public void RemovePoi(PoI p)
        {
            Execute.OnUIThread(() =>
            {
                if (!p.Data.ContainsKey("layer")) return;
                var gl = p.Data["layer"] as ElementLayer;
                if (gl == null) return;
                if (p.Data.ContainsKey("layer")) p.Data.Remove("layer");
                if (p.Data.ContainsKey("Image"))
                {
                    if (gl.Children.Contains((UIElement) p.Data["Image"]))
                    {
                        gl.Children.Remove((UIElement) p.Data["Image"]);
                    }
                }
                
                
            }); 
        }
    }
}