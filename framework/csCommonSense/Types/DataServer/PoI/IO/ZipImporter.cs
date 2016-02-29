using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using csCommon.Utils.IO;
using DataServer;
using Ionic.Zip;
using ZipFile = Ionic.Zip.ZipFile;

namespace csCommon.Types.DataServer.PoI.IO
{
    /// <summary>
    /// Input of PoiService from zipped data files and strings. 
    /// String input can read only compressed GeoJson.
    /// File input can read any file format supported by other importers. We look for a 
    /// single file inside the archive, or in case of multiple files, a file with the 
    /// same file name as the archive (which is also how ZipExporter itself exports files).
    /// </summary>
    public class ZipImporter : IImporter<string, PoiService>, IImporter<FileLocation, PoiService>
    {
        public string DataFormatExtension { get { return "zip"; } }
        public string DataFormat {
            get { return "Zipped data source";  }
        }

        public bool SupportsMetaData { get { return true; } } // Actually depends on what's inside the ZIP.

        public IOResult<PoiService> ImportData(FileLocation source)
        {            
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(source.LocationString);
            if (fileNameWithoutExtension == null) return null;

            FileLocation tempFileLocation = null;
            using (ZipFile zip = new ZipFile())
            {
                zip.Initialize(source.LocationString);
                ICollection<ZipEntry> zipEntries = zip.Entries;

                if (zipEntries.Count == 1)
                {
                    ZipEntry zipEntry = zipEntries.First();
                    tempFileLocation = ExtractEntryToTempFile(zipEntry);
                }
                else
                {
                    foreach (var zipEntry in zipEntries.Where(zipEntry => zipEntry.FileName.StartsWith(fileNameWithoutExtension)))
                    {
                        tempFileLocation = ExtractEntryToTempFile(zipEntry);
                        break;
                    }                    
                }
            }
            if (tempFileLocation != null)
            {
                IOResult<PoiService> data = PoiServiceImporters.Instance.Import(tempFileLocation);
                File.Delete(tempFileLocation.LocationString);
                if (data == null)
                {
                    data = new IOResult<PoiService>(new Exception("Could not unzip the data file! Probably, this is not a ZIP file containing a data file we can deal with."));
                }
                return data;
            }
            else
            {
                return new IOResult<PoiService>(new Exception("Could not unzip the data file! It does not contain a single file, or a file with the name '" + fileNameWithoutExtension + "'."));
            }
        }

        private static FileLocation ExtractEntryToTempFile(ZipEntry zipEntry)
        {
            string tempPath = System.IO.Path.GetTempPath();
            zipEntry.Extract(tempPath);
            FileLocation tempFileLocation = new FileLocation(System.IO.Path.Combine(tempPath, zipEntry.FileName));
            return tempFileLocation;
        }

        /// <summary>
        /// Import the string to a PoiService. Note that this method can only read GeoJson strings that have been compressed
        /// using the ExportData method of ZipExporter.
        /// </summary>
        /// <param name="source">The string to read.</param>
        /// <returns>A result; either containing a PoiService, or an error.</returns>
        public IOResult<PoiService> ImportData(string source)
        {
            source = StringCompressor.DecompressString(source);
            GeoJsonIO geoJsonIo = new GeoJsonIO();
            // geoJsonIo.IncludeMetaData = IncludeMetaData; // not needed, import only here.
            return geoJsonIo.ImportData(source);
        }
    }
}
