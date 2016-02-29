using System.Windows;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Controls;

namespace csDataServerPlugin.Views
{
    /// <summary>
    /// Interaction logic for MetaLabelsView.xaml
    /// </summary>
    public partial class MediaView
    {
        public MediaView()
        {
            InitializeComponent();
        }

        public void SelectOption_OnClick(object s, RoutedEventArgs e)
        {
            var sender = s as SurfaceButton;
            sender.Background = Brushes.Yellow;
        }
    }
}
