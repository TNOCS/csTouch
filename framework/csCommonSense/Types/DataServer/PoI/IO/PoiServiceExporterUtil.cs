using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Services;
using System.Text;
using csCommon.Utils;
using csCommon.Utils.IO;
using DataServer;
using Humanizer;

namespace csCommon.Types.DataServer.PoI.IO
{
    /// <summary>
    /// Exporting one file format from another, with a PoiService as content, can have a common 
    /// implementation. This is static so that any classes using this code can still extend other 
    /// classes. 
    /// </summary>
    public static class PoiServiceExporterUtil
    {
        public static IOResult<FileLocation> ExportPoiService(PoiService data, IExporter<PoiService, string> exporter, FileLocation destination = null, bool includeGuid = false)
        {
            if (destination == null)
            {
                destination = GetOutputFileLocation(data, exporter.DataFormatExtension, includeGuid);                
            }
            return ConvertFile(null, data, exporter, destination, includeGuid);
        }

        public static IOResult<FileLocation> ConvertFile(FileLocation source, IExporter<PoiService, FileLocation> exporter, FileLocation destination = null)
        {
            // Import.
            IOResult<PoiService> import = PoiServiceImporters.Instance.Import(source);
            if (import == null)
            {
                return new IOResult<FileLocation>(new Exception("Cannot read file '" + source + "'."));
            }
            if (!import.Successful)
            {
                return new IOResult<FileLocation>(import.Exception);
            }

            // Export.
            return exporter.ExportData(import.Result, destination);
        }

        public static IOResult<FileLocation> ConvertFile(FileLocation source, IExporter<PoiService, string> exporter, FileLocation destination = null, bool includeGuid = false)
        {
            // Import the file.
            IOResult<PoiService> data = PoiServiceImporters.Instance.Import(source);
            if (data == null)
            {
                return new IOResult<FileLocation>(new Exception("Cannot read file '" + source + "'.")); 
            }
            if (!data.Successful)
            {
                return new IOResult<FileLocation>(data.Exception);
            }

            // Export the file.
            PoiService poiService = data.Result;
            return ConvertFile(source, poiService, exporter, destination, includeGuid);
        }

        private static IOResult<FileLocation> ConvertFile(FileLocation source, PoiService poiService, IExporter<PoiService, string> exporter, FileLocation destination, bool includeGuid)
        {
            // Load content if necessary and possible.
            if (! poiService.PoIs.Any() && ! poiService.ContentLoaded)
            {
                if (source == null)
                {
                    throw new Exception("Cannot save a PoI service to a file when it is not fully loaded!");                    
                }
                try
                {
                    DataServiceIO.LoadPoiServiceData(poiService, source);
                }
                catch (Exception e) // Will throw exception if we read from any source but a DS.                    
                {
                    return new IOResult<FileLocation>(e);
                }
            }

            // Export the data.
            IOResult<string> ioResult = exporter.ExportData(poiService, null); // No template for string export.
            if (! ioResult.Successful)
            {
                return new IOResult<FileLocation>(ioResult.Exception);
            }

            // Save the file.
            if (destination == null)
            {
                destination = GetOutputFileLocation(source, poiService, exporter.DataFormatExtension, includeGuid);
            }
            string exportString = ioResult.Result;
            using (var outputFileWriter = new StreamWriter(destination.LocationString)) // Encoding.UTF8 is default.
            {
                outputFileWriter.AutoFlush = true;
                outputFileWriter.Write(exportString);
            }

            // Save the header file (temporary functionality). TODO Make a setting in a global settings container (see GeoJsonIO for another setting that needs this).
            string headerFileString = Path.ChangeExtension(destination.LocationString, "headers");
            using (var outputFileWriter = new StreamWriter(headerFileString))
            {
                foreach (var poIType in poiService.PoITypes)
                {
                    outputFileWriter.WriteLine(poIType.PoiId);
                    List<MetaInfo> metaInfos = poIType.MetaInfo;
                    if (metaInfos == null) continue;
                    bool first = true;
                    foreach (var metaInfo in metaInfos)
                    {
                        if (!first)
                        {
                            outputFileWriter.Write(";");
                        }
                        outputFileWriter.Write(metaInfo.Label);
                        first = false;
                    }
                    outputFileWriter.Write("\n");
                }
            }

            return new IOResult<FileLocation>(new FileLocation(destination.LocationString));
        }

        public static FileLocation GetOutputFileLocation(PoiService poiService, string extension, bool includeGuid)
        {
            string folder = string.IsNullOrEmpty(poiService.Folder) ? Path.GetFullPath(".") : poiService.Folder;
            string name = string.IsNullOrEmpty(poiService.Name) ? "Unnamed" : poiService.Name;
            string fileName = Path.Combine(folder, name);
            return GetOutputFileLocation(new FileLocation(fileName), poiService, extension, includeGuid);
        }
        
        public static FileLocation GetOutputFileLocation(FileLocation file, PoiService poiService, string extension, bool includeGuid)
        {
            string directory = Path.GetDirectoryName(file.LocationString) ?? "";
            string filenameNoExt = Path.GetFileNameWithoutExtension(file.LocationString);
            filenameNoExt = StripGuid.Strip(filenameNoExt, '.');
            if (filenameNoExt.Length == 0)
            {
                filenameNoExt = "Unnamed";
            }
            if (includeGuid)
            {
                filenameNoExt = StripGuid.Strip(filenameNoExt, '.');
                filenameNoExt = Guid.NewGuid().ToString() + '.' + filenameNoExt;
                if (poiService.StaticService)
                {
                    filenameNoExt = '~' + filenameNoExt;
                }
            }
            else
            {
                filenameNoExt = filenameNoExt + "_export";
            }
            string outputFile = Path.Combine(directory, filenameNoExt + "." + extension);
            return new FileLocation(outputFile);
        }
    }
}