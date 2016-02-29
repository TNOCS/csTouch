using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csGeoLayers.Plugins.DemoScript;
using csPresenterPlugin.Controls;
using csShared;
using csShared.Documents;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;
using csShared.Utils;
using nl.tno.cs.presenter;
using nl.tno.tnopresenter.Helpers;

namespace csPresenterPlugin.ViewModels
{    
    public class TnoPresenterViewModel : Screen
    {
        //private AttractDemo ad;

        private readonly Dictionary<string, Shortcut> shortcutsDictionary = new Dictionary<string, Shortcut>();
        private const string BaseMap = "Google Satellite";
        private bool scriptRunning;
        private Thread scriptThread;
        private TnoPresenterView view;

        public int Metroheight;

        public bool ScriptRunning
        {
            get { return scriptRunning; }
            set
            {
                scriptRunning = value;
                NotifyOfPropertyChange(() => ScriptRunning);
            }
        }

        public int MetroHeight
        {
            get { return Metroheight; }
            set
            {
                Metroheight = value;
                NotifyOfPropertyChange(() => MetroHeight);
            }
        }

        private static readonly AppStateSettings AppState = AppStateSettings.Instance;

        public MetroExplorer MetroExplorer { get; set; }

        public ITimelineManager TimelineManager { get; set; }


        public MapViewDef ViewDef
        {
            get
            {
                return AppState.ViewDef;
            }
        }

        public readonly List<string> AdditionalFolders = new List<string>();

        private FileSystemWatcher watcher = new FileSystemWatcher();

        public static DemoScript DemoScript { get; set; }

        #region IModule Members

        public string Name
        {
            get { return "TnoPresenter"; }
        }

        private PresenterPlugin plugin;

