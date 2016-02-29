using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ExcelServiceModel
{
    public class NameValue
    {
        public int SessionId { get; set; }
        public string Workbook { get; set; }
        public string Name { get; set; }
        public object[] Values { get; set; }

        [JsonIgnore]
        public object Value
        {
            get
            {
                if (Values == null || Values.Length == 0) return null;

                return Values[0];
            }
        }

        public NameValue(int sessionId, string workbook, string name, object[] values)
        {
            SessionId = sessionId;
            Workbook = workbook;
            Name = name;
            Values = values;
        }
    }
}
