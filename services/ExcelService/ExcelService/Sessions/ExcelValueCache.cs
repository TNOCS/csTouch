using System.ComponentModel;

namespace ExcelService.Sessions
{
    public abstract class ExcelValueCache : INotifyPropertyChanged
    {
        public string Workbook { get; set; }

        protected ExcelValueCache(string workbook, object value)
        {
            Workbook = workbook;
            
            _value = value;
        }

        protected object _value;

        public virtual object Value
        {
            get { return _value; }
            set
            {
                var valueChanged = ExcelWorkbookSession.CheckValueChanged(_value, value);

                if (!valueChanged) return;
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
