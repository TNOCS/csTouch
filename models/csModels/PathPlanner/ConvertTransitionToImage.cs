using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace csModels.PathPlanner
{
    public class ConvertTransitionToImage : MarkupExtension, IValueConverter
    {
        private static ConvertTransitionToImage instance;


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ?? (instance = new ConvertTransitionToImage());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format("Images/{0}.png", value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}