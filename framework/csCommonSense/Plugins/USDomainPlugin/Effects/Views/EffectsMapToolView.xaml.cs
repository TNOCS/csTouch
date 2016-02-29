using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using csCommon.csMapCustomControls.MapIconMenu;
using csCommon.MapPlugins.MapTools.MeasureTool;
using csGeoLayers.MapTools.MeasureTool;
using csShared;
using csShared.FloatingElements;
using Caliburn.Micro;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using IMB3;
using IMB3.ByteBuffers;
using Newtonsoft.Json.Linq;
using SessionsEvents;
using csUSDomainPlugin.Effects.Util;

namespace csUSDomainPlugin.Effects.Views
{
    /// <summary>
    /// Interaction logic for EffectsMapToolView.xaml
    /// </summary>
    public partial class EffectsMapToolView
    {
        #region fields

        private static readonly WebMercator Mercator = new WebMercator();
        private DirectionTool _direction;
        private bool _showInfo;
        private GroupLayer _layer;

        private string _state = "start";

        #endregion

        #region dependency properties

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(MapPoint), typeof(EffectsMapToolView),
                                        new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for Grph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GrphProperty =
            DependencyProperty.Register("Grph", typeof(Graphic), typeof(EffectsMapToolView), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for LatLon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LatLonProperty =
            DependencyProperty.Register("LatLon", typeof(string), typeof(EffectsMapToolView), new UIPropertyMetadata(""));


        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(EffectsMapToolView),
                                        new UIPropertyMetadata(false));



        #endregion

        #region constructor

        public EffectsMapToolView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _layer = AppState.ViewDef.MapToolsLayer;
        }

        
        #endregion

        #region properties

        public string LatLon
        {
            get { return (string)GetValue(LatLonProperty); }
            set { SetValue(LatLonProperty, value); }
        }


        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public bool CanBeDragged
        {
            get { return (bool)GetValue(CanBeDraggedProperty); }
            set { SetValue(CanBeDraggedProperty, value); }
        }

