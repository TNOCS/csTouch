using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using DataServer;
using TasksPlugin.Images;

namespace TasksPlugin.Converters {
    public class ConvertTaskStatusToImage : MarkupExtension, IValueConverter {
        private ConvertTaskStatusToImage instance;

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return instance ?? (instance = new ConvertTaskStatusToImage());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var trafficLight = new TrafficLight();
            switch ((TaskState) value) {
                case TaskState.None:
                case TaskState.Open:
                    trafficLight.Top.Fill = Brushes.Red;
                    trafficLight.Top.Fill = Brushes.Red;
                    return trafficLight;
                case TaskState.Inprogress:
                    trafficLight.Middle.Fill = Brushes.Orange;
                    return trafficLight;
                case TaskState.Finished:
                    trafficLight.Bottom.Fill = Brushes.Green;
                    return trafficLight;
                default:
                    return trafficLight;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}