#region

using System.Text.RegularExpressions;
using Caliburn.Micro;
using csCommon.Types.DataServer.PoI.IO;
using csGeoLayers;
using csGeoLayers.Plugins.DemoScript;
using csGeoLayers.Plugins.LocalTileLayer;
using csPresenterPlugin.Layers;
using csPresenterPlugin.Utils;
using csShared;
using csShared.Utils;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Behaviors;
using ESRI.ArcGIS.Client.Geometry;
using Microsoft.Surface.Presentation.Controls;
using nl.tno.cs.presenter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using System.Windows.Media;
using PdfToImage;
using ESRI.ArcGIS.Client.Projection;

#endregion

namespace csPresenterPlugin.Controls
{
    public class ItemSelectedArgs : EventArgs
    {
        public ItemClass Item { get; set; }

        public Point Pos { get; set; }
    }

    public class ExplorerPathConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    //public class SelectFolderCommand : ICommand
    //{
    //    public void Execute(object parameter)
    //    {
    //        Debug.WriteLine("Hello, world");
    //    }

    //    public bool CanExecute(object parameter)
    //    {
    //        return true;
    //    }
    //    public event EventHandler CanExecuteChanged;
    //}
    public delegate void ExtensionDelegate(ItemClass item);

    public class MetroExplorer : Control
    {
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;
        public static readonly DependencyProperty ActivePathProperty =
            DependencyProperty.Register("ActivePath", typeof(string), typeof(MetroExplorer),
                                        new UIPropertyMetadata(""));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(MetroExplorer),
                                        new UIPropertyMetadata("Title"));

