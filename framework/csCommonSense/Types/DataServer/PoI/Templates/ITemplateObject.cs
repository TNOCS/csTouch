using csCommon.Types.DataServer.Interfaces;

namespace csCommon.Types.DataServer.PoI.Templates
{
    public interface ITemplateObject : IConvertibleXml, IConvertibleGeoJson
    {
        /// <summary>
        /// Get a unique identifier for this object; overrides will only happen with objects that have the same identifier.
        /// </summary>
        /// <returns></returns>
        string Id { get; }
    }
}
