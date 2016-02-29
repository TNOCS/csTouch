using System.Collections.Generic;

namespace csCommon.Types.TextAnalysis.TextFinder
{
    /// <summary>
    /// A collection of searchable objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITextSearchableCollection<out T> : IEnumerable<T> where T : ITextSearchable
    {
        /// <summary>
        /// Return a unique Id for this collection (e.g. hash code), which is used by some text finders to construct caches.
        /// </summary>
        long IndexId { get; }
    }
}
