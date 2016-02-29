using System;
using System.IO;
using csCommon.Utils.IO;
using csShared;
using DataServer;

namespace csCommon.Types.DataServer
{
    /// <summary>
    /// Create a data service description (i.e. template). 
    /// </summary>
    public class DataServiceTemplateImporter : IImporter<FileLocation, PoiService>
    {
        public IOResult<PoiService> ImportData(FileLocation source)
        {
            try
            {
                string file = source.LocationString;
                string folder = Path.GetDirectoryName(file) ?? "";
                string originFolder = Path.Combine(Directory.GetCurrentDirectory(), AppStateSettings.Instance.Config.Get("Poi.LocalFolder", "PoiLayers"));

                var idName = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(idName))
                {
                    return new IOResult<PoiService>(new Exception(string.Format("Invalid filename: {0}", file)));
                }

                var guid = Guid.NewGuid();
                var ps = new PoiService
                {
                    IsLocal = true,
                    Folder = folder,
                    Id = guid,
                    Name = idName,
                    IsTemplate = true,
                    StaticService = false,
                    RelativeFolder = folder.Replace(originFolder, string.Empty) // TODO REVIEW: This was "baseFolder" in "DataServerBase" in the old code.
                };
                ps.InitPoiService();
                ps.ContentLoaded = false; // TODO Check whether this is true.
                return new IOResult<PoiService>(ps);
            }
            catch (Exception e)
            {
                return new IOResult<PoiService>(e);
            }
        }

        public string DataFormatExtension
        {
            get { return "dsd"; }
        }

        public string DataFormat
        {
            get { return "Data Service Description"; }
        }

        public bool SupportsMetaData
        {
            get { return true; }
        }
    }
}