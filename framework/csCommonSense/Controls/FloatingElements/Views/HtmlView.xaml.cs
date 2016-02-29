using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using csShared.Utils;

namespace csShared
{
  public partial class HtmlView
  {

    System.Windows.Style s = null;

    public HtmlView()
    {
      InitializeComponent();
      Loaded += ImageViewLoaded;
     

      
      foreach (var a in Application.Current.Resources.MergedDictionaries)
      {
        var b = a["SimpleFloatingStyle"];
        if (b != null) s = (Style)b;
      }

      

    }

    
    

    //private BitmapImage bi; // Not used

    void ImageViewLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
      try
      {
       
        if ((DataContext as HtmlViewModel) == null) return;

        var ivm = (HtmlViewModel)DataContext;

          if (File.Exists(ivm.Doc.OriginalUrl))
          {
              HtmlFormattingRichTextBox.Text = File.ReadAllText(ivm.Doc.OriginalUrl);
          }
          



      }
      catch (Exception es)
      {
        Logger.Log("Html View",es.Message,"",Logger.Level.Error);
      }
      
      
    }

    
  }
}
