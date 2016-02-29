using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DataServer;
using Microsoft.Surface.Presentation;
using csGeoLayers;
using csShared.Documents;
using csShared.Utils;

namespace csDataServerPlugin.Views
{
    /// <summary>
    /// Interaction logic for MediaItem.xaml
    /// </summary>
    public partial class MediaItem : UserControl
    {
        // Using a DependencyProperty as the backing store for CanBeDragged.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof (bool), typeof (MediaItem), new PropertyMetadata(true));


        // Using a DependencyProperty as the backing store for Media.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaProperty =
            DependencyProperty.Register("Media", typeof (Media), typeof (MediaItem), new PropertyMetadata(null));


        // List to store the input devices those do not need do the dragging check.
        private readonly List<InputDevice> _ignoredDeviceList = new List<InputDevice>();


        private SoundPlayer player;


        public MediaItem()
        {
            InitializeComponent();
            Loaded += MediaItemLoaded;
        }

        public bool CanBeDragged
        {
            get { return (bool) GetValue(CanBeDraggedProperty); }
            set { SetValue(CanBeDraggedProperty, value); }
        }

        public Media Media
        {
            get { return (Media) GetValue(MediaProperty); }
            set { SetValue(MediaProperty, value); }
        }

        private void MediaItemLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TouchDown += UcDocumentContactDown;
                MouseDown += UcDocumentMouseDown;
                if (Media.Content == null || Media.Content.Service == null)
                {
                    return;
                }
                FileStore store = Media.Content.Service.store;
                Service service = Media.Content.Service;
                if (CanBeDragged && Media.Type != MediaType.Photo) CanBeDragged = false;
                Media.MediaUpdated += Media_MediaUpdated;

                PlayButton.Visibility = (Media.Type == MediaType.PTT) ? Visibility.Visible : Visibility.Collapsed;
                iMedia.Visibility = (Media.Type == MediaType.Photo) ? Visibility.Visible : Visibility.Collapsed;
                byte[] b = new byte[0];
                if (string.IsNullOrEmpty(Media.LocalPath)) Media.LocalPath = Media.Id;
                if (String.IsNullOrEmpty(Media.LocalPath))
                    Media.LocalPath = store.GetLocalUrl(service.Folder, @"_Media\" + Media.Id);
                if (!store.HasFile(Media.LocalPath))
                {
                    //service.RequestData("_Media\\" + Media.Id, MediaReceived);
                    service.RequestData(Media.LocalPath, MediaReceived);
                }
                else
                {
                    b = store.GetBytes("", Media.LocalPath);
                    if (b == null) return;
                    if (Media.Type == MediaType.Photo)
                    {
                        SetImage(b);
                    }
                }
            }
            catch (Exception es)
            {
                // FIXME TODO Deal with exception!    
            }
        }

        void Media_MediaUpdated(object sender, EventArgs e)
        {
            Execute.OnUIThread(()=>
                                   {
                                       SetImage(Media.ByteArray);
              
                                   });
   
            
        }

        
        public void MediaReceived(string contentId, byte[] content, Service service)
        {
            Execute.OnUIThread(()=>
                                   {
                                       switch (Media.Type)
                                       {
                                           case MediaType.Photo:
                                               SetImage(content);
                                               break;
                                       }
                                   });
           
        }

        private void SetImage(byte[] b)
        {
            Execute.OnUIThread(() =>
            {
                try
                {
                    if (b == null || b.Length == 0) return;
                    var byteStream = new MemoryStream(b);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = byteStream;
                    image.EndInit();

                    if (Media.Type == MediaType.Photo) iMedia.Source = image;
                }
                catch (Exception ex)
                {
                    Logger.Log("MediaItem", "Error reading image", ex.Message, Logger.Level.Error);
                }
            });
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (Media.Type == MediaType.PTT)
            {
                var lp = Media.LocalPath; // Media.Content.Service.store.GetLocalUrl(Media.Content.Service.Folder, Media.LocalPath);
                if (File.Exists(lp))
                {
                    try
                    {
                        player = new SoundPlayer();
                        player.SoundLocation = lp;
                        player.Play();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("MediaItem", "Error playing sound", ex.Message, Logger.Level.Error);
                    }
                }
            }
        }

        #region dragdrop

        private void UcDocumentMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CanBeDragged)
            {
                var lb = GetVisualAncestor<FrameworkElement>((DependencyObject) sender);
                //if (HorizontalDragThreshold == 0 || Math.Abs(pos.X - OriginalContactPos.X) < HorizontalDragThreshold)
                {
                    //Document.OriginalRotation = -90; // e.Contact.GetOrientation(this);
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
                    //Document.OriginalRotation = e.Device.GetOrientation(this) - 90;
                    StartDragDrop(lb, e);
                }
                e.Handled = true;
            }
        }

        private DateTime lastDrag = DateTime.Now;

        /// <summary>
        /// Try to start Drag-and-drop for a listBox.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void StartDragDrop(FrameworkElement source, InputEventArgs e)
        {
            try
            {
                if (DateTime.Now < lastDrag.AddMilliseconds(200)) return;
                lastDrag = DateTime.Now;
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
                //if (Media.LocalPath == null)
                    Media.LocalPath = Media.Content.Service.store.GetLocalUrl(Media.Content.Service.Folder, @"_Media\" + Media.Id);
                var d = new Document {Location = Media.LocalPath, OriginalUrl = Media.LocalPath, ShareUrl = Media.LocalPath};
                var cursorVisual = new ucDocument {Document = d, CanBeDragged = false};
                IEnumerable<InputDevice> devices;
                cursorVisual.Width = source.ActualWidth;
                cursorVisual.Height = source.ActualHeight;

                var contactEventArgs = e as TouchEventArgs;
                if (contactEventArgs != null)
                {
                    devices = new List<InputDevice>(new[] {e.Device});
                    // MergeContacts(Contacts.GetContactsCapturedWithin(parrent), contactEventArgs.Contact);
                }
                else
                {
                    devices = new List<InputDevice>(new[] {e.Device});
                }

                SurfaceDragDrop.BeginDragDrop(parent, source, cursorVisual, cursorVisual.Document, devices,
                                              DragDropEffects.Copy);
                //if (!SurfaceDragDrop.BeginDragDrop(source, parrent, cursorVisual, Document, devices, DragDropEffects.Copy))
                //{                
                //    return;cu
                //}
            }
            catch (Exception es)
            {
                Logger.Log("Document", "Error drag drop", es.Message, Logger.Level.Error);
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

        #endregion

        
    }
}