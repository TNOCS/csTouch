using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using csCommon.Types.DataServer.Interfaces;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace csCommon.Types.TextAnalysis.TextFinder
{
    /// <summary>
    /// A text finder based on the C# implementation of Lucene. 
    /// </summary>
    /// <typeparam name="TS">The type of object that we search in.</typeparam>
    public class LuceneTextFinder<TS> : ITextFinder<TS> where TS : ITextSearchable
    {
        private const string DocIdFieldName = "ID_FIELD";
        private const string ContentFieldName = "CONTENT_FIELD";

        private bool _prefixesOnly; // Not supported at the moment.

        private Directory _indexDir;

        private readonly Dictionary<ulong, string> _keywordIndex = new Dictionary<ulong, string>();
        private readonly Dictionary<ulong, TS> _reverseIndex = new Dictionary<ulong, TS>();
        private IndexSearcher _indexSearcher;

        /// <summary>
        /// Initialize the text finder.
        /// </summary>
        /// <param name="collection">Content to search in.</param>
        /// <param name="keywordsOnly">Whether to search based on all text (except keywords), or on keywords only. Default is to search based on all text.</param>
        /// <param name="prefixesOnly">Whether to search prefixes only (generally faster), or also within words. 
        ///                            This text finder only supports prefix searching at the moment.</param>
        public void Initialize(ITextSearchableCollection<TS> collection, bool keywordsOnly = false, bool prefixesOnly = true)
        {
            _prefixesOnly = prefixesOnly;

            _keywordIndex.Clear();
            _reverseIndex.Clear();

            ulong id = 0;
            foreach (TS element in collection)
            {
                _keywordIndex[id] = keywordsOnly ? element.Keywords.ExpandHistogram() : element.FullText; 
                _reverseIndex[id] = element;
                id++;
            }

            long indexId = collection.IndexId + (keywordsOnly ? 0 : 1);
            string directory = Path.Combine(Path.GetTempPath(), indexId + "");

            // TODO Persistence. Now we simply delete what is already there.
            if (System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.Delete(directory, true); // Delete all of it.
            }
            
            // Create the new index.
            _indexDir = new SimpleFSDirectory(new DirectoryInfo(directory));
            Index(_keywordIndex);
        }

        /// <summary>
        /// Find a certain term.
        /// </summary>
        /// <param name="searchTerm">The term.</param>
        /// <returns>A list of results found in the content.</returns>
        public IEnumerable<TextFinderResult<TS>> Find(string searchTerm)
        {
            searchTerm = searchTerm + "*"; // Otherwise we always match whole words.
//            if (!_prefixesOnly)
//            {
//                searchTerm = "*" + searchTerm;
//            } // TODO infix search is not supported in this way in Lucene.
            ulong[] ids;
            string[] results;
            float[] scores;
            Search(searchTerm, out ids, out results, out scores); // TODO Somehow all scores are 1.
            List<TextFinderResult<TS>> resultList = new List<TextFinderResult<TS>>(ids.Length);
            resultList.AddRange(ids.Select((t, i) => new TextFinderResult<TS>(_reverseIndex[t], scores[i])));
            return resultList;
        }

        /// <summary>
        /// This method indexes the content that is sent across to it. Each piece of content (or "document")
        /// that is indexed has to have a unique identifier (so that the caller can take action based on the
        /// document id). Therefore, this method accepts key-value pairs in the form of a dictionary. The key
        /// is a ulong which uniquely identifies the string to be indexed. The string itself is the value
        /// within the dictionary for that key. Be aware that stop words (like the, this, at, etc.) are _not_
        /// indexed.
        /// </summary>
        /// <param name="txtIdPairToBeIndexed">A dictionary of key-value pairs that are sent by the caller
        /// to uniquely identify each string that is to be indexed.</param>
        /// <returns>The number of documents indexed.</returns>
        private int Index(Dictionary<ulong, string> txtIdPairToBeIndexed)
        {
            IndexWriter indexWriter = new IndexWriter(_indexDir, new StandardAnalyzer(Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED);
            //indexWriter.SetUseCompoundFile(false);

            Dictionary<ulong, string>.KeyCollection keys = txtIdPairToBeIndexed.Keys;
            foreach (ulong id in keys)
            {
                string text = txtIdPairToBeIndexed[id];
                Document document = new Document();
                Field bodyField = new Field(ContentFieldName, text, Field.Store.YES, Field.Index.ANALYZED);
                document.Add(bodyField);
                Field idField = new Field(DocIdFieldName, (id).ToString(CultureInfo.CurrentUICulture), Field.Store.YES, Field.Index.ANALYZED);
                document.Add(idField);
                indexWriter.AddDocument(document);
            }

            int numIndexed = indexWriter.NumDocs();
            indexWriter.Optimize();
            indexWriter.Dispose();

            _indexSearcher = new IndexSearcher(_indexDir);

            return numIndexed;
        }

        /// <summary>
        /// This method searches for the search term passed by the caller.
        /// </summary>
        /// <param name="searchTerm">The search term as a string that the caller wants to search for within the
        /// index as referenced by this object.</param>
        /// <param name="ids">An out parameter that is populated by this method for the caller with docments ids.</param>
        /// <param name="results">An out parameter that is populated by this method for the caller with docments text.</param>
        /// <param name="scores">An out parameter that is populated by this method for the caller with docments scores.</param>
        private void Search(string searchTerm, out ulong[] ids, out string[] results, out float[] scores)
        {
            try
            {
                QueryParser queryParser = new QueryParser(Version.LUCENE_30, ContentFieldName, new StandardAnalyzer(Version.LUCENE_30));
                Query query = queryParser.Parse(searchTerm);

                TopDocs topDocs = _indexSearcher.Search(query, int.MaxValue);
                ScoreDoc[] scoreDocsArray = topDocs.ScoreDocs;

                int numHits = topDocs.TotalHits;
                ids = new ulong[numHits];
                results = new string[numHits];
                scores = new float[numHits];
                int idx = 0;
                foreach (ScoreDoc scoreDoc in scoreDocsArray) 
                {

                    Document doc = _indexSearcher.Doc(scoreDoc.Doc);
                    String idAsText = doc.Get(DocIdFieldName);
                    ids[idx] = UInt64.Parse(idAsText);
                    scores[idx] = scoreDoc.Score;
                    results[idx] = doc.Get(ContentFieldName);
                    idx++;
                }
            }
            finally
            {
                _indexSearcher.Dispose();
            }
        }
    }
}


