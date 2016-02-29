using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace csCommon.Controls
{
  public class StopHoldDoubleAnimation : DoubleAnimation
  {
    private readonly DependencyProperty _dp;
    private readonly FrameworkElement _target;

    public StopHoldDoubleAnimation()
    {
      Completed += StopHoldDoubleAnimationCompleted;
    }

    public StopHoldDoubleAnimation(double toValue, Duration duration, FrameworkElement target, DependencyProperty dp)
      : base(toValue, duration)
    {
      FillBehavior = FillBehavior.Stop;
      Completed += StopHoldDoubleAnimationCompleted;
      _target = target;
      _dp = dp;
    }

    private void StopHoldDoubleAnimationCompleted(object sender, EventArgs e)
    {
      _target.SetValue(_dp, To);
    }
  }

  public class StopHoldSizeAnimation : SizeAnimation
  {
    private readonly DependencyProperty _dp;
    private readonly FrameworkElement _target;

    public StopHoldSizeAnimation()
    {
      Completed += StopHoldSizeAnimationCompleted;
    }

    public StopHoldSizeAnimation(Size toValue, Duration duration, FrameworkElement target, DependencyProperty dp)
      : base(toValue, duration)
    {
      FillBehavior = FillBehavior.Stop;
      Completed += StopHoldSizeAnimationCompleted;
      _target = target;
      _dp = dp;
    }

    private void StopHoldSizeAnimationCompleted(object sender, EventArgs e)
    {
      _target.SetValue(_dp, To);
    }
  }
}