using csShared.Interfaces;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace csCommon.Converters
{
    public class PluginStartStopIconConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var plugin = value as IPlugin;
            if (plugin == null) return "";
            var p = plugin;
            if (p.IsRunning && p.CanStop)
            {
                var strUri1 = String.Format(@"pack://application:,,,/csCommon;component/Resources/Icons/{0}",
                    "appbar.stop.png");
                return new BitmapImage(new Uri(strUri1));
            }

            if (p.IsRunning) return "";
            var strUri2 = String.Format(@"pack://application:,,,/csCommon;component/Resources/Icons/{0}",
                "appbar.play.png");
            return new BitmapImage(new Uri(strUri2));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}