using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Version = Lucene.Net.Util.Version;

namespace csCommon.Types.TextAnalysis
{
    public static class WordExtractorExtensions
    {
        public static IEnumerable<string> Words(this string text)
        {
            List<string> words = new List<string>();

            StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);
            TokenStream stream = analyzer.TokenStream(null, new StringReader(text));

            var termAttr = stream.GetAttribute<ITermAttribute>();
            while (stream.IncrementToken())
            {
                string term = termAttr.Term;
                string[] termWords = term.Split(new char[] {'.', ',', ';', ':', '\'', '\"'}); // TODO This is not a complete filter.
                words.AddRange(termWords);
            }
            stream.End();
            stream.Dispose();

            return words;
        }
    }
}
