using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Surface.Presentation.Controls;

namespace csShared.Controls.TwoWaySliding
{
  /// <summary>
  /// Interaction logic for TwoWaySlidingView.xaml
  /// </summary>
  public partial class TwoWaySlidingView : UserControl
  {
    DispatcherTimer timer;
    //DispatcherTimer animateTimer;
    bool scrollChanged = false;
    //bool autoScroll = false;
    double vertScrolled = 0;
    double horScrolled = 0;

    double scrollFrom = 0;
    //double scrollCurrent = 0;
    double scrollTo = 0;
    bool scrollAnimating = false;
    //double scrollStep = 50;

    bool mouseButtonDown = false;
    bool touchDown = false;
    Point mouseMoveFrom;
    Point mouseMoveTo;
    Point mouseMoveOriginal;

    double horScrollMargin = 25;

    public double listboxsize;

    double lastHorScrolled = -1;
    double lastVerScrolled = 0;

    bool doScrollToVert = false;

    public TwoWaySlidingView()
    {
      InitializeComponent();
      this.Collection.ScrollChanged += Collection_ScrollChanged;


      // Mouse events voor scrollen
      this.CollectionList.PreviewMouseLeftButtonDown += CollectionList_MouseLeftButtonDown;
      this.CollectionList.PreviewMouseLeftButtonUp += CollectionList_MouseLeftButtonUp;
      this.CollectionList.MouseLeave += CollectionList_MouseLeave;
      this.CollectionList.PreviewMouseMove += CollectionList_PreviewMouseMove;

      // Touch events voor scrollen
      this.CollectionList.PreviewTouchDown += CollectionList_PreviewTouchDown;
      this.CollectionList.PreviewTouchUp += CollectionList_PreviewTouchUp;
      this.CollectionList.PreviewTouchMove += CollectionList_PreviewTouchMove;
      this.CollectionList.TouchLeave += CollectionList_TouchLeave;

      this.Collection.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
      this.Collection.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

      timer = new DispatcherTimer();
      timer.Tick += DispatchWorker;
      timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
      timer.Start();
      this.SizeChanged += TwoWaySlidingView_SizeChanged;
    }

    void CollectionList_TouchLeave(object sender, TouchEventArgs e)
    {
      touchDown = false;
    }

    void CollectionList_PreviewTouchMove(object sender, TouchEventArgs e)
    {
      if (touchDown)
      {
        mouseMoveTo = e.TouchDevice.GetTouchPoint(this.Collection).Position;
        double difX = mouseMoveFrom.X - mouseMoveTo.X;
        if (difX != 0)
          this.Collection.ScrollToHorizontalOffset(this.Collection.HorizontalOffset + difX);
        mouseMoveFrom = mouseMoveTo;
      }
    }



    void CollectionList_PreviewTouchUp(object sender, TouchEventArgs e)
    {
      touchDown = false;
      mouseMoveTo = e.TouchDevice.GetTouchPoint(this.Collection).Position;
      double difX = mouseMoveOriginal.X - mouseMoveTo.X;
      double difY = mouseMoveOriginal.Y - mouseMoveTo.Y;
      if (Math.Abs(difX) > horScrollMargin) // || Math.Abs(difY) > horScrollMargin)
        e.Handled = true;
    }

    void CollectionList_PreviewTouchDown(object sender, TouchEventArgs e)
    {
      touchDown = true;
      mouseMoveFrom = e.TouchDevice.GetTouchPoint(this.Collection).Position;
    }

    void CollectionList_MouseLeave(object sender, MouseEventArgs e)
    {
      mouseButtonDown = false;
    }

    void CollectionList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      mouseButtonDown = false;

      mouseMoveTo = e.MouseDevice.GetPosition(this.Collection);
      double difX = mouseMoveOriginal.X - mouseMoveTo.X;
      double difY = mouseMoveOriginal.Y - mouseMoveTo.Y;
      //e.Handled = true;
      if (Math.Abs(difX) > horScrollMargin) // || Math.Abs(difY) > horScrollMargin)
      {
        //do generate mouseclick or select item which is undereath the mouse
        e.Handled = true;
        e.MouseDevice.DirectlyOver.ReleaseMouseCapture();
      }
      //  e.Handled = true;

    }

