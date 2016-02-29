#region references

using System.Reactive.Linq;
using Caliburn.Micro;
using csCommon.csMapCustomControls.MapIconMenu;
using csShared;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Graphics;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Point = SharpMap.Geometries.Point;

#endregion

namespace csCommon.MapTools.GeoCodingTool
{
    public partial class ucGeoCodingTool
    {
        #region fields

        private static readonly WebMercator Mercator = new WebMercator();
        private ReverseGeoCoding _geo;
        private bool _showInfo;

        #endregion

        #region dependency properties

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(MapPoint), typeof(ucGeoCodingTool),
                                        new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for Grph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GrphProperty =
            DependencyProperty.Register("Grph", typeof(Graphic), typeof(ucGeoCodingTool), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for LatLon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LatLonProperty =
            DependencyProperty.Register("LatLon", typeof(string), typeof(ucGeoCodingTool), new UIPropertyMetadata(""));


        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucGeoCodingTool),
                                        new UIPropertyMetadata(false));

        private readonly GraphicsLayer gLayer;
        private readonly GroupLayer _layer;
        private Graphic graphic;

        #endregion

        #region constructor

        public ucGeoCodingTool()
        {
            InitializeComponent();
            Loaded += UcPlacemarkLoaded;
            _layer = AppState.ViewDef.MapToolsLayer;
            Layer l = _layer.ChildLayers.FirstOrDefault(k => k.ID == "Geo");
            if (l != null && l is GraphicsLayer)
            {
                gLayer = (GraphicsLayer)l;
            }
            else
            {
                gLayer = new GraphicsLayer { ID = "Geo" };
                _layer.ChildLayers.Add(gLayer);
                gLayer.Initialize();
            }
        }

        #endregion

        #region properties

        private bool _firstMove;

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



        public bool Menu
        {
            get { return (bool)GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Menu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuProperty =
            DependencyProperty.Register("Menu", typeof(bool), typeof(ucGeoCodingTool), new UIPropertyMetadata(false));




        #endregion

        #region private methods

        private void UcPlacemarkLoaded(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(Tag, "First"))
            {
                CanBeDragged = false;
                _firstMove = true;
                bCircle.IsExpanded = false;
            }
            else
            {
                bCircle.IsExpanded = true;

            }

            bCircle.RelativeElement = AppState.ViewDef.MapControl;
            bCircle.IconMoved += BCircleIconMoved;
            bCircle.IconTapped += BCircleIconTapped;
            bCircle.IconReleased += BCircleIconReleased;

            _geo = new ReverseGeoCoding();
            _geo.Result += GeoResult;

            if (DataContext is DataBinding)
            {
                var db = (DataBinding)DataContext;
                graphic = db.Attributes["graphic"] as Graphic;
                if (graphic != null)
                {
                    Position = (MapPoint)Mercator.ToGeographic(graphic.Geometry);
                    var timeChanged = Observable.FromEventPattern<DictionaryChangedEventArgs>(ev => graphic.AttributeValueChanged += ev, ev => graphic.AttributeValueChanged -= ev);
                    timeChanged.Sample(TimeSpan.FromMilliseconds(333)).Subscribe(k => GraphicAttributeValueChanged());

                    //graphic.AttributeValueChanged += GraphicAttributeValueChanged;
                }

                Menu = Convert.ToBoolean(db.Attributes["menuenabled"]);
            }
            else
            {
                CanBeDragged = true;
            }

            if (!CanBeDragged)
            {
                ShowInfo = true;
            }
        }

        private void BCircleIconReleased(object sender, IconMovedEventArgs e)
        {
            if (_firstMove)
            {
                if (Remove(this, null))
                {

                    Point pos = AppState.ViewDef.ViewToWorld(e.Position.X, e.Position.Y);
                    var g = new Graphic();
                    g.Geometry = Mercator.FromGeographic(new MapPoint(pos.Y, pos.X));
                    g.Attributes["position"] = new MapPoint(pos.Y, pos.X);
                    g.Attributes["graphic"] = g;
                    g.Attributes["menuenabled"] = true;
                    g.Symbol = FindResource("GeoCodingTool") as Symbol;
                    gLayer.Graphics.Add(g);

                    CanBeDragged = true;
                }
            }

        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            ShowInfo = !ShowInfo;
        }

        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            if (CanBeDragged)
            {
                graphic = new Graphic();
                System.Windows.Point p = e.Position;
                Point pos = AppState.ViewDef.ViewToWorld(p.X, p.Y);
                graphic.Geometry = Mercator.FromGeographic(new MapPoint(pos.Y, pos.X));
                graphic.Attributes["position"] = new MapPoint(pos.Y, pos.X);

                graphic.Symbol = FindResource("GeoCodingTool") as Symbol;
                graphic.Attributes["graphic"] = graphic;
                gLayer.Graphics.Add(graphic);

                CanBeDragged = false;
            }
            else
            {
                Point w = AppState.ViewDef.ViewToWorld(e.Position.X, e.Position.Y);
                //Position = 
                if (graphic != null)
                {
                    graphic.Attributes["position"] = new MapPoint(w.Y, w.X);
                    if (!gLayer.Graphics.Contains(graphic))
                    {
                        gLayer.Graphics.Add(graphic);
                    }
                    if (graphic != null)
                    {
                        graphic.Geometry = Mercator.FromGeographic(new MapPoint(w.Y, w.X));
                    }
                }
            }
        }

        private void GraphicAttributeValueChanged()
        {
            var Position = (MapPoint)graphic.Attributes["position"];
            
                
                if (ShowInfo) _geo.RetrieveFormatedAddress(Position, false);
            
        }

        private void GeoResult(object sender, ReverseGeocodingCompletedEventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                Position = e.Address.Position;
                spAddress.DataContext = e.Address;
            });
        }

        #endregion

        private bool Remove(object sender, ExecutedRoutedEventArgs e)
        {
            if (gLayer != null && gLayer.Graphics.Contains(graphic))
            {
                gLayer.Graphics.Remove(graphic);
                graphic = null;
                return true;
            }
            return false;
        }

        private void Camera(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void RemoveItem(object sender, RoutedEventArgs e)
        {
            if (gLayer != null && gLayer.Graphics.Contains(graphic))
            {
                gLayer.Graphics.Remove(graphic);
                graphic = null;
            }
        }
    }
}