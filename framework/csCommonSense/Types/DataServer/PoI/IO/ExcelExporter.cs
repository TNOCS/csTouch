using System.Collections.Generic;
using System.Globalization;
using Aspose.Cells;
using csCommon.Utils.IO;
using csShared.Utils;
using DataServer;
using System.Linq;

namespace csCommon.Types.DataServer.PoI.IO
{
    // TODO Keywords are not supported.

    public class XlsxExporter : ExcelExporter
    {
        public XlsxExporter() : base("Excel", "xlsx", SaveFormat.Xlsx) { }
    }

    public class XlsExporter : ExcelExporter
    {
        public XlsExporter() : base("Excel (97-2003)", "xls", SaveFormat.Excel97To2003) { }
    }

    public abstract class ExcelExporter : IExporter<PoiService, FileLocation>, IExporter<FileLocation, FileLocation>
    {
        private readonly SaveFormat saveFormat;

        protected ExcelExporter(string dataFormatName, string dataFormatExtension, SaveFormat dataFormat)
        {
            DataFormat = dataFormatName;
            DataFormatExtension = dataFormatExtension;
            saveFormat = dataFormat;
        }

        public string DataFormat { get; private set; }
        public string DataFormatExtension { get; private set; }
        public bool EnableValidation { get; set; }

        public IOResult<FileLocation> ExportData(PoiService source, FileLocation destination)
        {
            Workbook workbook = CreateWorkbook(source);
            if (destination == null)
            {
                destination = PoiServiceExporterUtil.GetOutputFileLocation(source, DataFormatExtension, false); // No Guid.                
            }

            workbook.Save(destination.LocationString, saveFormat);

            IOResult<FileLocation> ioResult = new IOResult<FileLocation>(destination);
            return ioResult;
        }

        public IOResult<FileLocation> ExportData(FileLocation source, FileLocation destination)
        {
            IOResult<PoiService> import = PoiServiceImporters.Instance.Import(source);
            if (!import.Successful)
            {
                return new IOResult<FileLocation>(import.Exception);
            }
            return ExportData(import.Result, destination);
        }

        private static Workbook CreateWorkbook(PoiService service)
        {
            var workbook = new Workbook();

            foreach (var poIType in service.PoITypes.Where(pt => pt.MetaInfo != null))
            {
                var sheet = workbook.Worksheets.Add(poIType.Name);
                // var headers = poIType.MetaInfo.Select(mi => mi.Label).ToList();

                // Account for duplicate label entries
                var headerLookup = poIType.MetaInfo.GroupBy(mi => mi.Label, mi => mi.Title).ToDictionary(g => g.Key, g => g.First());
                // var addLocation = false;
                var drawingMode = poIType.NEffectiveStyle.DrawingMode;
                bool includeLatLon = drawingMode == DrawingModes.Image ||
                                     drawingMode == DrawingModes.Point;
                var headers = new Dictionary<string, string>(headerLookup);
                if (includeLatLon)
                {
                    headers[Position.LAT_LABEL] = Position.LAT_LABEL;
                    headers[Position.LONG_LABEL] = Position.LONG_LABEL; // Can be read back if converted to Csv!.
                }
                // sheet.Cells.ImportArray(headers.Values.ToArray(), 0, 0, false); // Put titles in the first row.
                sheet.Cells.ImportArray(headers.Keys.ToArray(), 0, 0, false); // Put original label IDs in the first row. This preserves e.g. the relation with templates.

                var rowIndex = 1;
                var pois = service.PoIs.Where(poi => poi.PoiTypeId == poIType.ContentId);
                foreach (var poi in pois)
                {
                    string value;
                    var cellValues = headerLookup.Keys.Select(header => poi.Labels.TryGetValue(header, out value) ? value.RestoreInvalidCharacters().Truncate(30000) : string.Empty).ToList();
                    if (poi.Position != null)
                    {
                        cellValues.Add(poi.Position.Latitude.ToString(CultureInfo.InvariantCulture));
                        cellValues.Add(poi.Position.Longitude.ToString(CultureInfo.InvariantCulture));
                    }
                    sheet.Cells.ImportArray(cellValues.ToArray(), rowIndex++, 0, false);
                }
            }

            // remove empty default sheet
            if (workbook.Worksheets.Count > 1)
            {
                workbook.Worksheets.RemoveAt(0);
            }
            workbook.Worksheets.ActiveSheetIndex = 0;
            foreach (var worksheet in workbook.Worksheets) {
                worksheet.Cells.ConvertStringToNumericValue();
            }
            return workbook;
        }

        public bool SupportsMetaData
        {
            get { return false; }
        }

        public bool IncludeMetaData
        {
            set { } // Ignored.
        }
    }
}
