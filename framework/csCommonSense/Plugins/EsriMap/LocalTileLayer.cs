using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace csGeoLayers.Plugins.LocalTileLayer
{
    public class LocalTileLayer : TiledMapServiceLayer
    {
        public string Path { get; set; }

        public override string GetTileUrl(int level, int row, int col)
        {
            var s = new Uri(@"C:\temp\tnosmartcity\4\4\1.png").LocalPath; //C:\\temp\\tnosmartcity\\" + level + "\\" + row + "\\" + col + ".png").LocalPath;
            return File.Exists(s) ? s : null;
        }

        /// <summary>Simple constant used for full extent and tile origin specific to this projection.</summary>
        private const double cornerCoordinate = 20037508.3427892;

        /// <summary>ESRI Spatial Reference ID for Web Mercator.</summary>
        private const int WKID = 102100;

        protected override void GetTileSource(int level, int row, int col, Action<ImageSource> onComplete)
        {
            var max = (int) Math.Pow(2, level) - 1 - row;
            var s = Path + "\\" + level + "\\" + col + "\\" + max + ".png";
            if (!File.Exists(s)) return;
            var ims = new BitmapImage(new Uri(s));
            onComplete(ims);
        }

        public override void Initialize()
        {
            FullExtent = new Envelope(-cornerCoordinate, -cornerCoordinate, cornerCoordinate, cornerCoordinate)
            {
                SpatialReference = new SpatialReference(WKID)
            };
            //This layer's spatial reference
            //Set up tile information. Each tile is 256x256px, 19 levels.
            TileInfo = new TileInfo
            {
                Height = 256,
                Width = 256,
                Origin = new MapPoint(-cornerCoordinate, cornerCoordinate) { SpatialReference = new SpatialReference(WKID) },
                Lods = new Lod[6]
            };
            //Set the resolutions for each level. Each level is half the resolution of the previous one.
            var resolution = cornerCoordinate * 2 / 256;
            for (var i = 0; i < TileInfo.Lods.Length; i++)
            {
                TileInfo.Lods[i] = new Lod { Resolution = resolution };
                resolution /= 2;
            }
            //Call base initialize to raise the initialization event
            base.Initialize();

            base.Initialize();
        }
    }
}
