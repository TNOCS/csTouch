using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspose.Cells;
using csCommon.Utils;
using csCommon.Utils.IO;
using DataServer;
using DocumentFormat.OpenXml.Office.CustomUI;

namespace csCommon.Types.DataServer.PoI.IO
{
    public class CsvExporter : IExporter<PoiService, string>, IExporter<PoiService, FileLocation>, IExporter<FileLocation, FileLocation>
    {
        public string DataFormat { get { return "Comma Separated Values"; } }
        public string DataFormatExtension { get { return "csv"; } }
        public bool EnableValidation { get; set; }

        public bool SupportsMetaData
        {
            get { return false; }
        }

        public bool IncludeMetaData
        {
            set { } // Ignore.
        }

        public IOResult<string> ExportData(PoiService source, string templateIgnored)
        {
            var labelHeaders = new List<string>();
            var csvStringBuffer = new StringBuilder();

            ContentList PoIs = source.PoIs;

            bool csvHeaderAddPosition = false;
            bool csvHeaderAddWkt = false;
            bool csvHeaderAddKeywords = false;

            // Headers.
            // Since dictionaries do not guarantee key ordering, enforce an order. 
            foreach (KeyValuePair<string, string> kv in source.PoIs.SelectMany(poi => poi.Labels))
            {
                string label = kv.Key;
                if (label.Contains(";"))
                {
                    throw new Exception("Column headers cannot contain separator characters such as ; and ,!");
                }
                if (labelHeaders.Contains(label))
                {
                    continue;
                }
                labelHeaders.Add(label);
                csvStringBuffer.Append(label + ";");
            }
            if (labelHeaders.Count > 0)
            {
                csvStringBuffer.Remove(csvStringBuffer.Length - 1, 1); // Remove the last ;
                csvStringBuffer.Append("\n");
            }

//            // Unfortunately we go through the whole collection twice.
//            foreach (global::DataServer.PoI poi in PoIs)
//            {
//                Dictionary<string, string> labels = poi.Labels;
//                foreach (string labelHeader in labels.Keys)
//                {
//                    if (labelHeader.Contains(";"))
//                    {
//                        throw new Exception("Column headers cannot contain separator characters such as ; and ,!");
//                    }
//                    if (!labelHeaders.Contains(labelHeader))
//                    {
//                        labelHeaders.Add(labelHeader);
//                        csvStringBuffer.Append(labelHeader + ";");
//                    }
//                }
//            }
//            if (labelHeaders.Count > 0)
//            {
//                csvStringBuffer.Remove(csvStringBuffer.Length - 1, 1); // Remove the last ;
//                csvStringBuffer.Append("\n");
//            }

            // Labels.
            foreach (global::DataServer.PoI poi in PoIs)
            {
                Dictionary<string, string> labels = poi.Labels;
                foreach (string labelHeader in labelHeaders)
                {
                    string value;
                    bool hasValue = labels.TryGetValue(labelHeader, out value);
                    if (!hasValue)
                    {
                        value = "";
                    }
                    csvStringBuffer.Append(StringCleanupExtensions.RemoveDelimiters(value, ';'));
                    csvStringBuffer.Append(";");
                }
                if (labelHeaders.Count > 0)
                {
                    csvStringBuffer.Remove(csvStringBuffer.Length - 1, 1); // Remove the last ;
                }
                // Location.
                Position position = poi.Position;
                if (position != null)
                {
                    csvHeaderAddPosition = true;
                    csvStringBuffer.Append(";");
                    csvStringBuffer.Append(position.Latitude);
                    csvStringBuffer.Append(";");
                    csvStringBuffer.Append(position.Longitude);
                }
                // WKT.
                if (!string.IsNullOrEmpty(poi.WktText))
                {
                    csvHeaderAddWkt = true;
                    csvStringBuffer.Append(";");
                    csvStringBuffer.Append(StringCleanupExtensions.RemoveDelimiters(poi.WktText, ';'));
                }
                // Keywords.
                if (poi.HasKeywords)
                {
                    csvHeaderAddKeywords = true;
                    csvStringBuffer.Append(";").Append(poi.Keywords.ToGeoJson());
                }
                // Newline at the end.
                csvStringBuffer.Append("\n");
            }

            // If needed, add the position headers to the first line of the CSV string.
            string csvString = csvStringBuffer.ToString();
            if (csvHeaderAddPosition)
            {
                csvString = csvString.Substring(0, csvString.IndexOf("\n", StringComparison.CurrentCulture)) + ";" + Position.LAT_LABEL + ";" + Position.LONG_LABEL +
                            csvString.Substring(csvString.IndexOf("\n", StringComparison.CurrentCulture));
            }

            // If needed, also add the WKT header to the first line of the CSV string.
            if (csvHeaderAddWkt)
            {
                csvString = csvString.Substring(0, csvString.IndexOf("\n", StringComparison.CurrentCulture)) + ";" + WellKnownTextIO.WKT_LABEL + 
                            csvString.Substring(csvString.IndexOf("\n", StringComparison.CurrentCulture));                
            }

            // If needed, also add the Keywords header to the first line of the CSV string.
            if (csvHeaderAddKeywords)
            {
                csvString = csvString.Substring(0, csvString.IndexOf("\n", StringComparison.CurrentCulture)) + ";Keywords" +
                            csvString.Substring(csvString.IndexOf("\n", StringComparison.CurrentCulture));
            }

            IOResult<string> result = new IOResult<string>(csvString);
            return result;
        }

        public IOResult<FileLocation> ExportData(PoiService source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ExportPoiService(source, this, destination);
        }

        public IOResult<FileLocation> ExportData(FileLocation source, FileLocation destination)
        {
            return PoiServiceExporterUtil.ConvertFile(source, this, destination);
        }
    }
}
