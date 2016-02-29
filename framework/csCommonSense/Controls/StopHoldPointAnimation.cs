using System;
using System.Windows.Media.Animation;
using System.Windows;

namespace csCommon.Controls
{
  public class StopHoldPointAnimation : PointAnimation
  {

    private readonly FrameworkElement _target;
    private readonly DependencyProperty _dp;

    public StopHoldPointAnimation()
    {
      Completed += StopHoldDoubleAnimationCompleted;
    }

    public StopHoldPointAnimation(Point toValue, Duration duration, FrameworkElement target, DependencyProperty dp)
      : base(toValue, duration)
    {
      FillBehavior = FillBehavior.Stop;
      Completed += StopHoldDoubleAnimationCompleted;
      _target = target;
      _dp = dp;
    }

    void StopHoldDoubleAnimationCompleted(object sender, EventArgs e)
    {
      if (_target!=null) _target.SetValue(_dp, To);
    }
  }

  
}
