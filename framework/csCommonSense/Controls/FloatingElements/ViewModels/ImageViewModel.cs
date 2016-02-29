using Caliburn.Micro;
using csShared.Documents;

namespace csShared
{
  using System.ComponentModel.Composition;

  [Export(typeof(IDocument))]
  public class ImageViewModel : Screen, IDocument
  {
    private Document _doc;

    [ImportingConstructor]
    public ImageViewModel()
    {
      
      
    }

    public Document Doc
    {
      get { return _doc; }
      set { _doc = value; NotifyOfPropertyChange(()=>Doc); }
    }
  }
}
