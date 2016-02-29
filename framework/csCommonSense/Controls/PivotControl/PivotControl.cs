using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace csCommon.csMapCustomControls.PivotControl
{
    public class PivotControl : TabControl
    {
        static PivotControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PivotControl), new FrameworkPropertyMetadata(typeof(PivotControl)));
        }

        public PivotControl()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var pivotControl = sender as PivotControl;
            if (pivotControl == null) return;
            SelectionChanged += OnSelectionChanged;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PivotItem;
        }

        //protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        //{
        //    base.PrepareContainerForItemOverride(element, item);
        //    var tabItem = element as TabItem;
        //    if (ReferenceEquals(element, item) || tabItem == null) return;
        //    //if (this.HeaderTemplate != null)
        //    //{
        //    //    tabItem.HeaderContent = item;
        //    //    tabItem.HeaderTemplate = this.HeaderTemplate;
        //    //}
        //}

        protected override DependencyObject GetContainerForItemOverride()
        {
            var pivotItem = new PivotItem {HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch};
            //if (this.ItemsContainerStyle != null)
            //    tabItem.Style = this.ItemsContainerStyle;
            return pivotItem;
        }

        private int oldPosition;
        private bool isBusy;
        /// <summary>
        /// When the selection is changed, reorder the tabs such that the selected one becomes the first.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivotControl = sender as PivotControl;
            if (pivotControl == null) return;
            if (isBusy) return;
            isBusy = true;
            var selectedIndex = pivotControl.SelectedIndex;
            if (ItemsSource == null)
            {
                // Data is provided directly, not by data binding.
                for (var i = 0; i < selectedIndex; i++)
                {
                    var item = Items[0];
                    Items.RemoveAt(0);
                    Items.Add(item);
                }
                isBusy = false;
                return;
            }
            var itemsSource = ItemsSource as IList;
            if (itemsSource == null || selectedIndex <= 0 || itemsSource.Count <= 2) { 
                isBusy = false;
                return;
            }
            // Shuffle the items: 0,1,2,3 --> click on 0 --> 0,1,2,3
            // Shuffle the items: 0,1,2,3 --> click on 1 --> 1,2,3,0
            // Shuffle the items: 0,1,2,3 --> click on 2 --> 2,3,0,1
            // Shuffle the items: 0,1,2,3 --> click on 3 --> 3,0,1,2
            // Take the index i=[0..selectedIndex-1] and place them at the end
            for (var i=0; i<selectedIndex; i++)
            {
                var item = itemsSource[0];
                itemsSource.RemoveAt(0);
                itemsSource.Insert(itemsSource.Count, item);
            }
            isBusy = false;
        }
    }
}
