using DataServer;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace csCommon.Converters
{
    public class PoiIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var p = value as PoI;
            if (p == null) return null;
            if (p.NEffectiveStyle.Picture != null) return p.NEffectiveStyle.Picture;
            var s = p.Service.MediaFolder + p.NEffectiveStyle.Icon;

            if (p.Service.store.HasFile(s))
                p.NEffectiveStyle.Picture = new BitmapImage(new Uri(s));
            return p.NEffectiveStyle.Picture;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }
    }
}