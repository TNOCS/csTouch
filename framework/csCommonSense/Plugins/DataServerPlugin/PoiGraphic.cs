using Caliburn.Micro;
using csCommon.csMapCustomControls.MapIconMenu;
using csDataServerPlugin.ViewModels;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Documents;
using csShared.FloatingElements;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using CallOutOrientation = DataServer.CallOutOrientation;
using Geometry = ESRI.ArcGIS.Client.Geometry.Geometry;
using Point = System.Windows.Point;

namespace csDataServerPlugin
{
    public class PoiImageOverlay : Image
    {
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;
        private double lastRes;
        public BaseContent Poi;
        public bool Visible { get; set; }

        public PoiImageOverlay()
            : base()
        {
            AppState.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
        }

        private void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            if (Math.Abs(lastRes - AppState.ViewDef.MapControl.Resolution) < 0.00001) return;
            UpdateVisibility();
            lastRes = AppState.ViewDef.MapControl.Resolution;
        }

        public void UpdateVisibility()
        {
            try
            {
                if (Poi == null || Poi.NEffectiveStyle == null) return;
                Poi.CalculateVisible(AppState.ViewDef.MapControl.Resolution);

                if (Poi.IsVisible != Visible) Visible = Poi.IsVisible;
                this.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;


            }
            catch (Exception e)
            {
                // FIXME TODO Deal with exception!
            }
        }

    }

    [DebuggerDisplay("Name: {Poi.Name}, Layer {Name}, Visible: {Visible}")]
    public class PoiGraphic : Graphic
    {
        private static readonly WebMercator wm = new WebMercator();

        private MapCallOutViewModel callOut;
        private bool callOutVisible;
        private double lastRes;
        private double lastScale;
        private DateTime lastTap;
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;

        public PoiGraphic()
        {
            //AppState.ViewDef.MapControl.ExtentChanged += MapControlExtentChanged;
            //MouseLeftButtonUp += PoiGraphic_MouseLeftButtonUp;
            MouseLeftButtonDown += PoiGraphic_MouseLeftButtonDown;
            MouseLeftButtonUp += PoiGraphic_MouseLeftButtonUp;
        }

        public void PoiGraphic_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
           if (iconLongTappedTimer != null) // Did long tapped fired?
            {
                StopLongTappedTimer();
                EndTapped();
            }
        }

 

        public void PoiGraphic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mLastKnownTapPoint = AppState.ViewDef.MapControl.ScreenToMap(e.GetPosition(AppState.ViewDef.MapControl));
            StartLongTappedTimer();
            e.Handled = false;
        }

        public Graphic InnerTextGraphic { get; set; }
        public Graphic LabelGraphic { get; set; }
        public Graphic ImageGraphic { get; set; }

        public IdsChildLayer Layer { get; set; }
        public IServiceLayer GroupLayer { get; set; }
        public DataServerPlugin Plugin { get; set; }
        public PoiService Service { get; set; }

        public BaseContent Poi { get; set; }
        public Geometry BaseGeometry { get; set; }

        public bool Visible { get; set; }

        internal void Stop()
        {
            AppState.ViewDef.MapControl.ExtentChanged -= MapControlExtentChanged;
            //MouseLeftButtonUp -= PoiGraphic_MouseLeftButtonUp;
            MouseLeftButtonDown -= PoiGraphic_MouseLeftButtonDown;
            MouseLeftButtonUp -= PoiGraphic_MouseLeftButtonUp;
        }



        private void MapControlExtentChanged(object sender, ExtentEventArgs e)
        {
            if (Math.Abs(lastRes - AppState.ViewDef.MapControl.Resolution) < 0.00001) return;
            System.Threading.Tasks.Task.Run(() => Execute.OnUIThread(() =>
            {
                UpdateForCurrentMapExtent();
                //UpdateVisibility();
                //UpdateScale();
                //lastRes = AppState.ViewDef.MapControl.Resolution;
            }));
        }

        public void UpdateForCurrentMapExtent()
        {
            if (Math.Abs(lastRes - AppState.ViewDef.MapControl.Resolution) < 0.00001) return;
            UpdateVisibility();
            //UpdateScale(); // Is already called in UpdateVisibility();
            lastRes = AppState.ViewDef.MapControl.Resolution;
        }

        /// <summary>
        /// Updates the scale of the poi graphics
        /// </summary>
        /// <param name="forceUpdate">When true, the scale update will be performed. When false(default) the scale update will only be performed if the scale has updated since the last call to UpdateScale</param>
        public void UpdateScale(bool forceUpdate = false)
        {
            if (Poi.NEffectiveStyle.ScalePoi == null || !Poi.NEffectiveStyle.ScalePoi.Value) return;

            double maxScale = Poi.NEffectiveStyle.MaxScale ?? 4;
            double scaleUnits = Poi.NEffectiveStyle.ScaleUnits ?? 50;
            double scaleStartResolution = Poi.NEffectiveStyle.ScaleStartResolution ?? 50;
            var currRes = AppState.ViewDef.MapControl.Resolution;
            var scale = Math.Min(Math.Max(1, scaleUnits * currRes / scaleStartResolution), maxScale);
            if (!forceUpdate && lastScale == scale) return;

            if (Poi == null) return;
            double height;
            double width;
            if (Poi != null)
            {
                height = Poi.NEffectiveStyle.IconHeight == null ? 32.0 : Poi.NEffectiveStyle.IconHeight.Value;
                width = Poi.NEffectiveStyle.IconWidth == null ? 32.0 : Poi.NEffectiveStyle.IconWidth.Value;
            }
            else
            {
                height = 32.0;
                width = 32.0;
            }

            if (Poi != null && Poi.Data != null && Poi.Data.ContainsKey("graphic"))
            {
                var staticGraphic = Poi.Data["graphic"] as StaticGraphic;
                if (staticGraphic != null)
                {
                    var symbol = staticGraphic.Symbol as SimpleMarkerSymbol;
                    if (symbol != null)
                    {
                        if (currRes > scaleStartResolution)
                        {
                            symbol.Size = height / scale;
                        }
                        else
                        {
                            symbol.Size = height;
                        }
                    }
                }
            }
            if (Symbol is PictureMarkerSymbol)
            {
                var pms = (PictureMarkerSymbol)Symbol;

                if (currRes > scaleStartResolution)
                {
                    pms.Width = width / scale;
                    pms.Height = height / scale;
                    //pms.Width *= 0.85f;
                    //pms.Height *= 0.85f;
                }
                else
                {
                    pms.Width = width;
                    pms.Height = height;
                }
                pms.OffsetX = pms.Width / 2;
                pms.OffsetY = pms.Height / 2;
            }
            if (ImageGraphic != null)
            {
                var pms = (PictureMarkerSymbol)ImageGraphic.Symbol;

                if (currRes > scaleStartResolution)
                {
                    pms.Width = width / scale;
                    pms.Height = height / scale;
                    //pms.Width *= 0.85f;
                    //pms.Height *= 0.85f;

                    if (ImageGraphic.MapTip == null)
                    {
                        ImageGraphic.MapTip = new Label() { Content = Poi.Labels.ContainsKey(Poi.NEffectiveStyle.NameLabel) ? Poi.Labels[Poi.NEffectiveStyle.NameLabel] : string.Empty };
                    }
                }
                else
                {
                    //pms.Width = width * 0.85f;
                    //pms.Height = height * 0.85f;
                    ImageGraphic.MapTip = null;
                }
                pms.OffsetX = pms.Width / 2;
                pms.OffsetY = pms.Height / 2;
            }

            if (LabelGraphic != null && LabelGraphic.Geometry != null && currRes > Poi.NEffectiveStyle.MaxTitleResolution)
            {
                LabelGraphic.Geometry = null;

            }
            else if (Visible && LabelGraphic != null && LabelGraphic.Geometry == null && currRes < Poi.NEffectiveStyle.MaxTitleResolution)
            {
                LabelGraphic.Geometry = BaseGeometry;
                MapTip = null;
            }
            lastScale = scale;
        }

        public void UpdateVisibility()
        {
            try
            {
                if (Poi == null || Poi.NEffectiveStyle == null) return;
                Poi.CalculateVisible(AppState.ViewDef.MapControl.Resolution);

                if (Poi.IsVisible != Visible) Visible = Poi.IsVisible;

                var g = (Visible) ? BaseGeometry : null;
                Geometry = g;

                if (LabelGraphic != null) LabelGraphic.Geometry = Geometry;
                if (InnerTextGraphic != null) InnerTextGraphic.Geometry = Geometry;
                if (ImageGraphic != null) ImageGraphic.Geometry = Geometry;
                if (Visible)
                {
                    UpdateScale(true);
                }
            }
            catch (Exception)
            {
                // FIXME TODO Deal with exception!
            }
        }

        public void SetGeometry(Geometry g)
        {
            try
            {
                BaseGeometry = g;
                Geometry = g;
            }
            catch (Exception e)
            {
                // FIXME TODO Deal with exception!                  
            }
        }

        private DispatcherTimer iconLongTappedTimer;

        /// <summary>
        /// The icon is clicked (mouse or touch) set timer to wait for x msec (while icon still touched); then fire event
        /// </summary>
        private void StartLongTappedTimer()
        {
            StopLongTappedTimer();
            iconLongTappedTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = new TimeSpan(0, 0, 0, 0, MapMenuItem.iconLongTappedTimerInMSec)
            };
            iconLongTappedTimer.Tick += LongTappedTimerElapsed;
            iconLongTappedTimer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        private void StopLongTappedTimer()
        {
            if (iconLongTappedTimer != null)
            {
                iconLongTappedTimer.Stop();
                iconLongTappedTimer.Tick -= LongTappedTimerElapsed;
                iconLongTappedTimer = null;
            }
        }


        private void LongTappedTimerElapsed(object sender, EventArgs e)
        {
            StopLongTappedTimer();
            FireIconLongTapped();
        }

        private void FireIconLongTapped()
        {
            if ((Poi is PoI) && (Poi.Service is PoiService)) (Poi.Service as PoiService).RaisePoiLongTapped(Poi as PoI);
        }
        /*
        public void LongTapped(MapPoint tapPos)
        {
            var tappedPosition = (MapPoint)wm.ToGeographic(tapPos);
        }*/


        /// <summary>
        /// This function is calles from multiple places outside the class!! 
        /// </summary>
        /// <param name="tapPos"></param>
        public void TappedByExternalMapControlMapGesture(MapPoint tapPos)
        {
            if (DateTime.Now < lastTap.AddMilliseconds(500)) return;
            lastTap = DateTime.Now;
            mLastKnownTapPoint = tapPos;
            EndTapped();

        }

        private void Tapped(MapPoint tapPos)
        {
            mLastKnownTapPoint = tapPos;

            StartLongTappedTimer();
        }



        private MapPoint mLastKnownTapPoint;

        private void EndTapped()
        {
            var tappedPosition = (MapPoint)wm.ToGeographic(mLastKnownTapPoint);
            Service.TriggerTapped(new TappedEventArgs { Content = Poi, Service = Service, TapPoint = new Point(tappedPosition.X, tappedPosition.Y) });

            var tm = Poi.NEffectiveStyle.TapMode.Value;
            if (tm == TapMode.CustomEvent) MessageBox.Show("Custom");
            if (tm == TapMode.None) return;
            if (tm == TapMode.Zoom)
            {
                if (Poi.Labels.ContainsKey("ZoomResolution"))
                {
                    var r = double.Parse(Poi.Labels["ZoomResolution"]);
                    Execute.OnUIThread(delegate
                    {
                        var pos = (MapPoint)wm.FromGeographic(new MapPoint(Poi.Position.Longitude, Poi.Position.Latitude));
                        var w = (AppState.ViewDef.MapControl.ActualWidth / 2) * r;
                        var h = (AppState.ViewDef.MapControl.ActualHeight / 2) * r;
                        var env = new Envelope(pos.X - w, pos.Y - h, pos.X + w, pos.Y + h);
                        AppState.ViewDef.MapControl.Extent = env;
                    });
                }
                if (Poi.Labels.ContainsKey("ZoomRotation"))
                {
                    AppState.ViewDef.MapControl.Rotation = double.Parse(Poi.Labels["ZoomRotation"]);
                }
            }
            if (tm == TapMode.OpenMedia)
            {
                AddMediaToFloatingElements(mLastKnownTapPoint);
            }
            if (callOutVisible)
            {
                callOut.Close();
                return;
            }

            if (tm == TapMode.Popup && GroupLayer != null)
            {
                GroupLayer.OpenPoiPopup(Poi);
            }

            switch (tm)
            {
                case TapMode.CallOutEdit:
                case TapMode.CallOutPopup:
                case TapMode.CallOut:
                    OpenPopup(mLastKnownTapPoint, tm);
                    break;
                default: return;
            }

        }

        public void OpenPopup(MapPoint tapPos, TapMode tm)
        {
            var pc = new PoiCallOutViewModel { Poi = Poi };

            callOut = new MapCallOutViewModel
            {
                Width = Poi.NEffectiveStyle.CallOutMaxWidth.HasValue ? Poi.NEffectiveStyle.CallOutMaxWidth.Value : 350,
                Title = Poi.Name,
                TimeOut = new TimeSpan(0, 0, 0, Poi.NEffectiveStyle.CallOutTimeOut.Value),
                CanClose = true,
                CanPin = Poi.NEffectiveStyle.AddMode != AddModes.OpenAfter,
                ForegroundBrush = new SolidColorBrush(Poi.NEffectiveStyle.CallOutForeground.Value),
                BackgroundBrush = new SolidColorBrush(Poi.NEffectiveStyle.CallOutFillColor.Value),
                ViewModel = pc,
                Point = (Geometry is MapPoint) ? (MapPoint)Geometry : tapPos,
                Graphic = this,
                Orientation = (CallOutOrientation) Enum.Parse(typeof(CallOutOrientation), Poi.NEffectiveStyle.CallOutOrientation.Value.ToString())
            };
            pc.CallOut = callOut;

            callOut.Tapped += (s, f) =>
            {
                if (tm != TapMode.CallOutPopup || GroupLayer == null) return;
                if (!callOut.Pinned) callOut.Close();
                //GroupLayer.OpenPoiPopup(Poi);
            };
            callOut.Closed += (s, f) => { callOutVisible = false; };

            UpdateCalloutActions();
            if (callOut.Orientation == CallOutOrientation.RightSideMenu)
            {
                // Close all open popups
                for (var i = AppState.Popups.Count-1; i>= 0; i--)
                {
                    var co = AppState.Popups[i] as MapCallOutViewModel;
                    if (co == null) continue;
                    co.Close();
                }
            }
            AppState.Popups.Add(callOut);
            if (tm == TapMode.CallOutEdit) callOut.StartEditing();
            callOutVisible = true;
        }

        private void UpdateCalloutActions()
        {
            var effStyle = Poi.NEffectiveStyle;
            if (effStyle.CanEdit.Value)
            {
                var editCallout = new CallOutAction
                {
                    IconBrush = new SolidColorBrush(effStyle.CallOutForeground.Value),
                    Title = "Edit",
                    Path =
                        "M0,44.439791L18.98951,54.569246 0.47998798,62.66881z M17.428029,12.359973L36.955557,23.568769 21.957478,49.686174 20.847757,46.346189 15.11851,45.756407 14.138656,42.166935 8.5292659,41.966761 6.9493899,38.037481 2.4399572,38.477377z M26.812517,0.0009765625C27.350616,-0.012230873,27.875986,0.10826397,28.348372,0.3782568L42.175028,8.3180408C43.85462,9.2780154,44.234529,11.777948,43.02482,13.89789L41.375219,16.767812 21.460039,5.3381228 23.10964,2.4582005C23.979116,0.941679,25.437378,0.034730911,26.812517,0.0009765625z"
                };
                editCallout.Clicked += (e, f) => callOut.StartEditing();
                callOut.Actions.Add(editCallout);
            }

            // Lock the PoI
            var canMoveCallout = new CallOutAction
            {
                IconBrush = new SolidColorBrush(effStyle.CallOutForeground.Value),
                Title = "Lock",
                Path = effStyle.CanMove.Value
                    ? "F1M648.2778,1043.3809L648.2778,1047.4329 645.6158,1047.4329 645.6158,1043.3839C644.5488,1042.9079 643.7998,1041.9019 643.7998,1040.7169 643.7998,1039.0759 645.2068,1037.7479 646.9428,1037.7479 648.6858,1037.7479 650.0938,1039.0759 650.0938,1040.7169 650.0938,1041.8989 649.3458,1042.9079 648.2778,1043.3809 M654.3988,1031.2069C654.3988,1031.2009,654.3998,1031.1959,654.4008,1031.1899L651.2988,1031.1529 641.3268,1031.1529 640.3338,1028.5859C639.1988,1025.6569 640.6488,1022.3669 643.5788,1021.2339 646.5088,1020.0989 649.7988,1021.5549 650.9328,1024.4789L652.0168,1027.2809C652.2178,1027.8009,652.3348,1028.3359,652.3778,1028.8669L654.4728,1028.8909C654.3998,1028.2769,654.2548,1027.6609,654.0208,1027.0559L652.5638,1023.2979C651.0408,1019.3659 646.6198,1017.4139 642.6888,1018.9379 638.7598,1020.4589 636.8068,1024.8789 638.3278,1028.8109L639.3428,1031.4309C637.3868,1032.1059,635.9778,1033.9609,635.9778,1036.1469L635.9778,1046.2999C635.9778,1049.0579,638.2148,1051.2949,640.9708,1051.2949L653.7078,1051.2949C656.4668,1051.2949,658.7008,1049.0579,658.7008,1046.2999L658.7008,1036.1469C658.7008,1033.6249,656.8298,1031.5439,654.3988,1031.2069"
                    : "F1M339.3071,1185.249L339.3071,1188.437 337.2111,1188.437 337.2111,1185.251C336.3721,1184.876 335.7831,1184.085 335.7831,1183.151 335.7831,1181.861 336.8901,1180.815 338.2561,1180.815 339.6281,1180.815 340.7371,1181.861 340.7371,1183.151 340.7371,1184.082 340.1471,1184.876 339.3071,1185.249 M331.6851,1168.456C331.6851,1165.017 334.4711,1162.228 337.9101,1162.228 341.3491,1162.228 344.1411,1165.017 344.1411,1168.456L344.1411,1171.745C344.1411,1172.16,344.0991,1172.565,344.0211,1172.959L331.8051,1172.959C331.7281,1172.565,331.6851,1172.16,331.6851,1171.745z M346.2351,1173.133C346.2611,1172.861,346.2761,1172.586,346.2761,1172.308L346.2761,1167.893C346.2761,1163.274 342.5291,1159.528 337.9101,1159.528 333.2921,1159.528 329.5511,1163.274 329.5511,1167.893L329.5511,1172.308C329.5511,1172.586 329.5661,1172.861 329.5921,1173.132 327.2211,1173.733 325.4651,1175.875 325.4651,1178.432L325.4651,1189.558C325.4651,1192.578,327.9121,1195.028,330.9361,1195.028L344.8901,1195.028C347.9091,1195.028,350.3601,1192.578,350.3601,1189.558L350.3601,1178.432C350.3601,1175.876,348.6031,1173.733,346.2351,1173.133"
            };
            canMoveCallout.Clicked += (e, f) =>
            {
                if (Poi.Style == null)
                {
                    Poi.Style = new PoIStyle { CanMove = !effStyle.CanMove };
                }
                else Poi.Style.CanMove = !effStyle.CanMove;
                if (Poi.Style.CanMove == true && (effStyle.DrawingMode == DrawingModes.Polygon || effStyle.DrawingMode == DrawingModes.Polyline))
                    (Poi.Data["graphic"] as Graphic).MakeDraggable();

                Poi.TriggerLabelChanged("", "", "");
                Poi.UpdateEffectiveStyle();
                callOut.Close();
            };
            callOut.Actions.Add(canMoveCallout);

            if (effStyle.CanRotate.Value &&
                (Poi.NEffectiveStyle.DrawingMode.Value == DrawingModes.Point ||
                 Poi.NEffectiveStyle.DrawingMode.Value == DrawingModes.Image))
            {
                var rotateCallOutAction = new CallOutAction
                {
                    IconBrush = new SolidColorBrush(Poi.NEffectiveStyle.CallOutForeground.Value),
                    Title = "Rotate",
                    Path = "F1M225.713,1773.49L232.795,1776.66 231.995,1768.94 231.192,1761.23 226.002,1764.99C221.113,1758.99 213.677,1755.15 205.337,1755.15 190.61,1755.15 178.672,1767.1 178.672,1781.82 178.672,1796.55 190.61,1808.49 205.337,1808.49 211.902,1808.49 217.903,1806.11 222.543,1802.17 222.573,1802.11 222.593,1802.06 222.627,1801.99 224.257,1798.82 220.791,1798.99 220.781,1798.99 216.686,1802.68 211.271,1804.93 205.337,1804.93 192.595,1804.93 182.228,1794.56 182.228,1781.82 182.228,1769.08 192.595,1758.71 205.337,1758.71 212.481,1758.71 218.867,1761.98 223.106,1767.09L218.631,1770.33 225.713,1773.49z"
                };
                rotateCallOutAction.Clicked += (e, f) =>
                {
                    callOut.Close();
                    Poi.StartRotation();
                };
                callOut.Actions.Add(rotateCallOutAction);
            }
            if (effStyle.CanDelete.Value)
            {
                var deleteCalloutAction = new CallOutAction
                {
                    IconBrush = new SolidColorBrush(Poi.NEffectiveStyle.CallOutForeground.Value),
                    Title = "Delete",
                    Path =
                        "M33.977998,27.684L33.977998,58.102997 41.373998,58.102997 41.373998,27.684z M14.841999,27.684L14.841999,58.102997 22.237998,58.102997 22.237998,27.684z M4.0319996,22.433001L52.183,22.433001 52.183,63.999001 4.0319996,63.999001z M15.974,0L40.195001,0 40.195001,7.7260003 56.167001,7.7260003 56.167001,16.000999 0,16.000999 0,7.7260003 15.974,7.7260003z"
                };
                deleteCalloutAction.Clicked += (e, f) =>
                {
                    callOut.Close();
                    var nea = new NotificationEventArgs
                    {
                        Text = "Are you sure?",
                        Header = "Delete " + Poi.Name,
                        Duration = new TimeSpan(0, 0, 30),
                        Background = Brushes.Red,
                        Image = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/Delete.png")),
                        Foreground = Brushes.White,
                        Options = new List<string> { "Yes", "No" }
                    };
                    nea.OptionClicked += (s, n) =>
                    {
                        if (n.Option != "Yes") return;
                        //AppState.TriggerNotification(Poi.Name + " was deleted", pathData: MenuHelpers.DeleteIcon);
                        Service.RemovePoi(Poi as PoI);
                        AppState.Popups.Remove(callOut);
                    };
                    AppState.TriggerNotification(nea);
                };
                callOut.Actions.Add(deleteCalloutAction);
            }
            foreach (var action in Poi.CalloutActions)
            {
                callOut.Actions.Add(action);
            }
        }

        /// <summary>
        ///     Add a media element (photo) to the floating elements collection.
        ///     Based on the current tap position, find a suitable location for the photo's. In addition,
        ///     in case multiple photo's need to be added, offset them a bit so they don't overlap and cover each other.
        ///     Also make sure that you don't add a photo twice (based on the floating element's title).
        /// </summary>
        /// <param name="tapPos">Tap or click position on the map in map coordinates.</param>
        private void AddMediaToFloatingElements(MapPoint tapPos)
        {
            if (Poi.AllMedia == null) return;
            Vector offset;
            var startPosition = ComputeStartPositionAndOffset(tapPos, out offset);
            foreach (var m in Poi.AllMedia)
            {
                var imageFile = Path.GetFileName(m.Id);
                var title = string.IsNullOrEmpty(Poi.Name)
                    ? imageFile
                    : Poi.Name + " - " + imageFile;
                if (AppState.FloatingItems.Any(f => string.Equals(f.Title, title)))
                    continue; // Do not add the same photo twice.
                if (m.Type != MediaType.Photo) continue;
                if (string.IsNullOrEmpty(m.LocalPath))
                    m.LocalPath = Poi.Service.store.GetLocalUrl(Poi.Service.MediaFolder, m.Id);

                var d = new Document { Location = m.LocalPath, OriginalUrl = m.PublicUrl };
                var fed = FloatingHelpers.CreateFloatingElement(d);
                fed.Title = title;
                fed.CanFullScreen = true;
                fed.StartPosition = startPosition + offset;
                offset += new Vector(10, 25);
                AppState.FloatingItems.AddFloatingElement(fed);
            }
        }

        /// <summary>
        ///     Compute suitable start position within the screen boundary.
        /// </summary>
        /// <param name="tapPos">Current location on the screen in map coordinates</param>
        /// <param name="offset">Offset in screen coordinates.</param>
        /// <returns>Suitable start position.</returns>
        private static Point ComputeStartPositionAndOffset(MapPoint tapPos, out Vector offset)
        {
            var offsetX = 0;
            var offsetY = 0;
            var startPosition = AppState.ViewDef.MapControl.MapToScreen(tapPos, true);
            startPosition += new Vector(-300, -200);
            if (startPosition.X < 300) offsetX = (int)(-startPosition.X + Application.Current.MainWindow.Width - 700);
            else if (startPosition.X > Application.Current.MainWindow.Width - 500)
                offsetX = (int)(-startPosition.X + 300);
            if (startPosition.Y < 250) offsetY = (int)(-startPosition.Y + Application.Current.MainWindow.Height - 500);
            else if (startPosition.Y > Application.Current.MainWindow.Height - 400)
                offsetY = (int)(-startPosition.Y + 250);
            offset = new Vector(offsetX, offsetY);
            return startPosition;
        }

        public void CloseCallout()
        {
            if (callOutVisible) callOut.Close();
        }
    }
}