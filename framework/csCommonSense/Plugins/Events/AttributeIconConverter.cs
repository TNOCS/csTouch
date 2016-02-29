using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace csCommon.Plugins.Events
{
    public class AttributeIconConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var s = value as string;
            if (!string.IsNullOrEmpty(s)) return null;
            try
            {
                var im = new BitmapImage(new Uri(s));
                return im;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            // FIXME TODO: Unreachable code
            // return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}