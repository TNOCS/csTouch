using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using BaseWPFHelpers;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using IMB3;
using Microsoft.Surface.Presentation.Controls;
using csGeoLayers.Plugins.Layers;
using csShared;
using csShared.Geo;

namespace csCommon
{
    public class LayerExtensions
    {
        public static readonly DependencyProperty ParentProperty = DependencyProperty.RegisterAttached(
            "Parent",
            typeof (Layer),
            typeof (Layer),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty StoredProperty = DependencyProperty.RegisterAttached(
            "Stored",
            typeof (StoredLayer),
            typeof (Layer),
            new PropertyMetadata(null)
            );

        public static void SetParent(UIElement element, Layer value)
        {
            element.SetValue(ParentProperty, value);
        }

        public static string GetParent(UIElement element)
        {
            return (string) element.GetValue(ParentProperty);
        }

        public static void SetStored(UIElement element, StoredLayer value)
        {
            element.SetValue(StoredProperty, value);
        }

        public static StoredLayer GetStored(UIElement element)
        {
            return (StoredLayer) element.GetValue(StoredProperty);
        }
    }

    public partial class LayersView
    {
        // Using a DependencyProperty as the backing store for _previousLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty _previousLayersProperty =
            DependencyProperty.Register("_previousLayers", typeof (ObservableCollection<GroupLayer>),
                                        typeof (LayersView), new UIPropertyMetadata(null));


        // Using a DependencyProperty as the backing store for AddLayerEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddLayerEnabledProperty =
            DependencyProperty.Register("AddLayerEnabled", typeof (bool), typeof (LayersView),
                                        new UIPropertyMetadata(true));

        private Layer _accselectedGroup;
        private Layer _selectedGroup;


        private List<WmsLayerLayer> layers = new List<WmsLayerLayer>();
        //private WmsLayer wl;

        public LayersView()
        {
            InitializeComponent();
            Loaded += LayersView_Loaded;
        }

        private AppStateSettings state
        {
            get { return AppStateSettings.Instance; }
        }

        public ObservableCollection<GroupLayer> PreviousLayers
        {
            get { return (ObservableCollection<GroupLayer>) GetValue(_previousLayersProperty); }
            set { SetValue(_previousLayersProperty, value); }
        }

        public bool AddLayerEnabled
        {
            get { return (bool) GetValue(AddLayerEnabledProperty); }
            set { SetValue(AddLayerEnabledProperty, value); }
        }


        private void LayersView_Loaded(object sender, RoutedEventArgs e)
        {
            _selectedGroup = AppStateSettings.Instance.ViewDef.Layers;
            _accselectedGroup = AppStateSettings.Instance.ViewDef.AcceleratedLayers;

            AppStateSettings.Instance.ViewDef.Layers.PropertyChanged += Layers_PropertyChanged;
            AppStateSettings.Instance.ViewDef.Layers.ChildLayers.CollectionChanged += ChildLayers_CollectionChanged;
            PropertyChangedLayer(AppStateSettings.Instance.ViewDef.Layers);
            UpdateSource();
        }

        private void ChildLayers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PropertyChangedLayer(AppStateSettings.Instance.ViewDef.Layers);
        }

        private void Layers_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var wmslayer = sender as WmsLayer;
            if (wmslayer.Layers != null)
                if (wmslayer.Visible && AppStateSettings.Instance.Imb.IsConnected)
                {
                    TEventEntry kmlpublish =
                        AppStateSettings.GetInstance().Imb.Imb.Publish(
                            AppStateSettings.GetInstance().Imb.Imb.ClientHandle + ".kml");
                    string kml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                 "<kml xmlns=\"http://www.opengis.net/kml/2.2\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\" xmlns:kml=\"http://www.opengis.net/kml/2.2\" xmlns:atom=\"http://www.w3.org/2005/Atom\">";
                    kml += "<Folder id=\"WMSTest - " + wmslayer.ID + "\"><name>WMSTest - " + wmslayer.ID + "</name>";
                    //foreach (var w in wl.Layers)
                    {
                        string u = CreateUrl(wmslayer);
                        kml += "<GroundOverlay><name>" + wmslayer.ID + "</name><Icon>";
                        kml += "<href>" + u + "</href>";
                        kml +=
                            "<viewRefreshMode>onStop</viewRefreshMode><viewBoundScale>1.1</viewBoundScale></Icon></GroundOverlay>";
                    }
                    kml += "</Folder></kml>";
                    kml = kml.Replace("&", "&amp;");
                    kmlpublish.SignalString(kml);
                }
                else
                {
                    if (AppStateSettings.Instance.Imb.IsConnected)
                    {
                        TEventEntry kmlpublish =
                            AppStateSettings.GetInstance()
                                .Imb.Imb.Publish(AppStateSettings.GetInstance().Imb.Imb.ClientHandle +
                                                 ".kml");
                        string kml =
                            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><kml xmlns=\"http://www.opengis.net/kml/2.2\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\" xmlns:kml=\"http://www.opengis.net/kml/2.2\" xmlns:atom=\"http://www.w3.org/2005/Atom\">";
                        kml += "<Folder id=\"WMSTest - " + wmslayer.ID + "\"><name>WMSTest - " + wmslayer.ID + "</name>";
                        kml += "</Folder></kml>";
                        kml = kml.Replace("&", "&amp;");
                        kmlpublish.SignalString(kml);
                    }
                }
        }

