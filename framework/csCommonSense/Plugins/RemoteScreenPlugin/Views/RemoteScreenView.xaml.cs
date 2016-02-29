using System.Windows;
using BaseWPFHelpers;
using Microsoft.Surface.Presentation.Controls;

namespace csRemoteScreenPlugin
{
    /// <summary>
    /// Interaction logic for RemoteScreenView.xaml
    /// </summary>
    public partial class RemoteScreenView
    {
        public RemoteScreenView()
        {
            InitializeComponent();
            Loaded += RemoteScreenViewLoaded;
        }

        //private ScatterViewItem _svi;


        void RemoteScreenViewLoaded(object sender, RoutedEventArgs e)
        {
            //var s = Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
            

        }



    }
}
