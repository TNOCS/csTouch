using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Hunspell;

namespace csCommon.Types.TextAnalysis
{
    /// <summary>
    /// Entry point for Hunspell functionalities. Scans the project resources for Hunspell dictionaries
    /// (which should be located anywhere, in an Embedded Resource with a path containing the word 'Hunspell').
    /// </summary>
    public class Hunspell
    {
        private static Hunspell _instance;

        /// <summary>
        /// Get the singleton Hunspell instance.
        /// </summary>
        public static Hunspell Instance
        {
            get { return _instance ?? (_instance = new Hunspell()); }
        }

        /// <summary>
        /// List the supported language codes.
        /// </summary>
        public IEnumerable<string> SupportedLanguages
        {
            get { return _dictionaries.Keys; }
        }

        private readonly Dictionary<string, HunspellDictionary> _dictionaries = new Dictionary<string, HunspellDictionary>(); 

        /// <summary>
        /// Get the Hunspell dictionary for the provided language code.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns></returns>
        public HunspellDictionary GetDictionary(string language)
        {
            HunspellDictionary dict;
            return _dictionaries.TryGetValue(language, out dict) ? dict : null;
        }

        private Hunspell()
        {
            // TODO Lazily initializing the dictionaries would be better, but then we might list languages we do not support (as we don't load them immediately).
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] manifestResourceNames = assembly.GetManifestResourceNames();
            foreach (var manifestResourceName in manifestResourceNames.Where(manifestResourceName => manifestResourceName.Contains("Hunspell")))
            {
                try
                {
                    string[] split = manifestResourceName.Split('.');
                    string language = split[split.Length - 2];
                    if (_dictionaries.ContainsKey(language)) continue;
                    string resourceName = Path.GetFileNameWithoutExtension(manifestResourceName);
                    Stream affixStream = assembly.GetManifestResourceStream(resourceName + ".aff");
                    Stream dictionaryStream = assembly.GetManifestResourceStream(resourceName + ".dic");
                    HunspellDictionary hunspellDictionary = new HunspellDictionary(affixStream, dictionaryStream);
                    _dictionaries[language] = hunspellDictionary;
                }
                catch
                {
                    // Ignore the exception.
                }
            }
        }

    }
}
