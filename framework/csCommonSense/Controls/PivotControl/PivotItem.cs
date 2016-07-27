using System.Windows;
using System.Windows.Controls;

namespace csCommon.csMapCustomControls.PivotControl
{
    public class PivotItem : TabItem
    {
        static PivotItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PivotItem), new FrameworkPropertyMetadata(typeof(PivotItem)));            
        }

        public PivotItem()
        {
            TouchDown += (sender, e) =>
            {
                var pivotItem = sender as PivotItem;
                if (pivotItem == null) return;
                pivotItem.IsSelected = true;
                e.Handled = true;
            };
        }
    }
}