        private string CreateUrl(WmsLayer wl)
        {
            int num = (wl.SpatialReference != null) ? wl.SpatialReference.WKID : 0;
            string str = wl.Url;
            var builder = new StringBuilder(str);
            if (!str.Contains("?"))
            {
                builder.Append("?");
            }
            else if (!str.EndsWith("&"))
            {
                builder.Append("&");
            }
            builder.Append("SERVICE=WMS&REQUEST=GetMap");
            builder.AppendFormat("&WIDTH={0}", 512);
            builder.AppendFormat("&HEIGHT={0}", 512);
            builder.AppendFormat("&FORMAT={0}", wl.ImageFormat);
            builder.AppendFormat("&LAYERS={0}", (wl.Layers == null) ? "" : string.Join(",", wl.Layers));
            builder.Append("&STYLES=");
            builder.AppendFormat("&BGCOLOR={0}", "0xFFFFFF");
            builder.AppendFormat("&TRANSPARENT={0}", "TRUE");
            builder.AppendFormat("&VERSION={0}", "1.1.1");
            if (((wl.SupportedSpatialReferenceIDs != null) &&
                 (((num == 0x18ed4) || (num == 0x18ee1)) || ((num == 0xf11) || (num == 0xdbf31)))) &&
                !wl.SupportedSpatialReferenceIDs.Contains(num))
            {
                if (wl.SupportedSpatialReferenceIDs.Contains(0xf11))
                {
                    num = 0xf11;
                }
                else if (wl.SupportedSpatialReferenceIDs.Contains(0x18ed4))
                {
                    num = 0x18ed4;
                }
                else if (wl.SupportedSpatialReferenceIDs.Contains(0x18ee1))
                {
                    num = 0x18ee1;
                }
                else if (wl.SupportedSpatialReferenceIDs.Contains(0xdbf31))
                {
                    num = 0xdbf31;
                }
            }
            builder.AppendFormat("&SRS=EPSG:{0}", num);
            string returnstring = builder.ToString();
            returnstring.Replace("&", "&amp;");
            return returnstring;
        }

        private void PropertyChangedLayer(object layer)
        {
            if (layer is GroupLayer)
            {
                var gl = layer as GroupLayer;
                foreach (Layer cl in gl.ChildLayers)
                    PropertyChangedLayer(cl);
            }
            else if (layer is WmsLayer)
            {
                var wl = layer as WmsLayer;
                wl.PropertyChanged += Layers_PropertyChanged;
            }
            else if (layer is AcceleratedDisplayLayers)
            {
                var wl = layer as AcceleratedDisplayLayers;
                wl.PropertyChanged += Layers_PropertyChanged;
            }
        }


        private void UpdateSource()
        {
            var ts = new LayerTemplateSelector();
            //ts.GraphicsTemplate = FindResource("GraphicsTemplate") as DataTemplate;
            //ts.SettingsGraphicsTemplate = FindResource("SettingsGraphicsTemplate") as DataTemplate;
            //ts.SettingsElementTemplate = FindResource("SettingsElementTemplate") as DataTemplate;
            //ts.GroupTemplate = FindResource("GroupTemplate") as DataTemplate;
            //ts.WmsTemplate = FindResource("WmsTemplate") as DataTemplate;
            //ts.DefaultTemplate = FindResource("GraphicsTemplate") as DataTemplate;
            //ts.StartStopGraphicsTemplate = FindResource("StartStopTemplate") as DataTemplate;
        }

