using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using csCommon.Plugins.MgrsGrid;

namespace csCommon.Types.Geo
{
    internal class MapCoordinateCopyPasteCommand : ICommand
    {
        private static MapCoordinateCopyPasteCommand mMgrsHotkey;
        private static MapCoordinateCopyPasteCommand mLatLonHotkey;

        public enum CoordinateKind { Mgrs, LatLon };

        public static void UnRegisterHotkeys(Map pMap)
        {
            // Only one map created, no dispose needed
        }

        public static void RegisterHotkeys(Map pMap)
        {
           var mMgrsHotkey = new MapCoordinateCopyPasteCommand(CoordinateKind.Mgrs);
            InputBinding ib = new InputBinding(mMgrsHotkey, new KeyGesture(Key.M, ModifierKeys.Shift | ModifierKeys.Control));
            pMap.InputBindings.Add(ib);

            var mLatLonHotkey = new MapCoordinateCopyPasteCommand(CoordinateKind.LatLon);
            InputBinding ib1 = new InputBinding(mLatLonHotkey, new KeyGesture(Key.L, ModifierKeys.Shift | ModifierKeys.Control));
            pMap.InputBindings.Add(ib1);
            
        }
    
        public MapCoordinateCopyPasteCommand(CoordinateKind pKind)
        {
            Kind = pKind;
        }


        public CoordinateKind Kind { get; private set; }
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            //var UIElement = System.Windows.Input.Mouse.DirectlyOver as UIElement;
            MouseToClipboard();

        }

        public void MouseToClipboard()
        {
            try
            {
                System.Windows.Point screenPoint = System.Windows.Input.Mouse.GetPosition(AppStateSettings.Instance.ViewDef.MapControl);
                MapPoint mapPoint = AppStateSettings.Instance.ViewDef.MapControl.ScreenToMap(screenPoint);
                var latlon = mapPoint.ToGenericGeometry();
                var mgrs = latlon.ConvertToMgrs();
                var text = "";
                switch(Kind)
                {
                    case CoordinateKind.LatLon:
                        text = string.Format(CultureInfo.InvariantCulture, "{0:00.000000} {1:000.000000}", latlon.Latitude, latlon.Longitude);
                        break;
                    case CoordinateKind.Mgrs:
                        text = string.Format(CultureInfo.InvariantCulture, "{0}", mgrs.ToLongString());
                        break;
                }
                //
                AppStateSettings.Instance.TriggerNotification("Coordinate copied to clipboard", text);
                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                Clipboard.SetText("No valid coordinate");
            }
        }
    }
}
