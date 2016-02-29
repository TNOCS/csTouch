using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Aspose.Cells;
using ExcelService.Properties;
using ExcelServiceModel;
using log4net;

namespace ExcelService.Sessions
{
    public class ExcelWorkbookSession : ExcelSession
    {
        private readonly ILog log = log4net.LogManager.GetLogger(typeof(ExcelWorkbookSession));

        protected readonly List<ExcelValueCache> _watchedItems;
        public List<ExcelValueCache> WatchedItems { get { return _watchedItems; } }

        public event EventHandler<CellCache> CellValueChanged = delegate { };
        public event EventHandler<NameCache> NameValueChanged = delegate { }; 

        public static bool CheckValueChanged(object left, object right)
        {
            if (right is int || right is long || right is float || right is short)
            {
                right = Convert.ToDouble(right);
            }
            
            var comparable = left as IComparable;
            if (comparable != null)
            {
                if (comparable.CompareTo(right) != 0)
                {
                    return true;
                }
                return false;
            }

            if ((left == null) && (right == null))
            {
                return false;
            }

            if ((left == null) != (right == null))
            {
                return true;
            }

            if (left.ToString() == right.ToString())
            {
                return true;
            }

            return false;
        }

        public static bool CheckNameRangeChanged(object[] left, object[] right)
        {
            if (left == null && right == null) return false;

            if ((left == null) != (right == null))
            {
                return true;
            }

            if (left.Length != right.Length) return true;

            return left.Where((t, i) => CheckValueChanged(t, right[i])).Any();
        }

