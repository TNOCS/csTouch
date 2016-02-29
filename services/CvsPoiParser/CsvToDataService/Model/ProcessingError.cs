using System.Diagnostics;
using csCommon.Types.DataServer.Interfaces;

namespace CsvToDataService.Model
{
    [DebuggerDisplay("{FieldNumber}: {Description}, {OriginalText}")]
    public class ProcessingError : IConvertibleCsv
    {
        public ProcessingError() { } // Cannot be serialized unless we have this.

        public ProcessingError(string description, string originalText, long fieldNumber)
        {
            Description = description;
            OriginalText = originalText;
            FieldNumber = fieldNumber;
        }

        public string Description { get; set; }
        public string OriginalText { get; set; }
        public long FieldNumber { get; set; }

        public string ToCsv(char separator = ',')
        {
            return string.Format("{0}{1}{2}{3}{4}\n", FieldNumber, separator, Description, separator, OriginalText);
        }

        public IConvertibleCsv FromCsv(string s, char separator = ',', bool newObject = true)
        {
            throw new System.NotImplementedException("Cannot initialize ProcessingError objects from CSV yet.");
        }
    }
}