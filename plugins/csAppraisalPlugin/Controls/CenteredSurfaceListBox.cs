using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Controls;
using csAppraisalPlugin.Classes;

namespace csAppraisalPlugin
{
    public class CenteredSurfaceListBox : SurfaceListBox
    {

        public CenteredSurfaceListBox()
        {
            
        }

        public override void BeginInit()
        {
            base.BeginInit();
            this.Loaded += CenteredSurfaceListBox_Loaded;
        }

        private Grid grid;
        private SurfaceScrollViewer ssv;

        private double pos = 0;

        void CenteredSurfaceListBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            base.OnApplyTemplate();

            ssv = GetTemplateChild("scrollViewer") as SurfaceScrollViewer;
            ssv.ScrollChanged += ssv_ScrollChanged;
            
            grid = GetTemplateChild("grid") as Grid;
            SelectionChanged += (et, s) =>
            {
                if (SelectedIndex >= 0)
                {
                    var v1 = (SurfaceListBoxItem) ItemContainerGenerator.ContainerFromIndex(SelectedIndex);
                    var p = v1.TranslatePoint(new Point(v1.ActualWidth/2, v1.ActualHeight/2), grid);
                    
                    ssv.ScrollToHorizontalOffset(p.X + ssv.ContentHorizontalOffset - grid.ActualWidth/2);
                    
                    //ssv.ScrollToHome();
                    pos += 50;

                    //grid.RenderTransform = new TranslateTransform(t - p.X - v1.ActualWidth/2, 0);
                }

            };
            if (SelectedIndex == -1 && Items.Count > 0) SelectedIndex = 0;
        }

        private List<Appraisal> hitResultsList = new List<Appraisal>();

        // Return the result of the hit test to the callback. 
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            if (result.VisualHit is FrameworkElement)
            {
                if (((FrameworkElement) result.VisualHit).DataContext is Appraisal)
                {
                    hitResultsList.Add(((FrameworkElement)result.VisualHit).DataContext as Appraisal);
                }
            }
                
            // Add the hit test result to the list that will be processed after the enumeration.
            

            // Set the behavior to return visuals at all z-order levels. 
            return HitTestResultBehavior.Stop;
        }

        void ssv_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Point pt = new Point(Application.Current.MainWindow.ActualWidth/2,
                                 Application.Current.MainWindow.ActualHeight/2);

            // Clear the contents of the list used for hit test results.
            hitResultsList.Clear();

            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(grid, null,
                new HitTestResultCallback(MyHitTestResult),
                new PointHitTestParameters(pt));

            // Perform actions on the hit test results list. 
            if (hitResultsList.Count > 0)
            {
                Appraisal a = hitResultsList[0];
                if (SelectedValue != a) SelectedValue = a;
                //Console.WriteLine("Number of Visuals Hit: " + hitResultsList.Count);
            }
        }

     
    }
}
