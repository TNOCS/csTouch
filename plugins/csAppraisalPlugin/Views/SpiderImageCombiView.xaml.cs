using WpfCharts;
using csAppraisalPlugin.ViewModels;

namespace csAppraisalPlugin.Views
{
    /// <summary>
    /// Interaction logic for SpiderImageCombiView.xaml
    /// </summary>
    public partial class SpiderImageCombiView {
        public SpiderImageCombiView()
        {
            InitializeComponent();
        }

        private void SpiderChartOnValuesChanged(object sender, SpiderChartPanel.ValuesChangedEventArgs e)
        {
            var vm = DataContext as SpiderImageCombiViewModel;
            if (vm == null) return;
            vm.ValuesChanged(e);
        }
    }
}
