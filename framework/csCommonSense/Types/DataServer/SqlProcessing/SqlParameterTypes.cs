namespace DataServer.SqlProcessing
{
    public enum SqlParameterTypes
    {
        /// <summary>
        /// Parameter is the region (e.g. from a zone) in WGS84.
        /// </summary>
        RegionWGS84,
        /// <summary>
        /// Parameter is the region (e.g. from a zone) in RD (Rijksdriehoeks coordinaten).
        /// </summary>
        RegionRD,
        /// <summary>
        /// Parameter is based on a label.
        /// Can only be used on PoI.
        /// </summary>
        Label,
        /// <summary>
        /// Parameter is based on a sensor.
        /// Can only be used on PoI.
        /// </summary>
        Sensor,
        /// <summary>
        /// Parameter is the extent in WGS84.
        /// </summary>
        ExtentWGS84,
        /// <summary>
        /// Parameter is the extent in RD (Rijksdriehoeks coordinaten).
        /// </summary>
        ExtentRD,
        /// <summary>
        /// Parameter is the radius in [m].
        /// </summary>
        RadiusInMeter,
        ///// <summary>
        ///// Current location in WGS84.
        ///// </summary>
        //CurrentLocationWGS84,
        ///// <summary>
        ///// Current location in RD (Rijksdriehoeks coordinaten).
        ///// </summary>
        //CurrentLocationRD
        PointWGS84,
        PointRD,
        /// <summary>
        /// Parameter is the WKT( Well Known Text) of the geometry.
        /// </summary>
        ShapeWKT,
        /// <summary>
        /// Parameter is the EWKT(PostGIS Well Known Text) of the geometry, which is basically WKT, but it starts with the SRID=4326;WKT....
        /// </summary>
        ShapeEWKT
    }
}