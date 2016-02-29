using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using csCommon.Utils.IO;
using csDataServerPlugin;
using csShared;
using DataServer;

namespace csCommon.Types.DataServer.PoI.IO
{
    public class GeoJsonIO : IImporter<string, PoiService>, IImporter<FileLocation, PoiService>, IExporter<PoiService, string>, IExporter<PoiService, FileLocation>, IExporter<FileLocation, FileLocation>
    {
        #region IImporter, IExporter

        //   { "type": "FeatureCollection",
        //    "features": [
        //      { "type": "Feature",
        //        "geometry": {"type": "Point", "coordinates": [102.0, 0.5]},
        //        "properties": {"prop0": "value0"}
        //        },
        //      { "type": "Feature",
        //        "geometry": {
        //          "type": "LineString",
        //          "coordinates": [
        //            [102.0, 0.0], [103.0, 1.0], [104.0, 0.0], [105.0, 1.0]
        //            ]
        //          },
        //        "properties": {
        //          "prop0": "value0",
        //          "prop1": 0.0
        //          }
        //        },
        //      { "type": "Feature",
        //         "geometry": {
        //           "type": "Polygon",
        //           "coordinates": [
        //             [ [100.0, 0.0], [101.0, 0.0], [101.0, 1.0],
        //               [100.0, 1.0], [100.0, 0.0] ]
        //             ]
        //         },
        //         "properties": {
        //           "prop0": "value0",
        //           "prop1": {"this": "that"}
        //           }
        //         }
        //       ]
        //     }

        public string DataFormat { get { return "GeoJSON"; } }
        public string DataFormatExtension { get { return "json"; } }

        public bool EnableValidation { get; set; }

        //private readonly Dictionary<string, string> abbreviateLabels = new Dictionary<string, string>(); 

        public IOResult<string> ExportData(PoiService source, string templateIgnored)
        {
            var geoJson = new StringBuilder("{");
            var first = true;
            if (IncludeMetaData)
            {
                // Add the poi types
                geoJson.Append("\"featureTypes\":{"); // Dictionary.
                var poiTypes = source.PoITypes;
                foreach (var poiType in poiTypes.Cast<global::DataServer.PoI>())
                {
                    //foreach (var label in poiType.Labels) {
                    //    string newKey;
                    //    if (!abbreviateLabels.TryGetValue(label.Key, out newKey)) {
                    //        newKey = string.Format("c{0}", abbreviateLabels.Count);
                    //        abbreviateLabels[label.Key] = newKey;
                    //    }
                    //    label.Key = newKey;
                    //}
                    if (!first)
                    {
                        geoJson.Append(",");
                    }
                    geoJson.Append(poiType.TypeToGeoJson());
                    first = false;
                }
                geoJson.Append("},");
            }
            // Add the pois
            geoJson.Append("\"type\":\"FeatureCollection\",\"features\":["); // List.
            var poIs = source.PoIs;
            first = true;
            foreach (var poi in poIs.Cast<global::DataServer.PoI>())
            {
                //var poi = (global::DataServer.PoI)baseContent;
                if (!first)
                {
                    geoJson.Append(",");
                }
                geoJson.Append(poi.ToGeoJson());
                first = false;
            }
            geoJson.Append("]}");
            var geoJsonString = geoJson.ToString();

            // Validate the GeoJSON.
            if (!EnableValidation) return new IOResult<string>(geoJsonString);
            try
            {
                using (var wb = new WebClient())
                {
                    wb.Encoding = Encoding.UTF8;
                    var response = wb.UploadString("http://geojsonlint.com/validate", geoJsonString);
                    if (!response.Contains("ok"))
                    {
                        return new IOResult<string>(new Exception(
                            "Error in GeoJSON conversion.\n\nValidator message:\n" + response + "\n\nJSON:\n" + geoJsonString));
                    }
                }
            }
            catch (Exception)
            {
                // TODO Warn the user that the JSON was not validated?
                // If we cannot check, we must assume it is valid.
            }

            return new IOResult<string>(geoJsonString);
        }

        public IOResult<FileLocation> ExportData(PoiService source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ExportPoiService(source, this, destination);
        }

        public IOResult<FileLocation> ExportData(FileLocation source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ConvertFile(source, this, destination);
        }

        public IOResult<PoiService> ImportData(string source)
        {
            // TODO Now works via a file. This is upside down of course; we could do this in memory and base a file-based importer on that.
            FileLocation tempFileLocation = new FileLocation(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TEMP-" + Guid.NewGuid() + "." + DataFormatExtension));
            using (var s = new StreamWriter(tempFileLocation.LocationString)) // UTF-8 is default.
            {
                s.Write(source);
            }
            IOResult<PoiService> data = ImportData(tempFileLocation);
            File.Delete(tempFileLocation.LocationString);
            return data;
        }

        public IOResult<PoiService> ImportData(FileLocation source)
        {
            string filename = source.LocationString;
            AppStateSettings.Instance.DataServer = new DataServerBase();

            string folder = Path.GetDirectoryName(filename);
            GeoJsonService geoJsonService = GeoJsonService.CreateGeoJsonService(filename, Guid.NewGuid(), folder, folder);
            geoJsonService.Layer = new dsBaseLayer();
            geoJsonService.File = filename;

            Exception openFileException = geoJsonService.OpenFileSync(false);
            // geoJsonService.SaveXml(Path.ChangeExtension(filename, "ds"));  // Do not save the file.

            return openFileException != null 
                ? new IOResult<PoiService>(openFileException) 
                : new IOResult<PoiService>(geoJsonService);
        }

        public bool IncludeMetaData { get; set; }

        public bool SupportsMetaData
        {
            get { return true; }
        }
        #endregion IImporter, IExporter
    }
}
