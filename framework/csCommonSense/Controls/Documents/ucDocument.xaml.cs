using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ESRI.ArcGIS.Client;
using Microsoft.Surface.Presentation.Input;
using csShared;
using csShared.Documents;
using csShared.Utils;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;

namespace csGeoLayers
{
  /// <summary>
  /// Interaction logic for ucGraphItem.xaml
  /// </summary>
  public partial class ucDocument
  {
    // Using a DependencyProperty as the backing store for Document. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DocumentProperty =
      DependencyProperty.Register("Document", typeof (Document), typeof (ucDocument),
                    new UIPropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty ColorProperty =
      DependencyProperty.Register("Color", typeof (Brush), typeof (ucDocument),
                    new UIPropertyMetadata(Brushes.Red));



    public string Icon
    {
      get { return (string)GetValue(IconProperty); }
      set { SetValue(IconProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Icon. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconProperty =
      DependencyProperty.Register("Icon", typeof(string), typeof(ucDocument), new UIPropertyMetadata(null));

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((ucDocument) d).UpdateDocument();
    }



    // List to store the input devices those do not need do the dragging check.
    private readonly List<InputDevice> _ignoredDeviceList = new List<InputDevice>();

    public bool CanBeDragged
    {
      get { return (bool)GetValue(CanBeDraggedProperty); }
      set { SetValue(CanBeDraggedProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CanBeDragged. This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CanBeDraggedProperty =
      DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucDocument), new UIPropertyMetadata(true));

    //readonly BackgroundWorker _iworker = new BackgroundWorker();
    //readonly BackgroundWorker _vworker = new BackgroundWorker();
    //readonly BackgroundWorker _xworker = new BackgroundWorker();
    //private BitmapImage image = new BitmapImage();

    public bool Cachable;

    public ucDocument()
    {
      InitializeComponent();
      Loaded += UcDocumentItemLoaded;
      TouchDown += UcDocumentContactDown;
      MouseDown += UcDocumentMouseDown;
      //AppStateSettings.GetInstance().MediaC.DownloadCompleted += MediaC_DownloadCompleted;
    }

    

    void MediaC_DownloadCompleted(string orig, string cached, string hashcode)
    {
      Dispatcher.Invoke(delegate
      {
          if (Document == null) return;
          Cachable = Document.IsCachable;
          if (Cachable && hashcode == Document.Location.GetHashCode().ToString())
          {
              switch (Document.FileType)
              {
                  case FileTypes.video:
                      IMedia.Visibility = Visibility.Visible;
                      IMedia.Source = new Uri(cached);
                      break;
                  case FileTypes.image:
                      iMain.Visibility = Visibility.Visible;
                      BitmapImage bi = new BitmapImage();
                      bi.BeginInit();
                      //bi.DecodePixelWidth = 100;
                      bi.CacheOption = BitmapCacheOption.OnLoad;
                      bi.CreateOptions = BitmapCreateOptions.DelayCreation;
                      bi.UriSource = new Uri( cached );
                      bi.EndInit();
              
                      iMain.Source = bi;
                      break;
              }
              var svi = (ScatterViewItem)BaseWPFHelpers.Helpers.FindElementOfTypeUp(this, new ScatterViewItem().GetType());
              if (svi != null)
              {
                  svi.Height = iMain.Height;
                  svi.Width = iMain.Width;

              }
          }
      });
    }


    public Document Document
    {
      get { return (Document) GetValue(DocumentProperty); }
      set { SetValue(DocumentProperty, value); }
    }

    public Brush Color
    {
      get { return (Brush) GetValue(ColorProperty); }
      set { SetValue(ColorProperty, value); }
    }

    void UcDocumentMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (CanBeDragged)
      {
        var lb = GetVisualAncestor<FrameworkElement>((DependencyObject) sender);
        //if (HorizontalDragThreshold == 0 || Math.Abs(pos.X - OriginalContactPos.X) < HorizontalDragThreshold)
        {
          Document.OriginalRotation = -90; // e.Contact.GetOrientation(this);
          StartDragDrop(lb, e);
        }
        e.Handled = true;
      }
    }

    private void UcDocumentContactDown(object sender, TouchEventArgs e)
    {
      if (CanBeDragged)
      {
        var lb = GetVisualAncestor<FrameworkElement>((DependencyObject) sender);
        //if (HorizontalDragThreshold == 0 || Math.Abs(pos.X - OriginalContactPos.X) < HorizontalDragThreshold)
        {
          Document.OriginalRotation = e.Device.GetOrientation(this) - 90;
          StartDragDrop(lb, e);
        }
        e.Handled = true;
      }
    }

    /// <summary>
    /// Try to start Drag-and-drop for a listBox.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void StartDragDrop(FrameworkElement source, InputEventArgs e)
    {
      try
      {


        // Check whether the input device is in the ignore list.
        if (_ignoredDeviceList.Contains(e.Device) || source == null)
        {
          return;
        }

        InputDeviceHelper.InitializeDeviceState(e.Device);


        // try to start drag-and-drop,
        // verify that the cursor the contact was placed at is a ListBoxItem
        var parent = source.Parent as FrameworkElement;

        //var parrent = GetVisualAncestor<FrameworkElement>(downSource);
        //Debug.Assert(parrent != null);

        var cursorVisual = new ucDocument { Document = Document, DataContext = Document, CanBeDragged = false };
        IEnumerable<InputDevice> devices;
        cursorVisual.Width = source.ActualWidth;
        cursorVisual.Height = source.ActualHeight;

        var contactEventArgs = e as TouchEventArgs;
        if (contactEventArgs != null)
        {
          devices = new List<InputDevice>(new[] { e.Device });
          // MergeContacts(Contacts.GetContactsCapturedWithin(parrent), contactEventArgs.Contact);
        }
        else
        {
          devices = new List<InputDevice>(new[] { e.Device });
        }

        SurfaceDragDrop.BeginDragDrop(parent, source, cursorVisual, cursorVisual.Document, devices, DragDropEffects.Copy);
        //if (!SurfaceDragDrop.BeginDragDrop(source, parrent, cursorVisual, Document, devices, DragDropEffects.Copy))
        //{        
        //  return;
        //}

        
      }
      catch (Exception es)
      {
        Logger.Log("Document","Error drag drop",es.Message,Logger.Level.Error);

      }
      finally
      {
        // Reset the input device's state.
        InputDeviceHelper.ClearDeviceState(e.Device);
        _ignoredDeviceList.Remove(e.Device); 
      }
    }

    

    /// <summary>
    /// Attempts to get an ancestor of the passed-in element with the given type.
    /// </summary>
    /// <typeparam name="T">Type of ancestor to search for.</typeparam>
    /// <param name="descendent">Element whose ancestor to find.</param>
    /// <param name="ancestor">Returned ancestor or null if none found.</param>
    /// <returns>True if found, false otherwise.</returns>
    private static T GetVisualAncestor<T>(DependencyObject descendent) where T : class
    {
      T ancestor = null;
      DependencyObject scan = descendent;

      while (scan != null && ((ancestor = scan as T) == null))
      {
        scan = VisualTreeHelper.GetParent(scan);
      }

      return ancestor;
    }

     //public static void DoEvents()
     //  {
     //  try
     //  {
     //    var frame = new DispatcherFrame();
     //    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
     //      new DispatcherOperationCallback(f =>
     //      {
     //        ((DispatcherFrame)f).Continue = false; return null;
     //      }), frame);
     //    Dispatcher.PushFrame(frame);
     //  }
     //  catch (Exception)
     //  {
         
         
     //  }
        
     // }

    private void UcDocumentItemLoaded(object sender, RoutedEventArgs e)
    {
      UpdateDocument();
    }

    public void UpdateDocument()
    {
      if (Document == null)
      {
        if (DataContext is DataBinding)
        {
          DataBinding db = (DataBinding) DataContext;
          if (db.Attributes.ContainsKey("Document") && db.Attributes["Document"] is Document)
            this.Document = (Document) db.Attributes["Document"];
        }
      }
      if (Document == null) return;

      Cachable = Document.IsCachable;
      if (Cachable)
      {
        if (Document.Location.StartsWith("http"))
        {
          Document.Location = AppStateSettings.GetInstance().MediaC.GetFile(Document.Location, false);
          if (Document.Location.StartsWith("http"))
            return;
        }
      }
      try
      {
        switch (Document.FileType)
        {
          case FileTypes.video:
            IMedia.Visibility = Visibility.Visible;
            IMedia.Source = new Uri(Document.Location);
            break;
                case FileTypes.imageFolder:
                case FileTypes.html:
          case FileTypes.image:
            iMain.Visibility = Visibility.Visible;

            if (Document.Image == null)
            {
                try
                {
                    var bmi = new BitmapImage();
                    bmi.BeginInit();
                    if (Document.Location != null)
                        bmi.UriSource = new Uri(Document.Location);
                    else if (Document.IconUrl != null)
                        bmi.UriSource = new Uri(Document.IconUrl);
                    else
                        bmi.UriSource = new Uri(Document.OriginalUrl);
                    // new Uri(Document.Location, UriKind.Relative);
                    //bmi.CreateOptions = BitmapCreateOptions.DelayCreation;
                    //bmi.CacheOption = BitmapCacheOption.None;
                    if (Document.ShowThumbNail)
                        bmi.DecodePixelWidth = 50;
                    bmi.EndInit();

                    iMain.Source = bmi;
                }
                catch (Exception e)
                {
                    Logger.Log("Document","Error loading image",e.Message,Logger.Level.Error);
                }

            }
            else
            {
                 try
                {
              iMain.Source = Document.Image;
                }
                 catch (Exception e)
                 {
                     Logger.Log("Document", "Error updating image", e.Message, Logger.Level.Error);
                 }
              ;
            }

            
            break;
            //case FileTypes.xps:
            //  iDocument.Visibility = Visibility.Visible;
            //  _xworker.RunWorkerAsync(new Uri(Document.Location));
            //  break;
          case FileTypes.web:
            iMain.Visibility = Visibility.Visible;
            break;
        }
        var svi = (ScatterViewItem) BaseWPFHelpers.Helpers.FindElementOfTypeUp(this, new ScatterViewItem().GetType());
        if (svi != null)
        {
          svi.Height = iMain.Height;
          svi.Width = iMain.Width;
        }
      }
      catch (Exception)
      {
        Logger.Log("Document", "Error opening document", Document.FileType.ToString(), Logger.Level.Error);
      }
    }

    // public void UcDocumentChangeDocument()
    // {
    //   if (Document == null) return;

    //   Cachable = Document.IsCachable;
    ////   IMedia.Visibility = Visibility.Collapsed;
    //   iMain.Visibility = Visibility.Collapsed;
      
    // //  iDocument.Visibility = Visibility.Collapsed;
    //  // iWebPage.Visibility = Visibility.Collapsed;


    //   switch (Document.FileType)
    //   {
    //     //case FileTypes.video:
    //     //  IMedia.Visibility = Visibility.Visible;
    //     //  _vworker.RunWorkerAsync(new Uri(Document.Location));
    //     //  break;
    //     case FileTypes.image:
    //       iMain.Visibility = Visibility.Visible;
          
    //       _iworker.RunWorkerAsync(new Uri(Document.Location));
    //       break;
    //     //case FileTypes.xps:
    //     //  iDocument.Visibility = Visibility.Visible;
    //     //  _xworker.RunWorkerAsync(new Uri(Document.Location));
    //     //  break;
    //     case FileTypes.web:
    //      iMain.Visibility = Visibility.Visible;
          
    //       _iworker.RunWorkerAsync(new Uri(Document.IconUrl));
    //       break;
    //   }
    // }
  }
}