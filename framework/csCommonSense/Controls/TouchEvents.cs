using System.Windows;

namespace csShared.Controls
{
  public class TouchEvents
  {
    public static readonly RoutedEvent NeedsCleaningEvent = EventManager.RegisterRoutedEvent("NeedsCleaning", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TouchEvents));

    public TouchEvents()
    {
      
    }

    public static void AddNeedsCleaningHandler(DependencyObject d, RoutedEventHandler handler)
    {
      UIElement uie = d as UIElement;
      if (uie != null)
      {
        uie.AddHandler(TouchEvents.NeedsCleaningEvent, handler);
      }
    }
    public static void RemoveNeedsCleaningHandler(DependencyObject d, RoutedEventHandler handler)
    {
      UIElement uie = d as UIElement;
      if (uie != null)
      {
        uie.RemoveHandler(TouchEvents.NeedsCleaningEvent, handler);
      }
    }
  }
}
