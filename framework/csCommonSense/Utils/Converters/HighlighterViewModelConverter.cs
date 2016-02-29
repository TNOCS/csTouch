using csDataServerPlugin;
using DataServer;
using PoiServer.PoI;
using System;
using System.Globalization;
using System.Windows.Data;

namespace csCommon.Converters
{
    public class HighlighterViewModelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return new HighlighterViewModel { Highlighter = (Highlight)values[0], Service = (PoiService)values[1] };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
