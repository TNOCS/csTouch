using System.Windows;

namespace csCommon.csMapCustomControls.MapIconMenu
{
    /// <summary>
    /// This name is too generic and imprecise.
    /// It's made for conrols that can report a certain reference point. This will be used for alignment
    /// purposes by the ReferenceAlignPanel, which also needs a much better name.
    /// </summary>
    interface ICustomAlignedControl
    {
        Point AlignReferencePoint { get; }
    }
}