        private void _selectedGroup_LegendChanged(object sender, EventArgs e)
        {
            UpdateSource();
        }


    
        private void sbZoom_Click(object sender, RoutedEventArgs e)
        {
            var l = (Layer) ((SurfaceButton) sender).DataContext;
            if (l.SpatialReference == AppStateSettings.Instance.ViewDef.MapControl.SpatialReference ||
                l.SpatialReference == null)
            {
                state.ViewDef.MapControl.ZoomTo(l.FullExtent);
            }
        }

       
        private void sbSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sgl = (SettingsGraphicsLayer) ((FrameworkElement) sender).DataContext;
                if (sgl != null)
                    sgl.StartSettings();
            }
            catch
            {
            }
            try
            {
                var sel = (SettingsElementLayer) ((FrameworkElement) sender).DataContext;
                if (sel != null)
                    sel.StartSettings();
            }
            catch
            {
            }
        }


        //private void sbWmsSelect_Click(object sender, RoutedEventArgs e)
        //{
        //    AddLayerEnabled = false;
        //    wl = (WmsLayer) ((FrameworkElement) sender).DataContext;

        //    layers = new List<WmsLayerLayer>();
        //    if (wl != null)
        //    {
        //        foreach (WmsLayer.LayerInfo l in wl.LayerList.ToList())
        //        {
        //            var wmsl = new WmsLayerLayer {Title = l.Title, Selected = wl.Layers.Contains(l.Title)};
        //            layers.Add(wmsl);
        //        }
        //        tvWmsLayers.ItemsSource = layers;

        //        sbBack.Visibility = Visibility.Visible;
        //        //_selectedGroup.LegendChanged -= _selectedGroup_LegendChanged;
        //        PreviousLayers.Insert(0, tvLayers.DataContext as GroupLayer);
        //        _selectedGroup = (WmsLayer) ((SurfaceButton) sender).DataContext;
        //        //_selectedGroup.LegendChanged += _selectedGroup_LegendChanged;            
        //        GroupLayer pl = PreviousLayers[0];
        //        sbBack.Content = pl.ID;
        //        sbCurrent.Content = "> " + _selectedGroup.ID;
        //    }

        //    tvWmsLayers.Visibility = Visibility.Visible;
        //    tvLayers.Visibility = Visibility.Collapsed;
        //    // TODO: Add event handler implementation here.
        //}

        //private void scbWmsLayer_Checked(object sender, RoutedEventArgs e)
        //{
        //    UpdateLayers();
        //}

        //private void scbWmsLayer_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    UpdateLayers();
        //}

        //private void UpdateLayers()
        //{
        //    if (wl != null)
        //    {
        //        wl.Layers = layers.Where(k => k.Selected).Select(k => k.Title).ToArray();
        //        PropertyChangedLayer(AppStateSettings.Instance.ViewDef.Layers);

        //        //var kmlpublish = AppStateSettings.GetInstance().Imb.Imb.Publish(AppStateSettings.GetInstance().Imb.Imb.ClientHandle + ".kml");
        //        //var kml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"+
        //        //    "<kml xmlns=\"http://www.opengis.net/kml/2.2\" xmlns:gx=\"http://www.google.com/kml/ext/2.2\" xmlns:kml=\"http://www.opengis.net/kml/2.2\" xmlns:atom=\"http://www.w3.org/2005/Atom\">";
        //        //foreach (var w in wl.Layers)
        //        //{
        //        //    kml += "<GroundOverlay><name>" + w + "</name><Icon>";
        //        //    kml +=
        //        //        "<href>http://geodata.nationaalgeoregister.nl/ahn25m/wms?SERVICE=WMS&amp;VERSION=1.1.1&amp;REQUEST=GetMap&amp;SRS=EPSG:4326&amp;WIDTH=512&amp;HEIGHT=512&amp;LAYERS=ahn25m&amp;STYLES=ahn25_cm&amp;TRANSPARENT=TRUE&amp;FORMAT=image/gif</href>";
        //        //    kml +=
        //        //        "<viewRefreshMode>onStop</viewRefreshMode><viewBoundScale>0.75</viewBoundScale></Icon></GroundOverlay>";
        //        //}
        //        //kml += "</kml>";

        //        //kmlpublish.SignalString(kml);
        //    }
        //}


        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            string path = "";
            if (_selectedGroup != AppStateSettings.Instance.ViewDef.Layers)
            {
                path = _selectedGroup.ID;
                Layer l = _selectedGroup;
                object p = l.GetValue(LayerExtensions.ParentProperty);
                while (p != null && p is Layer && p != AppStateSettings.Instance.ViewDef.Layers)
                {
                    path = ((Layer) p).ID + @"/" + path;
                    l = (Layer) p;
                    p = l.GetValue(LayerExtensions.ParentProperty);
                    if (((Layer) p).ID == "Layers") break;
                }
            }

            FrameworkElement a = Helpers.FindElementOfTypeUp(this, typeof (ScatterViewItem));
            if (a != null)
            {
                var Element = a.DataContext as FloatingElement;
                (Element.ModelInstanceBack as NewLayerViewModel).Path = path;
                Element.Flip();
            }


            //var viewModel = new NewLayerView();
            //if (viewModel != null)
            //{
            //    var Element = FloatingHelpers.CreateFloatingElement("Twitter Config", new Point(400, 400), new Size(400, 400), viewModel);
            //    AppStateSettings.Instance.FloatingItems.AddFloatingElement(Element);
            //}
        }

        private void sbDelete_Click(object sender, RoutedEventArgs e)
        {
            var l = (Layer) ((FrameworkElement) sender).DataContext;
            if (l != null && _selectedGroup is GroupLayer)
            {
                var sg = (GroupLayer) _selectedGroup;
                if (sg.ChildLayers.Contains(l))
                {
                    sg.ChildLayers.Remove(l);
                }
                var sl = l.GetValue(LayerExtensions.StoredProperty) as StoredLayer;
                if (sl != null && AppStateSettings.Instance.ViewDef.StoredLayers.Contains(sl))
                {
                    AppStateSettings.Instance.ViewDef.StoredLayers.Remove(sl);
                    AppStateSettings.Instance.ViewDef.StoredLayers.Save();
                }
            }
        }
    }
}