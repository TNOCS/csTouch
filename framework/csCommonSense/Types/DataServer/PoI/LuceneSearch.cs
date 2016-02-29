using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using csShared.Utils;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace DataServer
{
    public class LuceneSearch : ISearchEngine
    {
        public Analyzer analyzer;
        private RAMDirectory directory;
        public PoiService Service { get; set; }
        public string[] Fields { get; set; }

        public System.Threading.Tasks.Task<bool> Init()
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                CreateSearchIndex();
                return true;
            });
            
        }

        private Dictionary<string, BaseContent> LookupTable; 

        public Task<SearchResultCollection> Search(string search)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                var src = new SearchResultCollection();
                if (string.IsNullOrWhiteSpace(search)) return src;
                try
                {

                                                       
                    var parser = new QueryParser(Version.LUCENE_30,"All", analyzer);
                    Query q = new TermQuery(new Term("All", search));
                                                           
                    using (var indexSearcher = new IndexSearcher(directory, true))
                    {
                        Query query = parser.Parse(search);
                        TopDocs result = indexSearcher.Search(query, 50);
                        foreach (ScoreDoc h in result.ScoreDocs)
                        {
                            Document doc = indexSearcher.Doc(h.Doc);
                            string id = doc.Get("id");
                            BaseContent value;
                            if (LookupTable.TryGetValue(id, out value)) src.Add(new SearchResult {Relevance = h.Score, Content = value});
                        }
                    }
                }
                catch (Exception e)
                {

                    Logger.Log("DataServer","Error lucene search",e.Message,Logger.Level.Error);
                }
                return src;
            });
        }

        public void CreateSearchIndex()
        {
            directory = new RAMDirectory();
            analyzer = new StandardAnalyzer(Version.LUCENE_30);
            var ixw = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            LookupTable = new Dictionary<string, BaseContent>();
            foreach (BaseContent p in Service.PoIs.ToList())
            {
                var document = new Document();
                document.Add(new Field("id", p.Id.ToString(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO));
                string all = p.Name + " ";
                foreach (MetaInfo mi in p.EffectiveMetaInfo)
                {
                    string value;
                    if (mi.Type != MetaTypes.text || !p.Labels.TryGetValue(mi.Label, out value)) continue;
                    document.Add(new Field(mi.Label, value, Field.Store.YES, Field.Index.ANALYZED));
                    all += value + " ";
                }
                document.Add(new Field("All", all, Field.Store.YES, Field.Index.ANALYZED));

                LookupTable[p.Id.ToString()] = p;
                ixw.AddDocument(document);
            }
            ixw.Commit();
        }
    }
}