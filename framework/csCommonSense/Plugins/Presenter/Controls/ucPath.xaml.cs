using System.IO;
using System.Windows;
using System.Windows.Media;
using nl.tno.cs.presenter;

namespace csPresenterPlugin.Controls
{
    /// <summary>
    /// Interaction logic for ucPath.xaml
    /// </summary>
    public partial class ucPath
    {
        public ucPath()
        {
            InitializeComponent();
            Loaded += UcPathLoaded;
        }

        private MetroExplorer me;
        private string path;

        void UcPathLoaded(object sender, RoutedEventArgs e)
        {
            var d = ((FrameworkElement) sender).DataContext as DirectoryInfo;
            if (d == null || !d.Exists) return;
            path = d.FullName;
            Path.Content = d.Name.CleanName();
            me = FindAncestor<MetroExplorer>(this);

            tbArrow.Visibility = (new DirectoryInfo(me.StartPath).FullName == path)
                                     ? Visibility.Collapsed
                                     : Visibility.Visible;
        }

        public static T FindAncestor<T>(DependencyObject from) where T : class
        {
            if (from == null)
            {
                return null;
            }

            var candidate = from as T;
            return candidate ?? FindAncestor<T>(VisualTreeHelper.GetParent(@from));
        }

        private void Path_Click(object sender, RoutedEventArgs e)
        {            
            me.SelectFolder(path);
        }
    }
}
