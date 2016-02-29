using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ESRI.ArcGIS.Client;
using csPresenterPlugin.Controls;

namespace csPresenterPlugin.Layers
{

    public class MarginSizeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double d = double.Parse(value.ToString());
            return new Thickness(-d/2, -d/2, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for RCDictionary.xaml
    /// </summary>
    public partial class PDictionary : ResourceDictionary
    {
        public PDictionary()
        {
            
        }

        private void Canvas_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = SelectPath(sender);
            
        }

        private bool SelectPath(object sender)
        {
            FrameworkElement c = (FrameworkElement) sender;
            MetroExplorer me = ((DataBinding) c.DataContext).Attributes["explorer"] as MetroExplorer;

            var p = ((DataBinding) c.DataContext).Attributes["path"].ToString();

            
            if (!string.IsNullOrEmpty(p) && me != null)
            {
                me.SelectFolder(p);
                
            }
            return bool.Parse(((DataBinding)c.DataContext).Attributes["fulllayer"].ToString());
        }

        private void Canvas_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = bool.Parse(((DataBinding)((Canvas)sender).DataContext).Attributes["fulllayer"].ToString()); ;
        }

        private void Canvas_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = SelectPath(sender);
            
        }

        

        
    }
}
