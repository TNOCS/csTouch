namespace ExcelService.Sessions
{
    public class NameCache : ExcelValueCache
    {
        public string Name { get; set; }

        public NameCache(string workbook, string name, object value) : base(workbook, value)
        {
            Name = name;
        }

        public override object Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                var valueChanged = ExcelWorkbookSession.CheckNameRangeChanged((object[])_value, (object[])value);

                if (!valueChanged) return;
                _value = value;
                OnPropertyChanged("Value");
            }
        }
    }
}
