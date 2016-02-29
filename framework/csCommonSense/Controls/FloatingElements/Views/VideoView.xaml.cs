using System;
using System.Windows;
using System.Windows.Threading;
using BaseWPFHelpers;
using Microsoft.Surface.Presentation.Controls;

namespace csShared
{
  public partial class VideoView
  {

    System.Windows.Style s = null;

    public DispatcherTimer ProgressTimer;


    public VideoView()
    {
      InitializeComponent();
      Loaded += ImageViewLoaded;

      meMain.SourceUpdated += MainSourceUpdated;

      
      foreach (var a in Application.Current.Resources.MergedDictionaries)
      {
        var b = a["SimpleFloatingStyle"];
        if (b != null) s = (Style)b;
      }

      

    }

    void ImageViewLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
      //BitmapImage bi = new BitmapImage(new Uri(((ImageViewModel) this.DataContext).Doc.Location));
      var s = (VideoViewModel) this.DataContext;

      meMain.SourceUpdated += meMain_SourceUpdated;
      
      //fe.MinSize = new Size(bi.Width, bi.Height);
      meMain.Source = new Uri(s.Doc.Location);
      meMain.BufferingEnded += meMain_BufferingEnded;
      meMain.BufferingStarted += meMain_BufferingStarted;
      meMain.MediaOpened += meMain_MediaOpened;
      
      ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
      if (_svi!=null)
      {
        _svi.SizeChanged += _svi_SizeChanged;
      }

      ProgressTimer = new DispatcherTimer();
      ProgressTimer.Interval = new TimeSpan(0,0,0,1);
      ProgressTimer.Tick += ProgressTimer_Tick;
      ProgressTimer.Start();
    }

    void ProgressTimer_Tick(object sender, EventArgs e)
    {
      if (meMain != null)
      {
        try
        {
          if (meMain.NaturalDuration.HasTimeSpan)
          {
            tbProgress.Text = ((int)meMain.Position.TotalMinutes).ToString("D2") + ":" + meMain.Position.Seconds.ToString("D2") + " / " +
                   ((int)meMain.NaturalDuration.TimeSpan.TotalMinutes).ToString("D2") + ":" + meMain.NaturalDuration.TimeSpan.Seconds.ToString("D2");
          }
        }
        catch (Exception)
        {
          Console.WriteLine("Test");
          
        }
        
      }
    }

    void _svi_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
      if (_svi != null)
      {
        _svi.Width = e.NewSize.Width;
        _svi.Height = (e.NewSize.Width/meMain.NaturalVideoWidth)*meMain.NaturalVideoHeight;
      }
    }


    void meMain_MediaOpened(object sender, RoutedEventArgs e)
    {
      ScatterViewItem _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
      FloatingElement fe = (FloatingElement)_svi.DataContext;

      if (s != null)
      {
        //fe.Style = s;
      }

      fe.ShowShadow = true;
      fe.Width = 400;// meMain.NaturalVideoWidth;
      fe.Height = (400f/meMain.NaturalVideoWidth) * meMain.NaturalVideoHeight;
      if (_svi != null)
      {
        _svi.Width = fe.Width;
        _svi.Height = fe.Height;
      }
    }

    void meMain_BufferingStarted(object sender, RoutedEventArgs e)
    {
      
    }

    void meMain_BufferingEnded(object sender, RoutedEventArgs e)
    {
      
    }

    void meMain_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
    {
      
    }

   

    void MainSourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
    {
      
      //fe.Height = (300 / bi.Width) * bi.Height;

    }
  }
}
