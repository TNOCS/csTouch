using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.Utils
{
    /// <summary>
    /// Clusterer based on a certain PoI label. 
    /// Note that the Label needs to contain a number in order to sum it.
    /// </summary>
    public class SumClusterer : GraphicsClusterer
    {
        public SumClusterer(string aggregateLabel)
        {
            MinimumColor = Colors.Red;
            MaximumColor = Colors.Yellow;
            SymbolScale = 1;
            Radius = 50;
            AggregateLabel = aggregateLabel;
        }

        public string AggregateLabel { get; set; }
        public double SymbolScale { get; set; }
        public Color MinimumColor { get; set; }
        public Color MaximumColor { get; set; }

        protected override Graphic OnCreateGraphic(GraphicCollection cluster, MapPoint point, int maxClusterCount)
        {
            if (cluster.Count == 1) return cluster[0];

            double sum = 0;

            foreach (var g in cluster) {
                if (!g.Attributes.ContainsKey("Poi")) continue;
                try {
                    var poi = g.Attributes["Poi"] as PoI;
                    if (poi == null) continue;
                    if (poi.Labels.ContainsKey(AggregateLabel))
                        sum += Convert.ToDouble(poi.Labels[AggregateLabel]);
                }
                catch { }
            }
            //double size = (sum + 450) / 30;
            var size = (Math.Log(sum * SymbolScale / 10) * 10 + 20);
            if (size < 12) size = 12;
            var graphic = new Graphic
            {
                Symbol = new ClusterSymbol { Size = size },
                Geometry = point
            };
            graphic.Attributes.Add("Count", sum);
            graphic.Attributes.Add("Size", size);
            graphic.Attributes.Add("Color", InterpolateColor(size - 12, 100));
            return graphic;
        }

        private static Brush InterpolateColor(double value, double max)
        {
            value = (int)Math.Round(value * 255.0 / max);
            if (value > 255) value = 255;
            else if (value < 0) value = 0;
            return new SolidColorBrush(Color.FromArgb(127, 255, (byte)value, 0));
        }
    }

    internal class ClusterSymbol : ESRI.ArcGIS.Client.Symbols.MarkerSymbol
    {
        public ClusterSymbol() {
            const string template = @"
        <ControlTemplate xmlns=""http://schemas.microsoft.com/client/2007"" 
                         xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" 
                         xmlns:esriConverters=""clr-namespace:ESRI.ArcGIS.Client.ValueConverters;assembly=ESRI.ArcGIS.Client"">
            <Grid IsHitTestVisible=""False"">
                <Grid.Resources>
                    <esriConverters:DictionaryConverter x:Key=""MyDictionaryConverter"" />
                </Grid.Resources>
                <Ellipse
                    Fill=""{Binding Attributes, Converter={StaticResource MyDictionaryConverter}, ConverterParameter=Color}"" 
                    Width=""{Binding Attributes, Converter={StaticResource MyDictionaryConverter}, ConverterParameter=Size}""
                    Height=""{Binding Attributes, Converter={StaticResource MyDictionaryConverter}, ConverterParameter=Size}"" />
                <Grid HorizontalAlignment=""Center"" VerticalAlignment=""Center"">
                    <TextBlock 
                        Text=""{Binding Attributes, Converter={StaticResource MyDictionaryConverter}, ConverterParameter=Count}"" 
                        FontSize=""19"" Margin=""1,1,0,0"" FontWeight=""Bold""
                        Foreground=""#99000000"" />
                    <TextBlock
                        Text=""{Binding Attributes, Converter={StaticResource MyDictionaryConverter}, ConverterParameter=Count}"" 
                        FontSize=""19"" Margin=""0,0,1,1"" FontWeight=""Bold""
                        Foreground=""White"" />
                </Grid>
            </Grid>
        </ControlTemplate>";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(template)))
            {
                ControlTemplate = System.Windows.Markup.XamlReader.Load(stream) as ControlTemplate;
            }

        }

        public double Size { get; set; }
        public override double OffsetX
        {
            get
            {
                return Size / 2;
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        public override double OffsetY
        {
            get
            {
                return Size / 2;
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}