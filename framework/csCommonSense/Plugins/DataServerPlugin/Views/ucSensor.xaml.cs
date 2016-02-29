using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Surface.Presentation;
using csEvents.Sensors;
using csShared.Utils;

namespace csGeoLayers.Sensors
{
	/// <summary>
	/// Interaction logic for ucSensor.xaml
	/// </summary>
	public partial class ucSensor : UserControl
	{

       
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Label.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ucSensor), new PropertyMetadata(""));

         

        public DataSet DataSet
        {
            get { return (DataSet)GetValue(DataSetProperty); }
            set { SetValue(DataSetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataSetProperty =
            DependencyProperty.Register("DataSet", typeof(DataSet), typeof(ucSensor), new PropertyMetadata(null));


        public ucSensor()
		{
			this.InitializeComponent();
            this.Loaded += ucSensor_Loaded;
		}




        public bool CanBeDragged
        {
            get { return (bool)GetValue(CanBeDraggedProperty); }
            set { SetValue(CanBeDraggedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanBeDragged.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanBeDraggedProperty =
            DependencyProperty.Register("CanBeDragged", typeof(bool), typeof(ucSensor), new PropertyMetadata(true));

        
        void ucSensor_Loaded(object sender, RoutedEventArgs e)
        {
            TouchDown += UcDocumentContactDown;
            MouseDown += UcDocumentMouseDown;
        }

        private readonly List<InputDevice> _ignoredDeviceList = new List<InputDevice>();

        void UcDocumentMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CanBeDragged)
            {
                var lb = GetVisualAncestor<FrameworkElement>((DependencyObject)sender);
                //if (HorizontalDragThreshold == 0 || Math.Abs(pos.X - OriginalContactPos.X) < HorizontalDragThreshold)
                {                    
                    StartDragDrop(lb, e);
                }
                e.Handled = true;
            }
        }

        private void UcDocumentContactDown(object sender, TouchEventArgs e)
        {
            if (CanBeDragged)
            {
                var lb = GetVisualAncestor<FrameworkElement>((DependencyObject)sender);
                //if (HorizontalDragThreshold == 0 || Math.Abs(pos.X - OriginalContactPos.X) < HorizontalDragThreshold)
                {                    
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

                var cursorVisual = new ucSensor { DataSet = DataSet, CanBeDragged = false };
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

                SurfaceDragDrop.BeginDragDrop(parent, source, cursorVisual, DataSet, devices, DragDropEffects.Copy);
                //if (!SurfaceDragDrop.BeginDragDrop(source, parrent, cursorVisual, Document, devices, DragDropEffects.Copy))
                //{                
                //    return;
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
	}
}