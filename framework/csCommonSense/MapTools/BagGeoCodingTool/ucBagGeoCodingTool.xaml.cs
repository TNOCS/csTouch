#region references

using System.Reactive.Linq;
using Caliburn.Micro;
using csCommon.csMapCustomControls.MapIconMenu;
using csCommon.MapTools.GeoCodingTool;
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


#endregion

namespace csCommon.MapTools.BagGeoCodingTool
{
    public partial class ucBagGeoCodingTool
    {
        #region fields

        private static readonly WebMercator Mercator = new WebMercator();
        private ReverseBagGeoCoding geo;
        private bool showInfo;

        #endregion

        #region dependency properties

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(MapPoint), typeof(ucBagGeoCodingTool), new UIPropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Grph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GrphProperty =
            DependencyProperty.Register("Grph", typeof(Graphic), typeof(ucBagGeoCodingTool), new UIPropertyMetadata(null));

        // Using a DependencyProperty as the backing store for LatLon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LatLonProperty =
            DependencyProperty.Register("LatLon", typeof(string), typeof(ucBagGeoCodingTool), new UIPropertyMetadata(""));

        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucBagGeoCodingTool), new UIPropertyMetadata(false));

        private readonly GraphicsLayer gLayer;
        private Graphic graphic;

        #endregion

        #region constructor

        public ucBagGeoCodingTool()
        {
            InitializeComponent();
            Loaded += UcPlacemarkLoaded;
            var layer = AppState.ViewDef.MapToolsLayer;
            var l = layer.ChildLayers.FirstOrDefault(k => k.ID == "BagGeo") as GraphicsLayer;
            if (l != null)
            {
                gLayer = l;
            }
            else
            {
                gLayer = new GraphicsLayer { ID = "BagGeo" };
                layer.ChildLayers.Add(gLayer);
                gLayer.Initialize();
            }
        }

        #endregion

        #region properties

        private bool firstMove;

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
            get { return showInfo; }
            set
            {
                showInfo = value;
                if (showInfo && !CanBeDragged)
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
            DependencyProperty.Register("Menu", typeof(bool), typeof(ucBagGeoCodingTool), new UIPropertyMetadata(false));

        #endregion

        #region private methods

        private void UcPlacemarkLoaded(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(Tag, "First"))
            {
                CanBeDragged = false;
                firstMove = true;
            }
            else
            {
                bCircle.IsExpanded = true;
            }

            bCircle.RelativeElement = AppState.ViewDef.MapControl;
            bCircle.IconMoved       += BCircleIconMoved;
            bCircle.IconTapped      += BCircleIconTapped;
            bCircle.IconReleased    += BCircleIconReleased;

            geo = new ReverseBagGeoCoding();
            geo.Result += GeoResult;

            var binding = DataContext as DataBinding;
            if (binding != null)
            {
                var db = binding;
                graphic = db.Attributes["graphic"] as Graphic;
                if (graphic != null)
                {
                    Position = (MapPoint)Mercator.ToGeographic(graphic.Geometry);
                    var timeChanged = Observable.FromEventPattern<DictionaryChangedEventArgs>(ev => graphic.AttributeValueChanged += ev, ev => graphic.AttributeValueChanged -= ev);
                    timeChanged.Sample(TimeSpan.FromMilliseconds(333)).Subscribe(k => GraphicAttributeValueChanged());
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
            if (!firstMove) return;
            if (!Remove(this, null)) return;
            var pos = AppState.ViewDef.ViewToWorld(e.Position.X, e.Position.Y);
            var g = new Graphic { Geometry = Mercator.FromGeographic(new MapPoint(pos.Y, pos.X)) };
            g.Attributes["position"] = new MapPoint(pos.Y, pos.X);
            g.Attributes["graphic"] = g;
            g.Attributes["menuenabled"] = true;
            g.Symbol = FindResource("BagGeoCodingTool") as Symbol;
            gLayer.Graphics.Add(g);

            CanBeDragged = true;
        }

        private void BCircleIconTapped(object sender, IconMovedEventArgs e)
        {
            ShowInfo = !ShowInfo;
        }

        private void BCircleIconMoved(object sender, IconMovedEventArgs e)
        {
            if (CanBeDragged)
            {
                var p = e.Position;
                var pos = AppState.ViewDef.ViewToWorld(p.X, p.Y);
                graphic = new Graphic { Geometry = Mercator.FromGeographic(new MapPoint(pos.Y, pos.X)) };
                graphic.Attributes["position"] = new MapPoint(pos.Y, pos.X);

                graphic.Symbol = FindResource("BagGeoCodingTool") as Symbol;
                graphic.Attributes["graphic"] = graphic;
                gLayer.Graphics.Add(graphic);

                CanBeDragged = false;
            }
            else
            {
                var w = AppState.ViewDef.ViewToWorld(e.Position.X, e.Position.Y);
                //Position = 
                if (graphic == null) return;
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

        private void GraphicAttributeValueChanged()
        {
            var pos = (MapPoint)graphic.Attributes["position"];
            if (ShowInfo) geo.RetrieveFormatedAddress(pos, false);
        }

        private void GeoResult(object sender, ReverseGeocodingCompletedEventArgs e)
        {
            Execute.OnUIThread(() => {
                Position = e.Address.Position;
                spAddress.DataContext = e.Address;
            });
        }

        #endregion

        private bool Remove(object sender, ExecutedRoutedEventArgs e)
        {
            if (gLayer == null || !gLayer.Graphics.Contains(graphic)) return false;
            gLayer.Graphics.Remove(graphic);
            graphic = null;
            return true;
        }

        private void RemoveItem(object sender, RoutedEventArgs e)
        {
            if (gLayer == null || !gLayer.Graphics.Contains(graphic)) return;
            gLayer.Graphics.Remove(graphic);
            graphic = null;
        }
    }
}