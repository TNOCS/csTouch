using System;
using System.Windows;
using Microsoft.Surface.Presentation;

namespace csShared.Utils
{
  public class DropEventArgs : EventArgs
  {
    public Point Pos { get; set; }
    public Double Orientation { get; set; }
    public SurfaceDragDropEventArgs EventArgs { get; set; }

  }
}