        public Brush TextBrush
        {
            get { return (Brush)GetValue(TextBrushProperty); }
            set { SetValue(TextBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBrushProperty =
            DependencyProperty.Register("TextBrush", typeof(Brush), typeof(MetroExplorer), new PropertyMetadata(Brushes.Black));

        public PresenterPlugin Plugin { get; set; }

        public bool CanShowNextPrevious
        {
            get { return (bool)GetValue(CanShowNextPreviousProperty); }
            set { SetValue(CanShowNextPreviousProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanShowNextPrevious.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanShowNextPreviousProperty =
            DependencyProperty.Register("CanShowNextPrevious", typeof(bool), typeof(MetroExplorer),
                                        new UIPropertyMetadata(false));

        // Using a DependencyProperty as the backing store for CanSelectFolder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanSelectFolderProperty =
            DependencyProperty.Register("CanSelectFolder", typeof(bool), typeof(MetroExplorer),
                                        new UIPropertyMetadata(true));

        public static readonly DependencyProperty NextVisibilityProperty =
            DependencyProperty.Register("NextVisibility", typeof(Visibility), typeof(MetroExplorer),
                                        new UIPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty PreviousVisibilityProperty =
            DependencyProperty.Register("PreviousVisibility", typeof(Visibility), typeof(MetroExplorer),
                                        new UIPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty TitleVisibilityProperty =
            DependencyProperty.Register("TitleVisibility", typeof(Visibility), typeof(MetroExplorer),
                                        new UIPropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register("Path", typeof(string), typeof(MetroExplorer),
                                        new UIPropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(Folder), typeof(MetroExplorer), new UIPropertyMetadata(null));

        public static readonly DependencyProperty FoldersProperty =
            DependencyProperty.Register("Folders", typeof(ObservableCollection<Folder>), typeof(MetroExplorer),
                                        new UIPropertyMetadata(new ObservableCollection<Folder>()));

        public static readonly DependencyProperty CanSelectProperty =
            DependencyProperty.Register("CanSelect", typeof(bool), typeof(MetroExplorer), new UIPropertyMetadata(true));

        private readonly Dictionary<string, ExtensionDelegate> _extensions = new Dictionary<string, ExtensionDelegate>();

        private static readonly string[] _images = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
        private static readonly string[] _scripts = { ".mcs" };
        private static readonly string[] _videos = { ".avi", ".wmv", ".mp4", ".m4v", ".mov", ".mkv", ".mpeg", ".mpg", ".vob" };

        private static string[] PoiServiceExtensions
        {
            get
            {
                IEnumerable<string> supportedExtensions = PoiServiceImporters.Instance.GetSupportedExtensions();
                string[] ret = new string[supportedExtensions.Count()];
                int i = 0;
                foreach (var supportedExtension in supportedExtensions)
                {
                    ret[i] = supportedExtension;
                    if (!ret[i].StartsWith("."))
                    {
                        ret[i] = "." + ret[i];
                    }
                    i++;
                }
                return ret;
            }
        }

        private readonly Dictionary<string, PoiService> dataServices = new Dictionary<string, PoiService>();
        private readonly Dictionary<string, LocalTileLayer> localTileLayers = new Dictionary<string, LocalTileLayer>();
        public bool AttractorMode;
        public readonly Dictionary<string, List<ItemClass>> Cache = new Dictionary<string, List<ItemClass>>();
        public bool HasFullLayer;
        public ItemsControl IcTitle;
        public PlaylistManager PlManager = new PlaylistManager(null);
        public GroupLayer PresenterLayers;
        public Envelope Previous;

        public DirectoryInfo StartDirectory;

        private List<string> history = new List<string>();
        private string       startPath;
        private List<string> additionalFolders = new List<string>();
        private bool         gsBusy;
        public string[]      images = _images;
        public SurfaceButton sbBack;
        public string[]      scripts = _scripts;
        public string[]      videos = _videos;


        public MetroExplorer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroExplorer), new FrameworkPropertyMetadata(typeof(MetroExplorer)));
        
            var cachefolder = System.IO.Path.Combine(AppStateSettings.CacheFolder, "presenter"); // REVIEW TODO: Used Path instead of String concat.
            if (!Directory.Exists(cachefolder)) Directory.CreateDirectory(cachefolder);
            Loaded += MetroExplorer_Loaded;
        }

        private void MetroExplorer_Loaded(object sender, RoutedEventArgs e)
        {
            if (Plugin!=null && Plugin.DataServer != null)
            {
                Plugin.DataServer.Tapped += DataServer_Tapped;
            }
        }

        private static readonly Regex Regex = new Regex(
            "^\\[url=file:\\/\\/\\/?([^\\]]*)\\].*$",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );

        private void DataServer_Tapped(object sender, TappedEventArgs e) {
            if (!e.Content.Labels.ContainsKey("PresenterPath") ||
                string.IsNullOrEmpty(e.Content.Labels["PresenterPath"])) return;
            var presenterPath = e.Content.Labels["PresenterPath"];
            // Check if folder is part of the startup path
            if (presenterPath.ToLower().Contains("[/url]")) {
                var m = Regex.Match(presenterPath);
                if (m.Success) presenterPath = m.Groups[1].Value;
            }
            
            // Check if we need to navigate to a subfolder of the activate path
            var p = System.IO.Path.Combine(StartPath, presenterPath);
            if (Directory.Exists(p)) {
                SelectFolder(p);
                return;
            }
            // Check if we need to navigate to a subfolder of the active path
            p = System.IO.Path.Combine(ActivePath, presenterPath);
            if (Directory.Exists(p)) {
                SelectFolder(p);
                return;
            }
            // Check if we need to navigate to a subfolder of start path
            p = System.IO.Path.Combine(StartPath, presenterPath);
            if (Directory.Exists(p)) {
                SelectFolder(p);
                return;
            }
            // Check if we need to navigate to a fully speficied path
            if (Directory.Exists(presenterPath))
            {
                SelectFolder(presenterPath);
            }
            else {
                // Check if the folder is a subfolder of one of the addiotional folders.
                foreach (
                    var path in AdditionalFolders.Select(folder => System.IO.Path.Combine(folder, presenterPath)).Where(Directory.Exists)) {
                    SelectFolder(path);
                    return;
                }
            }
            // Check to see if we are dealing with a single file
            var file = System.IO.Path.Combine(Path, presenterPath);
            if (File.Exists(file) && (file.EndsWith("exe") || file.EndsWith("bat"))) {
                Process.Start(file);
                return;
            }
            file = System.IO.Path.Combine(ActivePath, presenterPath);
            if (File.Exists(file) && (file.EndsWith("exe") || file.EndsWith("bat"))) {
                Process.Start(file);
                return;
            }
            file = presenterPath;
            if (File.Exists(file) && (file.EndsWith("exe") || file.EndsWith("bat"))) Process.Start(file);
        }

        //void MetroExplorer_Loaded(object sender, RoutedEventArgs e)
        //{
        //    TextBrush = AppStateSettings.Instance.Config.GetBrush("Presenter.TextBrush", Brushes.DeepPink);
        //}

        public bool CanSelectFolder
        {
            get { return (bool)GetValue(CanSelectFolderProperty); }
            set { SetValue(CanSelectFolderProperty, value); }
        }

        public ItemClass CurrentItem
        {
            get { return PlManager.CurrentItem; }
        }

        public string ActivePath
        {
            get { return (string)GetValue(ActivePathProperty); }
            set { SetValue(ActivePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActivePath.  This enables animation, styling, binding, etc...

        public DemoScript DemoScript { get; set; }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...

        public Visibility NextVisibility
        {
            get { return (Visibility)GetValue(NextVisibilityProperty); }
            set { SetValue(NextVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NextVisibility.  This enables animation, styling, binding, etc...

        public Visibility PreviousVisibility
        {
            get { return (Visibility)GetValue(PreviousVisibilityProperty); }
            set { SetValue(PreviousVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PreviousVisibility.  This enables animation, styling, binding, etc...

        public bool CanSelect
        {
            get { return (bool)GetValue(CanSelectProperty); }
            set { SetValue(CanSelectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanSelect.  This enables animation, styling, binding, etc...

        public Visibility TitleVisibility
        {
            get { return (Visibility)GetValue(TitleVisibilityProperty); }
            set { SetValue(TitleVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TitleVisibility.  This enables animation, styling, binding, etc...

        public string LastPath { get; set; }

        public List<string> History
        {
            get { return history; }
            set { history = value; }
        }

        public string StartPath
        {
            get { return startPath; }
            set
            {
                if (!Directory.Exists(value)) return;
                var di = new DirectoryInfo(value);
                startPath = di.FullName;
                StartDirectory = di;
                SelectFolder(startPath);
               
            }
        }

        public new string Parent { get; set; }

        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Path.  This enables animation, styling, binding, etc...

        public Folder Items
        {
            get { return (Folder)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...

        public ObservableCollection<Folder> Folders
        {
            get { return (ObservableCollection<Folder>)GetValue(FoldersProperty); }
            set { SetValue(FoldersProperty, value); }
        }

        public Dictionary<string, ExtensionDelegate> Extensions
        {
            get { return _extensions; }
        }

        public Queue<FileInfo> Pdfs { get; set; }

        public List<string> AdditionalFolders
        {
            get { return additionalFolders; }
            set
            {
                additionalFolders = value;
                Cache.Clear();
                if (StartPath != null) SelectFolder(StartPath);
            }
        }

        public event EventHandler<ItemSelectedArgs> ActivePathChanged;

        public static void OnPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
        }

        // Using a DependencyProperty as the backing store for Folders.  This enables animation, styling, binding, etc...

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Pdfs = new Queue<FileInfo>();

            if (Execute.InDesignMode) return;
            PresenterLayers = AppStateSettings.Instance.ViewDef.FindOrCreateGroupLayer("Presenter");

            Folders = new ObservableCollection<Folder>();

            if (StartPath != null)
            {
                StartDirectory = new DirectoryInfo(StartPath);
                SelectFolder(StartPath);
            }

            sbBack = GetTemplateChild("Back") as SurfaceButton;
            var sbNext = GetTemplateChild("Next") as SurfaceButton;
            var sbPrevious = GetTemplateChild("Previous") as SurfaceButton;
            IcTitle = GetTemplateChild("icTitle") as ItemsControl;

            if (sbBack != null) sbBack.Click += SbBackClick;

            if (sbNext != null) sbNext.Click += SbNextClick;
            if (sbPrevious != null) sbPrevious.Click += SbPreviousClick;

            UpdateTitle();
        }

        private readonly int pdfImageWidth = AppState.Config.GetInt("Presenter.Pdf.ImageWidth", 800);
        private readonly int pdfResolution = AppState.Config.GetInt("Presenter.Pdf.Resolution", 300);

        public void AddPdfDownload(FileInfo p)
        {
            if (Pdfs == null) Pdfs = new Queue<FileInfo>();
            Pdfs.Enqueue(p);

            if (gsBusy) return;
            gsBusy = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                while (Pdfs.Count > 0)
                {
                    try
                    {
                        var pdf = Pdfs.Dequeue();
                        var nd = pdf.DirectoryName + "\\^" + pdf.Name;
                        Directory.CreateDirectory(nd);
                        var converter = new PDFConvert
                        {
                            OutputToMultipleFile = true,
                            FirstPageToConvert   = -1,
                            LastPageToConvert    = -1,
                            FitPage              = false,
                            JPEGQuality          = 10,
                            OutputFormat         = "png16m",
                            Width                = pdfImageWidth,
                            ResolutionX          = pdfResolution,
                            ResolutionY          = pdfResolution
                        };

                        var input = new FileInfo(pdf.FullName);
                        var output = string.Format("{0}\\{1}{2}", nd, input.Name, ".png");
                        //If the output file exist alrady be sure to add a random name at the end until is unique!
                        while (File.Exists(output))
                        {
                            output = output.Replace(".png", string.Format("{1}{0}", ".png", DateTime.Now.Ticks));
                        }
                        //Just avoid this code, isn't working yet
                        //if (checkRedirect.Checked)
                        //{
                        //    Image newImage = converter.Convert(input.FullName);
                        //    Converted = (newImage != null);
                        //    if (Converted)
                        //        pictureOutput.Image = newImage;
                        //}
                        //else
                        converter.Convert(input.FullName, output);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Presenter", "Error creating pdf", e.Message, Logger.Level.Error);
                    }
                    //Reload();
                }
                gsBusy = false;
                Reload();
            });
        }

        public void InitPlaylistManager()
        {
            PlManager = new PlaylistManager(DemoScript);
            PlManager.ItemFinishedEvent += PlManagerItemFinishedEvent;
            //PlManager.PlaylistFinishedEvent += new PlaylistManager.PlaylistFinished(PlManager_PlaylistFinishedEvent);
        }

        private void PlManager_PlaylistFinishedEvent()
        {
            // Execute.OnUIThread(() =>
            //     {
            if (!AttractorMode) return;
            //PlManager.Stop();
            //PlManager.ClearList();
            var items = GetAttractorItemList(StartPath);
            foreach (var itm in items)
                PlManager.AddItem(itm);
            PlManager.Play();
            Execute.OnUIThread(() => { ActivePath = StartPath; });
            //   });
        }

        private void PlManagerItemFinishedEvent(ItemClass ic)
        {
            Execute.OnUIThread(() =>
            {
                ActivePath = "";
                if (ActivePathChanged == null || ic == null) return;
                ic.Explorer = this;
                ActivePathChanged(this, new ItemSelectedArgs { Item = ic });
            });
        }

        private void SbNextClick(object sender, RoutedEventArgs e)
        {
            //PlManager.Next();
            PlayNext();
        }

        private void SbPreviousClick(object sender, RoutedEventArgs e)
        {
            //PlManager.Next();
            PlayPrevious();
        }

        private void SbBackClick(object sender, RoutedEventArgs e)
        {
            PlManager.ClearList();
            PlManager.Stop();
            if (Path == null) return;
            var di = new DirectoryInfo(Path);
            foreach (var s in AdditionalFolders)
            {
                if (s == di.FullName)
                {
                    SelectFolder(StartPath);
                    return;
                }
                if (s == di.Parent.FullName)
                {
                    SelectFolder(s);
                    return;
                }
            }
            if (di.FullName != StartDirectory.FullName) SelectFolder(di.Parent.FullName);

            //if (History.Count > 0)
            //{
            //    string l = History.Last();
            //    History.RemoveAt(History.Count - 1);
            //    SelectFolder(l);
            //}
            //else
            //    SelectFolder(LastPath);
        }

        public void Up()
        {
            PlManager.ClearList();
            PlManager.Stop();
            SelectFolder(StartDirectory.FullName);
        }

        public void PlayNext()
        {
            var curitem = PlManager.CurrentItem;
            var path = curitem.Path.Remove(curitem.Path.LastIndexOf("\\"));
            var items = GetOrderedItemListCurrentFolder(path).Where(k => k.Type != ItemType.folder).ToList();
            //List<ItemClass> items = GetOrderedItemList(Path);

            if (curitem == null) return;
            var tempi = items.FirstOrDefault(k => k.Name == curitem.Name && k.Visible);
            var idx = items.IndexOf(tempi) + 1;
            var nextitem = items[idx];

            if (!SameDirectoryPath(tempi, nextitem)) return;
            PreviousVisibility = (!CanShowNextPrevious && idx == 0) ? Visibility.Collapsed : Visibility.Visible;
            NextVisibility = (!CanShowNextPrevious && items.Count == idx + 1)
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (items.Count > idx) PlManager.AddItem(items[idx]);
            PlManager.Next();
        }

        public static bool SameDirectoryPath(ItemClass source, ItemClass destination)
        {
            var sd = source.Path.Split('\\');
            var dd = destination.Path.Split('\\');
            if (sd.Any() && dd.Any())
                return sd[sd.Length - 2] == dd[dd.Length - 2];
            return false;
        }

        public void PlayPrevious()
        {
            var curitem = PlManager.CurrentItem;
            if (curitem == null) return;
            var path = curitem.Path.Remove(curitem.Path.LastIndexOf("\\"));
            var items = GetOrderedItemListCurrentFolder(path).Where(k => k.Type != ItemType.folder).ToList();
            //List<ItemClass> items = GetOrderedItemList(Path);

            var tempi = items.FirstOrDefault(k => k.Name == curitem.Name && k.Visible);
            var idx = items.IndexOf(tempi) - 1;
            var nextitem = items[idx];

            if (!SameDirectoryPath(tempi, nextitem)) return;
            PreviousVisibility = (!CanShowNextPrevious || idx == 0) ? Visibility.Collapsed : Visibility.Visible;
            NextVisibility = (!CanShowNextPrevious || items.Count == idx + 1)
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (idx >= 0 && items.Count > idx)
                PlManager.AddItem(items[idx]);
            PlManager.Next();
        }

        public void PlayAll()
        {
            PlManager.ClearList();
            var items = GetOrderedItemList(Path);
            foreach (var itm in items)
                PlManager.AddItem(itm);
            //_plManager.AddItem(item);
            PlManager.Play();
        }

        public void PlayAttractor()
        {
            AttractorMode = true;
            PlManager.ClearList();
            var items = GetAttractorItemList(Path);
            foreach (var itm in items)
                PlManager.AddItem(itm);
            //_plManager.AddItem(item);
            PlManager.Play();
        }

        public void StopAttractor()
        {
            AttractorMode = false;
            PlManager.Items.Clear();
            PlManager.Stop();
            DemoScript.CloseAll();
        }

        public void SelectItem(ItemClass item, double x = 0, double y = 0)
        {
            if (item == null)
            {
                ActivePath = "";
            }
            else
            {
                //var r = AppStateSettings.Instance.ViewDef.MapControl.Resolution;
                ActivePath = item.Path;
                if (item.Type == ItemType.folder || item.Type == ItemType.shortcut)
                {
                    SelectFolder(item.Path);
                    return;
                }
                if (item.Type == ItemType.web)
                {
                    AppState.TriggerScriptCommand(this, "web:" + item.Path);
                    //Process.Start(item.Path);
                }
                if (item.Type == ItemType.batch)
                {
                    Process.Start(item.Path);
                }
                if (item.Type == ItemType.script)
                {
                    var pth = new FileInfo(item.Path);
                    if (pth.Directory != null) {
                        //                        List<ItemClass> items = GetOrderedItemList(pth.Directory.FullName);
                        var items = GetOrderedItemListCurrentFolder(pth.Directory.FullName)
                                .Where(k => k.Type != ItemType.folder && k.Visible)
                                .ToList();
                        var tempi = items.FirstOrDefault(k => k.Path == item.Path && k.Visible);
                        var idx = items.IndexOf(tempi);

                        PreviousVisibility = (!CanShowNextPrevious || idx == 0)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                        NextVisibility = (!CanShowNextPrevious || items.Count == idx + 1)
                            ? Visibility.Collapsed
                            : Visibility.Visible;

                        PlManager.AddItem(item);
                        if (PlManager.Items.Count == 1)
                            PlManager.Play();
                        else
                            PlManager.Next();
                    }

                    //PlManager.ClearList();
                    //var pth = new FileInfo(item.Path);
                    //var items = GetOrderedItemList(pth.Directory.FullName);
                    ////foreach(var itm in items)
                    ////    PlManager.AddItem(itm);
                    //PlManager.AddItem(item);
                    //var idx = items.IndexOf(items.FirstOrDefault(k=>k.Name == item.Name));
                    //PreviousVisibility = idx == 0 ? Visibility.Collapsed : Visibility.Visible;
                    //NextVisibility = items.Count == idx + 1 ? Visibility.Collapsed : Visibility.Visible;
                    //PlManager.Play();
                    ////PlManager.Next();
                }

                //NextVisibility = Visibility.Visible;
                //PreviousVisibility = Visibility.Visible;
            }
            var a = new ItemSelectedArgs { Item = item };
            if (item != null) item.Explorer = this;

            if (x != 0) a.Pos = new Point(x, y);
            OnActivePathChanged(a);
        }

        private void OnActivePathChanged(ItemSelectedArgs a) {
            var handler = ActivePathChanged;
            if (handler != null) handler(this, a);
        }

        public ItemClass ActiveConfig;
        public ItemClass ActiveBackground;

        public ItemClass GetActiveBackground(string path)
        {
            while (true)
            {
                var di = new DirectoryInfo(path);
                var visible = GetItems(path).Where(k => k.Type == ItemType.videobackground).ToList();
                if (visible.Any()) return visible.First();

                if (di.Parent == null || path == StartDirectory.FullName || AdditionalFolders.Contains(path)) return null;
                var parent = di.Parent.FullName;
                path = parent;
            }
        }

        public ItemClass GetActiveConfig(string path)
        {
            while (true)
            {
                var di = new DirectoryInfo(path);
                var visible = GetItems(path).Where(k => k.Type == ItemType.config).ToList();
                if (visible.Any()) return visible.First();

                if (di.Parent == null || path == StartDirectory.FullName || AdditionalFolders.Contains(path)) return null;
                var parent = di.Parent.FullName;
                path = parent;
            }
        }

        public void SetBackground(ItemClass bg)
        {
            ActiveBackground = bg;
            AppState.ViewDef.BaseLayers.Opacity = (bg == null) ? 1 : 0;
            if (bg != null)
            {
                AppState.TriggerScriptCommand(this, "background:" + bg.Path);
            }
        }

        internal void SelectFolder(string p)
        {
            History.Add(Path);
            LastPath = Path;
            Path = p;

            GetFolders();
            ActiveConfig = GetActiveConfig(Path);
            var bg = GetActiveBackground(Path);
            if (bg != ActiveBackground) SetBackground(bg);

            var slb = GetTemplateChild("slbFolders") as SurfaceListBox;
            if (slb != null) slb.ItemsSource = Folders;

            //MetroFolderList _items = GetTemplateChild("mflItems") as MetroFolderList;
            //_items.Folder = Items;

            UpdateTitle();
            if (p != null && p.Contains("+"))
            {
                var items = GetOrderedItemListCurrentFolder(p).Where(k => k.Type != ItemType.folder).ToList();
                if (items.Any())
                {
                    SelectItem(items.First());
                }
            }
            //if (p!=null && p.Contains("+")) PlayAll();

            if (PresenterLayers == null) return;
            foreach (var i in PresenterLayers.ChildLayers)
            {
                var presenterLayer = i as PresenterLayer;
                if (presenterLayer == null) continue;
                foreach (var g in presenterLayer.Graphics.Graphics)
                {
                    var pa = g.Attributes["path"];
                    var fl = Convert.ToBoolean(g.Attributes["fulllayer"]);

                    if (!fl && pa.ToString() == Path)
                    {
                        AppStateSettings.Instance.ViewDef.MapControl.PanTo(g.Geometry);
                        AppStateSettings.Instance.ViewDef.MapControl.ZoomToResolution(3);
                    }
                }
            }

            CheckDataServices(Path);
            CheckLocalTileLayers(Path);

            if (ActivePathChanged != null)
                ActivePathChanged(this, new ItemSelectedArgs { Item = new ItemClass { Path = p, Type = ItemType.folder } });
        }

        private void GetActiveLocalTileLayer(string path, ref List<string> active) {
            while (true) {
                var di = new DirectoryInfo(path);
                var tilelayers = GetItems(path).Where(k => k.Type == ItemType.tilelayer);
                foreach (var v in tilelayers) {
                    if (active.Contains(v.Path)) continue;
                    active.Add(v.Path);
                    return;
                }
                if (di.Parent == null || path == StartDirectory.FullName || AdditionalFolders.Contains(path)) return;

                //var maplayers = GetItems(path).Where(k => k.Type == ItemType.maplayer);
                //foreach (var v in maplayers)
                //{
                //    if (!active.Contains(v.Path)) active.Add(v.Path);
                //}
                if (di.Parent == null || path == StartDirectory.FullName || AdditionalFolders.Contains(path)) return;

                var parent = di.Parent.FullName;

                path = parent;
            }
        }

        private void CheckLocalTileLayers(string path)
        {
            var active = new List<string>();
            GetActiveLocalTileLayer(path, ref active);

            if (ActiveConfig != null)
            {
                Interaction.GetBehaviors(AppStateSettings.Instance.ViewDef.MapControl).Clear();

                if (ActiveConfig.Config.ContainsKey("extent"))
                {
                    AppStateSettings.Instance.ViewDef.MapControl.Extent = (Envelope)new EnvelopeConverter().ConvertFromString(ActiveConfig.Config["extent"]);
                }
                
                if (ActiveConfig.Config.ContainsKey("map"))
                {
                    AppStateSettings.Instance.ViewDef.ChangeMapType(ActiveConfig.Config["map"]);
                    if (localTileLayers.Any()) localTileLayers.ForEach(k => k.Value.Visible = false);
                    AppStateSettings.Instance.ViewDef.BaseLayer.Visible = true;
                    Interaction.GetBehaviors(AppStateSettings.Instance.ViewDef.MapControl).Clear();
                    //AppStateSettings.Instance.ViewDef.MapControl.Extent = new Envelope(-20037396.6730682,
                    //                                                                   -11321250.4653167,
                    //                                                                   20076755.7709922,
                    //                                                                   11242960.2844672);

                    //Interaction.GetBehaviors(AppStateSettings.Instance.ViewDef.MapControl).Add(new ConstrainExtentBehavior
                    //{
                    //    ConstrainedExtent = AppStateSettings.Instance.ViewDef.MapControl.Extent
                    //});

                    return;
                }
                
            }
           

            foreach (var ltl in localTileLayers)
            {
                if (!active.Contains(ltl.Key) && ltl.Value.Visible)
                {
                    AppStateSettings.Instance.ViewDef.BaseLayer.Visible = true;
                    ltl.Value.Visible = false;
                    Interaction.GetBehaviors(AppStateSettings.Instance.ViewDef.MapControl).Clear();
                }
                
                
                Interaction.GetBehaviors(AppStateSettings.Instance.ViewDef.MapControl).Add(new ConstrainExtentBehavior
                {
                    ConstrainedExtent = AppStateSettings.Instance.ViewDef.MapControl.Extent
                });

                if (!active.Contains(ltl.Key) || ltl.Value.Visible) continue;
                AppStateSettings.Instance.ViewDef.BaseLayer.Visible = false;
                ltl.Value.Visible = true;
                
                
            }
        }

        public event EventHandler Refreshed;

        public void AddExtension(string ext, ExtensionDelegate del)
        {
            Extensions.Add(ext, del);
            if (Refreshed != null) Refreshed(this, null);
        }

        private void GetFolders()
        {
            if (Path == null) return;
            TextBrush = AppStateSettings.Instance.Config.GetBrush("Presenter.TextBrush", Brushes.DeepPink);
            Folders.Clear();
            var l = GetItems(Path);
            var di = new DirectoryInfo(Path);
            Title = di.Name;
            foreach (var d in l.Where(k => k.Type == ItemType.folder && k.Visible))
            {
                Folders.Add(new Folder(d, this) { SeeThru = d.SeeThru, TextBrush = TextBrush });
            }
            var me = new ItemClass { Path = Path, Explorer = this };
            //MetroFolderList _items = GetTemplateChild("mflItems") as MetroFolderList;
            Items = new Folder(me, this) { TitleVibility = Visibility.Collapsed, Templated = false };
            Folders.Add(Items);

            if (PresenterLayers == null) return;
            if (!HasFullLayer) Previous = AppStateSettings.Instance.ViewDef.MapControl.Extent;
            var tl = HasFullLayer;
            HasFullLayer = false;
            PresenterLayers.ChildLayers.Clear();
            AppStateSettings.Instance.ViewDef.BaseLayer.Opacity = 1;
            AppStateSettings.Instance.ViewDef.MapControl.IsEnabled = true;
            var mainLayer = l.FirstOrDefault(k => k.Type == ItemType.csvlayer && k.Name.ToLower() == "layer");
            if (mainLayer != null)
            {
                AddLayer(mainLayer.Path);
            }
            else
            {
                FindLayerUp();
            }

            // add any other layer
            foreach (var a in l.Where(k => k.Type == ItemType.csvlayer && k.Name.ToLower() != "layer"))
            {
                AddLayer(a.Path);
            }

            //foreach (var ds in l.Where(k => k.Type == ItemType.dataservice))
            //{
            //    if (Plugin != null && Plugin.DataServer != null)
            //    {
            //        var ps = Plugin.DataServer.AddLocalDataService(ds.Path.Replace(ds.Name,""),Mode.server, ds.Name);
            //        if (ps != null)
            //        {
            //        }
            //    }
            //}

            if (tl && !HasFullLayer) AppStateSettings.Instance.ViewDef.MapControl.Extent = Previous;
        }

        private void FindLayerUp()
        {
            if (!Directory.Exists(Path)) return;
            var di = new DirectoryInfo(Path);

            while (di != null && di.FullName != StartDirectory.FullName)
            {
                var f = di.EnumerateFiles("layer.csv");
                if (f.Any())
                {
                    AddLayer(f.First().FullName);
                    break;
                }
                di = di.Parent;
            }
        }

        private void GetActiveDataServices(string path, ref List<string> active, bool inherit)
        {
            while (true)
            {
                var di = new DirectoryInfo(path);
                var visible = GetItems(path).Where(k => k.Type == ItemType.dataservice);
                foreach (var v in visible)
                {
                    if (!active.Contains(v.Path)) active.Add(v.Path);
                }

                if (di.Parent == null || path == StartDirectory.FullName || AdditionalFolders.Contains(path) || !inherit)
                    return;
                var parent = di.Parent.FullName;

                path = parent;
            }
        }

        private void CheckDataServices(string path)
        {
            var active = new List<string>();

            var inherit = true;

            if (ActiveConfig != null)
            {
                if (ActiveConfig.Path == Path + "\\folder.config")
                {
                    inherit = !ActiveConfig.Config.ContainsKey("inheritlayers")
                              || bool.Parse(ActiveConfig.Config["inheritlayers"]);
                }
                else
                {
                    inherit = !ActiveConfig.Config.ContainsKey("subinherit")
                              || bool.Parse(ActiveConfig.Config["subinherit"]);
                }
            }

            GetActiveDataServices(path, ref active, inherit);
            foreach (var ds in dataServices)
            {
                if (ds.Key == null || ds.Value == null) continue;
                if (active.Contains(ds.Key) && !ds.Value.IsVisible)
                {
                    ds.Value.IsVisible = true;
                }
                if (!active.Contains(ds.Key) && ds.Value.IsVisible) ds.Value.IsVisible = false;
            }
        }

        private void RemoveDataService(string file)
        {
        }

        private void AddDataService(string file)
        {
            Console.WriteLine(file);
        }

        private void AddLayer(string a)
        {
            var pl = new PresenterLayer { LayerFile = a, Explorer = this };
            PresenterLayers.ChildLayers.Add(pl);
            pl.Initialize();
        }

        public List<ItemClass> GetAttractorItemList(string startPath)
        {
            var l = new List<ItemClass>();
            GetOrderedItemList(startPath, ref l);
            return l.Where(k => k.Attractor && k.Type != ItemType.folder).ToList();
        }

        public List<ItemClass> GetOrderedItemList(string startPath)
        {
            var l = new List<ItemClass>();
            GetOrderedItemList(startPath, ref l);
            return l;
        }

        public void GetOrderedItemList(string startPath, ref List<ItemClass> files)
        {
            if (!Directory.Exists(startPath)) return;
            var di = new DirectoryInfo(startPath);
            foreach (var d in di.GetDirectories())
            {
                if (startPath != d.FullName) GetOrderedItemList(d.FullName, ref files);
            }
            files.AddRange(GetItems(startPath));
        }

        public List<ItemClass> GetOrderedItemListCurrentFolder(string startPath)
        {
            return Directory.Exists(startPath) ? GetItems(startPath) : new List<ItemClass>();
        }

        public void EmptyCache()
        {
            Cache.Clear();

            var f = System.IO.Path.Combine(AppStateSettings.CacheFolder, "presenter"); // REVIEW TODO: Used Path instead of String concat.
            if (Directory.Exists(f))
            {
                var ff = Directory.GetFiles(f);
                AppStateSettings.Instance.TriggerNotification("Clearing cached files.\r\nThis may take some while...");
                foreach (var fa in ff)
                {
                    try
                    {
                        File.Delete(fa);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Presenter", "Error deleting file from cache", e.Message, Logger.Level.Error);
                    }
                }
            }

            f = System.IO.Path.Combine(AppStateSettings.CacheFolder, "Media"); // REVIEW TODO: Used Path instead of String concat.
            if (Directory.Exists(f))
            {
                var ff = Directory.GetFiles(f);
                AppStateSettings.Instance.TriggerNotification("Clearing cached media.\r\nThis may take some while...");
                foreach (var fa in ff)
                {
                    try
                    {
                        File.Delete(fa);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Presenter", "Error deleting file from cache", e.Message, Logger.Level.Error);
                    }
                }
            }

            SelectFolder(Path);
        }

        public List<ItemClass> GetItems(string folder)
        {
            if (Cache.ContainsKey(folder)) return Cache[folder];
            var l = new List<ItemClass>();
            var dir = new DirectoryInfo(folder);

            try
            {
                var di = dir.GetDirectories().ToList().OrderBy(k => k.Name).ToList();
                if (folder == StartPath && AdditionalFolders.Any())
                {
                    di.AddRange(from af in AdditionalFolders where Directory.Exists(af) select new DirectoryInfo(af));
                }
                foreach (var d in di)
                {
                    //if (!d.Name.StartsWith("_"))
                    {
                        var ic = new ItemClass { Name = d.Name, Path = d.FullName, Type = ItemType.folder };

                        if (d.Name.StartsWith("_")) ic.Visible = false;

                        if (ic.Name.ToLower() == "_tiles")
                        {
                            if (!localTileLayers.ContainsKey(d.FullName))
                            {
                                var ltl = new LocalTileLayer { Path = d.FullName, Visible = false };
                                AppStateSettings.Instance.ViewDef.Layers.ChildLayers.Add(ltl);
                                localTileLayers.Add(d.FullName, ltl);
                            }
                            ic.Type = ItemType.tilelayer;
                        }                        
                        if (ic.Name.Contains("^"))
                        {
                            ic.Presentation = true;
                            ic.Name = ic.Name.Replace("^", "");
                            ic.Type = ItemType.presentation;
                        }

                        l.Add(ic);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("MetroExplorer", "Error get directories", e.Message, Logger.Level.Error);
            }

            if (!dir.Exists) return l;

            var f = dir.GetFiles();

            if (f.Length > 0)
            {
                var fi = f.ToList().OrderBy(k => k.Name).ToList();
                foreach (var d in fi)
                {
                    var dd = d.Name.ToLower().Split('.');
                    var path = d.FullName;
                    var fileExtension = d.Extension.ToLower();
                    if (dd[0] != dir.Name.ToLower())
                    {
                        // TODO EV Transform to switch statement
                        if (fileExtension == ".config")
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                Type      = ItemType.config,
                                Attractor = false,
                                Visible   = false,
                                Name      = d.Name.Substring(0, d.Name.Length - 7)
                            };
                            ic.ReadConfig();
                            l.Add(ic);
                        }
                        if (fileExtension == ".pdf")
                        {
                            // check if dir exists
                            if (!l.Any(k => k.Name == d.Name && k.Type == ItemType.presentation))
                            {
                                AddPdfDownload(new FileInfo(path));
                            }
                        }
                        if (fileExtension == ".url")
                        {
                            var c = File.ReadAllLines(path);
                            if (c.Length > 0)
                            {
                                var ic = new ItemClass
                                {
                                    Path      = c[0],
                                    Type      = ItemType.web,
                                    Attractor = false,
                                    Visible   = true,
                                    Name      = d.Name.Substring(0, d.Name.Length - 4)
                                };
                                l.Add(ic);
                            }
                        }
                        if (fileExtension == ".link")
                        {
                            var c = File.ReadAllLines(path);
                            if (c.Length > 0)
                            {
                                var nd = new DirectoryInfo(Path + c[0]);
                                if (nd.Exists)
                                {
                                    var ic = new ItemClass
                                    {
                                        Path      = nd.FullName,
                                        Type      = ItemType.shortcut,
                                        Attractor = false,
                                        Visible   = true,
                                        Name      = d.Name.Substring(0, d.Name.Length - 5)
                                    };
                                    l.Add(ic);
                                }
                            }
                        }
                        if (fileExtension == ".bat")
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                Type      = ItemType.batch,
                                Attractor = false,
                                Visible   = true,
                                Name      = d.Name
                            };
                            l.Add(ic);
                        }
                        if (fileExtension == ".csv")
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                Type      = d.Name.StartsWith("shortcuts") ? ItemType.shortcutList : ItemType.csvlayer,
                                Attractor = false,
                                Visible   = false,
                                Name      = d.Name
                            };
                            l.Add(ic);
                        }
                        if (fileExtension.StartsWith(".htm"))
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                Type      = ItemType.website,
                                Attractor = false,
                                Visible   = true,
                                Name      = d.Name.Substring(0, d.Name.Length - 5)
                            };
                            l.Add(ic);
                        }
                        if (fileExtension == ".ds") // UNDONE: PoiServiceExtensions.Contains(fileExtension)) // == ".ds"))
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                Type      = ItemType.dataservice,
                                Attractor = false,
                                Visible   = false,
                                Name      = d.Name
                            };
                            l.Add(ic);
                            //if (!dataServices.ContainsKey(d.FullName)) {
                            if (!Plugin.DataServer.Services.Any(s => Equals(s.FileName, path))) {
                                var ps = Plugin.DataServer.AddLocalDataService(d.Directory.FullName, Mode.client, path, d.Directory.FullName);
                                ps.IsVisible = false;
                                ps.Tapped += PsTapped;
                                ps.AutoStart = true;
                                Plugin.DataServer.Services.Add(ps);
                                //Plugin.DataServer.Subscribe(ps);
                                if (!dataServices.ContainsKey(path)) {
                                    try {
                                        dataServices.Add(path, ps);
                                    }
                                    catch (SystemException e) {
                                        AppStateSettings.Instance.TriggerNotification("Error parsing " + path);
                                        AppStateSettings.Instance.TriggerNotification(e.Message);
                                        if (e.InnerException != null) AppStateSettings.Instance.TriggerNotification(e.InnerException.Message);
                                    }
                                    Dispatcher.Invoke(() => CheckDataServices(Path));
                                }
                            }
                        }

                        if (_scripts.Contains(fileExtension))
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                ImagePath = path,
                                Type      = ItemType.script,
                                Name      = dd[0]
                            };
                            l.Add(ic);
                        }
                        if (_images.Contains(fileExtension))
                        {
                            var ic = new ItemClass
                            {
                                Path      = path,
                                ImagePath = path,
                                Type      = ItemType.image,
                                Name      = dd[0]
                            };
                            l.Add(ic);
                        }
                        if (_videos.Contains(fileExtension))
                        {
                            if (dd[0].ToLower() == "_background")
                            {
                                var ic = new ItemClass
                                {
                                    Path = path,
                                    Type = ItemType.videobackground,
                                    Name = dd[0]
                                };
                                l.Add(ic);
                            }
                            else
                            {
                                var ic = new ItemClass
                                {
                                    Path = path,
                                    Type = ItemType.video,
                                    Name = dd[0]
                                };
                                l.Add(ic);
                            }
                        }
                    }
                    if (Extensions.ContainsKey(fileExtension))
                    {
                        var ic = new ItemClass { Path = path, ImagePath = "", Type = ItemType.unknown, Name = dd[0] };
                        l.Add(ic);
                    }
                }

                foreach (var i in l)
                {
                    if (i.Visible) i.Visible = !i.Name.StartsWith("_");
                    i.Attractor = i.Name.Contains("~");
                    i.Name = i.Name.Replace("~", "");

                    if (i.Name.Contains("!")) i.SeeThru = false;
                    if (i.SeeThru) i.Name = i.Name.Replace("!", "");

                    i.Name = i.Name.Replace("+", "");
                }

                var tbr = new List<ItemClass>();
                var currentDirectory = Directory.GetCurrentDirectory();
                foreach (var ic in l.Where(k => k.Type != ItemType.image))
                {
                    if (ic == null || ic.Name == null) continue;
                    var i = l.FirstOrDefault(k => k.Name != null && string.Equals(k.Name, ic.Name, StringComparison.InvariantCultureIgnoreCase) && k.Type == ItemType.image);
                    if (i == null)
                    {
                        switch (ic.Type)
                        {
                            case ItemType.web:
                                ic.ImagePath = System.IO.Path.Combine(currentDirectory, @"Images\html.jpg");
                                break;

                            case ItemType.video:
                                ic.ImagePath = System.IO.Path.Combine(currentDirectory, @"Images\video.jpg");
                                break;

                            case ItemType.website:
                                ic.ImagePath = System.IO.Path.Combine(currentDirectory, @"Images\info.jpg");
                                break;

                            case ItemType.batch:
                                ic.ImagePath = System.IO.Path.Combine(currentDirectory, @"Images\batch.png");
                                break;
                        }
                        continue;
                    }

                    tbr.Add(i);
                    ic.ImagePath = i.Path;
                }
                foreach (var i in tbr) l.Remove(i);

                // if a presentation has nog imagepath, use first file( must be image) in presentation folder
                foreach (var i in l.Where(k => k.Type == ItemType.presentation && k.ImagePath == null))
                {
                    var iff = Directory.GetFiles(i.Path);
                    if (iff.Length > 0)
                    {
                        i.ImagePath = iff.First();
                    }
                }
            }

            Cache[folder] = l;

            return l;
        }

        /// <summary>
        ///     Poi was tapped, look for 'Path' label and select folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PsTapped(object sender, TappedEventArgs e)
        {
            var ps = sender as PoiService;
            if (ps == null) return;
            ActivePath = ps.Folder;
            if (!e.Content.Labels.ContainsKey("Path")) return;
            var p = StartPath + "\\" + e.Content.Labels["Path"];
            if (Directory.Exists(p)) SelectFolder(p);
        }

        private void Reload()
        {
            Execute.OnUIThread(() =>
            {
                Cache.Clear();
                SelectFolder(StartPath);
            });
        }

        public void UpdateTitle()
        {
            if (!Directory.Exists(Path)) return;
            //SelectItem(null);
            var di = new DirectoryInfo(Path);
            var spi = new DirectoryInfo(StartPath);
            if (!Path.StartsWith(StartPath))
            {
                foreach (var s in AdditionalFolders)
                {
                    if (Path.StartsWith(s))
                    {
                        spi = new DirectoryInfo(s);
                        break;
                    }
                }
            }
            var dd = new List<DirectoryInfo> { di };
            while (di != null && (di.FullName != spi.FullName))
            {
                di = di.Parent;
                dd.Add(di);
            }
            if (di != null)
            {
                var p = di.FullName.Replace(spi.FullName, "");
                var bc = new List<string> { spi.Name };
                foreach (var s in p.Split('\\'))
                {
                    if (!string.IsNullOrEmpty(s)) bc.Add(s.CleanName());
                }
            }
            Title = "";

            if (IcTitle == null) return;
            IcTitle.Items.Clear();
            //icTitle.ItemsSource = bc;
            foreach (var s in dd)
            {
                IcTitle.Items.Insert(0, s);
                //if (s == dd.First()) {
                //    IcTitle.Items.Insert(0, s);
                //}
                //else {
                //    IcTitle.Items.Insert(0, s);
                //}
            }
        }

        public static void Reset()
        {
        }

        public void RefreshAll()
        {
            Cache.Clear();
            SelectFolder(Path);
            
        }
    }
}