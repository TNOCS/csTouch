using System;
using System.Data;
using System.IO;
using System.Text;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Utils.IO;
using DataServer;
using Excel;

namespace CsvToDataService
{
    // TODO Keywords are not supported.

    public class XlsxImporter : ExcelImporter
    {
        public XlsxImporter() : base("Excel (first sheet only)", "xlsx") { }

        protected override IExcelDataReader GetReader(Stream stream)
        {
            return ExcelReaderFactory.CreateOpenXmlReader(stream);
        }
    }

    public class XlsImporter : ExcelImporter
    {
        public XlsImporter() : base("Excel (97-2003) (first sheet only)", "xls") { }

        protected override IExcelDataReader GetReader(Stream stream)
        {
            return ExcelReaderFactory.CreateBinaryReader(stream);
        }
    }

    public abstract class ExcelImporter : IImporter<FileLocation, PoiService>
    {
        protected ExcelImporter(string dataFormatName, string dataFormatExtension)
        {
            DataFormat = dataFormatName;
            DataFormatExtension = dataFormatExtension;
        }

        public string DataFormat { get; private set; }
        public string DataFormatExtension { get; private set; }
        public bool EnableValidation { get; set; }

        public IOResult<PoiService> ImportData(FileLocation source)
        {
            try
            {
                // Load the Excel data.
                FileStream stream = File.Open(source.LocationString, FileMode.Open, FileAccess.Read);
                IExcelDataReader excelDataReader = GetReader(stream);
                DataSet result = excelDataReader.AsDataSet();
                excelDataReader.IsFirstRowAsColumnNames = true;

                // Convert the first sheet to CSV.
                StringBuilder csvData = new StringBuilder("");
                int row_no = 0;
                const int ind = 0; // First sheet only.
                while (row_no < result.Tables[ind].Rows.Count) 
                {
                    for (int i = 0; i < result.Tables[ind].Columns.Count; i++)
                    {
                        string rawData = result.Tables[ind].Rows[row_no][i].ToString();
                        rawData = rawData.RemoveInvalidCharacters();
                        csvData.Append(rawData).Append(";");
                    }
                    row_no++;
                    csvData.Append("\n");
                }
                excelDataReader.Close();

                // Save the CSV file.
                FileLocation tempCsvFile = new FileLocation(Path.Combine(Path.GetTempPath(), "TEMP-" + Guid.NewGuid() + ".csv"));
                using (var writer = new StreamWriter(tempCsvFile.LocationString))
                {
                    writer.Write(csvData);
                }

                // Import the CSV file.
                IOResult<PoiService> ioResult = PoiServiceImporters.Instance.Import(tempCsvFile);

                // Delete the temp file.
                File.Delete(tempCsvFile.LocationString);

                // And return.
                return ioResult;
            }
            catch (Exception e)
            {
                return new IOResult<PoiService>(e);
            }
        }

        protected abstract IExcelDataReader GetReader(Stream stream);

        public bool SupportsMetaData
        {
            get { return false; }
        }
    }
}
