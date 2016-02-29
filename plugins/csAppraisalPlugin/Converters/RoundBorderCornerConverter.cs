using System;
using System.Globalization;
using System.Windows.Data;

namespace csAppraisalPlugin.Converters
{
    public class RoundBorderCornerConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value) / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}