        public PresenterPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; NotifyOfPropertyChange(()=>Plugin); }
        }
        
      
        #endregion

        public void StartPlaylist()
        {
            MetroExplorer.PlayAll();
        }

        public void Reset()
        {
            MetroExplorer.Reset();
        }

        private static string presenterFolder;

        public static string PresenterFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(presenterFolder)) return presenterFolder;
                var c = AppStateSettings.Instance.Config.Get("Presenter.Path", "Presenter");
                //if (c == null) c = @"%TEMP%\cs\";
                //if (c[c.Length - 1] != '\\') c += @"\";
                presenterFolder = Environment.ExpandEnvironmentVariables(c);
                if (!Directory.Exists(presenterFolder)) Directory.CreateDirectory(presenterFolder);
                return presenterFolder;
            }
        }

        public void StartScript(ItemClass item)
        {
            MetroExplorer.NextVisibility = Visibility.Visible;
            MetroExplorer.PreviousVisibility = Visibility.Visible;
            DemoScript.ScriptPath = item.Path;
            if (scriptThread != null && scriptThread.IsAlive) scriptThread.Abort();
            scriptThread = new Thread(DemoScript.StartScript);
            DemoScript.ScriptStarted += (e, s) =>
                ScriptRunning = true;
            DemoScript.ScriptFinished += (e, s) => Execute.OnUIThread(() =>
            {
                ScriptRunning = false;
                AppState.ViewDef.ChangeMapType(BaseMap);
                AppState.ViewDef.StartTransition();

                DemoScript.ZoomTo(-19913273.2361002,
                    -16885380.7637054,
                    20161743.4494782,
                    15174632.5847574,
                    effect: true);
                MetroExplorer.UpdateTitle();
                MetroExplorer.NextVisibility =
                    Visibility.Collapsed;
            });
            scriptThread.Start();
        }

        public void Switch()
        {
            AppState.DockedFloatingElementsVisible = !AppState.DockedFloatingElementsVisible;
        }

        protected override void OnViewLoaded(object v)
        {
            base.OnViewLoaded(v);
            view                            = (TnoPresenterView) v;
            MetroExplorer                   = view.MetroExplorer2;;
            MetroExplorer.AdditionalFolders = AdditionalFolders;
            MetroExplorer.NextVisibility    = Visibility.Collapsed;
            MetroExplorer.ActivePathChanged += MetroExplorerActivePathChanged;
            MetroExplorer.DemoScript        = DemoScript;
            MetroExplorer.InitPlaylistManager();
            MetroExplorer.PlManager.PlaylistFinishedEvent += PlManagerPlaylistFinishedEvent;
            MetroExplorer.PlManager.PlaylistStartedEvent += PlManagerPlaylistStartedEvent;
            MetroExplorer.Foreground        = new SolidColorBrush(Colors.White);
            MetroExplorer.CanSelect         = false;
            MetroHeight                     = Convert.ToInt16(ConfigurationManager.AppSettings.Get("navigationheight"));;
            MetroExplorer.Plugin            = Plugin;
            var path                        = PresenterFolder;// "Presenter";// Directory.GetCurrentDirectory()[0] + @":\presenter";
            if (Directory.Exists(path))
            {
                MetroExplorer.StartPath = path;
                watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true };
                watcher.Created += watcher_Changed;
                watcher.Deleted += watcher_Changed;
                watcher.Renamed += watcher_Changed;
                watcher.EnableRaisingEvents = true;
            }
            
            InitShortcuts(MetroExplorer.StartPath);
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name.StartsWith("^")) return;
            Execute.OnUIThread(() =>
            {
                try
                {
                    var ap = MetroExplorer.Path;
                    MetroExplorer.Cache.Clear();
                    MetroExplorer.SelectFolder(MetroExplorer.StartPath);
                    MetroExplorer.SelectFolder(ap);
                }
                catch (Exception exception)
                {                    
                    Logger.Log("PresenterPlugin","Error refreshing content",exception.Message,Logger.Level.Error);
                }
            });
        }

        private void PlManagerPlaylistStartedEvent()
        {
            Execute.OnUIThread(() => { ScriptRunning = true; });
        }

        private void PlManagerPlaylistFinishedEvent()
        {
            Execute.OnUIThread(() =>
            {
                ScriptRunning = false;
                AppState.ViewDef.ChangeMapType(BaseMap);
                if (DemoScript.ZoomBack)
                {
                    AppState.ViewDef.StartTransition();
                    //DemoScript.ZoomTo(-180,-70,180,70,effect:true);
                    DemoScript.ZoomTo(-19913273.2361002, -16885380.7637054, 20161743.4494782,
                        15174632.5847574, effect: true);
                }
                DemoScript.ZoomBack = true;
                MetroExplorer.UpdateTitle();
                MetroExplorer.NextVisibility = Visibility.Collapsed;
                MetroExplorer.PreviousVisibility = Visibility.Collapsed;
            });
        }

        readonly Random rnd = new Random();

        private void MetroExplorerActivePathChanged(object sender, ItemSelectedArgs e)
        {
            if (e.Item == null || e.Item.Type == ItemType.folder) return;

            var d = e.Item.GetDocument();
            if (d.FileType == FileTypes.unknown) return;
            var shortcut = RetreiveShortcut(e.Item.Path);

            var fe = shortcut != null && shortcut.ItemType == ItemType.qr
                ? FloatingHelpers.CreateFloatingElementWithQrBackside(d, shortcut.Path)
                : FloatingHelpers.CreateFloatingElement(d);
            if (e.Pos != new Point(0, 0))
            {
                fe.AnimationSpeed = new TimeSpan(0, 0, 0, 0, 500);
                //fe.StartSize= new Size(0,0);
                fe.OriginPosition = e.Pos;
                fe.Height = 400;
                fe.OriginSize = new Size(0, 0);
            }
            fe.CanFullScreen = true;
            var x = ((view.ActualWidth - 600)/view.ActualWidth)*e.Pos.X + 300;
            fe.StartPosition = new Point(x, 300 + rnd.Next(200) - 100);
            fe.Title = e.Item.Name;
            AppState.FloatingItems.AddFloatingElement(fe);
            //if (e.Item.Type == ItemType.script) StartScript(e.Item);
        }

        private void InitShortcuts(string startPath)
        {
            if (startPath == null) return;
            var fileName = Path.Combine(startPath, "shortcuts.csv");
            if (!File.Exists(fileName)) return;
            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ';' });
                var key = parts[0];
                key = key.ToLower().Replace("^", string.Empty);
                if (parts.Length < 3 || shortcutsDictionary.ContainsKey(key)) continue;
                switch (parts[1].Trim().ToLower())
                {
                    case "qr":
                        shortcutsDictionary.Add(key, new Shortcut(parts[2], ItemType.qr));
                        break;
                    case "url":
                    case "web":
                        shortcutsDictionary.Add(key, new Shortcut(parts[2], ItemType.web));
                        break;
                    case "path":
                    case "link":
                        shortcutsDictionary.Add(key, new Shortcut(parts[2], ItemType.folder));
                        break;
                }
            }
        }

        //private void AddShortcut(FloatingElement fe, string path) {
        //    var shortcut = RetreiveShortcut(path);
        //    if (shortcut == null) return;
        //    switch (shortcut.ItemType) {
        //        case ItemType.qr:
        //            var qrCode = AppStateSettings.Instance.Container.GetExportedValue<IQrCode>();
        //            qrCode.Text = new Uri(shortcut.Path, UriKind.RelativeOrAbsolute).AbsoluteUri;
        //            fe.CanFlip = true;
        //            fe.ModelInstanceBack = qrCode;
        //            break;
        //    }
        //}

        private Shortcut RetreiveShortcut(string path) {
            var key = Path.GetFileNameWithoutExtension(path);
            if (key == null) return null;
            key = key.ToLower().Replace("^", string.Empty);
            return !shortcutsDictionary.ContainsKey(key) ? null : shortcutsDictionary[key];
        }

        internal void AddFolder(string path)
        {
            if (AdditionalFolders.Contains(path)) return;
            AdditionalFolders.Add(path);
            if (MetroExplorer!=null)
            {
                MetroExplorer.AdditionalFolders = AdditionalFolders;                
            }
        }

        internal void RemoveFolder(string path)
        {
            if (!AdditionalFolders.Contains(path)) return;
            AdditionalFolders.Remove(path);
            if (MetroExplorer != null)
            {
                MetroExplorer.AdditionalFolders = AdditionalFolders;
            }
        }
    }
}