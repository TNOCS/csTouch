using System.Globalization;

namespace Dis2PoiService.DisMessages
{
    public partial class Message
    {
        public override string ToString()
        {
            if (Entity != null)
                return string.Format(CultureInfo.InvariantCulture, "{1}: Entity {0}", Entity, Entity.TimeSpecified
                                                                                                  ? Entity.Time.ToShortTimeString()
                                                                                                  : string.Empty);
            if (Track != null)
                return string.Format(CultureInfo.InvariantCulture, "{1}: Track {0}", Track.Position, Track.TimeSpecified
                                                                                                                         ? Track.Time.ToShortTimeString()
                                                                                                                         : string.Empty);
            if (Detonation != null)
                return string.Format(CultureInfo.InvariantCulture, "{1}: Detonation {0}", Detonation.Classification, Detonation.TimeSpecified
                                                                                                                         ? Detonation.Time.ToShortTimeString()
                                                                                                                         : string.Empty);
            if (Fire != null)
                return string.Format(CultureInfo.InvariantCulture, "{0}: Fire", Fire.TimeSpecified
                                                                                    ? Fire.Time.ToShortTimeString()
                                                                                    : string.Empty);
            if (PointOfImpact != null)
                return string.Format(CultureInfo.InvariantCulture, "{0}: Point Of Impact", PointOfImpact.TimeSpecified
                                                                                               ? PointOfImpact.Time.ToShortTimeString()
                                                                                               : string.Empty);
            if (PointOfOrigin != null)
                return string.Format(CultureInfo.InvariantCulture, "{0}: PointOfOrigin ", PointOfOrigin.TimeSpecified
                                                                                              ? PointOfOrigin.Time.ToShortTimeString()
                                                                                              : string.Empty);
            return "Unknown message";
        }
    }

    public partial class EntityType
    {
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} at ({1:0.000}, {2:0.000}, {3:0.000}), course: {4:0.0} ", Name, Position.Longitude, Position.Latitude, Position.Altitude, Course);
        }
    }

    public partial class PositionType
    {
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "at ({0:0.000}, {1:0.000}, {2:0.000})", Longitude, Latitude, Altitude);
        }
    }

    public partial class CourseType
    {
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "at {0:0.0} m/s, heading {1:0.0}, elevation {2:0.0})", Speed, Heading, Elevation);
        }
    }

    public partial class TrackType
    {
        public string Name { get { return string.Format(CultureInfo.InvariantCulture, "Track {0}", TrackNumber); } }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Name, Position);
        }
    }

    public partial class PointOfOriginType
    {
        public string Name { get { return string.Format(CultureInfo.InvariantCulture, "Point of Origin {0}", TrackNumber); } }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Name, Position);
        }
    }

    public partial class PointOfImpactType
    {
        public string Name { get { return string.Format(CultureInfo.InvariantCulture, "Point of Impact {0}", TrackNumber); } }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Name, Position);
        }
    }

    public partial class FireType
    {
        public string Name { get { return string.Format(CultureInfo.InvariantCulture, "Fire rate/quantity/fuse/warhead {0}/{1}/{2}/{3}", Rate, Quantity, Fuse, Warhead); } }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Name, Position);
        }
    }

    public partial class DetonationType
    {
        public string Name { get { return string.Format(CultureInfo.InvariantCulture, "Detonation rate/quantity/fuse/warhead {0}/{1}/{2}/{3}", Rate, Quantity, Fuse, Warhead); } }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Name, Position);
        }
    }

}