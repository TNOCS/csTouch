using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using csCommon.Types.DataServer.Interfaces;
using DataServer;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Hunspell;
using Lucene.Net.Analysis.Nl;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace csCommon.Types.TextAnalysis
{
    public static class WordHistogramExtensions
    {
        public static WordHistogram Histogram(this string text)
        {
            WordHistogram wordHistogram = new WordHistogram(text.Language(), text); // Slow approximate language detector.
            return wordHistogram;
        }

        public static string DistinctWordsInHistogram(this WordHistogram histogram)
        {
            return String.Join(" ", histogram.DistinctWords);
        }

        public static string ExpandHistogram(this WordHistogram histogram)
        {
            StringBuilder ret = new StringBuilder();
            foreach (string word in histogram.DistinctWords)
            {
                for (int i = 0; i < histogram.GetFrequency(word); i++)
                {
                    ret.Append(word).Append(" ");
                }
            }
            return ret.ToString();
        }

        public static string DistinctWordsInText(this string text, string language = null)
        {
            if (language == null) language = text.Language(); // Slow approximate language detector.
            IEnumerable<string> distinctWords = new WordHistogram(language, text).DistinctWords; 
            return String.Join(" ", distinctWords);
        }
    }

    public class WordHistogram : IConvertibleGeoJson, IConvertibleXml
    {
        private string _language;
        private readonly Dictionary<string, int> _frequencies = new Dictionary<string, int>();

        public WordHistogram(string language = null, string text = null)
        {
            _language = language;
            if (text == null) return;
            Text = text;
        }

        public string Text
        {
            set
            {
                if (value == null) return;
                DoWordCount(value, false);
            }
        }

        public WordHistogram Clone()
        {
            WordHistogram clone = new WordHistogram(_language);
            foreach (KeyValuePair<string, int> kv in _frequencies)
            {
                clone._frequencies[kv.Key] = kv.Value;
            }
            return clone;
        }

        public void Merge(string otherWordFrequencies)
        {
            WordHistogram parsed = WordHistogram.FromGeoJson(otherWordFrequencies);
            Merge(parsed);
        }

        public void Merge(WordHistogram other)
        {
            foreach (KeyValuePair<string, int> kv in other._frequencies)
            {
                int currentFrequency;
                if (_frequencies.TryGetValue(kv.Key, out currentFrequency))
                {
                    _frequencies[kv.Key] = currentFrequency + other._frequencies[kv.Key];
                }
                else
                {
                    _frequencies[kv.Key] = other._frequencies[kv.Key];
                }
            }
        }

        public IEnumerable<string> DistinctWords
        {
            get { return _frequencies.Keys; }
        }

        public int GetFrequency(string word)
        {
            int value;
            return _frequencies.TryGetValue(word, out value) ? value : 0;
        }

//        public string Stem(string word)  // Stems are not really words.
//        {
//            HunspellDictionary dictionary = Hunspell.Instance.GetDictionary(_language);
//            HunspellStemmer hunspellStemmer = new HunspellStemmer(dictionary);
//            IEnumerable<HunspellStem> hunspellStems = hunspellStemmer.Stem(word);
//            IEnumerable<HunspellStem> enumerable = hunspellStems as HunspellStem[] ?? hunspellStems.ToArray();
//            word = enumerable.Any() ? enumerable.First().Stem : word;
//            return word;
//        }

        public void Append(string moreText)
        {
            DoWordCount(moreText, true);
        }

        private void DoWordCount(string text, bool append)
        {
            if (! append)
            {
                _frequencies.Clear();
            }

            if (_language == null)
            {
                _language = text.Language(); // Detected. May be null.
            }

            var stopwords = Stopwords.GetStopwords(_language); // May be empty list.

            IEnumerable<string> words = text.Words();
            foreach (string word in words.Where(word => !stopwords.Contains(word)))
            {
                int value;
                if (_frequencies.TryGetValue(word, out value))
                {
                    _frequencies[word] = value + 1;
                }
                else
                {
                    _frequencies[word] = 1;
                }
            }
        }

        public string ToGeoJson()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            if (_language != null) builder.Append("\"language\":\"").Append(_language).Append("\",");
            builder.Append("\"histogram\":{");
            foreach (KeyValuePair<string, int> kv in _frequencies)
            {
                builder.Append("\"").Append(kv.Key).Append("\":").Append(kv.Value).Append(", ");
            }
            if (_frequencies.Any())
            {
                builder.Remove(builder.Length - 2, 2); // Remove trailing comma
            }
            builder.Append("}}");
            return builder.ToString();
        }

        public IConvertibleGeoJson FromGeoJson(string geoJson, bool newObject = true)
        {
            WordHistogram ret = newObject ? new WordHistogram(null) : this;

            JObject jObject = JObject.Parse(geoJson);
            JToken token;
            if (jObject.TryGetValue("language", out token))
            {
                _language = token.ToString();                
            }
            _frequencies.Clear();
            foreach (var prp in jObject["histogram"].OfType<JProperty>())
            {
                int frequency;
                if (int.TryParse(prp.Value.ToString(), out frequency))
                {
                    _frequencies[prp.Name] = frequency;
                }
            }
            return ret;
        }

        public IConvertibleGeoJson FromGeoJson(JObject geoJsonObject, bool newObject = true)
        {
            return FromGeoJson(geoJsonObject.ToString(), newObject);
        }

        public static WordHistogram FromGeoJson(string geoJson)
        {
            return (new WordHistogram(null).FromGeoJson(geoJson, false) as WordHistogram);
        }

        public string XmlNodeId
        {
            get { return "WordHistogram"; }
        }

        public XElement ToXml()
        {
            var res = new XElement(XmlNodeId);
            res.SetAttributeValue(XName.Get("Language"), _language);
            foreach (KeyValuePair<string, int> kv in _frequencies)
            {
                var child = new XElement("Histogram");
                child.SetAttributeValue(XName.Get("Word"), kv.Key);
                child.SetAttributeValue(XName.Get("Count"), kv.Value);
                res.Add(child);
            }
            return res;
        }

        public void FromXml(XElement element)
        {
            _language = element.GetString("Language");
            _frequencies.Clear();
            XElement xhistogram = element.Element("Histogram");
            foreach (XElement xhelement in xhistogram.Elements())
            {
                string key = xhelement.GetString("Word");
                int value = xhelement.GetInt("Count");
                _frequencies[key] = value;
            }
        }
    }
}
