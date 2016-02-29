using System.Windows.Data;

namespace csRemoteScreenPlugin
{
    public class CompletedOpacityConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ? 1.0 : 0.5;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}