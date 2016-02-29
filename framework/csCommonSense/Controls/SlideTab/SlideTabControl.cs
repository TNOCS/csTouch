using System.Windows;
using System.Windows.Controls;

namespace csShared.Controls.SlideTab
{
  public class SlideTabControl : TabControl
  {
    public static readonly DependencyProperty ContainerProperty = DependencyProperty.Register("Container", typeof (FrameworkElement), typeof (SlideTabControl),new UIPropertyMetadata(null));

    public TabOrientation Orientation
    {
      get { return (TabOrientation)GetValue(OrientationProperty); }
      set { SetValue(OrientationProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Orientation. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty OrientationProperty =
      DependencyProperty.Register("Orientation", typeof(TabOrientation), typeof(SlideTabControl), new PropertyMetadata(TabOrientation.Horizontal));


    public FrameworkElement Container
    {
      get { return (FrameworkElement) GetValue(ContainerProperty); }
      set { SetValue(ContainerProperty, value); }
    }

    public SlideTabControl()
    {
      this.SelectionChanged += SlideTabControl_SelectionChanged;
    }

    void SlideTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      foreach (var a in e.AddedItems)
      {
        if (a is SlideTabItem && ((SlideTabItem)a).Item!=null)
        {
          ((SlideTabItem) a).Item.IsSelected = true;
        }
      }
      foreach (var a in e.RemovedItems)
      {
        if (a is SlideTabItem && ((SlideTabItem)a).Item!=null)
        {
          ((SlideTabItem) a).Item.IsSelected = false;
        }
      }
      
    }
  }
}