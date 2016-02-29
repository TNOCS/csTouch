using System;
using System.Windows;
using System.Windows.Controls;
using WPFSpark;

namespace iTable.Plugins.TimeTab.Views
{
    /// <summary>
    /// Interaction logic for RowView.xaml
    /// </summary>
    public partial class RowView : UserControl
    {
        public RowView()
        {
            InitializeComponent();
        }

        private void FluidMouseDragBehavior_OnChanged(object sender, EventArgs e)
        {
            //var changed = false;
            //var tr = (csEvents.TimeRow) this.DataContext;
            var wp = (FluidWrapPanel) sender;
            int i = 0;
            foreach (FrameworkElement rv in wp.fluidElements)
            {
                var r = (csEvents.TimeRow)rv.DataContext;
                if (r.Order != i)
                {
                    r.Order = i;
                    //changed = true;
                }
                i += 1;
            }
            //if (changed) tr.Tab.ResetTimeline();
            
        }
    }
}
