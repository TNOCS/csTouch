using System;
using System.IO;
using System.Linq;
using csCommon.Utils;
using csCommon.Utils.IO;
using csShared;
using DataServer;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace csCommon.Types.DataServer.PoI.IO
{
    /// <summary>
    /// Create a data service based on a file. This is code that was often repeated throughout, so now moved to a utility class.
    /// </summary>
    public class DataServiceIO : IImporter<FileLocation, PoiService>, IExporter<PoiService, string>, IExporter<PoiService, FileLocation>, IExporter<FileLocation, FileLocation>
    {
        public string DataFormatExtension
        {
            get { return "ds"; }
        }

        public string DataFormat
        {
            get { return "Data Service"; }
        }

        public bool EnableValidation { get; set; }

        // Note: the PoiService still requires loading content if needed.
        public IOResult<PoiService> ImportData(FileLocation source)
        {
            try
            {
                string file = source.LocationString;
                string folder = Path.GetDirectoryName(file) ?? "";
                string originFolder = Path.Combine(Directory.GetCurrentDirectory(), AppStateSettings.Instance.Config.Get("Poi.LocalFolder", "PoiLayers"));

                var filename = Path.GetFileName(file);
                if (filename == null)
                {
                    return new IOResult<PoiService>(new Exception("Cannot open file " + file + "."));
                }
                var stat = (filename.StartsWith("~"));
                if (stat)
                    filename = filename.Remove(0, 1);
                string filenameNoGuid = StripGuid.Strip(filename, '.');
                string guidStr = (filenameNoGuid.Length < filename.Length)
                    ? filename.Substring(0, filename.Length - filenameNoGuid.Length - 1)
                    : "";
                var guid = (!string.IsNullOrEmpty(guidStr))
                    ? Guid.Parse(guidStr)
                    : Guid.NewGuid();
                var ps = new PoiService
                {
                    IsLocal = true,
                    Folder = folder,
                    Id = guid,
                    Name = filenameNoGuid,
                    StaticService = stat,
                    RelativeFolder = folder.Replace(originFolder, string.Empty),
                };
                ps.InitPoiService();
                ps.ContentLoaded = false;  // Is the default, but explicitly set to remind us.
                return new IOResult<PoiService>(ps);
            }
            catch (Exception e)
            {
                return new IOResult<PoiService>(e);
            }
        }

        public bool SupportsMetaData
        {
            get { return true; }
        }

        public bool IncludeMetaData
        {
            get; set;
        }

        public IOResult<string> ExportData(PoiService source, string templateIgnored)
        {
            return new IOResult<string>(source.ToXml(IncludeMetaData, false).ToString());
        }

        public IOResult<FileLocation> ExportData(PoiService source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ExportPoiService(source, this, destination, true); // Guid in front        
        }

        public IOResult<FileLocation> ExportData(FileLocation source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ConvertFile(source, this, destination, true); // Guid in front        
        }

        /// <summary>
        ///     Create a data service based on loading the provided file.
        /// </summary>
        /// <param name="file">The file to read.</param>
        /// <returns>The data service created.</returns>
        public static PoiService LoadDataService(FileLocation file)
        {
            Exception e;
            return LoadDataService(file, out e);
        }
        
        /// <summary>
        ///     Create a data service based on loading the provided file.
        /// </summary>
        /// <param name="file">The file to read.</param>
        /// <param name="exception">Any exception first encountered.</param>
        /// <returns>The data service created.</returns>
        public static PoiService LoadDataService(FileLocation file, out Exception exception) // REVIEW TODO fix: async removed
        {
            try
            {
                string folder = Path.GetDirectoryName(file.LocationString) ?? "";
                var poiService = new PoiService
                {
                    Folder = folder, 
                    IsFileBased = true, 
                    IsLocal = true, 
                };
                poiService.InitPoiService();
                LoadPoiServiceData(poiService, file);
                exception = null;
                return poiService;
            }
            catch (Exception e)
            {
                exception = e;
                return null;
            }
        }

        public static void LoadPoiServiceData(PoiService poiService, FileLocation dsFile)
        {
            string theFolder = Path.GetDirectoryName(dsFile.LocationString) ?? "";
            string xml = poiService.store.GetString(dsFile.LocationString);
            poiService.SettingsList.Clear();
            poiService.FromXml(xml, theFolder);
            poiService.ContentLoaded = true;
        }
    }
}