using System;
using System.Globalization;
using Caliburn.Micro;
using ProtoBuf;

namespace DataServer
{
    /// <summary>
    /// Field of view class
    /// </summary>
    public class FieldOfView : PropertyChangedBase
    {
        private double orientation;
        private double range;
        private double rangeMin;
        private double angle;
        private double height;

        public FieldOfView(double orientation, double range, double rangeMin, double angle, double height)
        {
            this.orientation = orientation;
            this.range = range;
            this.rangeMin = rangeMin;
            this.angle = angle;
            this.height = height;
        }

        public FieldOfView()
        {
        }

        /// <summary>
        /// Direction at which you are looking, where 0 represents North.
        /// </summary>
        [ProtoMember(1)]
        public double Orientation
        {
            get { return orientation; }
            set
            {
                if (Math.Abs(orientation - value) < double.Epsilon) return;
                orientation = value;
                NotifyOfPropertyChange(() => Orientation);
            }
        }

        /// <summary>
        /// Angle at which you are looking. The angle is evenly divided left and right of the FoV Orientation.
        /// </summary>
        [ProtoMember(2)]
        public double Angle
        {
            get { return angle; }
            set
            {
                if (Math.Abs(angle - value) < double.Epsilon) return;
                angle = value;
                NotifyOfPropertyChange(() => Angle);
            }
        }

        /// <summary>
        /// How far are you looking
        /// </summary>
        [ProtoMember(3)]
        public double Range
        {
            get { return range; }
            set
            {
                if (Math.Abs(range - value) < double.Epsilon) return;
                range = value;
                NotifyOfPropertyChange(() => Range);
            }
        }

        /// <summary>
        /// Where do you start looking
        /// </summary>
        [ProtoMember(5)]
        public double RangeMin
        {
            get { return rangeMin; }
            set
            {
                if (Math.Abs(rangeMin - value) < double.Epsilon) return;
                rangeMin = value;
                NotifyOfPropertyChange(() => RangeMin);
            }
        }

        /// <summary>
        /// Height at which you are standing, relative to the ground (so 0 means your eyes are at ground level).
        /// </summary>
        [ProtoMember(4)]
        public double Height
        {
            get { return height; }
            set
            {
                if (Math.Abs(height - value) < double.Epsilon) return;
                height = value;
                NotifyOfPropertyChange(() => Height);
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.000};{1:0.000};{2:0.000};{3:0.000};{4:0.000}", orientation, angle, range, height, rangeMin);
        }

        /// <summary>
        /// Create a Field of View object from a string
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static FieldOfView FromString(string f)
        {
            var fov = new FieldOfView();
            try
            {
                var split = f.Split(new[] { ';' }, StringSplitOptions.None);
                if (split.Length != 5) return null;
                double result;
                if (double.TryParse(split[0], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) fov.orientation = result;
                if (double.TryParse(split[1], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) fov.angle = result;
                if (double.TryParse(split[2], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) fov.range = result;
                if (double.TryParse(split[3], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) fov.height = result;
                if (double.TryParse(split[4], NumberStyles.Number, CultureInfo.InvariantCulture, out result)) fov.rangeMin = result;
                return fov;
            }
            catch
            {
                return null;
            }
        }
    }
}