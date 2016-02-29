using System.Windows;
using BaseWPFHelpers;
using csShared.Documents;
using Microsoft.Surface.Presentation.Controls;

namespace csShared
{
  public partial class WebView
  {

    System.Windows.Style s = null;



    public WebView()
    {
      InitializeComponent();
      Loaded += ImageViewLoaded;


      foreach (var a in Application.Current.Resources.MergedDictionaries)
      {
        var b = a["SimpleFloatingStyle"];
        if (b != null) s = (Style)b;
      }



    }

    void ImageViewLoaded(object sender, System.Windows.RoutedEventArgs e)
    {

      ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
      FloatingElement fe = (FloatingElement)_svi.DataContext;
      if (_svi!=null)
      {
        _svi.SizeChanged += _svi_SizeChanged;
      }
      if (s != null)
      {
        fe.Style = s;
      }

      fe.ShowShadow = true;
      fe.Width = 400;
      fe.Height = 300;
      //_svi.Width = 300;
      //_svi.Height = 225;
      if (fe.Document != null && fe.Document.FileType == FileTypes.web)
      {
        //iWebPage.LoadURL(((Document)fe.Document).Location);
      }

      //fe.MinSize = new Size(bi.Width, bi.Height);


    }

    void _svi_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
      if (_svi != null)
      {
        _svi.Height = e.NewSize.Height;
        // _svi.Width = e.NewSize.Height * ((double)iWebPage.Width / (double)iWebPage.Height);
      }
    }



    void MainSourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
    {

    }
  }
}
