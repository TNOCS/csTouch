﻿using System.Windows.Input;

namespace csMapCustomControls.MapIconMenu
{
    /// <summary>
    /// Declare a new <see cref="RoutedCommand">RoutedCommand</see> that
    /// is used by the <see cref="Window1">Window1</see> class where the 
    /// Command bindings and Command Sink events are declared. The Actual
    /// Comman is used on a Button within the <see cref="UserControlThatUsesCustomCommand">
    /// UserControlThatUsesCustomCommand</see> UserControl
    /// </summary>
    public class MapMenuCommands
    {
        #region Instance Fields
        public static readonly RoutedUICommand ToggleExpansion = new RoutedUICommand("ToggleExpansion", "ToggleExpansion", typeof(MapMenuCommands));
        public static readonly RoutedUICommand Expand = new RoutedUICommand("Expand", "Expand", typeof(MapMenuCommands));
        #endregion
    }
}
