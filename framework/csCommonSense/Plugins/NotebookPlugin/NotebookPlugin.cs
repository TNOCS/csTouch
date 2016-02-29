using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using csImb;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using csShared.TabItems;
using csShared.Utils;

namespace csCommon.Plugins.NotebookPlugin
{
    [Export(typeof(IPlugin))]
    public class ScreenshotPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return true; } }

        private NotebookCollection notebooks;

        public NotebookCollection Notebooks
        {
            get { return notebooks; }
            set { notebooks = value; NotifyOfPropertyChange(()=>Notebooks); }
        }

        private ISettingsScreen settings;

        public ISettingsScreen Settings
        {
            get { return settings; }
            set { settings = value; NotifyOfPropertyChange(() => Settings); }
        }
        private IPluginScreen screen;

        public IPluginScreen Screen
        {
            get { return screen; }
            set { screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private bool hideFromSettings;

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set { hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }

        public const string ShowButton = "Notebook.ShowScreenshotButton";
        public const string ShowTab    = "Notebook.ShowNotebookTab";
        
        public bool ShowScreenshotButton
        {
            get { return AppState.Config.GetBool(ShowButton, false); }
            set
            {
                AppState.Config.SetLocalConfig(ShowButton, value.ToString(),true);
                NotifyOfPropertyChange(()=>ShowScreenshotButton);
                UpdateButtons();
            }
        }

        public bool ShowNotebookTab
        {
            get { return AppState.Config.GetBool(ShowTab, false); }
            set
            {
                AppState.Config.SetLocalConfig(ShowTab, value.ToString(),true);
                NotifyOfPropertyChange(() => ShowNotebookTab);
                UpdateButtons();
            }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/Camerawhite.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public string Name
        {
            get { return "Screenshot"; }
        }

        public void Init()
        {       
            Notebooks = new NotebookCollection();
            Notebooks.Load();
            var c = Notebooks.FirstOrDefault(k=>k.Name == Notebooks.ActiveNotebookConfig);
            if (c != null) Notebooks.ActiveNotebook = c;
            if (Notebooks.ActiveNotebook == null && Notebooks.Any()) Notebooks.ActiveNotebook = Notebooks[0];
        }

        private NoteTabViewModel timeTabViewModel;

        public void UpdateButtons()
        {
            if (ShowScreenshotButton && circularMenuItem==null)
            {
                circularMenuItem = new CircularMenuItem {Title = "Take Screenshot"};
                circularMenuItem.Selected += TakeScreenshot;

                circularMenuItem.Id = Guid.NewGuid().ToString();
                circularMenuItem.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/Camerablack.png";
                AppState.AddCircularMenu(circularMenuItem);
            }
            if (!ShowScreenshotButton && circularMenuItem!=null)
            {
                AppState.RemoveCircularMenu(circularMenuItem.Id);
                circularMenuItem = null;
            }

            if (ShowNotebookTab && st==null)
            {
                timeTabViewModel = new NoteTabViewModel() { Plugin = this };

                st = new StartPanelTabItem
                {
                    Name = "Background",
                    HeaderStyle = TabHeaderStyle.Image,
                    Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/Book-Open.png")),
                    ModelInstance = timeTabViewModel
                };
                AppState.AddStartPanelTabItem(st);
            }
            if (!ShowNotebookTab && st != null)
            {
                AppState.RemoveStartPanelTabItem(st);
                st = null;
            }
        }

        public StartPanelTabItem st { get; set; }

        public void Clear()
        {
            ((NotificationViewModel) screen).EndNotifications();
        }

        private CircularMenuItem circularMenuItem;

        private List<string> screenshotFolder;
        private static readonly ImageSource cameraImage = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/Camerawhite.png"));

        public List<string> ScreenshotFolder
        {
            get
            {
                if (screenshotFolder != null) return screenshotFolder;
                var cc = AppState.Config.Get("Screenshot.Folder", @"%TEMP%\cs\Screenshots\");
                screenshotFolder = new List<string>();
                if (string.IsNullOrEmpty(cc)) return screenshotFolder;
                foreach (var c in cc.Split(','))
                {
                    if (string.IsNullOrEmpty(c)) continue;
                    var r = c;
                    if (r[r.Length - 1] != '\\') r += @"\";
                    r = Environment.ExpandEnvironmentVariables(r);
                    screenshotFolder.Add(r);
                }
                return screenshotFolder;
            }
        }

        private NotebookConfigViewModel config;

        public void Start()
        {
            IsRunning = true;

            UpdateButtons();

            AppState.ScriptCommand += AppState_ScriptCommand;

            config = new NotebookConfigViewModel() {DisplayName = "Notebooks", Plugin = this};

            AppState.ConfigTabs.Add(config);
        }

        void AppState_ScriptCommand(object sender, string command)
        {
            if (command.ToLower().StartsWith("notebookscreenshot"))
            {
                Execute.OnUIThread(()=>TakeScreenshot(this,null));
            }
        }

        void TakeScreenshot(object sender, MenuItemEventArgs e) {
            try {
                var now = DateTime.Now;
                if (Notebooks.ActiveNotebook != null)
                {
                    var f = Notebooks.ActiveNotebook.Folder;
                    if (Directory.Exists(f))
                    {
                        var filename = Path.Combine(f,
                        string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH_mm_ss} Screenshot.png", now));
                        // f + @"\Screenshot-" + DateTime.Now.Ticks + ".jpg";
                        Screenshots.SaveImageOfControl(Application.Current.MainWindow, filename);
                        AppState.TriggerNotification(
                            "Screenshot has been stored", "Screenshot",
                            image: cameraImage);
                    }
                    Notebooks.ActiveNotebook.LoadItems();
                }
                else
                {
                    AppState.TriggerNotification(
                        "No screenshots have been stored - no Notebook active.", "Screenshot",
                        image: cameraImage);
                }
                //foreach (var folder in ScreenshotFolder) {
                //    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                //    var filename = Path.Combine(folder,
                //        string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH_mm_ss} Screenshot.png", now));
                //    // f + @"\Screenshot-" + DateTime.Now.Ticks + ".jpg";
                //    Screenshots.SaveImageOfControl(Application.Current.MainWindow, filename);
                //}
                //if (Notebooks.ActiveNotebook!=null)
                //    Notebooks.ActiveNotebook.LoadItems();
            }
            catch (SystemException ex) {
                Logger.Log("ScreenshotPlugin", "Error saving screenshot", ex.Message, Logger.Level.Error, true, true);
            }
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Stop()
        {
            IsRunning = false;
            if (circularMenuItem != null)
                AppState.RemoveCircularMenu(circularMenuItem.Id);
            AppState.RemoveStartPanelTabItem(st);
            AppState.ConfigTabs.Remove(config);
        }
    }
}
