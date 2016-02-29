namespace ExcelService.Sessions
{
    public class CellCache : ExcelValueCache
    {
        public string Worksheet { get; set; }
        public string CellName { get; set; }

        public CellCache(string workbook, string worksheet, string cellName, object value) : base(workbook, value)
        {
            Worksheet = worksheet;
            CellName = cellName;   
        }
    }
}
