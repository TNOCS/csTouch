using Aspose.Cells;

namespace ExcelService.Excel
{
    public static class CellExtensions
    {
        public static bool IsNumeric(this Cell cell)
        {
            var cellstyle = cell.GetStyle();
            return cell.Value != null && cellstyle.Number != 0 && cellstyle.Number != 49 && !cell.IsErrorValue;
        }
    }
}
