using System.Windows.Data;
using DataServer;

namespace csModels.Flow
{

    public class FlowStopNameConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var fs = (FlowStop) value;
            
            return (fs.Direction == FlowDirection.source) ? fs.Source.Name : fs.Target.Name;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class FlowStopImageConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var fs = (FlowStop)value;

            return (fs.Direction == FlowDirection.source) ? fs.Source.NEffectiveStyle.Picture : fs.Target.NEffectiveStyle.Picture;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public static class FlowPoIExtensions
    {
        public static FlowCollection Flow(this PoI source)
        {
            FlowCollection fc = null;

            if (source == null) return null;

            if (!source.Data.ContainsKey("Flow"))
            {
                fc = new FlowCollection();
                source.Data["Flow"] = fc;
            }
            else if (source.Data["Flow"] is FlowCollection)
            {
                fc = (FlowCollection)source.Data["Flow"];
            }
            return fc ?? (fc = new FlowCollection());
        }

        public static FlowCollection InFlow(this PoI source)
        {
            FlowCollection fc = null;

            if (!source.Data.ContainsKey("InFlow"))
            {
                fc = new FlowCollection();
                source.Data["InFlow"] = fc;
            }
            else if (source.Data["InFlow"] is FlowCollection)
            {
                fc = (FlowCollection)source.Data["InFlow"];
            }
            return fc ?? (fc = new FlowCollection());
        }
    }
}