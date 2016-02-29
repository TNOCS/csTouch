using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using csCommon.Utils.IO;
using DataServer;

namespace csCommon.Types.DataServer.PoI.IO
{
    public class PoiServiceImporters : AssemblyClassEnumerator<IImporter<FileLocation, PoiService>>
    {
        private static PoiServiceImporters _instance;
        public static PoiServiceImporters Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PoiServiceImporters();
                }
                return _instance;
            }
        }

        private PoiServiceImporters()
        {
            _importers = new Dictionary<string, IImporter<FileLocation, PoiService>>();
            foreach (IImporter<FileLocation, PoiService> importer in AssemblyClasses)
            {
                if (!_importers.ContainsKey(importer.DataFormatExtension))
                {
                    _importers.Add(importer.DataFormatExtension, importer);
                }
                else
                {
                    // TODO REVIEW Somehow warn the user in debug mode.
                }
            }
        }

        private Dictionary<string, IImporter<FileLocation, PoiService>> _importers;

        public IEnumerable<string> GetSupportedExtensions(IEnumerable<string> excludedExtensions = null)
        {
            if (excludedExtensions == null)
                return _importers.Keys;
            else
            {
                return _importers.Keys.Where(key => !excludedExtensions.Contains(key));
            }
        }

        public IImporter<FileLocation, PoiService> GetImporter(string extension)
        {
            if (extension.StartsWith(".")) extension = extension.Substring(1);
            IImporter<FileLocation, PoiService> importer;
            bool tryGetValue = _importers.TryGetValue(extension, out importer);
            if (tryGetValue)
            {
                return importer;
            }
            return null;
        }

        public IOResult<PoiService> Import(FileLocation file)
        {
            var extension = System.IO.Path.GetExtension(file.LocationString);
            IImporter<FileLocation, PoiService> importer = GetImporter(extension);
            if (importer == null)
            {
                return null;
            }
            IOResult<PoiService> importData = importer.ImportData(file);
            return importData;
        }
    }
}