    void CollectionList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      mouseButtonDown = true;
      mouseMoveOriginal = e.MouseDevice.GetPosition(this.Collection);
      mouseMoveFrom = e.MouseDevice.GetPosition(this.Collection);
      //e.Handled = true;
    }

    void CollectionList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
      // Prefer touch over mouse moves
      if (mouseButtonDown && !touchDown)
      {
        mouseMoveTo = e.MouseDevice.GetPosition(this.Collection);
        double difX = mouseMoveFrom.X - mouseMoveTo.X;

        if (difX != 0)
          this.Collection.ScrollToHorizontalOffset(this.Collection.HorizontalOffset + difX);
        double difY = mouseMoveFrom.Y - mouseMoveTo.Y;
        if (difY != 0)
        {
          //this.Collection.ScrollToVerticalOffset(this.Collection.VerticalOffset + difY);
        }
        mouseMoveFrom = mouseMoveTo;
        //e.Handled = true;
      }
    }

    void TwoWaySlidingView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (e.WidthChanged)
      {
        double newWidth = this.Collection.ActualWidth;
        int itemNr = 1;
        int maxNr = this.HeaderList.Items.Count;
        foreach (singleSlide ss in this.HeaderList.Items)
        {
          if (itemNr == 1 || itemNr == maxNr)
          {
            ss.ColWidth = newWidth * 0.5;
            ss.ColWidthHeader = newWidth * 0.25;
          }
          else
          {
            ss.ColWidth = newWidth;
            ss.ColWidthHeader = newWidth * 0.5;
          }
          itemNr++;


        }
      }
      if (e.HeightChanged)
      {
        foreach (singleSlide ss in this.HeaderList.Items)
        {
          if (this.ActualHeight > 40)
            ss.ColHeight = this.ActualHeight - 40;
          else
          {
            ss.ColHeight = 0;
          }
        }
      }
    }



    void DispatchWorker(object sender, EventArgs e)
    {
      if (doScrollToVert)
      {
        doScrollToVert = false;
        this.Collection.ScrollToVerticalOffset(lastVerScrolled);
      }
      if (!this.Collection.IsScrolling && scrollChanged && !mouseButtonDown && !touchDown)
      {
        // Scroll has ended, check position to scroll further to an item
        int scrollRemVert;
        if (this.CollectionList.Items.Count == 0) return;
        int verWidth = Convert.ToInt32(this.CollectionList.ActualHeight) / (((singleSlide)this.CollectionList.Items[0]).Items.Count);
        int scrollToVert = Math.DivRem(Convert.ToInt32(vertScrolled), verWidth, out scrollRemVert);

        // For now no vertical scrolling
        if (scrollRemVert != 0)
        {
          //autoScroll = true;
          //this.Collection.ScrollToVerticalOffset(Convert.ToDouble((scrollToVert + 1) * verWidth));
        }

        //int scrollToHor = Convert.ToInt32(vertScrolled) / 100;

        double scrollDif = horScrolled - scrollTo;
        int direction = 1;
        if (scrollDif < 0)
          direction = 0;
        int nrItems = this.CollectionList.Items.Count;
        int horWidth = Convert.ToInt32(this.CollectionList.ActualWidth) / nrItems;
        int scrollRemHor;
        int scrollToHor = Math.DivRem(Convert.ToInt32(horScrolled), horWidth, out scrollRemHor);
        if (scrollRemHor != 0 && Math.Abs(scrollDif) > horScrollMargin)
        {
          //autoScroll = true;
          scrollFrom = horScrolled;
          if (scrollToHor == (nrItems - 1))
            direction = 0;
          scrollTo = Convert.ToDouble((scrollToHor + direction) * horWidth);
          if (scrollTo < 0)
            scrollTo = 0;

          scrollAnimating = true;
          int tmAnim = Convert.ToInt32(Convert.ToDouble(Math.Abs(scrollFrom - scrollTo)) * 600d / Convert.ToDouble(horWidth));
          Duration animDur = new Duration(new TimeSpan(0, 0, 0, 0, tmAnim));
          DoubleAnimation danim = new DoubleAnimation(scrollFrom, scrollTo, animDur);
          danim.EasingFunction = new ExponentialEase() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut, Exponent = 6 };

          Storyboard myStoryboard = new Storyboard();
          myStoryboard.Completed += myStoryboard_Completed;
          myStoryboard.Children.Add(danim);
          Storyboard.SetTarget(danim, this.Collection);
          Storyboard.SetTargetProperty(danim, new PropertyPath(SurfaceScrollViewerUtilities.HorizontalOffsetProperty));
          myStoryboard.Begin();
        }
        else if (Math.Abs(scrollDif) <= horScrollMargin)
        {
          this.Collection.ScrollToHorizontalOffset(this.Collection.HorizontalOffset - scrollDif);
        }
        scrollChanged = false;
      }
    }

    void myStoryboard_Completed(object sender, EventArgs e)
    {
      scrollAnimating = false;
    }



    void Collection_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      // Makes sure the header will also scroll
      vertScrolled = e.VerticalOffset;
      if (Math.Abs(e.HorizontalChange) != 0)
      {
        horScrolled = e.HorizontalOffset;
      }
      if ((Math.Abs(e.VerticalChange) != 0) && (touchDown || mouseButtonDown))
        lastVerScrolled = e.VerticalOffset;
      if (lastHorScrolled == -1)
        lastHorScrolled = horScrolled;
      if (!scrollAnimating)
        scrollChanged = true;

      if (Math.Abs(e.HorizontalChange) == 0)
        return;
      int nrItems = this.CollectionList.Items.Count;
      if (nrItems == 0) return;
      double dScr = horScrolled / 2.0d; //((((singleSlide)this.CollectionList.Items[0]).ColWidth / 3.0d) * horScrolled / this.CollectionList.ActualWidth) + (horScrolled / 2.0d);
      int horWidth = Convert.ToInt32(this.CollectionList.ActualWidth) / nrItems;
      Header.ScrollToHorizontalOffset(dScr);
      lastHorScrolled = horScrolled;
    }

    public void ScrollToLastVerPos()
    {
      //Collection.ScrollToVerticalOffset(lastVerScrolled);
      doScrollToVert = true;
    }

    private SurfaceListBox selectedListBox = null;
    private object selectedItem = null;

    public string selectedItemString = "";

    //  void lbSelectionChanged(object sender, SelectionChangedEventArgs args)
    //  {

    //    if (args.AddedItems.Count > 0)
    //    {
    //      foreach (object selIt in args.AddedItems)
    //      {
    //        TwoWaySliding.SelectableItem sit = selIt as TwoWaySliding.SelectableItem;
    //        var slb = sender as SurfaceListBox;

    //        var rem = new List<SelectableItem>();
    //        foreach (SelectableItem si in slb.SelectedItems)
    //        {
    //          if (si.Name != sit.Name)
    //            rem.Add(si);
    //        }
    //        foreach (var r in rem)
    //          slb.SelectedItems.Remove(r);

    //        if ((sit != null) && ( sit.Name == selectedItemString))
    //        {
    //          selectedListBox = sender as SurfaceListBox;
    //          selectedItem = args.AddedItems[0];
    //          //selectedListBox.ScrollIntoView(selectedItem);
    //        }
    //      }
    //    }
    //    //if (args.RemovedItems.Count >0)
    //    //{
    //    //  foreach (object desIt in args.RemovedItems)
    //    //  {
    //    //    SelectableItem rem = desIt as SelectableItem;
    //    //    selectedItem = rem;
    //    //    if ((rem != null) && rem.Name == selectedItemString)
    //    //    {
    //    //      var slb = sender as SurfaceListBox;
    //    //      if (rem.Selected == true)
    //    //        rem.Selected = false;
    //    //    }
    //    //  }
    //    //}
    //  }

    void lbItemsScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      SurfaceListBox slb = sender as SurfaceListBox;
      foreach (object slbIt in slb.Items)
      {
        TwoWaySliding.SelectableItem sit = slbIt as TwoWaySliding.SelectableItem;

        if ((sit != null) && (sit.Name == selectedItemString.Split('.').Last()))
        {
          slb.ScrollIntoView(sit);
          selectedItemString = "_";
        }
      }
      if (selectedListBox != null && selectedItem != null)
      {
        //selectedListBox.ScrollIntoView(selectedItem);
      }
      /*if (slb != null && selectedListBox == slb)
      {
        slb.ScrollIntoView(slb.SelectedItem);
      }*/
      //((SurfaceListBox)sender).ScrollIntoView(((SurfaceListBox)sender).SelectedItem);
      //e.Handled = false;
      //e.VerticalOffset = lastVerScrolled;
      //selectedListBox = (SurfaceListBox)sender;
      //selectedListBox.
    }
  }

  public class SurfaceScrollViewerUtilities
  {
    public static readonly DependencyProperty HorizontalOffsetProperty =
    DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(SurfaceScrollViewerUtilities),
      new FrameworkPropertyMetadata((double)0.0,
        OnHorizontalOffsetChanged));

    /// <summary>
    /// Gets the HorizontalOffset property. This dependency property 
    /// indicates ....
    /// </summary>
    public static double GetHorizontalOffset(DependencyObject d)
    {
      return (double)d.GetValue(HorizontalOffsetProperty);
    }


    /// <summary>
    /// Sets the HorizontalOffset property. This dependency property 
    /// indicates ....
    /// </summary>
    public static void SetHorizontalOffset(DependencyObject d, double value)
    {
      d.SetValue(HorizontalOffsetProperty, value);
    }

    /// <summary>
    /// Handles changes to the HorizontalOffset property.
    /// </summary>
    private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      SurfaceScrollViewer viewer = (SurfaceScrollViewer)d;
      viewer.ScrollToHorizontalOffset((double)e.NewValue);
    }
  }
}
