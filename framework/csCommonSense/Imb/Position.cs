using System;
using Caliburn.Micro;

namespace csImb
{
    public class Position : PropertyChangedBase
    {
        private double _course;
        private double _latitude;

        private double _longitude;

        private double _precision;
        private double _speed;
        public DateTime Date { get; set; }

        public double Latitude
        {
            get { return _latitude; }
            set
            {
                _latitude = value;
                NotifyOfPropertyChange(() => Latitude);
            }
        }

        public double Longitude
        {
            get { return _longitude; }
            set
            {
                _longitude = value;
                NotifyOfPropertyChange(() => Longitude);
            }
        }

        public double Precision
        {
            get { return _precision; }
            set
            {
                _precision = value;
                NotifyOfPropertyChange(() => Precision);
            }
        }

        public double Course
        {
            get { return _course; }
            set
            {
                _course = value;
                NotifyOfPropertyChange(() => Course);
            }
        }

        public double Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                NotifyOfPropertyChange(() => Speed);
            }
        }
    }
}