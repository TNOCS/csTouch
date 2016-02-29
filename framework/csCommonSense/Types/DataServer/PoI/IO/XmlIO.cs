using System;
using System.IO;
using System.Xml;
using csCommon.Utils.IO;
using DataServer;
using Newtonsoft.Json;

namespace csCommon.Types.DataServer.PoI.IO
{
    // Made abstract, so it is not used. There are still big bugs due to incompatibilities between JSON and XML. This does not work as it should.

    abstract class XmlIO : IImporter<string, PoiService>, IImporter<FileLocation, PoiService>, IExporter<PoiService, string>, IExporter<PoiService, FileLocation>, IExporter<FileLocation, FileLocation>
    {
        private GeoJsonIO _geoJsonIo = new GeoJsonIO(); // Underlying I/O; we simply convert from and to XML with standard functionality.

        public string DataFormat
        {
            get { return "XML"; }
        }

        public string DataFormatExtension
        {
            get { return "xml"; }
        }

        public bool SupportsMetaData { get { return _geoJsonIo.SupportsMetaData; } }

        public bool IncludeMetaData {
            private get { return _geoJsonIo.IncludeMetaData; }
            set { _geoJsonIo.IncludeMetaData = value; } 
        }

        public bool EnableValidation
        {
            get { return _geoJsonIo.EnableValidation;  }
            set { _geoJsonIo.EnableValidation = value; }
        }

        public IOResult<PoiService> ImportData(string source)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(source);
            string jsonText = JsonConvert.SerializeXmlNode(doc);
            return _geoJsonIo.ImportData(jsonText);
        }

        public IOResult<PoiService> ImportData(FileLocation source)
        {
            try
            {
                string text = File.ReadAllText(source.LocationString);
                return ImportData(text);
            }
            catch (Exception e)
            {
                return new IOResult<PoiService>(e);
            }
        }

        public IOResult<string> ExportData(PoiService source, string templateIgnored)
        {
            IOResult<string> exportData = _geoJsonIo.ExportData(source, templateIgnored);
            if (!exportData.Successful)
            {
                return exportData;
            }
            string json = "{\"root\": " + exportData.Result + "}"; // Make sure there is only one property in the root object.

            // TODO reading the file back in (e.g. HLZ file), we get "Name cannot begin with 1"... remove invalid stuff!

            XmlDocument doc = JsonConvert.DeserializeXmlNode(json);
            string xml = doc.OuterXml;
            return new IOResult<string>(xml);
        }

        public IOResult<FileLocation> ExportData(PoiService source, FileLocation destination)
        {
            try
            {
                IOResult<string> exportData = ExportData(source, "");
                if (!exportData.Successful)
                {
                    return new IOResult<FileLocation>(exportData.Exception);
                }
                File.WriteAllText(destination.LocationString, exportData.Result);
                return new IOResult<FileLocation>(destination);
            }
            catch (Exception e)
            {
                return new IOResult<FileLocation>(e);
            }
        }

        public IOResult<FileLocation> ExportData(FileLocation source, FileLocation destination)
        {
            IOResult<PoiService> importData = ImportData(source);
            if (!importData.Successful)
            {
                return new IOResult<FileLocation>(importData.Exception);
            }
            return ExportData(importData.Result, destination);
        }
    }
}
