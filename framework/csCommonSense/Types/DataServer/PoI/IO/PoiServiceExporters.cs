using System.Collections.Generic;
using System.IO;
using System.Linq;
using csCommon.Utils.IO;
using DataServer;
using Humanizer;

namespace csCommon.Types.DataServer.PoI.IO
{
    public class PoiServiceExporters : AssemblyClassEnumerator<IExporter<PoiService, FileLocation>> 
    {
        private static PoiServiceExporters _instance;

        public static PoiServiceExporters Instance
        {
            get { return _instance ?? (_instance = new PoiServiceExporters()); }
        }

        public PoiServiceExporters() // TODO FIX: Constructor should be private for a singleton, but I cannot get it to work as a resource in XAML then.
        {
            _exporters = new Dictionary<string, IExporter<PoiService, FileLocation>>();
            foreach (IExporter<PoiService, FileLocation> exporter in AssemblyClasses)
            {
                if (! _exporters.ContainsKey(exporter.DataFormatExtension))
                {
                    _exporters.Add(exporter.DataFormatExtension, exporter);
                }
                else
                {
                    // TODO REVIEW: May want to warn the user in debug mode.
                }
            }
        }

        private Dictionary<string, IExporter<PoiService, FileLocation>> _exporters;

        public IEnumerable<string> GetSupportedExtensions(IEnumerable<string> excludedExtensions = null) {
            return excludedExtensions == null 
                ? _exporters.Keys 
                : _exporters.Keys.Where(key => !excludedExtensions.Contains(key));
        }

        public IExporter<PoiService, FileLocation> GetExporter(string extension)
        {
            if (extension.StartsWith(".")) extension = extension.Substring(1);
            IExporter<PoiService, FileLocation> exporter;
            bool tryGetValue = _exporters.TryGetValue(extension, out exporter);
            return tryGetValue ? exporter : null;
        }

        public IOResult<FileLocation> Export(PoiService service, FileLocation destination, bool includeMetaData)
        {
            var extension = Path.GetExtension(destination.LocationString);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".json"; // TODO Ugly default extension :)
            }
            IExporter<PoiService, FileLocation> exporter = GetExporter(extension);
            exporter.IncludeMetaData = exporter.SupportsMetaData && includeMetaData;
            IOResult<FileLocation> exportData = exporter.ExportData(service, destination); 
            if (exportData.Successful)
            {
                // Move the file to where the user wants it, if needed.
                if (destination.LocationString != exportData.Result.LocationString)
                {
                    File.Move(exportData.Result.LocationString, destination.LocationString);                    
                }
                return new IOResult<FileLocation>(destination);
            }
            else
            {
                return new IOResult<FileLocation>(exportData.Exception);                
            }
        }
    }
}
