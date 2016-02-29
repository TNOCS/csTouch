using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using csShared.Geo;

namespace csCommon
{
    public class LayerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTemplate { get; set; }
        public DataTemplate GraphicsTemplate { get; set; }
        public DataTemplate SettingsGraphicsTemplate { get; set; }
        public DataTemplate StartStopGraphicsTemplate { get; set; }
        public DataTemplate SettingsElementTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate WmsTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item,DependencyObject container)
        {
            if (item is IStartStopLayer)          return StartStopGraphicsTemplate;
            if (item is WmsLayer)                 return WmsTemplate;
            if (item is GroupLayer)               return GroupTemplate;
            if (item is SettingsGraphicsLayer)    return SettingsGraphicsTemplate;
            if (item is SettingsElementLayer)     return SettingsElementTemplate;
            if (item is GraphicsLayer)            return GraphicsTemplate;
            if (item is AcceleratedDisplayLayers) return GroupTemplate;
            
            return DefaultTemplate;
        }
    }
}