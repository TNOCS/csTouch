using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using csGeoLayers;
using csShared;
using csShared.Documents;
using csShared.Utils;
using nl.tno.cs.presenter;

namespace csPresenterPlugin.Controls
{
    ///<summary>
    ///  Follow steps 1a or 1b and then 2 to use this custom control in a XAML file. Step 1a) Using this custom control in a XAML file that exists in the current project. Add this XmlNamespace attribute to the root element of the markup file where it is to be used: xmlns:MyNamespace="clr-namespace:csPresenterPlugin.Controls" Step 1b) Using this custom control in a XAML file that exists in a different project. Add this XmlNamespace attribute to the root element of the markup file where it is to be used: xmlns:MyNamespace="clr-namespace:csPresenterPlugin.Controls;assembly=csPresenterPlugin.Controls" You will also need to add a project reference from the project where the XAML file lives to this project and Rebuild to avoid compilation errors: Right click on the target project in the Solution Explorer and "Add Reference"->"Projects"->[Browse to and select this project] Step 2) Go ahead and use your control in the XAML file. <MyNamespace:MetroItem />
    ///</summary>
    public class MetroItem : Control
    {
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof (ItemClass), typeof (MetroItem), new UIPropertyMetadata(null));

        static MetroItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (MetroItem), new FrameworkPropertyMetadata(typeof (MetroItem)));
        }

        public ItemClass Item {
            get { return (ItemClass) GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        public int thresHold;

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            thresHold = AppStateSettings.Instance.Config.GetInt("DragThresHold", 2);
            if (Item.Type == ItemType.website)
            {
                
            }
            var sb = GetTemplateChild("sbButton") as SurfaceButton;
            if (sb != null)
            {
                sb.Click += SbClick;
                sb.PreviewTouchDown += sb_PreviewTouchDown;
                sb.PreviewTouchMove += sb_PreviewTouchMove;
                sb.PreviewTouchUp += sb_PreviewTouchUp;
                sb.PreviewMouseDown += sb_PreviewMouseDown;
                sb.PreviewMouseMove += sb_PreviewMouseMove;

                sb.PreviewMouseUp += sb_PreviewMouseUp;
                sb.MouseLeave += sb_MouseLeave;
                
            }
            this.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(MetroItem_MouseDown);
            //this.PreviewMouseMove += new MouseEventHandler(MetroItem_PreviewMouseMove);
            this.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(MetroItem_PreviewMouseLeftButtonUp);
            if (Item != null && Item.Explorer != null)
            {
                if (!string.IsNullOrEmpty(Item.Explorer.ActivePath))
                {
                    if (Item.Explorer.CurrentItem != null && Item.Explorer.CanSelect)
                    {
                        var e = Item.Explorer.CurrentItem.Type;
                        if (Item.Explorer.ActivePath == Item.Path)
                            Opacity = 1;
                        else if (e == ItemType.folder)
                            Opacity = 1;
                        else Opacity = 0.25;
                    }
                    else
                    {
                        Opacity = 1;
                    }
                }
                Item.Explorer.ActivePathChanged += ExplorerActivePathChanged;
            }
        }

        void sb_PreviewTouchUp(object sender, TouchEventArgs e)
        {

            down = false;
        }

        void sb_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (down && downTime.AddMilliseconds(1000) > DateTime.Now)
            {
                var newPos = e.GetTouchPoint(Item.Explorer).Position;
                if (Math.Abs(origPos.X - newPos.X) < thresHold && Math.Abs(origPos.Y - newPos.Y) > 2)
                    StartDragDrop(Item.Explorer, this, e);
            }
        }

        void sb_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            down = true;
            origPos = e.GetTouchPoint(Item.Explorer).Position;
            downTime = DateTime.Now;
        }

        void sb_MouseLeave(object sender, MouseEventArgs e)
        {
            down = false;
        }

        void sb_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            down = false;
        }

        private Point origPos;
        private bool down;
        private DateTime downTime;

        void sb_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (down && downTime.AddMilliseconds(1000) > DateTime.Now)
            {
                var newPos = e.GetPosition(Item.Explorer);
                if (Math.Abs(origPos.X - newPos.X) < thresHold && Math.Abs(origPos.Y - newPos.Y) > 2)
                    StartDragDrop(Item.Explorer, this, e);
            }
        }

        void sb_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            down = true;
            downTime = DateTime.Now;
            origPos = e.GetPosition(Item.Explorer);
            
        }

        void MetroItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            pos = new Point();
        }

        private void StartDragDrop(FrameworkElement parent, FrameworkElement source, InputEventArgs e)
        {
            try
            {
                if (Item.Type == ItemType.folder) return;
                Document d = Item.GetDocument();
                if (d.FileType == FileTypes.unknown) return;
                // Check whether the input device is in the ignore list.
                if (_ignoredDeviceList.Contains(e.Device) || source == null)
                {
                    return;
                }

                InputDeviceHelper.InitializeDeviceState(e.Device);


                // try to start drag-and-drop,
                // verify that the cursor the contact was placed at is a ListBoxItem
                //var parent = source.Parent as FrameworkElement;

                //var parrent = GetVisualAncestor<FrameworkElement>(downSource);
                //Debug.Assert(parrent != null);
                
                
                var cursorVisual = new ucDocument() { Document = d, DataContext = d, CanBeDragged = false, Width = 75, Height = 75};
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
                //e.Handled = false;

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

        private Point pos;

        void MetroItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var lb = GetVisualAncestor<FrameworkElement>((DependencyObject)sender);
            //if (HorizontalDragThreshold == 0 || Math.Abs(pos.X - OriginalContactPos.X) < HorizontalDragThreshold)
            {
                pos = e.GetPosition(null);
               

                //Document.OriginalRotation = -90; // e.Contact.GetOrientation(this);
                
            }
            
        }

        private readonly List<InputDevice> _ignoredDeviceList = new List<InputDevice>();

        
        private void ExplorerActivePathChanged(object sender, ItemSelectedArgs e) {
          
            if (e.Item != null && e.Item.Explorer != null && e.Item.Explorer.CanSelect)
            {
                if (e.Item == null || e.Item.Path == Item.Path)
                {
                    Opacity = 1;
                    return;
                }
                Opacity = 0.25;
                return;
            }
            
            Opacity = 1;
            
            //else
            //{
            //    if (e.Item == null || e.Item.Path == Item.Path)
            //    {
            //        Opacity = 1;
            //        return;
            //    }
            //    Opacity = 0.25;
            //}
        }

        private void SbClick(object sender, RoutedEventArgs e) {
            SelectItem();
        }

        public void SelectItem()
        {
            Point p = TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
            if (Item != null && Item.Explorer != null) Item.Explorer.SelectItem(Item,p.X,p.Y);
            //switch (Item.Type)
            //{
            //    case ItemType.folder:
            //        DirectoryInfo di = new DirectoryInfo(Item.Path);
            //        Item.Explorer.SelectFolder(di.FullName);
            //        break;
            //    case ItemType.image:
            //        Item.Explorer.SelectItem(Item);
            //        MessageBox.Show("Image");
            //        break;
            //    case ItemType.video:
            //        Item.Explorer.SelectItem(Item);
            //        MessageBox.Show("Video");
            //        break;
            //    case ItemType.unknown:                    
            //        FileInfo fi = new FileInfo(Item.Path);
            //        if (Item.Explorer.Extensions.ContainsKey(fi.Extension))
            //        {
            //            Item.Explorer.SelectItem(Item);
            //            Item.Explorer.Title = Item.Name;
            //            Item.Explorer.Extensions[fi.Extension].Invoke(Item);
            //        }
            //        break;
            //}
        }
    }
}