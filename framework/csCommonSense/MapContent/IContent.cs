using System.Collections.ObjectModel;
using System.Xml.Linq;
using DataServer;

namespace csShared.Geo.Content
{
  public interface IContent // TODO Clean up: there are at least two interfaces called IContent.
  {
    void Init();
    void Add();
    void Remove();
    void Configure();

      bool IsOnline { get; }

    /// <summary>
    /// only one instance
    /// </summary>
    bool IsRunning { get; set; }
    string Name { get; set; }
    string ImageUrl { get; set; }
    string Folder { get; set; }
  }

  public class ContentCollection : ObservableCollection<IContent>
  {
  
  }

  


  

}
