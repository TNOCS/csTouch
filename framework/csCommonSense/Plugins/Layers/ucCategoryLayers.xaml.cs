using System.Windows;
using System.Windows.Controls;
using csShared;

namespace csCommon
{
	/// <summary>
	/// Interaction logic for ucCategoryLayers.xaml
	/// </summary>
	public partial class ucCategoryLayers : UserControl
	{

        public AppStateSettings State { get { return AppStateSettings.GetInstance(); } }

        public string Category
        {
            get { return (string)GetValue(CategoryProperty); }
            set { SetValue(CategoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Category.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register("Category", typeof(string), typeof(ucCategoryLayers), new UIPropertyMetadata("all"));

        
		public ucCategoryLayers()
		{
			this.InitializeComponent();
            this.Loaded += ucCategoryLayers_Loaded;
		}

        void ucCategoryLayers_Loaded(object sender, RoutedEventArgs e)
        {
           
        }
	}
}