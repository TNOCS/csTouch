using System;
using System.Windows.Data;
using System.Windows.Media;
using csEvents;

namespace csCommon.Plugins.Events
{
    public class AttributeStatusFillConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (!(value[0] is EventState)) return Brushes.LightGray;

            var s = (EventState) value[0];

            if (value[1] == value[2]) {
                switch (s) {
                    case EventState.green:  return Brushes.Green;
                    case EventState.orange: return Brushes.Yellow;
                    case EventState.red:    return Brushes.Red;
                    default: return Brushes.Gray;
                }
            }
            switch (s) {
                case EventState.green:  return Brushes.LightGreen;
                case EventState.orange: return Brushes.LightYellow;
                case EventState.red:    return Brushes.LightCoral;
                default: return Brushes.LightGray;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}