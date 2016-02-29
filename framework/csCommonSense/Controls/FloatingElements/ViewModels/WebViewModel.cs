using Caliburn.Micro;
using csShared.Documents;

namespace csShared
{
  using System.ComponentModel.Composition;

  [Export(typeof(IDocument))]
  public class WebViewModel : Screen, IDocument
  {



    private Document _doc;

    [ImportingConstructor]
    public WebViewModel()
    {


    }
    protected override void OnViewLoaded(object view)
    {
      base.OnViewLoaded(view);
      var v = view as WebView;
      if (v != null)
      {
        //v.iWebPage.LoadCompleted += new System.EventHandler(WebContent_LoadCompleted);
        //if (!string.IsNullOrEmpty(_doc.Location))
        //  v.iWebPage.LoadURL(_doc.Location);
      }
      v.Unloaded += v_Unloaded;
    }

    void v_Unloaded(object sender, System.Windows.RoutedEventArgs e)
    {
      //throw new System.NotImplementedException();
    }

    public Document Doc
    {
      get { return _doc; }
      set { _doc = value; NotifyOfPropertyChange(() => Doc); }
    }

     void WebContent_LoadCompleted(object sender, System.EventArgs e)
    {
      
    }
  }
}
