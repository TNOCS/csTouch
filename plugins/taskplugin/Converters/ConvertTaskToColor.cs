using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using DataServer;
using csShared;

namespace TasksPlugin.Converters {
    public class ConvertTaskToColor : MarkupExtension, IValueConverter {
        private const string RecipientsKey = "Recipients";
        private static readonly SolidColorBrush DefaultBrush = Brushes.Transparent;

        private static ConvertTaskToColor instance;

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return instance ?? (instance = new ConvertTaskToColor());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var task = value as Task;
            if (task == null || !task.Labels.ContainsKey(RecipientsKey)) return DefaultBrush;
            var me = AppStateSettings.Instance.Imb.Imb.OwnerName;
            var recipients = Enumerable.ToList(task.Labels[RecipientsKey].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));
            return recipients.Contains(me)
                ? Brushes.LightSalmon
                : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}