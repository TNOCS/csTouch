using System.Collections.Generic;
using csCommon.Types.DataServer.Interfaces;

namespace csCommon.Types.TextAnalysis.TextFinder
{
    /// <summary>
    /// Text finder interface.
    /// </summary>
    /// <typeparam name="TS"></typeparam>
    public interface ITextFinder<TS> where TS : ITextSearchable
    {
        /// <summary>
        /// Initialize the text finder.
        /// </summary>
        /// <param name="collection">Content to search in.</param>
        /// <param name="keywordsOnly">Whether to search based on all text (except keywords), or on keywords only. Default is to search based on all text.</param>
        /// <param name="prefixesOnly">Whether to search prefixes only (generally faster), or also within words.</param>
        void Initialize(ITextSearchableCollection<TS> collection, bool keywordsOnly = false, bool prefixesOnly = true);

        /// <summary>
        /// Find a certain term.
        /// </summary>
        /// <param name="searchTerm">The term.</param>
        /// <returns>A list of results found in the content.</returns>
        IEnumerable<TextFinderResult<TS>> Find(string searchTerm);
    }
}