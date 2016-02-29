using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelServiceModel
{
    public class CellValue
    {
        public int SessionId { get; set; }
        public string Workbook { get; set; }
        public string Worksheet { get; set; }
        public string CellName { get; set; }
        public object Value { get; set; }

        public CellValue(int sessionId, string workbook, string worksheet, string cellName, object value)
        {
            SessionId = sessionId;
            Workbook = workbook;
            Worksheet = worksheet;
            CellName = cellName;
            Value = value;
        }

    }
}