        public ExcelWorkbookSession(ExcelSession session) : base(session)
        {
            _watchedItems = new List<ExcelValueCache>();

            log.DebugFormat("Open workbook {0}", WorkbookName);

            var workbookPath =  Path.Combine(Settings.Default.WorkbookFolder, WorkbookName);
            var workbookFile = new FileInfo(workbookPath);
            if (workbookFile.Exists)
            {
                log.DebugFormat("Load workbook from disk: {0}", workbookFile.FullName);
                var loadOptions = new LoadOptions
                    {
                        LoadDataAndFormatting = true
                    };
                Workbook = new Workbook(workbookPath, loadOptions);
            }
            else
            {
                log.DebugFormat("Workbook file {0} not found, look for workbook in embedded resources", workbookFile.FullName);
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var resourceInfo = assembly.GetManifestResourceInfo(WorkbookName);

                        if (resourceInfo == null) continue;
                        log.DebugFormat("Workbook resource found in asssembly {0}.", assembly.FullName);
                        using (var workbookStream = assembly.GetManifestResourceStream(WorkbookName))
                        {
                            Workbook = new Workbook(workbookStream);
                            log.DebugFormat("Workbook loaded: {0}", Workbook);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            if (Workbook == null)
            {
                log.ErrorFormat("Could not locate a workbook named {0}", WorkbookName);

                throw new Exception(string.Format("Could not locate a workbook named {0}", WorkbookName));
            }

            if (session.UseCalculationChain)
            {
                log.DebugFormat("Calculate workbook formulas");
                Workbook.Settings.CreateCalcChain = true;
                Workbook.CalculateFormula(true);
                log.DebugFormat("Formulas calculated");
            }
            else
            {
                Workbook.Settings.CreateCalcChain = false;
            }

            log.DebugFormat("Add watches");
            if (WatchAllFormulaCells)
            {
                foreach (var sheet in Workbook.Worksheets.Cast<Worksheet>())
                {
                    foreach (var cell in sheet.Cells.Cast<Cell>().Where(c => c.IsFormula))
                    {
                        WatchCell(sheet.Name, cell.Name);
                    }
                }
            }

            if (WatchAllNames)
            {
                foreach (var name in Workbook.Worksheets.Names.Cast<Name>())
                {
                    WatchName(name.Text);
                }
            }

            log.DebugFormat("Watches added");
        }

        public void UpdateWatchedItems()
        {
            lock (Workbook)
            {
                log.DebugFormat("Start recalculating sheet");
                Workbook.CalculateFormula(true);
                log.DebugFormat("Finished recalculating sheet");

                log.DebugFormat("Update watched items");
                foreach (var valueCache in _watchedItems)
                {
                    Cell cell = null;
                    if (valueCache is CellCache)
                    {
                        var cellCache = (CellCache)valueCache;
                        cellCache.Value = GetCellValue(cellCache.Worksheet, cellCache.CellName);
                    }
                    else if (valueCache is NameCache)
                    {
                        var nameCache = (NameCache)valueCache;
                        nameCache.Value = GetNameValue(nameCache.Name);
                    }

                }
                log.DebugFormat("Watched items updated");
            }
        }

        public void WatchCell(string worksheet, string cellName)
        {
            if (_watchedItems.OfType<CellCache>().Any(wi => wi.Worksheet == worksheet && wi.CellName == cellName)) return;

            Cell cell = null;
            try
            {
                cell = Workbook.Worksheets[worksheet].Cells[cellName];
            }
            catch (Exception ex)
            {
                log.Debug(ex.Message);
                return;
            }
            object cellValue = cell.Value;
            
            var cache = new CellCache(WorkbookName, worksheet, cellName, cellValue);
            cache.PropertyChanged += CacheOnPropertyChanged;
            _watchedItems.Add(cache);
        }

        public void WatchName(string name)
        {
            if (_watchedItems.OfType<NameCache>().Any(wi => wi.Name == name)) return;

            var nameValue = GetNameValue(name);

            var cache = new NameCache(WorkbookName, name, nameValue);
            cache.PropertyChanged += CacheOnPropertyChanged;
            _watchedItems.Add(cache);
        }

        public object GetCellValue(string worksheet, string cellName)
        {
            Cell cell;
            try
            {
                cell = Workbook.Worksheets[worksheet].Cells[cellName];
            }
            catch (Exception ex)
            {
                return null;
            }

            if (cell == null || cell.IsErrorValue) return null;

            var cellValue = cell.Value;

            return cellValue;
        }

        public object[] GetNameValue(string name)
        {
            var namedRange = Workbook.Worksheets.GetRangeByName(name);
            if (namedRange == null) return null;

            var cells = Enumerable.Range(0, namedRange.RowCount).SelectMany(rowIndex => Enumerable.Range(0, namedRange.ColumnCount).Select(columnIndex => namedRange.GetCellOrNull(rowIndex, columnIndex)));
            var cellValues = cells.Select(cell => (cell == null || cell.IsErrorValue) ? null : cell.Value).ToArray();

            return cellValues;
        }

        public void UpdateCell(string worksheet, string cellName, object value)
        {
            var cellValue = Workbook.Worksheets[worksheet].Cells[cellName].Value;
            if (!CheckValueChanged(value, cellValue))
            {
                return;
            }

            Workbook.Worksheets[worksheet].Cells[cellName].Value = value;

            UpdateWatchedItems();
        }

        public void UpdateName(string name, object[] values)
        {
            var namedRange = Workbook.Worksheets.GetRangeByName(name);
            if (namedRange == null) return;

            var nameValue = GetNameValue(name);
            if (!CheckNameRangeChanged(nameValue, values))
            {
                return;
            }

            var cells = Enumerable.Range(0, namedRange.RowCount).SelectMany(rowIndex => Enumerable.Range(0, namedRange.ColumnCount).Select(columnIndex => namedRange.GetCellOrNull(rowIndex, columnIndex))).ToArray();
            if (cells.Length != values.Length) throw new ArgumentException("Number of values passed do not match the number of cells in the named range");
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (!cells[cellIndex].IsFormula)
                {
                    var value = values[cellIndex];
                    if (value is double)
                    {
                        var cell = cells[cellIndex];
                        cell.PutValue((double) value);
                    }
                    cells[cellIndex].PutValue(value);
                }
                else
                {
                    log.DebugFormat("Cell {0}!{1} value not updated: cell contains a formula", cells[cellIndex].Worksheet.Name, cells[cellIndex].Name);
                }
            }

            UpdateWatchedItems();
        }

        private void CacheOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Value")
            {
                if (sender is CellCache)
                {
                    var cache = (CellCache) sender;
                    CellValueChanged(this, cache);                    
                }
                else if (sender is NameCache)
                {
                    var cache = (NameCache) sender;
                    NameValueChanged(this, cache);
                }
            }
        }

        public Workbook Workbook { get; private set; }
    }
}
