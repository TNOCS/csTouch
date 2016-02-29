using System;
using System.Windows;
using System.Windows.Media.Imaging;
using BaseWPFHelpers;
using Microsoft.Surface.Presentation.Controls;
using csShared.Utils;

namespace csShared
{
  public partial class ImageView
  {

    System.Windows.Style s = null;

    public ImageView()
    {
      InitializeComponent();
      Loaded += ImageViewLoaded;
      iMain.SourceUpdated += MainSourceUpdated;


      
      foreach (var a in Application.Current.Resources.MergedDictionaries)
      {
        var b = a["SimpleFloatingStyle"];
        if (b != null) s = (Style)b;
      }

      

    }

    private BitmapImage bi;

    void ImageViewLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
      try
      {
        string loc = "";
        if ((DataContext as ImageViewModel) == null) return;

        ImageViewModel ivm = (ImageViewModel) DataContext;

        if (ivm.Doc.Image==null)
        {
          loc = ivm.Doc.Location;
          bi = new BitmapImage(new Uri(loc));
          bi.DownloadCompleted += bi_DownloadCompleted;
          
          
        }
        else
        {
          
                        bi = ivm.Doc.Image;                        
                     
          ;
        }

        ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));

        if (_svi != null && _svi.DataContext is FloatingElement)
        {
          _svi.SizeChanged += _svi_SizeChanged;
          FloatingElement fe = (FloatingElement)_svi.DataContext;

          if (fe != null)
          {
            if (s != null)
            {
              fe.Style = s;
            }

            fe.ShowShadow = true;
            _svi.Height = fe.Height;
            _svi.Width = fe.Height * (bi.Width / bi.Height);
          }
        }
        iMain.Source = bi;
        
      }
      catch (Exception es)
      {
        Logger.Log("Image View",es.Message,"",Logger.Level.Error);
      }
      
      
    }

    void _svi_SizeChanged(object sender, SizeChangedEventArgs e)
    {

      try
      {
        ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
        if (_svi != null)
        {
          _svi.Height = e.NewSize.Height;
          _svi.Width = e.NewSize.Height * ((double)bi.PixelWidth / (double)bi.PixelHeight);
        }
      }
      catch (Exception)
      {        
        Logger.Log("ImageView","Unable to change image size","",Logger.Level.Error);
      }
      
    
    }

    void bi_DownloadCompleted(object sender, EventArgs e)
    {
      ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
      if (_svi != null)
      {
        if (_svi.DataContext is FloatingElement)
        {
          FloatingElement fe = (FloatingElement) _svi.DataContext;

          if (fe != null)
          {
            fe.Height = 300;
            fe.Width = 300*(bi.Width/bi.Height);
          }
        }
      }
      //fe.MinSize = new Size(bi.Width, bi.Height);
    }

   

    void MainSourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
    {
      
    }
  }
}
