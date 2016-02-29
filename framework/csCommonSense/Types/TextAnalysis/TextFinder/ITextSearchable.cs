namespace csCommon.Types.TextAnalysis.TextFinder
{
    /// <summary>
    /// Interface for objects that have searchable text.
    /// </summary>
    public interface ITextSearchable
    {
        /// <summary>
        /// Return a set of keywords, rather than the full text. May return null.
        /// </summary>
        WordHistogram Keywords { get; }

        /// <summary>
        /// Return the full text, *not* including the keywords (if any). May return null.
        /// </summary>
        string FullText { get; }

        /// <summary>
        /// Return a unique Id for this object (e.g. hash code), which is used by some text finders to construct caches.
        /// </summary>
        long IndexId { get; }
    }
}
