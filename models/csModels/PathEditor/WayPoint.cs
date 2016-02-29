using System;
using System.Globalization;
using System.Windows;
using Caliburn.Micro;

namespace csModels.PathEditor
{
    public class WayPoint : PropertyChangedBase
    {
        private int index;

        public WayPoint(Point point, Guid id) {
            Id    = id;
            Point = point;
        }

        public Guid Id { get; set; }

        public int Index
        {
            get { return index; }
            set
            {
                if (value == index) return;
                index = value;
                NotifyOfPropertyChange(() => Index);
            }
        }

        public Point Point { get; set; }

        public bool IsLastItem { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1}", Point.Y, Point.X);
        }
    }
}