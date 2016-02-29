namespace CsvToDataService.Model
{
    public enum ProcessingActions
    {
        AddressCity,
        AddressComplete,
        AddressCountry,
        AddressNumber,
        AddressStreetAndNumber,
        AddressStreet,
        AddressZipCode,
        AddressMunicipality,
        Description,

        /// <summary>
        ///     Ignore this field
        /// </summary>
        Ignore,

        /// <summary>
        ///     Add the field to the collection of labels
        /// </summary>
        Label,
        Latitude,
        LatitudeDegrees,
        Longitude,
        LongitudeDegrees,

        /// <summary>
        ///     Set the name of the PoI to this value.
        /// </summary>
        Name,

        /// <summary>
        ///     Used to generate a new type: for each value, a new type is created.
        /// </summary>
        TypeName,

        /// <summary>
        ///     Used to add a presenter path (folder name).
        /// </summary>
        Folder,

        /// <summary>
        ///     X coordinate in RD
        /// </summary>
        X_RD,

        /// <summary>
        ///     Y coordinate in RD
        /// </summary>
        Y_RD,
        /// <summary>
        /// Shape as WKT
        /// </summary>
        WKT
    }
}
