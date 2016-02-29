using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace csGeoLayers.ShapeFiles
{
    public class RgbConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var r = System.Convert.ToByte(values[0]);
            var g = System.Convert.ToByte(values[1]);
            var b = System.Convert.ToByte(values[2]);

            return Color.FromRgb(r, g, b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    /// <summary>
    /// Interaction logic for ShapeLayerColorPickerView.xaml
    /// </summary>
    public partial class ShapeLayerColorPickerView : UserControl
    {
        public ShapeLayerColorPickerView()
        {
            InitializeComponent();
        }
    }
}
