using System.Collections.Generic;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Types.TextAnalysis.TextFinder.Trie;

namespace csCommon.Types.TextAnalysis.TextFinder
{
    /// <summary>
    /// A fast (but rather memory intensive) text finder based on a trie data structure.
    /// </summary>
    /// <typeparam name="TS"></typeparam>
    public class TrieTextFinder<TS> : ITextFinder<TS> where TS : ITextSearchable
    {
        private ITrie<TextFinderResult<TS>> _dataTrie;

        /// <summary>
        /// Initialize the text finder.
        /// </summary>
        /// <param name="collection">Content to search in.</param>
        /// <param name="keywordsOnly">Whether to search based on all text (except keywords), or on keywords only. Default is to search based on all text.</param>
        /// <param name="prefixesOnly">Whether to search prefixes only (faster), or also within words.</param>
        public void Initialize(ITextSearchableCollection<TS> collection, bool keywordsOnly = false, bool prefixesOnly = true)
        {
            _dataTrie = new InfixTrie<TextFinderResult<TS>>(!prefixesOnly);

            foreach (TS ts in collection)
            {
                WordHistogram key = keywordsOnly ? ts.Keywords : ts.FullText.Histogram();
                foreach (string distinctWord in key.DistinctWords)
                {
                    _dataTrie.Add(distinctWord, new TextFinderResult<TS>(ts, key.GetFrequency(distinctWord)));
                }
            }
        }

        /// <summary>
        /// Find a certain term.
        /// </summary>
        /// <param name="searchTerm">The term.</param>
        /// <returns>A list of results found in the content.</returns>
        public IEnumerable<TextFinderResult<TS>> Find(string searchTerm)
        {
            IEnumerable<TextFinderResult<TS>> textFinderResults = _dataTrie.Retrieve(searchTerm);

            // There may be duplicates. Remove them.
            Dictionary<long, TextFinderResult<TS>> resultSet = new Dictionary<long, TextFinderResult<TS>>();
            foreach (TextFinderResult<TS> textFinderResult in textFinderResults)
            {
                long indexId = textFinderResult.Data.IndexId;
                TextFinderResult<TS> result;
                if (resultSet.TryGetValue(indexId, out result))
                {
                    resultSet[indexId].Score += textFinderResult.Score;
                }
                else
                {
                    resultSet[indexId] = textFinderResult;
                }
            }

            // And return.
            return resultSet.Values;
        }
    }
}