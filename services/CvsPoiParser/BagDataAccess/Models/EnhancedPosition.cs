namespace BagDataAccess.Models
{
    /// <summary>
    /// Besides the lat/lon in WGS84, also return the Woonplaats, Gemeente, en Provincie
    /// </summary>
    public class EnhancedPosition
    {
        public double Longitude { get; private set; }

        public double Latitude { get; private set; }

        public string Woonplaats { get; private set; }

        public string Gemeente { get; private set; }

        public string Provincie { get; private set; }
    }
}