using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BaseWPFHelpers;
using Microsoft.Surface.Presentation.Controls;
using csShared.Utils;

namespace csShared.FloatingElements
{
  public partial class ImageFolderView
  {
    private readonly Style s;

    public List<string> Images;
    private double OrigHeight;
    public string SelectedImage;
    private ScatterViewItem _svi;
    private BitmapImage bi; // FIXME TODO bi is never assigned.
    private DispatcherTimer dt;
      private DispatcherTimer rt;

    public ImageFolderView()
    {
      InitializeComponent();
      Loaded += ImageViewLoaded;
      iMain.SourceUpdated += MainSourceUpdated;


      foreach (ResourceDictionary a in Application.Current.Resources.MergedDictionaries)
      {
        object b = a["SimpleFloatingStyle"];
        if (b != null) s = (Style) b;
      }
    }

    private void ImageViewLoaded(object sender, RoutedEventArgs e)
    {
      try
      {
          rt = new DispatcherTimer();
          rt.Interval = new TimeSpan(0,0,0,0,100);
          rt.Tick += rt_Tick;
          rt.Start();
        dt = new DispatcherTimer();
        dt.Interval = new TimeSpan(0, 0, 0, 10);
        dt.Tick += dt_Tick;
        if ((DataContext as ImageFolderViewModel) != null)
          SelectedImage = (DataContext as ImageFolderViewModel).Doc.Location;
        var i = Directory.GetFiles(SelectedImage).ToList();
          List<FileInfo> fi = new List<FileInfo>();
          i.ForEach(k => fi.Add(new FileInfo(k)));
        Images = fi.OrderBy(k=>k.CreationTime).Select(k=>k.FullName).ToList();
        if (Images.Count > 0)
        {
          SelectedImage = Images.First();
          _svi = (ScatterViewItem) Helpers.FindElementOfTypeUp(this, typeof (ScatterViewItem));

          LoadImage();

          if (_svi != null && _svi.DataContext is FloatingElement)
          {
            _svi.SizeChanged += SviSizeChanged;
            _svi.ContainerActivated += _svi_ContainerActivated;
            _svi.MouseMove += _svi_MouseEnter;
          }
        }
          
      }
      catch (Exception es)
      {
        Logger.Log("Image View", es.Message, "", Logger.Level.Error);
      }
      
    }

      private int resizeCount = 0;

    void rt_Tick(object sender, EventArgs e)
    {
        resizeCount += 1;
        if (resizeCount>300) 
            rt.Stop();
        if (iMain.Source is BitmapSource)
        {
            BitmapSource bi = (BitmapSource) iMain.Source;
            if (bi != null)
            {
                _svi.Width = _svi.Height*(bi.PixelWidth/(double) bi.PixelHeight);
                rt.Stop();
            }
        }

    }

    private void _svi_MouseEnter(object sender, MouseEventArgs e)
    {
      gNavigation.Visibility = Visibility.Visible;
      dt.Start();
    }

      

    private void dt_Tick(object sender, EventArgs e)
    {
      gNavigation.Visibility = Visibility.Collapsed;
      dt.Stop();
    }

    private void _svi_ContainerActivated(object sender, RoutedEventArgs e)
    {
      gNavigation.Visibility = Visibility.Visible;
      dt.Start();
    }


    private void LoadImage()
    {
      //bi = new BitmapImage(new Uri(SelectedImage));
      //bi.DownloadCompleted += bi_DownloadCompleted;
     
      iMain.DataContext = SelectedImage;


      var fe = (FloatingElement) _svi.DataContext;

      if (fe != null)
      {
        if (s != null)
        {
          fe.Style = s;
        }

        //fe.ShowShadow = true;
        //if (OrigHeight != 0)
        //{
        //  _svi.Height = OrigHeight;
        //  _svi.Width = OrigHeight*(bi.Width/bi.Height);
        //}
        //else
        //{
        //  _svi.Height = fe.Height;
        //  _svi.Width = fe.Height*(bi.Width/bi.Height);
        //}
      }
    }

   

    private void SviSizeChanged(object sender, SizeChangedEventArgs e)
    { 
      if (iMain.Source is BitmapSource)
      {
        BitmapSource bi = (BitmapSource)iMain.Source;
        var _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
        if (_svi != null)
        {
          _svi.Height = e.NewSize.Height;
          _svi.Width = e.NewSize.Height * (bi.PixelWidth / (double)bi.PixelHeight);

          OrigHeight = e.NewSize.Height;
        }
      }
      
    }

    private void bi_DownloadCompleted(object sender, EventArgs e)
    {
      var _svi = (ScatterViewItem) Helpers.FindElementOfTypeUp(this, typeof (ScatterViewItem));
      if (_svi != null)
      {
        if (_svi.DataContext is FloatingElement)
        {
          var fe = (FloatingElement) _svi.DataContext;

          if (fe != null)
          {
            fe.Height = _svi.Width;
            fe.Width = _svi.Width*(bi.Width/bi.Height);
          }
        }
      }
      //fe.MinSize = new Size(bi.Width, bi.Height);
    }


    private void MainSourceUpdated(object sender, DataTransferEventArgs e)
    {
    }

    private void NextClick(object sender, RoutedEventArgs e)
    {
      int pos = Images.IndexOf(SelectedImage);
      if (pos < Images.Count - 1)
      {
        pos += 1;
      }
      else
      {
        pos = 0;
      }
      SelectedImage = Images[pos];
      LoadImage();
    }

    private void PreviousClick(object sender, RoutedEventArgs e)
    {
      int pos = Images.IndexOf(SelectedImage);
      if (pos > 0)
      {
        pos -= 1;
      }
      else
      {
        pos = Images.Count - 1;
      }
      SelectedImage = Images[pos];
      LoadImage();
    }

    private void PreviewImages_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count > 0)
      {
        SelectedImage = e.AddedItems[0] as string;
        LoadImage();
      }
    }
  }
}