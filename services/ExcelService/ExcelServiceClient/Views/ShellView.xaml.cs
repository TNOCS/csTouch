using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExcelServiceClient.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : UserControl
    {
        public ShellView()
        {
            InitializeComponent();
        }

        public void PreviewNumberInput(object sender, TextCompositionEventArgs args)
        {
            var regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            args.Handled = regex.IsMatch(args.Text);
        }
    }
}