        public bool ShowInfo
        {
            get { return _showInfo; }
            set
            {
                _showInfo = value;
                if (_showInfo && !CanBeDragged)
                {
                    VisualStateManager.GoToState(this, "Info", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Icon", true);
                }
            }
        }

        public MapPoint Position
        {
            get { return (MapPoint)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public Graphic Grph
        {
            get { return (Graphic)GetValue(GrphProperty); }
            set { SetValue(GrphProperty, value); }
        }


        #endregion

        #region private methods

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {

            if (ReferenceEquals(Tag, "First"))
            {
                CanBeDragged = true;
            }
            else
            {
                bCircle.IsExpanded = true;

            }




            bCircle.Background = Brushes.Black;

            //bCircle.PreviewMouseDown += UcGeorefenceMouseDown;
            //bCircle.PreviewMouseMove += UcGeorefenceMouseMove;
            //bCircle.PreviewMouseUp += UcGeorefenceMouseUp;

            //bCircle.PreviewTouchDown += BCircleTouchDown;
            //bCircle.PreviewTouchMove += BCircleTouchMove;
            //bCircle.PreviewTouchUp += BCircleTouchUp;

            bCircle.RelativeElement = AppState.ViewDef.MapControl;
            bCircle.IconMoved += BCircleIconMoved;
            bCircle.IconTapped += BCircleIconTapped;
            bCircle.IconReleased += BCircleIconReleased;


            if (DataContext is DataBinding)
            {
                var db = (DataBinding)DataContext;
                _direction = db.Attributes["direction"] as DirectionTool;
                _state = db.Attributes["state"].ToString();
                _direction.PropertyChanged += DirectionOnPropertyChanged;

                AppState.ViewDef.MapManipulationDelta += ViewDef_MapManipulationDelta;

                Menu = Convert.ToBoolean(db.Attributes["menuenabled"]);

            }

            if (!CanBeDragged)
            {
                ShowInfo = true;
            }

            

//            _effectsPlugin = IoC.Get<IEffectsMapToolPlugin>();
//            ImbSubscribe();

//            GetChemicalNames();
//            GetUnits();
//            GetModels();

            /*
            GetModelParameters("BLEVE (Static or Dynamic model)");

            var bleveFi = new FileInfo(@"Plugins\Effects\Data\Bleve.json");
            JObject bleveParams = null;
            if (bleveFi.Exists)
            {
                using (var reader = bleveFi.OpenText())
                {
                    var strBleveParams = reader.ReadToEnd();
                    bleveParams = JObject.Parse(strBleveParams);
                    reader.Close();
                }
            }

            if (bleveParams != null)
            {
                var inputParams = new JArray(bleveParams["Parameters"].Children().Where(p => !(bool) p["IsResult"]));
                var requestBody = new JObject(
                        new JProperty("CalculationRequest", new JObject(
                                new JProperty("ModelName", "BLEVE (Static or Dynamic model)"),
                                new JProperty("Parameters", inputParams)
                            ))
                    );
                
                CalculationRequest(requestBody.ToString());
            }
             */
        }
//
//        private void SignalEvent(int commandId, string body = null)
//        {
//            if (effectsEvent == null) return;
//
//            TByteBuffer Payload = new TByteBuffer();
//            Payload.Prepare((int)commandId);
//            if (body != null)
//            {
//                Payload.Prepare(body);
//            }
//            Payload.PrepareApply();
//            Payload.QWrite((int)commandId);
//            if (body != null)
//            {
//                Payload.QWrite(commandId);
//            }
//
//            effectsEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
//        }
//
//        public void GetChemicalNames()
//        {
//            SignalEvent(1109);
//        }
//
//        public void GetUnits()
//        {
//            SignalEvent(1107);
//        }
//
//        public void GetModels()
//        {
//            SignalEvent(1101);
//        }
//
//        public void GetModelParameters(string modelName)
//        {
//            SignalEvent(1103, modelName);
//        }
        
//        private void CalculationRequest(string calcRequestBody)
//        {
//            TByteBuffer Payload = new TByteBuffer();
//            Payload.Prepare((int)1105);
//            Payload.Prepare(calcRequestBody);
//            Payload.PrepareApply();
//            Payload.QWrite((int)1105);
//            Payload.QWrite(calcRequestBody);
//            effectsEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
//        }

        private void DirectionOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "WindDirection")
            {
                UpdateDirection();
            }
        }

        public bool Menu
        {
            get { return (bool)GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Menu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuProperty =
            DependencyProperty.Register("Menu", typeof(bool), typeof(EffectsMapToolView), new UIPropertyMetadata(false));



        void ViewDef_MapManipulationDelta(object sender, EventArgs e)
        {
            UpdateDirection();
        }

         
        #endregion

        #region touch

        private bool _firstMove;
        private FloatingElement _settingsFloatingElement;


        private void BCircleIconReleased(object sender, IconMovedEventArgs e)
        {
            if (_firstMove)
            {
                CanBeDragged = true;
                _direction = null;
            }
        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            ShowInfo = !ShowInfo;
            
        }

        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            if (_direction == null) _firstMove = true;
            if (CanBeDragged && _direction == null)
            {
                _direction = new DirectionTool() { Layer = this._layer };


                var p = e.Position;
                SharpMap.Geometries.Point pos = AppState.ViewDef.ViewToWorld(p.X, p.Y);
                var mpStart = (MapPoint)Mercator.FromGeographic(new MapPoint(pos.Y, pos.X));

                var p2 = e.Position;
                SharpMap.Geometries.Point pos2 = AppState.ViewDef.ViewToWorld(p2.X + 65, p2.Y - 65);
                var mpFinish = (MapPoint)Mercator.FromGeographic(new MapPoint(pos2.Y, pos2.X));

                var eSender = (FrameworkElement) sender;

                _direction.Init(_layer, mpStart, mpFinish, this.Resources);

                CanBeDragged = false;

                //e.TouchDevice.Capture(bCircle);
                //e.Handled = true;
            }
            else
            {
                if (_direction != null)
                {
                    var pos = e.Position;
                    var w = AppState.ViewDef.ViewToWorld(pos.X, pos.Y);
                    _direction.UpdatePoint(_state, (MapPoint)Mercator.FromGeographic(new MapPoint(w.Y, w.X)));
                    UpdateDirection();
                    //_center = e.GetTouchPoint(AppState.MapControl).Position;
                    //e.TouchDevice.Capture(bCircle);
                    //e.Handled = true;

                }
            }


        }



        #endregion

        #region mouse

        private void UpdateDirection()
        {
            if (this._state == "finish")
            {
                tIconImage.Visibility = Visibility.Collapsed;
                tIconLabel.Text = _direction.WindDirection;
            }
            else
            {
                RemoveMenuItem.Visibility = Visibility.Visible;             
                EditMenuItem.Visibility = Visibility.Visible;
                ZoomMenuItem.Visibility = Visibility.Visible;
            }
        }
        

        #endregion

        private void RemoveMenuItem_OnTap(object sender, RoutedEventArgs e)
        {
            if (_settingsFloatingElement != null)
            {
                _settingsFloatingElement.Close();
            }
            
            _settingsFloatingElement = null;

            _direction.Remove();
        }

        private void EditMenuItem_OnTap(object sender, RoutedEventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                if (_settingsFloatingElement == null)
                {
                    _settingsFloatingElement = FloatingHelpers.CreateFloatingElement("Effects model settings",
                        new Point(350, 340), new Size(500, 550), _direction.SettingsViewModel);
                    _settingsFloatingElement.Closed += SettingsFloatingElementOnClosed;
                    AppState.FloatingItems.AddFloatingElement(_settingsFloatingElement);
                }

            });
        }

        private void SettingsFloatingElementOnClosed(object sender, EventArgs eventArgs)
        {
            _settingsFloatingElement = null;
        }

        private void ZoomMenuItem_OnTap(object sender, RoutedEventArgs e)
        {
            var leftX = Math.Min(_direction.Start.Mp.X, _direction.Finish.Mp.X) * 0.999f;
            var leftY = Math.Min(_direction.Start.Mp.Y, _direction.Finish.Mp.Y) * 0.999f;
            var rightX = Math.Max(_direction.Start.Mp.X, _direction.Finish.Mp.X)*1.001f;
            var rightY = Math.Max(_direction.Start.Mp.Y, _direction.Finish.Mp.Y)*1.001f;
            
            var modelEnvelope = new Envelope(leftX, leftY, rightX, rightY);
            modelEnvelope.SpatialReference = _direction.Start.Mp.SpatialReference;
            var mvd = AppStateSettings.Instance.ViewDef;
            mvd.MapControl.ZoomTo(modelEnvelope);
        }
        
    }

    public class EffectsPoint
    {
        public MapPoint Mp;
        public TextBlock Label;
        public bool Fixed = false;
    }
}
