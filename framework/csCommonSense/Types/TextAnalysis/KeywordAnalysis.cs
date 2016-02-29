using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csCommon.Types.TextAnalysis.TextFinder;

namespace csCommon.Types.TextAnalysis
{
    public class KeywordAnalysis
    {
        public static IEnumerable<string> InterestingKeywords<T>(ITextSearchableCollection<T> collection) where T: ITextSearchable
        {
            int count = collection.Count();
            double lowerBound = 0.1 * count;
            double upperBound = 0.8 * count;

            Dictionary<string, int> frequencies = new Dictionary<string, int>();
            foreach (string word in collection.SelectMany(element => element.Keywords.DistinctWords))
            {
                int freq;
                if (frequencies.TryGetValue(word, out freq))
                {
                    frequencies[word] = freq + 1;
                }
                else
                {
                    frequencies[word] = 1;
                }
            }
            List<string> interestingKeywords = new List<string>(frequencies.Keys);
            foreach (KeyValuePair<string, int> kv in frequencies)
            {
                if (kv.Value < lowerBound)
                {
                    interestingKeywords.Remove(kv.Key);
                }
                else if (kv.Value > upperBound)
                {
                    interestingKeywords.Remove(kv.Key);
                }
            }
            return interestingKeywords;
        }
    }
}
