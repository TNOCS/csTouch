﻿using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace csCommon.Controls
{
    /// <Remarks>
    ///     As a side effect ClippingBorder will surpress any databinding or animation of 
    ///         its childs UIElement.Clip property until the child is removed from ClippingBorder
    /// </Remarks>
    public class ClippingBorder : Border
    {
        protected override void OnRender(DrawingContext dc)
        {
            OnApplyChildClip();
            base.OnRender(dc);
        }

        public override UIElement Child
        {
            get
            {
                return base.Child;
            }
            set
            {
                if (this.Child != value)
                {
                    if (this.Child != null)
                    {
                        // Restore original clipping
                        this.Child.SetValue(UIElement.ClipProperty, oldClip);
                    }

                    if (value != null)
                    {
                        oldClip = value.ReadLocalValue(UIElement.ClipProperty);
                    }
                    else
                    {
                        // If we dont set it to null we could leak a Geometry object
                        oldClip = null;
                    }

                    base.Child = value;
                }
            }
        }

        protected virtual void OnApplyChildClip()
        {
            UIElement child = this.Child;
            if (child != null)
            {
                _clipRect.RadiusX = _clipRect.RadiusY = Math.Max(0.0, this.CornerRadius.TopLeft - (this.BorderThickness.Left * 0.5));
                _clipRect.Rect = new Rect(Child.RenderSize);
                child.Clip = _clipRect;
            }
        }

        private RectangleGeometry _clipRect = new RectangleGeometry();
        private object oldClip;
    }
}
