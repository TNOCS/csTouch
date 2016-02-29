using System;
using System.Windows;

namespace csShared.Geo
{
  public sealed class MapManipulationStartedEventArgs : EventArgs
  {
    public Point MapOrigin { get; set; }
    public KmlPoint WorldOrigin { get; set; }
  }
}