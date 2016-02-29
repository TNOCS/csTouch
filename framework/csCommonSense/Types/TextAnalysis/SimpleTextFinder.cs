using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using csCommon.Types.TextAnalysis.TextFinder;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;

namespace csCommon.Types.TextAnalysis
{
    public class SimpleTextFinder<T> : ITextFinder<T> where T : ITextSearchable
    {
        private ITextSearchableCollection<T> _collection;
        private bool _keywordsOnly;
        private bool _prefixesOnly;

        public void Initialize(ITextSearchableCollection<T> collection, bool keywordsOnly = false, bool prefixesOnly = true)
        {
            _collection = collection;
            _keywordsOnly = keywordsOnly;
            _prefixesOnly = prefixesOnly;
        }

        public IEnumerable<TextFinderResult<T>> Find(string searchTerm)
        {
            List<TextFinderResult<T>> results = new List<TextFinderResult<T>>();
            foreach (T searchableText in _collection)
            {
                if (_keywordsOnly)
                {
                    WordHistogram keywords = searchableText.Keywords;
                    if (_prefixesOnly)
                    {
                        int count = keywords.DistinctWords.
                            Where(word => word.StartsWith(searchTerm)).
                            Sum(word => keywords.GetFrequency(word));
                        if (count > 0)
                        {
                            results.Add(new TextFinderResult<T>(searchableText, count));
                        }
                    }
                    else
                    {
                        int count = keywords.DistinctWords.
                            Where(word => word.Contains(searchTerm)).
                            Sum(word => keywords.GetFrequency(word));
                        if (count > 0)
                        {
                            results.Add(new TextFinderResult<T>(searchableText, count));
                        }
                    }
                }
                else
                {            
                    string fullText = searchableText.FullText;
                    string matchString = searchTerm;
                    matchString = Regex.Escape(matchString);
                    IEnumerable<int> indices = from Match match in Regex.Matches(fullText, matchString) select match.Index;
                    if (_prefixesOnly) // Filter those matches that occur inside words.
                    {
                        List<int> indicesCopy = new List<int>(indices);
                        foreach (int index in indices)
                        {
                            if (index > 0 && char.IsLetterOrDigit(fullText[index - 1]))
                            {
                                indicesCopy.Remove(index); // This is not a prefix.
                            }
                        }
                        indices = indicesCopy;                        
                    }
                    int count = indices.Count();
                    if (count > 0)
                    {
                        results.Add(new TextFinderResult<T>(searchableText, count));
                    }
                }
            }
            return results;
        }
    }
}
