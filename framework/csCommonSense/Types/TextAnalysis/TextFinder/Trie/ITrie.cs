using System.Collections.Generic;

namespace csCommon.Types.TextAnalysis.TextFinder.Trie
{
    /// <summary>
    /// Interface to be implemented by a data structure 
    /// which allows adding values <see cref="TValue"/> associated with <b>string</b> keys.
    /// The interface allows retrieveal of multiple values.
    /// </summary>
    /// <typeparam name="TValue">The type of object to store.</typeparam>
    public interface ITrie<TValue>
    {
        IEnumerable<TValue> Retrieve(string query);
        void Add(string key, TValue value);
    }
}