﻿using System.Windows;
using System.Windows.Controls;

namespace csShared.ThirdParty
{
    public static class Loader
    {
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static SourceType GetSourceType(Image obj)
        {
            return (SourceType)obj.GetValue(SourceTypeProperty);
        }

        public static void SetSourceType(Image obj, SourceType value)
        {
            obj.SetValue(SourceTypeProperty, value);
        }
        public static readonly DependencyProperty SourceTypeProperty =
          DependencyProperty.RegisterAttached("SourceType", typeof(SourceType), typeof(Loader), new UIPropertyMetadata(SourceType.LocalDisk));

        [AttachedPropertyBrowsableForType(typeof(double))]
        public static double GetCacheHeight(Image obj)
        {
            return (double)obj.GetValue(CacheHeightProperty);
        }

        public static void SetCacheHeight(Image obj, double value)
        {
            obj.SetValue(CacheHeightProperty, value);
        }
        public static readonly DependencyProperty CacheHeightProperty =
          DependencyProperty.RegisterAttached("CacheHeight", typeof(double), typeof(Loader), new UIPropertyMetadata(300.0));


        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static object GetSource(Image obj)
        {
            return obj.GetValue(SourceProperty);
        }

        public static void SetSource(Image obj, object value)
        {
            obj.SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty =
          DependencyProperty.RegisterAttached("Source", typeof(object), typeof(Loader), new UIPropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Manager.Instance.LoadImage(e.NewValue, d as Image, GetCacheHeight(d as Image));
        }

        //public static readonly RoutedEvent ImageLoadedEvent = EventManager.RegisterRoutedEvent("ImageLoaded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImageHelper));
        //public static void AddImageLoadedHandler(DependencyObject d, RoutedEventHandler handler)
        //{
        //  UIElement uie = d as UIElement;
        //  if (uie != null)
        //  {
        //    uie.AddHandler(Image.ImageLoadedEvent, handler);
        //  }
        //}
        //public static void RemoveImagedLoadedHandler(DependencyObject d, RoutedEventHandler handler)
        //{
        //  UIElement uie = d as UIElement;
        //  if (uie != null)
        //  {
        //    uie.RemoveHandler(Image.ImageLoadedEvent, handler);
        //  }
        //}

        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static bool GetDisplayWaitingAnimationDuringLoading(Image obj)
        {
            return (bool)obj.GetValue(DisplayWaitingAnimationDuringLoadingProperty);
        }

        public static void SetDisplayWaitingAnimationDuringLoading(Image obj, bool value)
        {
            obj.SetValue(DisplayWaitingAnimationDuringLoadingProperty, value);
        }
        public static readonly DependencyProperty DisplayWaitingAnimationDuringLoadingProperty =
          DependencyProperty.RegisterAttached("DisplayWaitingAnimationDuringLoading", typeof(bool), typeof(Loader), new UIPropertyMetadata(true));


        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static bool GetDisplayErrorThumbnailOnError(Image obj)
        {
            return (bool)obj.GetValue(DisplayErrorThumbnailOnErrorProperty);
        }

        public static void SetDisplayErrorThumbnailOnError(Image obj, bool value)
        {
            obj.SetValue(DisplayErrorThumbnailOnErrorProperty, value);
        }
        public static readonly DependencyProperty DisplayErrorThumbnailOnErrorProperty =
          DependencyProperty.RegisterAttached("DisplayErrorThumbnailOnError", typeof(bool), typeof(Loader), new UIPropertyMetadata(true));




        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static DisplayOptions GetDisplayOption(Image obj)
        {
            return (DisplayOptions)obj.GetValue(DisplayOptionProperty);
        }

        public static void SetDisplayOption(Image obj, DisplayOptions value)
        {
            obj.SetValue(DisplayOptionProperty, value);
        }
        public static readonly DependencyProperty DisplayOptionProperty =
          DependencyProperty.RegisterAttached("DisplayOption", typeof(DisplayOptions), typeof(Loader), new UIPropertyMetadata(DisplayOptions.Preview));



        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static bool GetIsLoading(Image obj)
        {
            return (bool)obj.GetValue(IsLoadingProperty);
        }

        internal static void SetIsLoading(Image obj, bool value)
        {
            obj.SetValue(IsLoadingProperty, value);
        }
        public static readonly DependencyProperty IsLoadingProperty =
          DependencyProperty.RegisterAttached("IsLoading", typeof(bool), typeof(Loader), new UIPropertyMetadata(true));


        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static bool GetErrorDetected(Image obj)
        {
            return (bool)obj.GetValue(ErrorDetectedProperty);
        }

        internal static void SetErrorDetected(Image obj, bool value)
        {
            obj.SetValue(ErrorDetectedProperty, value);
        }

        public static readonly DependencyProperty ErrorDetectedProperty =
          DependencyProperty.RegisterAttached("ErrorDetected", typeof(bool), typeof(Loader), new UIPropertyMetadata(false));



    }
}
