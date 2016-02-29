using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using csShared;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;

namespace csBookmarkPlugin
{
    [Export(typeof(IPlugin))]
    public class BookmarkPlugin : PropertyChangedBase, IPlugin
    {

        private IPluginScreen _screen;

        public IPluginScreen Screen
        {
            get { return _screen; }
            set { _screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        public bool CanStop { get { return true; } }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private bool _hideFromSettings;

        public bool HideFromSettings
        {
            get { return _hideFromSettings; }
            set { _hideFromSettings = value; NotifyOfPropertyChange(()=>HideFromSettings); }
        }
        

        public int Priority
        {
            get { return 3; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/bookmarkstar.png"; }           
        }

        private BookmarkList _bookmarks;

        public BookmarkList Bookmarks
        {
            get { return _bookmarks; }
            set { _bookmarks = value; NotifyOfPropertyChange(()=>Bookmarks); }
        }

        private string _file;

        public string BFile
        {
            get { return _file; }
            set { _file = value; NotifyOfPropertyChange(()=>BFile); }
        }
        


        #region IPlugin Members

        public string Name
        {
            get { return "BookmarkPlugin"; }
        }

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        public void Init()
        {
            // get file location
            BFile = ConfigurationManager.AppSettings["Bookmark.File"];
            if (string.IsNullOrEmpty(BFile)) BFile = "bookmarks.txt";
            var fi = new FileInfo(BFile);
            if (fi.Directory == null) return;
            // check if directory exists
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.Directory.FullName);
            }

            Screen = new PinViewModel();

            // load bookmarks
            Bookmarks = BookmarkList.Load(BFile);
            //var viewModel = IoC.GetAllInstances(typeof(IBookmark)).FirstOrDefault() as IBookmark;
            var viewModel = (BookmarkViewModel)IoC.GetInstance(typeof(IBookmark), "");
            
            //container.GetExportedValueOrDefault<IBookmark>();
            if (viewModel != null)
            {
                viewModel.BFile = BFile;
                Element = FloatingHelpers.CreateFloatingElement("Bookmarks", DockingStyles.Up, viewModel, Icon,Priority);
                Element.LastContainerPosition = new ContainerPosition()
                {
                    Center = new Point(500, 400),
                    Size = new Size(500, 500)

                };
                Element.SwitchWidth = 450;
                Element.DragScaleFactor = 40;
                UpdateVisibility();
            }
            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;

            
             
        }

        

       

        

        void ViewDefVisibleChanged(object sender, VisibleChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (AppState.ViewDef.Visible && IsRunning)
            {
                if (!AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.AddFloatingElement(Element);
            }
            else
            {
                if (AppState.FloatingItems.Contains(Element)) AppState.FloatingItems.RemoveFloatingElement(Element);
            }
        }

        public void GoTo()
        {

        }

        public void Start()
        {
            IsRunning = true;
            UpdateVisibility();
        }

        public void Pause()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        public void Stop()
        {
            IsRunning = false;
            UpdateVisibility();
        }

        

       

        #endregion


       
    }
}
