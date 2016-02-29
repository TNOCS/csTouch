using csShared.ThirdParty.ImageLoaders;
using csShared.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace csShared.ThirdParty
{
    internal sealed class Manager
    {
        internal class LoadImageRequest
        {
            public bool IsCanceled { get; set; }
            public string Source { get; set; }
            public Stream Stream { get; set; }
            public Image Image { get; set; }
            public double CachHeight { get; set; }
        }

        #region Properties
        private Thread _loaderThreadForThumbnails;
        private Thread _loaderThreadForNormalSize;

        private Dictionary<Image, LoadImageRequest> _imagesLastRunningTask = new Dictionary<Image, LoadImageRequest>();

        private Stack<LoadImageRequest> _loadThumbnailStack = new Stack<LoadImageRequest>();
        private Stack<LoadImageRequest> _loadNormalStack = new Stack<LoadImageRequest>();

        private AutoResetEvent _loaderThreadThumbnailEvent = new AutoResetEvent(false);
        private AutoResetEvent _loaderThreadNormalSizeEvent = new AutoResetEvent(false);

        private DrawingImage _loadingImage = null;
        private DrawingImage _errorThumbnail = null;
        private TransformGroup _loadingAnimationTransform = null;
        #endregion

        #region Singleton Implementation
        private static readonly Manager instance = new Manager();

        private Manager()
        {
            #region Creates Loading Threads

            _loaderThreadForThumbnails = new Thread(LoaderThreadThumbnails)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _loaderThreadForThumbnails.Start();

            _loaderThreadForNormalSize = new Thread(LoaderThreadNormalSize)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _loaderThreadForNormalSize.Start();

            #endregion

            Application.Current.Exit += Current_Exit;

            #region Loading Images from Resources

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("csCommon;component/Resources/Styles/Resources.xaml", UriKind.Relative)
            };
            _loadingImage = resourceDictionary["ImageLoading"] as DrawingImage;
            _loadingImage.Freeze();
            _errorThumbnail = resourceDictionary["ImageError"] as DrawingImage;
            _errorThumbnail.Freeze();

            #endregion

            # region Create Loading Animation

            ScaleTransform scaleTransform = new ScaleTransform(0.5, 0.5);
            SkewTransform skewTransform = new SkewTransform(0, 0);
            RotateTransform rotateTransform = new RotateTransform(0);
            TranslateTransform translateTransform = new TranslateTransform(0, 0);

            TransformGroup group = new TransformGroup();
            group.Children.Add(scaleTransform);
            group.Children.Add(skewTransform);
            group.Children.Add(rotateTransform);
            group.Children.Add(translateTransform);

            DoubleAnimation doubleAnimation = new DoubleAnimation(0, 359, new TimeSpan(0, 0, 0, 1));
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, doubleAnimation);

            _loadingAnimationTransform = group;

            #endregion
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            _loaderThreadForNormalSize.Abort();
            _loaderThreadForThumbnails.Abort();
        }

        public static Manager Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region Public Methods

        public void LoadImage(object source, Image image, double cacheHeight)
        {
            if (source == null) return;
            var imageSource = source as ImageSource;
            if (imageSource != null)
            {
                image.Source = imageSource;
            }
            else
            {
                var loadTask = new LoadImageRequest
                {
                    Image = image,
                    Source = source.ToString(),
                    CachHeight = cacheHeight
                };

                // Begin Loading
                BeginLoading(image, loadTask);

                lock (_loadThumbnailStack)
                {
                    _loadThumbnailStack.Push(loadTask);
                }

                _loaderThreadThumbnailEvent.Set();
            }

        }

        #endregion

        #region Private Methods
        private void BeginLoading(Image image, LoadImageRequest loadTask)
        {
            lock (_imagesLastRunningTask)
            {
                if (_imagesLastRunningTask.ContainsKey(image))
                { // Cancel previous loading...
                    _imagesLastRunningTask[image].IsCanceled = true;
                    _imagesLastRunningTask[image] = loadTask;
                }
                else
                {
                    _imagesLastRunningTask.Add(image, loadTask);
                }
            }

            image.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                // Set IsLoading Pty
                Loader.SetIsLoading(image, true);

                if (image.RenderTransform == MatrixTransform.Identity) // Don't apply loading animation if image already has transform...
                {
                    // Manage Waiting Image Parameter
                    if (Loader.GetDisplayWaitingAnimationDuringLoading(image))
                    {
                        image.Source = _loadingImage;
                        image.RenderTransformOrigin = new Point(0.5, 0.5);
                        image.RenderTransform = _loadingAnimationTransform;
                    }
                }
            }));
        }

        private void EndLoading(Image image, ImageSource imageSource, LoadImageRequest loadTask, bool markAsFinished)
        {
            lock (_imagesLastRunningTask)
            {
                if (_imagesLastRunningTask.ContainsKey(image))
                {
                    if (_imagesLastRunningTask[image].Source != loadTask.Source)
                        return; // if the last launched task for this image is not this one, abort it!

                    if (markAsFinished)
                        _imagesLastRunningTask.Remove(image);
                }
                else
                {
                    /* ERROR! */
                    System.Diagnostics.Debug.WriteLine("EndLoading() - unexpected condition: there is no running task for this image!");
                }

                image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (image.RenderTransform == _loadingAnimationTransform)
                    {
                        image.RenderTransform = MatrixTransform.Identity;
                    }

                    if (Loader.GetErrorDetected(image) && Loader.GetDisplayErrorThumbnailOnError(image))
                    {
                        imageSource = _errorThumbnail;
                    }

                    image.Source = imageSource;

                    if (markAsFinished)
                    {
                        // Set IsLoading Pty
                        Loader.SetIsLoading(image, false);
                    }
                }));
            }
        }



        private ImageSource GetBitmapSource(LoadImageRequest loadTask, DisplayOptions loadType)
        {
            DisplayOptions lt = loadType;
            Image image = loadTask.Image;
            string source = loadTask.Source;
            string nSource = source;
            string cacheFile = Path.Combine(AppStateSettings.CacheFolder, "Media", nSource.GetSHA1Hash().ToString() + "s" + loadTask.CachHeight + ".png"); // REVIEW TODO: Used Path instead of String concat.

            if (File.Exists(cacheFile))
            {
                nSource = cacheFile;
                lt = DisplayOptions.FullResolution;
                //return new BitmapImage(new Uri("file://" + cacheFile));
            }

            ImageSource imageSource = null;

            if (!string.IsNullOrWhiteSpace(nSource))
            {
                Stream imageStream = null;

                SourceType sourceType = SourceType.LocalDisk;

                image.Dispatcher.Invoke(new ThreadStart(delegate
                {
                    sourceType = Loader.GetSourceType(image);
                }));


                try
                {
                    if (loadTask.Stream == null)
                    {
                        ILoader loader = LoaderFactory.CreateLoader(sourceType, nSource);
                        imageStream = loader.Load(nSource);
                        loadTask.Stream = imageStream;
                    }
                    else
                    {
                        imageStream = new MemoryStream();
                        loadTask.Stream.Position = 0;
                        loadTask.Stream.CopyTo(imageStream);
                        imageStream.Position = 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                if (imageStream != null)
                {
                    try
                    {
                        if (lt == DisplayOptions.Preview)
                        {
                            BitmapFrame bitmapFrame = BitmapFrame.Create(imageStream);
                            imageSource = bitmapFrame.Thumbnail;

                            if (imageSource == null) // Preview it is not embedded into the file
                            {
                                // we'll make a thumbnail image then ... (too bad as the pre-created one is FAST!)
                                TransformedBitmap thumbnail = new TransformedBitmap();
                                thumbnail.BeginInit();
                                thumbnail.Source = bitmapFrame as BitmapSource;

                                // we'll make a reasonable sized thumnbail with a height of 240
                                int pixelH = bitmapFrame.PixelHeight;
                                int pixelW = bitmapFrame.PixelWidth;
                                int decodeH = (int)loadTask.CachHeight;
                                int decodeW = (bitmapFrame.PixelWidth * decodeH) / pixelH;
                                double scaleX = decodeW / (double)pixelW;
                                double scaleY = decodeH / (double)pixelH;
                                TransformGroup transformGroup = new TransformGroup();
                                transformGroup.Children.Add(new ScaleTransform(scaleX, scaleY));
                                thumbnail.Transform = transformGroup;
                                thumbnail.EndInit();

                                // this will disconnect the stream from the image completely ...
                                WriteableBitmap writable = new WriteableBitmap(thumbnail);


                                BitmapFrame frame = BitmapFrame.Create(writable);
                                var encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(frame);

                                using (var stream = File.Create(cacheFile))
                                {
                                    encoder.Save(stream);
                                }


                                writable.Freeze();
                                imageSource = writable;
                            }
                        }
                        else if (lt == DisplayOptions.FullResolution)
                        {
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = imageStream;
                            bitmapImage.EndInit();
                            imageSource = bitmapImage;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                if (imageSource == null)
                {
                    image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Loader.SetErrorDetected(image, true);
                    }));
                }
                else
                {
                    imageSource.Freeze();

                    image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Loader.SetErrorDetected(image, false);
                    }));
                }
            }
            else
            {
                image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Loader.SetErrorDetected(image, false);
                }));
            }

            return imageSource;
        }

        [DebuggerStepThrough]
        private void LoaderThreadThumbnails()
        {
            do
            {
                _loaderThreadThumbnailEvent.WaitOne();

                LoadImageRequest loadTask = null;

                do
                {

                    lock (_loadThumbnailStack)
                    {
                        loadTask = _loadThumbnailStack.Count > 0 ? _loadThumbnailStack.Pop() : null;
                    }

                    if (loadTask == null || loadTask.IsCanceled) continue;
                    var displayOption = DisplayOptions.Preview;

                    loadTask.Image.Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        displayOption = Loader.GetDisplayOption(loadTask.Image);
                    }));

                    var bitmapSource = GetBitmapSource(loadTask, DisplayOptions.Preview);

                    switch (displayOption)
                    {
                        case DisplayOptions.Preview:
                            EndLoading(loadTask.Image, bitmapSource, loadTask, true);
                            break;
                        case DisplayOptions.FullResolution:
                            EndLoading(loadTask.Image, bitmapSource, loadTask, false);
                            lock (_loadNormalStack)
                            {
                                _loadNormalStack.Push(loadTask);
                            }
                            _loaderThreadNormalSizeEvent.Set();
                            break;
                    }
                } while (loadTask != null);

            } while (true);
        }

        [DebuggerStepThrough]
        private void LoaderThreadNormalSize()
        {
            do
            {
                try
                {
                    _loaderThreadNormalSizeEvent.WaitOne();

                    LoadImageRequest loadTask = null;

                    do
                    {

                        lock (_loadThumbnailStack)
                        {
                            loadTask = _loadNormalStack.Count > 0 ? _loadNormalStack.Pop() : null;
                        }

                        if (loadTask != null && !loadTask.IsCanceled)
                        {
                            ImageSource bitmapSource = GetBitmapSource(loadTask, DisplayOptions.FullResolution);
                            EndLoading(loadTask.Image, bitmapSource, loadTask, true);
                        }

                    } while (loadTask != null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            } while (true);
        }

        #endregion
    }
}
