namespace csModels.FieldOfViewModel
{
    /// <summary>
    /// When you call the field of view service, do you wish to obtain an image or a polygon.
    /// </summary>
    public enum FieldOfViewOperatingMode
    {
        /// <summary>
        /// Return the results as image.
        /// </summary>
        Image,
        /// <summary>
        /// Return the results as polygon.
        /// </summary>
        Polygon,
        /// <summary>
        /// Not set.
        /// </summary>
        Unknown
    }
}