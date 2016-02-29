namespace csCommon.Utils.IO
{
    /// <summary>
    /// Super-interface for IExporter and IImporter.
    /// </summary>
    public interface IIO
    {
        /// <summary>
        /// Get the data format in a readable text, for example "Comma Separated Values".
        /// </summary>
        string DataFormat { get; }

        /// <summary>
        /// Get the data format file extension, for example "csv". Note: no dot, lowercase (if possible).
        /// </summary>
        string DataFormatExtension { get; }
    }
}