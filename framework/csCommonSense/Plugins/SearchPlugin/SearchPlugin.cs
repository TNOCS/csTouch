using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csCommon.Types.DataServer.PoI;
using csShared;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Interfaces;
using System.ComponentModel.Composition;
using DataServer;

namespace csCommon.MapPlugins.Search
{

    

    [Export(typeof(IPlugin))]
    public class SearchPlugin : PropertyChangedBase, IPlugin
    {
        public bool CanStop { get { return true; } }

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

        private bool isLoading;

        public bool IsLoading
        {
            get { return isLoading; }
            set { isLoading = value; NotifyOfPropertyChange(()=>IsLoading); }
        }
        

        public int Priority
        {
            get { return 4; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/SearchIcon.png"; }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        public DataServerBase DataServer { get; set; }

        public readonly object ServiceLock = new object();


        public string Name
        {
            get { return "FindLocation"; }
        }

        public SearchViewModel viewModel;

        public void Init()
        {
            AppState.DataServerChanged += (e, f) => InitPlugin();
            InitPlugin();
        }

        private void InitPlugin()
        {
            DataServer = AppState.DataServer;

            CreateService();

            viewModel = new SearchViewModel { Plugin = this }; // IoC.GetAllInstances(typeof (IFindLocation)).FirstOrDefault();

            Element = FloatingHelpers.CreateFloatingElement("Find", DockingStyles.Up, viewModel, Icon, Priority);
            Element.LastContainerPosition = new ContainerPosition
            {
                Center = new Point(500, 400),
                Size = new Size(500, 500)
            };
            Element.IconUri = Icon;
            Element.SwitchWidth = 450;
            Element.DragScaleFactor = 40;
            UpdateVisibility();

            AppState.ViewDef.VisibleChanged += ViewDefVisibleChanged;
        }

        private SaveService searchService;

        public SaveService SearchService
        {
            get { return searchService; }
            set { searchService = value; NotifyOfPropertyChange(()=>SearchService); }
        }
        

        
        private PoI result;

        public PoI CreatePoiTypeResult(string id, Color color, string icon = "")
        {
            var googleResult = new PoI
            {
                Service = SearchService,
                ContentId = id,
                Style = new PoIStyle
                {
                    DrawingMode        = DrawingModes.Point,
                    FillColor          = color,
                    TitleMode          = TitleModes.Bottom,
                    CallOutFillColor   = Colors.White,
                    CallOutOrientation = CallOutOrientation.Right,
                    CallOutForeground  = Colors.Black,
                    CallOutMaxWidth    = 400,
                    Icon               = icon,
                    InnerTextColor     = Colors.White
                }
                
            };
            return googleResult;
        }

        public void CreateService()
        {
            AppState.ViewDef.FolderIcons[@"AcceleratedLayers\Search Result"] = "pack://application:,,,/csCommon;component/Resources/Icons/Search.png";

            SearchService = new SaveService()
            {
                IsLocal = true,
                Name = "Search Results",
                Id = new Guid("5A9FB294-180A-4F56-9E26-4F5503E9A0A5"),
                IsFileBased = false,
                StaticService = true,
                IsVisible = false,
                RelativeFolder = "Search Result"
                              
            };

            SearchService.Init(Mode.client, DataServer);
            // TODO Check met Arnoud of dit niet de folder moet zijn die in de configoffline staat?
            SearchService.Folder = Directory.GetCurrentDirectory() + @"\PoiLayers\Search Result";
            SearchService.InitPoiService();
            
            SearchService.Settings.OpenTab = false;
            SearchService.Settings.Icon    = "brugwhite.png";
            SearchService.AutoStart        = true;

            result = new PoI { ContentId = "Brug", Style = new PoIStyle
            {
                DrawingMode       = DrawingModes.Point, 
                FillColor         = Colors.Red, 
                CallOutFillColor  = Colors.White,
                CallOutForeground = Colors.Black,
                IconWidth         = 30, 
                IconHeight        = 30,
                TitleMode         = TitleModes.Bottom
            } };
            
            result.AddMetaInfo("name", "name");
            //result.AddMetaInfo("height", "height", MetaTypes.number);
            //result.AddMetaInfo("width", "width", MetaTypes.number);
            //result.AddMetaInfo("description", "description");
            //result.AddMetaInfo("image", "image", MetaTypes.image);
            SearchService.PoITypes.Add(result);

            DataServer.Services.Add(SearchService);
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
    }
}
