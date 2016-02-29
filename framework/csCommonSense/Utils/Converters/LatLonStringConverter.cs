using csShared;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Globalization;
using System.Windows.Data;

namespace csCommon.Converters
{
    public class LatLonStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var point = value as MapPoint;
            if (point == null) return null;
            var r = AppStateSettings.Instance.ViewDef.Resolution;
            var kp = point;
            var format = "###.######";
            if (r > 1000)
                format = "###.##";
            else if (r > 100)
                format = "###.###";
            else if (r > 10)
                format = "###.#####";
            return string.Format(CultureInfo.InvariantCulture, "Lon: {0}, Lat: {1}", kp.X.ToString(format), kp.Y.ToString(format));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}