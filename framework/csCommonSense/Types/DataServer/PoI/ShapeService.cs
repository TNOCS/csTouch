using System.IO;
using Catfood.Shapefile;
using csCommon.Utils.IO;
using csDataServerPlugin;
using csDataServerPlugin.Extensions;
using csShared;
using csShared.Utils;
using DataServer;
using System;
using System.Linq;
using Extensions = csCommon.Types.Geometries.Extensions;

namespace csCommon.Types.DataServer.PoI
{
    public class ShapeService : ExtendedPoiService, IImporter<FileLocation, PoiService>
    {
        #region ExtendedPoiService

        public static ShapeService CreateShapeService(string name, Guid id, string folder = "", string relativeFolder = "")
        {
            var res = new ShapeService
            {
                IsLocal        = true,
                Name           = name,
                Id             = id,
                IsFileBased    = false,
                StaticService  = true,
                AutoStart      = false,
                HasSensorData  = true,
                Mode           = Mode.client,
                RelativeFolder = relativeFolder,
            };

            res.Init(Mode.client, AppState.DataServer);
            res.Folder = folder;
            res.InitPoiService();

            res.Settings.OpenTab = false;
            res.Settings.Icon = "layer.png";
            AppState.DataServer.Services.Add(res);
            return res;
        }

        protected override Exception ProcessFile()
        {
            try
            {
                //PoIs.StartBatch();
                using (var shapefile = new Catfood.Shapefile.Shapefile(File))
                {
                    var projection = shapefile.GetCoordinateType(File);
                    foreach (var shape in shapefile)
                    {
                        var p = new global::DataServer.PoI {Service = this, Id = Guid.NewGuid()};
                        var metadataNames = shape.GetMetadataNames();
                        if (metadataNames != null)
                        {
                            foreach (var md in metadataNames)
                            {
                                p.Labels[md] = shape.GetMetadata(md);
                            }
                        }
                        switch (shape.Type)
                        {
                            case ShapeType.Point:
                                p.PoiTypeId = "Point";
                                var shPoint = (ShapePoint) shape;

                                if (projection == CoordinateType.Rd)
                                {
                                    double lat, lon;
                                    CoordinateUtils.Rd2LonLat(shPoint.Point.X, shPoint.Point.Y, out lon, out lat);
                                    p.Position = new Position(lon, lat);
                                }
                                else
                                {
                                    p.Position = new Position(shPoint.Point.X, shPoint.Point.Y);
                                }
                                break;
                            case ShapeType.Polygon:
                                p.PoiTypeId = "WKT";
                                p.WktText = Extensions.ConvertToWkt(shape, projection);
                                break;
                            case ShapeType.PolygonZ:
                                p.PoiTypeId = "WKT";
                                p.WktText = Extensions.ConvertToWkt(shape, projection);
                                break;
                            case ShapeType.PolyLine:
                                p.PoiTypeId = "WKT";
                                p.WktText = Extensions.ConvertToWkt(shape, projection);
                                break;
                        }
                        var pt = PoITypes.FirstOrDefault(k => k.ContentId == p.PoiTypeId);
                        if (pt != null)
                        {
                            foreach (var l in p.Labels.Where(l => pt.MetaInfo.All(k => k.Label != l.Key)))
                                pt.AddMetaInfo(l.Key, l.Key, MetaTypes.text, true);
                        }
                        PoIs.Add(p);
                    }
                }
                //PoIs.FinishBatch();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        #endregion ExtendedPoiService

        #region IImporter

        public string DataFormat
        {
            get { return "Shape file"; }
        }

        public string DataFormatExtension
        {
            get { return "shp"; }
        }

        public IOResult<PoiService> ImportData(FileLocation source)
        {
            var filename = source.LocationString;
            AppStateSettings.Instance.DataServer = new DataServerBase();

            var folder = Path.GetDirectoryName(filename);
            var shapeService = CreateShapeService(filename, Guid.NewGuid(), folder, folder);
            shapeService.Layer = new dsBaseLayer();
            shapeService.File = filename;

            var openFileException = shapeService.OpenFileSync();
            // shapeService.ContentLoaded = true;  // Set by OpenFileSync

            return openFileException != null 
                ? new IOResult<PoiService>(openFileException) 
                : new IOResult<PoiService>(shapeService);
        }

        public bool SupportsMetaData
        {
            get { return false; }
        }

        #endregion IImporter
    }
}
