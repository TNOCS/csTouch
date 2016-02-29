using Caliburn.Micro;
using csShared;
using csShared.Documents;

namespace csCommon
{
  using System.ComponentModel.Composition;

  [Export(typeof(IDocument))]
  public class XpsViewModel : Screen, IDocument
  {
    private Document _doc;

    [ImportingConstructor]
    public XpsViewModel()
    {
      
      
    }

    public Document Doc
    {
      get { return _doc; }
      set { _doc = value; NotifyOfPropertyChange(()=>Doc); }
    }
  }
}
