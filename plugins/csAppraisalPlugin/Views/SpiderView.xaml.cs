using WpfCharts;
using csAppraisalPlugin.ViewModels;

namespace csAppraisalPlugin.Views
{
    /// <summary>
    /// Interaction logic for SpiderView.xaml
    /// </summary>
    public partial class SpiderView
    {
        public SpiderView()
        {
            InitializeComponent();
        }

        private void SpiderChartOnValuesChanged(object sender, SpiderChartPanel.ValuesChangedEventArgs e)
        {
            var vm = DataContext as SpiderViewModel;
            if (vm == null) return;
            vm.ValuesChanged(e);
        }
    }
}
