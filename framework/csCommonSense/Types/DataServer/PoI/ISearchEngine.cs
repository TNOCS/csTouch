using System.Threading.Tasks;

namespace DataServer
{
    public interface ISearchEngine
    {
        PoiService Service { get; set; }
        System.Threading.Tasks.Task<bool> Init();
        Task<SearchResultCollection> Search(string search);
    }
}