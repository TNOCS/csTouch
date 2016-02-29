using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using Caliburn.Micro;
using Microsoft.Surface.Presentation.Controls;

namespace csShared
{
  public class FloatingCollection : BindableCollection<FloatingElement>
  {
    public enum ClosingStyle
    {
      Direct,
      Shrink
    }


    public void AddFloatingElement(FloatingElement fe)
    {
      Application.Current.Dispatcher.Invoke(delegate
      {
          if (fe == null) return;
          if (!Contains(fe)) Add(fe);

          if (fe.DockingStyle != DockingStyles.None)
          {
              OrderDockingFloatingElement();
          }
      });
    }

    public void RemoveFloatingElement(FloatingElement fe)
    {
      Remove(fe);
    }

    /// <summary>
    ///   rearrange all docked floating elements
    /// </summary>
    /// <param name="ds"></param>
    public void OrderDockingFloatingElement(DockingStyles ds)
    {
      if (ds == DockingStyles.Left || ds == DockingStyles.Right)
      {
        var a = this.Where(k => k.DockingStyle == ds);
        double y = 50;
        if (a.Any())
        {
          foreach (FloatingElement b in a.OrderBy(k => k.Priority))
          {
            if (b.StartSize != null)
            {
              double x = (ds == DockingStyles.Right)
                      ? Application.Current.MainWindow.ActualWidth -
                       (b.DockWidth - (b.StartSize.Value.Width/2))
                      : b.DockWidth - (b.StartSize.Value.Width/2);
              if (b.MinSize != null)
                y += (b.MinSize.Value.Height + 10);
              b.StartPosition = new Point(x, y);
              b.Width = b.StartSize.Value.Width;
              b.Height = b.StartSize.Value.Height;
            }
          }
        }
      }
      else
      {
        IEnumerable<FloatingElement> a = this.Where(k => k.DockingStyle == ds);
          if (Application.Current.MainWindow == null) // After clicking the X (NEW)
          {
              return;
          }
        double x = Application.Current.MainWindow.ActualWidth - 100;
        if (a.Any())
        {
          foreach (FloatingElement b in a.OrderBy(k => k.Priority))
          {
            if (b.StartSize != null)
            {
              //x = (ds == DockingStyles.Up)
              //    ? Application.Current.MainWindow.ActualWidth -
              //     (b.DockWidth - (b.StartSize.Value.Width / 2))
              //    : b.DockWidth - (b.StartSize.Value.Width / 2);
              double y = b.DockWidth - (b.StartSize.Value.Height/2);
              if (b.MinSize != null) x -= (b.MinSize.Value.Height + 10);
              b.StartPosition = new Point(x, y);
              b.Width = b.StartSize.Value.Width;
              b.Height = b.StartSize.Value.Height;
            }
          }
        }
      }
    }

    public void RemoveFloatingElement(FloatingElement fe, ClosingStyle cs, int duration = 250,
                     Point target = new Point())
    {
      var d = new Duration(new TimeSpan(0, 0, 0, 0, duration));
      var da = new DoubleAnimation(0, d);
      da.Completed += (s, e) => Remove(fe);
      if (target != new Point())
      {
        var pa = new PointAnimation(target, d);
        fe.ScatterViewItem.BeginAnimation(ScatterContentControlBase.CenterProperty, pa);
      }
      fe.ScatterViewItem.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation(0, d));
      fe.ScatterViewItem.BeginAnimation(FrameworkElement.WidthProperty, da);
    }


    public void OrderDockingFloatingElement()
    {
      OrderDockingFloatingElement(DockingStyles.Left);
      OrderDockingFloatingElement(DockingStyles.Right);
      OrderDockingFloatingElement(DockingStyles.Up);
    }
  }
}