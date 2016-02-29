using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csShared.Utils;

namespace nl.tno.cs.presenter
{

    public class UriToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.DecodePixelHeight = 300;
                    bi.CacheOption = BitmapCacheOption.OnDemand;
                    bi.UriSource = new Uri(value.ToString());
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
                catch (Exception e)
                {                    
                    Logger.Log("Bitmap Converter","Error converting bitmap",e.Message,Logger.Level.Error);
                }
                
            }
            return null;
        }
    
       public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
       {
           throw new Exception("The method or operation is not implemented.");
       }
   }
    public class ItemImageConverter : IValueConverter
    {
        private static BitmapImage folderIcon, contentIcon, batchIcon, mediaIcon, imageIcon, videoIcon, scriptIcon, presentationIcon;

        public ItemImageConverter()
        {
            if (!Execute.InDesignMode)
            {
                const int pixelWidth = 48;
                batchIcon = CreateFreezableBitmapImage("batch.png", pixelWidth);
                folderIcon = CreateFreezableBitmapImage("folder.png", pixelWidth);
                contentIcon = CreateFreezableBitmapImage("content.png", pixelWidth);
                mediaIcon = CreateFreezableBitmapImage("play.png", pixelWidth);
                imageIcon = CreateFreezableBitmapImage("image.png", pixelWidth);
                videoIcon = CreateFreezableBitmapImage("video.png", pixelWidth);
                scriptIcon = CreateFreezableBitmapImage("video.png", pixelWidth);
                presentationIcon = CreateFreezableBitmapImage("presentation.png", pixelWidth);
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is ItemType)) return folderIcon;
            switch ((ItemType)value)
            {
                case ItemType.folder:
                    return folderIcon;
                case ItemType.content:
                    return contentIcon;
                case ItemType.mediafolder:
                    return mediaIcon;
                case ItemType.batch:
                    return batchIcon;
                case ItemType.image:
                    return imageIcon;
                case ItemType.video:
                    return videoIcon;
                case ItemType.script:
                    return scriptIcon;
                case ItemType.presentation:
                    return presentationIcon;
                default:
                    return folderIcon;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        private BitmapImage CreateFreezableBitmapImage(string fileName, int pixelWidth)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.DecodePixelWidth = pixelWidth;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri("file://" + Directory.GetCurrentDirectory() + @"/Images/" + fileName);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }

    /// <summary>
    /// Converts a file path to a freezable BitmapImage.
    /// <seealso cref="http://stackoverflow.com/questions/799911/in-what-scenarios-does-freezing-wpf-objects-benefit-performance-greatly"/>
    /// </summary>
    public class ConvertFileToFreezableImage : IValueConverter
    {
        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string)) return null;
            var path = (string)value;
            return CreateFreezableBitmapImage(path, 400);
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private BitmapImage CreateFreezableBitmapImage(string fileName, int pixelHeight)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.DecodePixelHeight = pixelHeight;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(fileName, UriKind.RelativeOrAbsolute);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

    }
}
