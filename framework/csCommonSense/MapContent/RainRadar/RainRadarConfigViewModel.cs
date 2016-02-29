using Caliburn.Micro;
using csShared;
using csShared.Geo;
using System.ComponentModel.Composition;
using System;
using Microsoft.Surface.Presentation.Controls;

namespace csGeoLayers.Content.RainRadar
{



    public interface IRainRadarConfig
    {
    }

    [Export(typeof(IRainRadarConfig))]
    public class RainRadarConfigViewModel : Screen, IRainRadarConfig
    {        

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }            
        }

        private RainRadarConfigView _view;

        public FloatingElement Element { get; set; }

        private RainRadarLayer _layer;

        public RainRadarLayer Layer { get { return _layer; } set { _layer = value; NotifyOfPropertyChange(()=>Layer); } }

        private bool _isrefreshing;
        public bool IsRefreshing { get { return _isrefreshing; } set { _isrefreshing = value; NotifyOfPropertyChange(() => IsRefreshing); NotifyOfPropertyChange(() => CanStart); NotifyOfPropertyChange(() => CanStop); } }

        
        public MapViewDef ViewDef
        {
            get { return AppState.ViewDef; }            
        }
        
        
        public RainRadarConfigViewModel(RainRadarLayer layer)
        {
            // Default caption
            Caption = "RainRadar Config";
            _layer = layer;
            IsRefreshing = Layer.IsRefreshing;
            Layer.PropertyChanged += Layer_PropertyChanged;
        }

        void Layer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsRefreshing")
            {
                IsRefreshing = Layer.IsRefreshing;
            }
        }


        public string Caption { get; set; }

        public SurfaceListBoxItem _selectedInterval;
        public SurfaceListBoxItem SelectedInterval 
        { 
            get 
            { 
                return _selectedInterval; 
            } 
            set 
            { 
                _selectedInterval = value; 
                if (value != null) 
                {
                    var i = Convert.ToInt16(value.Content);
                    Layer.SetInterval(i);
                    NotifyOfPropertyChange(() => SelectedInterval); 
                } 
                } 
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            _view = (RainRadarConfigView) view;
           
        }

        public bool CanStart { get { return !IsRefreshing; } }
        public bool CanStop { get { return IsRefreshing; } }

        public void Start()
        {
            Layer.Start();
        }

        public void Stop()
        {
            Layer.Stop();
        }

        public string Name
        {
            get { return "RainRadarConfiguration"; }
        }

        
 

}
}
