using System.Collections.Generic;
using System.Linq;
using SpellChecker = SpellChecker.Net.Search.Spell.SpellChecker;

namespace csCommon.Types.TextAnalysis
{
    // TODO We could use a library such as NTextCat for this, but that requires 75MB of data. 
    // Instead, we use a very crude estimator: the language that detects the most stop words in the string wins.
    public static class LanguageExtensions
    {
        /// <summary>
        /// Sloppily detect the language of the given text by determining how many stop words in each known language are present.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A language, if detected, or null if not.</returns>
        public static string Language(this string input)
        {
            IEnumerable<string> words = input.Words();
            IEnumerable<string> languagesKnown = Stopwords.GetLanguagesKnown();
            int maxCount = 0;
            string maxLang = "";
            foreach (string language in languagesKnown)
            {
                IEnumerable<string> stopwords = Stopwords.GetStopwords(language);
                int count = words.Count(word => stopwords.Contains(word));
                if (count <= maxCount) continue;
                maxCount = count;
                maxLang = language;
            }
            if (maxCount > 0)
            {
                return maxLang;
            }
            else
            {
                return null;
            }
        }

        // Really hard with Lucene :)
//        public static bool IsRecognized(this string word, string language)
//        {
//            SpellChecker spellChecker = new SpellChecker();
//        }
    }
}
