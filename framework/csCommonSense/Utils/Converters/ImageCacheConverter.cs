using csShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace csCommon.Converters
{
    public class ImageCacheConverter : IValueConverter
    {
        private static readonly Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uri = value.ToString();
            uri = AppStateSettings.Instance.MediaC.GetFile(uri, false);
            var u = uri.GetHashCode().ToString(CultureInfo.InvariantCulture);
            if (Cache.ContainsKey(u)) return Cache[u];
            ImageSource iss = new BitmapImage(new Uri(uri));
            Cache[u] = iss;
            return iss;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}