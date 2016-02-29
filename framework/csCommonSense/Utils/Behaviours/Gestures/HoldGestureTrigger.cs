using System;
using System.Windows.Interactivity;
using System.Windows;

namespace Blake.NUI.WPF.Gestures
{
  public class HoldGestureTrigger : TriggerBase<UIElement>
  {
    public bool HandlesTouches { get; set; }
    public TimeSpan HoldTimeout { get; set; }
    public double MaxMovement { get; set; }
    public event EventHandler Hold;

    public HoldGestureTrigger()
      : base()
    {
      HoldTimeout = TimeSpan.FromMilliseconds(200);
      MaxMovement = Double.MaxValue;
    }

    private void OnHold()
    {
      this.InvokeActions(null);
      if (Hold != null)
        Hold(this, EventArgs.Empty);
    }

    protected override void OnAttached()
    {
      base.OnAttached();
      var handler = new EngineHandler(() => new HoldGestureEngine(HoldTimeout, MaxMovement), base.AssociatedObject);
      handler.GestureCompleted += (s, e) => this.InvokeActions(null);
    }
  }
}
