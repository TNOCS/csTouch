using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using csShared.Utils;

namespace DataServer
{
    public class DefaultSearch : ISearchEngine
    {

        public PoiService Service
        {
            get; set;
        }

        public Task<bool> Init()
        {
            return System.Threading.Tasks.Task.Run(() => true);
        }

        public Task<SearchResultCollection> Search(string search)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                var src = new SearchResultCollection();
                if (string.IsNullOrWhiteSpace(search)) return src;
                try
                {
                    var s = search.ToLower();
                    foreach (var p in Service.PoIs)
                    {
                        if (p.Labels.Values.Any(k => k.ToLower().Contains(s)))
                            src.Add(new SearchResult { Relevance = 1, Content = p });                        
                    }
                    
                    
                }
                catch (Exception e)
                {

                    Logger.Log("DataServer", "Error default search", e.Message, Logger.Level.Error);
                }
                return src;
            });
        }
    }

    public class SearchResultCollection : List<SearchResult>
    {
    }
}