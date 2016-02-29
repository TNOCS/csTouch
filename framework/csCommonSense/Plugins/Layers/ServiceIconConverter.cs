using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using csDataServerPlugin;
using csShared;

namespace csCommon
{
    public class ServiceIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var l = (sLayer) value;
            var layer = l.Layer as dsBaseLayer;
            if (layer != null) {
                var s = layer.Service;
                var u = s.IconUri;
                if (u.AbsolutePath != "pack://application:,,,/iTable;component/icons/camera.png")
                    return new BitmapImage(s.IconUri);
            }

            foreach (var fi in AppStateSettings.Instance.ViewDef.FolderIcons.OrderByDescending(k => k.Key.Length)) {
                if (l.Path != null && l.Path.StartsWith(fi.Key)) return new BitmapImage(new Uri(fi.Value));
            }
            return new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/layers4.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}