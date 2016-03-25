using System;
using System.Globalization;
using System.IO;
using System.Management.Instrumentation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using csShared;
using DataServer;
using DrWPF.Windows.Data;

namespace WpfConverters
{

    public class DoubleIsZeroVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DictionaryBoolVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is String) || !(value is ObservableDictionary<string, bool>)) return Visibility.Collapsed;
            var v = (ObservableDictionary<string, bool>)value;
            if (v.ContainsKey((string)parameter))
                return v[(string)parameter] ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class ReverseBooleanVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new BooleanToVisibilityConverter().Convert(!(bool)value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Converts a boolean value to a visibility, where a value of true --> Visible. 
    /// In case the ConverterParameter = false, it is the reverse, i.e. a value of true --> Collapsed.
    /// </summary>
    public class ConvertBoolToVisibility : MarkupExtension, IValueConverter
    {
        private static ConvertBoolToVisibility converter;

        #region IValueConverter Members

        public object Convert(object pValue, Type pTargetType, object pParameter, CultureInfo pCulture)
        {
            if (pValue == null) return null;
            var visibility = true;
            if (pParameter != null) bool.TryParse(pParameter as string, out visibility);
            return ((bool)pValue == visibility) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object pValue, Type pTargetType, object pParameter, CultureInfo pCulture)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Overrides of MarkupExtension

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new ConvertBoolToVisibility());
        }

        #endregion
    }

    public class MapTypeTitleConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value is MapTypes)
            //{
            //    MapTypesToStringConverter m = new MapTypesToStringConverter();

            //}
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class TimeAgoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime)) return null;
            var d = (DateTime)value;
            var ts = (DateTime.Now - d);
            if (ts.TotalSeconds < 60) return (int)ts.TotalSeconds + " seconds ago";
            if (ts.TotalMinutes < 60) return (int)ts.TotalMinutes + " minutes ago";
            if (ts.TotalHours < 24) return (int)ts.TotalHours + " hours ago";
            if (ts.TotalDays < 24) return (int)ts.TotalDays + " days ago";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class String2ColorConverter : MarkupExtension, IValueConverter
    {
        private static String2ColorConverter instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                var c = (Color)ColorConverter.ConvertFromString(value as string);
                return new SolidColorBrush(c);
            }
            if (!(value is Color)) return null;
            var col = (Color)value;
            return new SolidColorBrush(col);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new String2ColorConverter());
        }
    }

    /// <summary>
    /// Convert a number in the range 0..100 to a color, where 0 is green and 100 is red.
    /// </summary>
    public class Number2ColorConverter : MarkupExtension, IValueConverter
    {
        private static Number2ColorConverter instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return new SolidColorBrush(scaleInputBetweenGreenAndRed((double)value));
            }
            else if (value is float)
            {
                return new SolidColorBrush(scaleInputBetweenGreenAndRed((float)value));
            }
            else if (value is int)
            {
                return new SolidColorBrush(scaleInputBetweenGreenAndRed((int)value));
            }
            return Brushes.Transparent;
        }

        /// <summary>
        /// Scale the input value, in the range of [0..100] to green (0), yellow (50) and red (100).
        /// </summary>
        /// <param name="inputValue"></param>
        /// <returns></returns>
        private static Color scaleInputBetweenGreenAndRed(double inputValue)
        {
            return Color.FromArgb(255, (byte)(inputValue < 50 ? 255 * inputValue / 50 : 255), (byte)(inputValue <= 50 ? 255 : 255 * (2 - inputValue / 50)), 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new Number2ColorConverter());
        }
    }

    public class NotNullVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null || (value is ItemCollection && ((ItemCollection)value).Count == 0))
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    public class ReverseBooleanActiveOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (!(value is bool)) return 1;
            if (!(bool)value) return 1;
            return string.IsNullOrEmpty((string)parameter)
                ? 0.25
                : Double.Parse(parameter.ToString(), CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BooleanActiveOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool)) return 1;
            if ((bool)value) return 1;
            return string.IsNullOrEmpty((string)parameter)
                ? 0.5
                : Double.Parse(parameter.ToString(), CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public class ReverseBooleanActiveOpacityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is bool)) return 1;
                if (!(bool)value) return 1;
                return string.IsNullOrEmpty((string)parameter)
                    ? 0.5
                    : Double.Parse(parameter.ToString(), CultureInfo.InvariantCulture);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }
    }

    public class BooleanHiddenConverter : IValueConverter
    {
        public static BooleanHiddenConverter Instance = new BooleanHiddenConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IconConverter : IValueConverter
    {
        private static readonly string RootFolder = Directory.GetCurrentDirectory();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Uri) value = value.ToString();
            if (!(value is string)) return null;
            var v = value as string;
            if (v.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)) return new BitmapImage(new Uri(v));
            var file = Path.Combine(RootFolder, value.ToString());
            return File.Exists(file) ? new BitmapImage(new Uri("file://" + file)) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }

        #endregion
    }

    public class BorderRadiusConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return ((double)value) / 2;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }

        #endregion
    }

    public class ConvertStringToInt : MarkupExtension, IValueConverter
    {
        private static ConvertStringToInt instance;
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertStringToInt());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string)) return value;
            double result;
            return double.TryParse((string)value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result)
                ? Math.Round(result)
                : double.TryParse((string)value, NumberStyles.AllowDecimalPoint, CultureInfo.InstalledUICulture, out result)
                    ? Math.Round(result)
                    : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// Subtract a certain amount of a double.
    /// </summary>
    public class SubtractConverter : MarkupExtension, IValueConverter
    {
        private static SubtractConverter instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new SubtractConverter());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double) || !(parameter is string)) return 0;
            var amount = (double)value;
            double subtractAmount;
            if (double.TryParse((string)parameter, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out subtractAmount))
            {
                return amount - subtractAmount;
            }
            return amount;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ConvertNumberToBool : MarkupExtension, IValueConverter
    {
        private static ConvertNumberToBool converter;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (value is bool) return value;
            var s = value as string;
            if (s != null)
            {
                if (string.IsNullOrEmpty(s)) return false;
                s = s.ToLower();
                if (s == "true") return true;
                if (s == "false") return false;
                double result;
                return (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out result)) && result > 0;
            }
            if (value is double) return (double)value > 0;
            if (value is int) return (int)value > 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider) { return converter ?? (converter = new ConvertNumberToBool()); }
    }

    public class ConvertIndexToCharacter : MarkupExtension, IValueConverter
    {
        private static ConvertIndexToCharacter instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = (ListBoxItem)value;
            if (item == null) return null;
            var listView = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
            if (listView == null) return null;
            var index = listView.ItemContainerGenerator.IndexFromContainer(item);
            return (char)('a' + index);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertIndexToCharacter());
        }
    }

    public class ConvertTitleResolutionToVisibility : MarkupExtension, IMultiValueConverter
    {
        private static ConvertTitleResolutionToVisibility instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertTitleResolutionToVisibility());
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var poi = values[0] as PoI;
            if (poi == null) return Visibility.Collapsed;
            return (poi.NEffectiveStyle.TitleMode == TitleModes.None 
                || poi.NEffectiveStyle.MaxTitleResolution < AppStateSettings.Instance.ViewDef.MapControl.Resolution)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConvertNothing : MarkupExtension, IValueConverter
    {
        private static ConvertNothing instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertNothing());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    /// <summary>
    /// Simple converter that returns unset value in case an image's source isn't available.
    /// Although you do not need to do this, it does cause a performance hit.
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/5399601/imagesourceconverter-error-for-source-null"/>
    public class NullImageConverter : MarkupExtension, IValueConverter
    {
        private static NullImageConverter instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value is string && string.IsNullOrEmpty((string)value)))
                return DependencyProperty.UnsetValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new NullImageConverter());
        }
    }

    /// <summary>
    /// Simple converter that returns unset value in case an image's source isn't available.
    /// Although you do not need to do this, it does cause a performance hit.
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/5399601/imagesourceconverter-error-for-source-null"/>
    public class ConvertColorToBrush : MarkupExtension, IValueConverter
    {
        private static ConvertColorToBrush instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Color 
                ? new SolidColorBrush((Color)value)
                : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertColorToBrush());
        }
    }

    /// <summary>
    /// This converter facilitates a couple of requirements around images. Firstly, it automatically disposes of image streams as soon as images
    /// are loaded, thus avoiding file access exceptions when attempting to delete images. Secondly, it allows images to be decoded to specific
    /// widths and / or heights, thus allowing memory to be saved where images will be scaled down from their original size.
    /// </summary>
    public sealed class ConvertBitmapFrame : MarkupExtension, IValueConverter
    {
        private ConvertBitmapFrame instance;

        //doubles purely to facilitate easy data binding

        public double DecodePixelWidth { get; set; }

        public double DecodePixelHeight { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var path = value as string;

            if (path == null) return DependencyProperty.UnsetValue;
            //create new stream and create bitmap frame
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
            bitmapImage.DecodePixelWidth = (int)DecodePixelWidth;
            bitmapImage.DecodePixelHeight = (int)DecodePixelHeight;
            //load the image now so we can immediately dispose of the stream
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            //clean up the stream to avoid file access exceptions when attempting to delete images
            bitmapImage.StreamSource.Dispose();

            return bitmapImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertBitmapFrame());
        }
    }
}