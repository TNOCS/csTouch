﻿using Caliburn.Micro;
using csShared.Geo;

namespace csCommon.MapPlugins.Search
{
    public class GeoCodeSearchResult : PropertyChangedBase
    {
        private string _name;
        public string Name { get { return _name; } set { _name = value; NotifyOfPropertyChange(()=>Name); } }

        private KmlPoint _location;
        public KmlPoint Location { get { return _location; } set { _location = value; NotifyOfPropertyChange(()=>Location); } }

        private LatLonBox _box;
        public LatLonBox Box { get { return _box; } set { _box = value; NotifyOfPropertyChange(()=>Box); } }

        private string icon;

        public string Icon
        {
            get { return icon; }
            set { icon = value; NotifyOfPropertyChange(()=>Icon); }
        }
        

    }
}