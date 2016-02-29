using System;
using System.Windows.Media;
using Caliburn.Micro;

namespace csGeoLayers.ShapeFiles
{
    using System.ComponentModel.Composition;
    using csShared;
    using csShared.Geo;
    using System.IO;
    

    public interface IShapeLayerColorPicker
    {
    }

    [Export(typeof(IShapeLayerColorPicker))]
    public class ShapeLayerColorPickerViewModel : Screen, IShapeLayerColorPicker
    {
        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        private ShapeLayerColorPickerView _view;

        public FloatingElement Element { get; set; }

        
        private SettingsGraphicsLayer _layer;
        private string _fileName;

        public MapViewDef ViewDef
        {
            get { return AppState.ViewDef; }            
        }

        public ShapeLayerColorPickerViewModel(SettingsGraphicsLayer layer, string fileName)
        {
            _layer = layer;
            _fileName = fileName;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            _view = (ShapeLayerColorPickerView)view;
            var fileInfo = new FileInfo(_fileName);
            var settingsFile = fileInfo.Directory + "/" + fileInfo.Name + ".xml";
            var settings = ShapeLayer.GetSettings(settingsFile);
            _view.clrR.Value = settings.LineColor.R;
            _view.clrG.Value = settings.LineColor.G;
            _view.clrB.Value = settings.LineColor.B;
        }

        public void ChangeColor()
        {
            // Change color of elements in this layer
            var color = Color.FromRgb(Convert.ToByte(_view.clrR.Value), Convert.ToByte(_view.clrG.Value), Convert.ToByte(_view.clrB.Value));
            var settings = new ShapeLayerSettings();
            settings.LineColor = color;
            var fileInfo = new FileInfo(_fileName);
            var settingsFile = fileInfo.Directory + "/" + fileInfo.Name + ".xml";
            ShapeLayer.WriteSettings(settings, settingsFile);
            var brush = new SolidColorBrush(color);
            foreach (var grp in _layer.Graphics)
            {
                dynamic symbol = grp.Symbol;
                if (symbol != null)
                {
                    if (symbol.ToString().ToLower().Contains("simplelinesymbol"))
                    {
                        try
                        {
                            symbol.Color = brush;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (symbol.ToString().ToLower().Contains("simplefillsymbol"))
                    {
                        try
                        {
                            symbol.Fill = brush;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    /*try
                    {
                        symbol.Fill = brush;
                    }
                    catch (Exception)
                    {
                    }*/
                }
            }
        }

        public string Name
        {
            get { return "ShapeLayerConfig"; }
        }
    }
   
}
