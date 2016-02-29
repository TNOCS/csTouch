using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using csCommon.Utils.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.De;
using Lucene.Net.Analysis.Fr;
using Lucene.Net.Analysis.Nl;

namespace csCommon.Types.TextAnalysis
{
    public static class Stopwords
    {
        private static Dictionary<string, IEnumerable<string>> stopwords;

        // TODO Might want to do this in a smarter way.
        static Stopwords()
        {
            stopwords = new Dictionary<string, IEnumerable<string>>();
            stopwords["nl_NL"] = DutchAnalyzer.DUTCH_STOP_WORDS;
            stopwords["en_GB"] = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
            stopwords["en_US"] = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
            stopwords["de_DE"] = GermanAnalyzer.GetDefaultStopSet();
            stopwords["fr_FR"] = FrenchAnalyzer.GetDefaultStopSet();
        }

        public static IEnumerable<string> GetLanguagesKnown()
        {
            return stopwords.Keys;
        }

        public static IEnumerable<string> GetStopwords(string language)
        {
            IEnumerable<string> sws;
            if (stopwords != null && language != null && stopwords.TryGetValue(language, out sws))
            {
                return sws;
            }
            return new string[] {};
        }
    }
}
