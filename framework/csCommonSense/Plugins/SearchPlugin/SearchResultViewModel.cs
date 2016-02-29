#region

using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared;
using DataServer;

#endregion

namespace csCommon.MapPlugins.Search
{ 
    public interface ISearchResult 
    {
        string SearchKey { get; set; }
    }

    [Export(typeof (IScreen))]
    public class SearchResultViewModel : Screen
    {
        private ISearchApi searchApi;

        public ISearchApi SearchApi
        {
            get { return searchApi; }
            set { searchApi = value; NotifyOfPropertyChange(()=>SearchApi); }
        }

        public ContentList Result { get { return SearchApi.Plugin!=null ? SearchApi.Plugin.SearchService.PoIs : null; } }

        public SearchResultViewModel()
        {
            
        }

        public SearchResultViewModel(ISearchApi api)
        {
            SearchApi = api;
           
        }
    }
}