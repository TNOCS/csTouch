#region File Header

// -------------------------------------------------------------------------------
// 
// This file is part of the WPFSpark project: http://wpfspark.codeplex.com/
//
// Author: Ratish Philip
// 
// WPFSpark v1.1
//
// -------------------------------------------------------------------------------

#endregion

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace WPFSpark
{
    /// <summary>
    /// Defines the Drag Behavior in the FluidWrapPanel using the Mouse
    /// </summary>
    public class FluidMouseDragBehavior : Behavior<UIElement>
    {
        #region Fields

        FluidWrapPanel parentFWPanel = null;
        ListBoxItem parentLBItem = null;

        #endregion

        public event EventHandler Changed; // FIXME TODO "new" keyword missing?

        #region Dependency Properties

        #region DragButton

        /// <summary>
        /// DragButton Dependency Property
        /// </summary>
        public static readonly DependencyProperty DragButtonProperty =
            DependencyProperty.Register("DragButton", typeof(MouseButton), typeof(FluidMouseDragBehavior),
                new FrameworkPropertyMetadata(MouseButton.Left));

        /// <summary>
        /// Gets or sets the DragButton property. This dependency property 
        /// indicates which Mouse button should participate in the drag interaction.
        /// </summary>
        public MouseButton DragButton
        {
            get { return (MouseButton)GetValue(DragButtonProperty); }
            set { SetValue(DragButtonProperty, value); }
        }

        #endregion

        #endregion

        #region Overrides

        /// <summary>
        /// 
        /// </summary>
        protected override void OnAttached()
        {
            // Subscribe to the Loaded event
            (this.AssociatedObject as FrameworkElement).Loaded += OnAssociatedObjectLoaded;
        }

        void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            // Get the parent FluidWrapPanel and check if the AssociatedObject is
            // hosted inside a ListBoxItem (this scenario will occur if the FluidWrapPanel
            // is the ItemsPanel for a ListBox).
            GetParentPanel();

            // Subscribe to the Mouse down/move/up events
            if (parentLBItem != null)
            {
                parentLBItem.PreviewMouseDown += OnPreviewMouseDown;
                parentLBItem.PreviewMouseMove += OnPreviewMouseMove;
                parentLBItem.PreviewMouseUp += OnPreviewMouseUp;

                parentLBItem.PreviewTouchDown += parentLBItem_PreviewTouchDown;
                parentLBItem.PreviewTouchMove += parentLBItem_PreviewTouchMove;
            }
            else
            {
                this.AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;
                this.AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
                this.AssociatedObject.PreviewMouseUp += OnPreviewMouseUp;
            }
        }

        void parentLBItem_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            Point position = parentLBItem != null ? e.GetTouchPoint(parentLBItem).Position : e.GetTouchPoint(this.AssociatedObject).Position;

            FrameworkElement fElem = this.AssociatedObject as FrameworkElement;
            if ((fElem != null) && (parentFWPanel != null))
            {
                Point positionInParent = e.GetTouchPoint(parentFWPanel).Position;
                if (parentLBItem != null)
                    parentFWPanel.FluidDrag(parentLBItem, position, positionInParent);
                else
                    parentFWPanel.FluidDrag(this.AssociatedObject, position, positionInParent);
            }
            e.Handled = true;
        }

        void parentLBItem_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (!AssociatedObject.AreAnyTouchesDirectlyOver) return;
            
            {
                Point position = parentLBItem != null ? e.GetTouchPoint(parentLBItem).Position : e.GetTouchPoint(this.AssociatedObject).Position;

                FrameworkElement fElem = this.AssociatedObject as FrameworkElement;

                if ((fElem != null) && (parentFWPanel != null))
                {
                    if (parentLBItem != null)
                        parentFWPanel.BeginFluidDrag(parentLBItem, position);
                    else
                        parentFWPanel.BeginFluidDrag(this.AssociatedObject, position);
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Get the parent FluidWrapPanel and check if the AssociatedObject is
        /// hosted inside a ListBoxItem (this scenario will occur if the FluidWrapPanel
        /// is the ItemsPanel for a ListBox).
        /// </summary>
        private void GetParentPanel()
        {
            FrameworkElement ancestor = this.AssociatedObject as FrameworkElement;

            while (ancestor != null)
            {
                if (ancestor is ListBoxItem)
                {
                    parentLBItem = ancestor as ListBoxItem;
                }

                if (ancestor is FluidWrapPanel)
                {
                    parentFWPanel = ancestor as FluidWrapPanel;
                    // No need to go further up
                    return;
                }

                // Find the visual ancestor of the current item
                ancestor = VisualTreeHelper.GetParent(ancestor) as FrameworkElement;
            }
        }

        protected override void OnDetaching()
        {
            (this.AssociatedObject as FrameworkElement).Loaded -= OnAssociatedObjectLoaded;
            if (parentLBItem != null)
            {
                parentLBItem.MouseDown -= OnPreviewMouseDown;
                parentLBItem.MouseMove -= OnPreviewMouseMove;
                parentLBItem.MouseUp -= OnPreviewMouseUp;
            }
            else
            {
                this.AssociatedObject.MouseDown -= OnPreviewMouseDown;
                this.AssociatedObject.MouseMove -= OnPreviewMouseMove;
                this.AssociatedObject.MouseUp -= OnPreviewMouseUp;
            }
        }

        #endregion

        #region Event Handlers

        void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AssociatedObject.IsMouseDirectlyOver || AssociatedObject.AreAnyTouchesDirectlyOver) return;
            if (e.ChangedButton == DragButton)
            {
                Point position = parentLBItem != null ? e.GetPosition(parentLBItem) : e.GetPosition(this.AssociatedObject);

                FrameworkElement fElem = this.AssociatedObject as FrameworkElement;
                
                if ((fElem != null) && (parentFWPanel != null))
                {
                    if (parentLBItem != null)
                        parentFWPanel.BeginFluidDrag(parentLBItem, position);
                    else
                        parentFWPanel.BeginFluidDrag(this.AssociatedObject, position);
                }
            }
            //e.Handled = true;
            //Mouse.Capture(this.AssociatedObject);
        }

        void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            bool isDragging = false;
            if (AssociatedObject.AreAnyTouchesDirectlyOver) return;
            switch (DragButton)
            {
                case MouseButton.Left:
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        isDragging = true;
                    }
                    break;
                case MouseButton.Middle:
                    if (e.MiddleButton == MouseButtonState.Pressed)
                    {
                        isDragging = true;
                    }
                    break;
                case MouseButton.Right:
                    if (e.RightButton == MouseButtonState.Pressed)
                    {
                        isDragging = true;
                    }
                    break;
                case MouseButton.XButton1:
                    if (e.XButton1 == MouseButtonState.Pressed)
                    {
                        isDragging = true;
                    }
                    break;
                case MouseButton.XButton2:
                    if (e.XButton2 == MouseButtonState.Pressed)
                    {
                        isDragging = true;
                    }
                    break;
                default:
                    break;
            }

            if (isDragging)
            {
                Point position = parentLBItem != null ? e.GetPosition(parentLBItem) : e.GetPosition(this.AssociatedObject);

                FrameworkElement fElem = this.AssociatedObject as FrameworkElement;
                if ((fElem != null) && (parentFWPanel != null))
                {
                    Point positionInParent = e.GetPosition(parentFWPanel);
                    if (parentLBItem != null)
                        parentFWPanel.FluidDrag(parentLBItem, position, positionInParent);
                    else
                        parentFWPanel.FluidDrag(this.AssociatedObject, position, positionInParent);
                }
            }
            e.Handled = true;
        }

        void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == DragButton)
            {
                Point position = parentLBItem != null ? e.GetPosition(parentLBItem) : e.GetPosition(this.AssociatedObject);

                FrameworkElement fElem = this.AssociatedObject as FrameworkElement;
                if ((fElem != null) && (parentFWPanel != null))
                {
                    Point positionInParent = e.GetPosition(parentFWPanel);
                    if (parentLBItem != null)
                        parentFWPanel.EndFluidDrag(parentLBItem, position, positionInParent);
                    else
                        parentFWPanel.EndFluidDrag(this.AssociatedObject, position, positionInParent);

                    if (Changed != null)
                    {
                        Changed(parentFWPanel, null);
                    }
                }
            }
        }

        #endregion
    }
}
