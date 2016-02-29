using System;
using System.IO;
using csCommon.Utils.IO;
using DataServer;
using ZipFile = Ionic.Zip.ZipFile;

namespace csCommon.Types.DataServer.PoI.IO
{
    /// <summary>
    /// Output of PoiService to zipped data files and strings. 
    /// Output (files and strings) uses compressed GeoJson.
    /// </summary>
    public class ZipExporter : IExporter<PoiService, string>, IExporter<PoiService, FileLocation>, IExporter<FileLocation, FileLocation>
    {
        public string DataFormatExtension { get { return "zip"; } }
        public string DataFormat {
            get { return "Zipped GeoJSON";  }
        }

        public bool SupportsMetaData { get { return true; } }
        public bool IncludeMetaData { set; private get; }

        public bool EnableValidation { get; set; }

        public IOResult<string> ExportData(PoiService source, string templateIgnored)
        {
            GeoJsonIO geoJsonIo = new GeoJsonIO();
            geoJsonIo.IncludeMetaData = IncludeMetaData;
            geoJsonIo.EnableValidation = EnableValidation;
            IOResult<string> exportData = geoJsonIo.ExportData(source, templateIgnored);
            if (exportData.Successful)
            {
                return new IOResult<string>(StringCompressor.CompressString(exportData.Result));
            }
            else
            {
                return exportData;
            }
        }

        public IOResult<FileLocation> ExportData(PoiService source, FileLocation destination)
        {
            // First do a regular export.
            GeoJsonIO geoJsonIo = new GeoJsonIO();
            geoJsonIo.IncludeMetaData = IncludeMetaData;
            geoJsonIo.EnableValidation = EnableValidation;

            FileLocation tempFileLocation = new FileLocation(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TEMP-" + Guid.NewGuid() + "." + geoJsonIo.DataFormatExtension));
            IOResult<FileLocation> exportData = geoJsonIo.ExportData(source, tempFileLocation);
            if (!exportData.Successful)
            {
                return exportData;
            }

            // Compress the resulting file.
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddFile(tempFileLocation.LocationString).FileName = 
                        System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(destination.LocationString), geoJsonIo.DataFormatExtension);
                    zip.Save(destination.LocationString);
                }
                File.Delete(tempFileLocation.LocationString);
            }
            catch (Exception e)
            {
                return new IOResult<FileLocation>(e);
            }

            // Return the result.
            return new IOResult<FileLocation>(destination);
        }

        public IOResult<FileLocation> ExportData(FileLocation source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ConvertFile(source, this, destination);
        }
    }
}
