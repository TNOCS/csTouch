using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using csShared.Documents;
using csShared.Interfaces;

namespace csShared.FloatingElements
{
  public static class FloatingHelpers
  {
    public static FloatingElement CreateFloatingElement(string title, DockingStyles docking, object modelInstance, string icon, int priority)
    {
      var fe = new FloatingElement
             {
               ModelInstance = modelInstance,
               OpacityDragging = 0.5,
               OpacityNormal = 1.0,
               CanMove = true,
               CanRotate = true,
               CanScale = true,
               StartOrientation = 0,
               Large = true,
               Background = AppStateSettings.Instance.AccentBrush,
               MinSize = new Size(75, 75),
               MaxSize = new Size(1500, 1500),
               StartSize = new Size(75, 75),
               Width = 75,
               Height = 75,
               ShowsActivationEffects = false,
               RemoveOnEdge = true,
               Contained = true,
               Title = title,
               AllowStream = false,

               Foreground = Brushes.White,
               AnimationSpeed = new TimeSpan(0, 0, 0, 0, 200),
               SwitchWidth = 250,
               DockingStyle = docking,
               DragScaleFactor = 80,
               RotateWithFinger = AppStateSettings.Instance.Config.GetBool("Layout.Floating.RotateWithFinger", true),
               IconUri = icon
             };
      return fe;
    }

    public static FloatingElement CreateFloatingElement(string title, Point position, Size size, object modelInstance)
    {
      var fe = new FloatingElement
             {
               ModelInstance = modelInstance,
               OpacityDragging = 0.5,
               OpacityNormal = 1.0,
               CanMove = true,
               CanRotate = true,
               CanScale = true,
               StartOrientation = 0,
               AllowStream = true,
               Background = AppStateSettings.Instance.AccentBrush,
               MinSize = size,
               MaxSize = new Size(1500, 1500),
               StartPosition = position,
               StartSize = size,
               ShowsActivationEffects = false,
               RemoveOnEdge = true,
               Contained = true,
               Title = title,
               Foreground = Brushes.White,
               RotateWithFinger = AppStateSettings.Instance.Config.GetBool("Layout.Floating.RotateWithFinger", true),
               DockingStyle = DockingStyles.None,
             };
      return fe;
    }

    public static FloatingElement CreateFloatingElement(Document e)
    {
      var fe = new FloatingElement
      {
        Document = e,
        OpacityDragging = 0.5,
        OpacityNormal = 1.0,
        CanMove = true,
        CanRotate = true,
        CanDrag = true,
        CanScale = true,
        StartOrientation = 0,
        Background = AppStateSettings.Instance.AccentBrush,
        MinSize = new Size(50, 50),
        MaxSize = new Size(1500, 1500),
        StartPosition = new Point(300, 300),
        ShowsActivationEffects = false,
        StartSize = new Size(300, 300),
        Width = 300,
        Height = 300,
        AllowStream = true,
        RemoveOnEdge = true,
        Contained = true,
        Foreground = Brushes.White,
        RotateWithFinger = AppStateSettings.Instance.Config.GetBool("Layout.Floating.RotateWithFinger", true),
        DockingStyle = DockingStyles.None
      };
      if (e.ShareUrl != null) fe.Contracts.Add("link", e.ShareUrl.Replace("^", string.Empty).Replace(" ", "%20"));
        if (e.OriginalUrl != null) fe.Contracts.Add("document", e.OriginalUrl.Replace("^", string.Empty));

      return fe;
    }
    
    /// <summary>
    /// Remove a floating element by id.
    /// </summary>
    /// <param name="id"></param>
    public static void RemoveFloatingElement(string id) {
      var floatingElement = AppStateSettings.Instance.FloatingItems.FirstOrDefault(f => string.Equals(f.Id, id));
      if (floatingElement == null) return;
      AppStateSettings.Instance.FloatingItems.Remove(floatingElement);
    }

    /// <summary>
    /// Creates a floating element with a QR code on the backside
    /// </summary>
    /// <param name="e"></param>
    /// <param name="uri"> </param>
    /// <returns></returns>
    public static FloatingElement CreateFloatingElementWithQrBackside(Document e, string uri)
    {
      var qrCode = AppStateSettings.Instance.Container.GetExportedValue<IQrCode>();
      qrCode.Text = new Uri(uri, UriKind.RelativeOrAbsolute).AbsoluteUri;

      var fe = new FloatingElement
      {
        Document               = e,
        OpacityDragging        = 0.5,
        OpacityNormal          = 1.0,
        CanMove                = true,
        CanRotate              = true,
        CanDrag                = true,
        CanScale               = true,
        StartOrientation       = 0,
        Background             = AppStateSettings.Instance.AccentBrush,
        MinSize                = new Size(50, 50),
        MaxSize                = new Size(1500, 1500),
        StartPosition          = new Point(300, 300),
        ShowsActivationEffects = false,
        StartSize              = new Size(300, 300),
        Width                  = 300,
        Height                 = 300,
        RemoveOnEdge           = true,
        Contained              = true,
        Foreground             = Brushes.White,
        RotateWithFinger       = AppStateSettings.Instance.Config.GetBool("Layout.Floating.RotateWithFinger", true),
        DockingStyle           = DockingStyles.None,
        CanFlip                = true,
        IsBacksideQrCode       = true,
        ModelInstanceBack      = qrCode
      };
      return fe;
    }

    
  }
}