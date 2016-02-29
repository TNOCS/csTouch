namespace csCommon.Types.DataServer.Interfaces
{
    public interface IConvertibleCsv
    {
        /// <summary>
        /// Convert the object to a CSV representation, without headers.
        /// </summary>
        /// <returns>The well-known-text string.</returns>
        string ToCsv(char separator = ',');

        /// <summary>
        /// Read the CSV string and set the appropriate attributes of this object (or a new object).
        /// </summary>
        /// <param name="s">The CSV string.</param>
        /// <param name="separator">The separator used (e.g. comma or semicolon).</param>
        /// <param name="newObject">Whether to change the object at hand, or to return a new one.</param>
        /// <returns></returns>
        IConvertibleCsv FromCsv(string s, char separator = ',', bool newObject = true);
    }
}