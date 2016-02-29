using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using DataServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace csModels.LocationModel
{
    

    [Export(typeof(IScreen))]
    public class LocationViewModel : Screen, IEditableScreen
    {
        private bool canEdit;

        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public LocationViewModel()
        {
            
        }

        public IModel Model { get; set; }

        private double latitude;

        public double Latitude
        {
            get { return latitude; }
            set { latitude = value;                
                NotifyOfPropertyChange(() => Latitude); }
        }

        private double longitude;

        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; NotifyOfPropertyChange(() => Longitude); }
        }
        
        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        private PoI poi;

        public PoI PoI
        {
            get { return poi; }
            set { poi = value;
            PoI.PositionChanged += UpdatePosition;
            UpdatePosition(this, null);
            }
        }

        void UpdatePosition(object sender, PositionEventArgs e)
        {
            if (PoI.Position != null)
            {
                this.Latitude = PoI.Position.Latitude;
                this.Longitude = PoI.Position.Longitude;
            }
        }

        public void Save()
        {            
            PoI.Position.Latitude = this.Latitude;
            PoI.Position.Longitude = this.Longitude;

            CallOut.Close();
            PoI.TriggerPositionChanged();
        }
    }
}
