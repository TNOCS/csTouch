using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using Caliburn.Micro;
using csCommon.csMapCustomControls.CircularMenu;
using DataServer;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.ViewModels;
using csImb;
using csShared;
using csShared.Interfaces;
using csShared.TabItems;
using csShared.Utils;
using PowerPointGenerator;

namespace csAppraisalPlugin {
    public class Rfi : BaseContent {
        private string title;

        public string Title {
            get { return title; }
            set {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }
    }


    public class RfiService : PoiService {
        private ContentList rfis;

        public ContentList Rfis {
            get { return rfis; }
            set {
                rfis = value;
                NotifyOfPropertyChange(() => Rfis);
            }
        }

        public void InitRfisService() {
            InitPoiService();
            Rfis = new ContentList {Service = this};
            AllContent.Add(Rfis);
        }
    }

    [Export(typeof (IPlugin))]
    public class AppraisalPlugin : PropertyChangedBase, IPlugin {
        private bool active;
        private string file;
        private FunctionList functions = new FunctionList();
        private bool hideFromSettings;
        private bool isRunning;
        private IPluginScreen screen;
        private string screenshotFolder;
        private BindableCollection<Appraisal> selectedAppraisals = new BindableCollection<Appraisal>();
        private ISettingsScreen settings;

        public string BFile {
            get { return file; }
            set {
                file = value;
                NotifyOfPropertyChange(() => BFile);
            }
        }

        public string ScreenshotFolder {
            get {
                if (!string.IsNullOrEmpty(screenshotFolder)) return screenshotFolder;
                var c = AppState.Config.Get("Appraisal.Screenshot.Folder", @"%TEMP%\cs\Appraisals\");
                if (c[c.Length - 1] != '\\') c += @"\";
                screenshotFolder = Environment.ExpandEnvironmentVariables(c);
                if (!Directory.Exists(screenshotFolder)) Directory.CreateDirectory(screenshotFolder);
                return screenshotFolder;
            }
        }

        public BindableCollection<Appraisal> SelectedAppraisals {
            get { return selectedAppraisals; }
            set {
                selectedAppraisals = value;
                NotifyOfPropertyChange(() => SelectedAppraisals);
            }
        }

        public FloatingElement Element { get; set; }

        #region IPlugin Members

        private AppraisalList appraisals;
        private Appraisal selectedAppraisal = new Appraisal();

        public FunctionList Functions {
            get { return functions; }
            set {
                functions = value;
                NotifyOfPropertyChange(() => Functions);
            }
        }

        public Appraisal SelectedAppraisal {
            get { return selectedAppraisal; }
            set {
                selectedAppraisal = value;
                NotifyOfPropertyChange(() => SelectedAppraisal);
            }
        }

        public AppraisalList Appraisals {
            get
            {
                return appraisals;
            }
            set
            {
                appraisals = value;
            }
        }

        public bool Active {
            get { return active; }
            set {
                if (active == value) return;
                active = value;
                NotifyOfPropertyChange(() => Active);
                //OnActiveChanged();
                UpdateTabs();
            }
        }

        public AppraisalTabViewModel TabViewModel { get; set; }
        public FunctionsTabViewModel FunctionsViewModel { get; set; }

        public StartPanelTabItem AppraisalTabItem { get; set; }
        public StartPanelTabItem FunctionsTabItem { get; set; }

        public string SavedFunctionsFileName {
            get { return Path.Combine(ScreenshotFolder, "Functions.xml"); }
        }

        public string SavedAppraisalsFileName {
            get { return Path.Combine(ScreenshotFolder, "Appraisals.xml"); }
        }

        public IPluginScreen Screen {
            get { return screen; }
            set {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool CanStop {
            get { return true; }
        }

        public ISettingsScreen Settings {
            get { return settings; }
            set {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public bool HideFromSettings {
            get { return hideFromSettings; }
            set {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority {
            get { return 3; }
        }

        public string Icon {
            get { return @"icons\ThumbsUp.png"; }
        }


        public string Name {
            get { return "AppraisalPlugin"; }
        }

        public bool IsRunning {
            get { return isRunning; }
            set {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }

        public void Init() {
            //appStateSettings = AppStateSettings.Instance;
            //isTimelineVisible = AppState.TimelineVisible;
            //UpdateTabs();
            // get file location
            appraisals = new AppraisalList();
            //DefaultFunctions = new List<Guid>();
            AppState.Drop += AppStateDrop;
            
            Load();

            functions.CollectionChanged += (sender, e) => {
                Save();
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Move:
                        UpdateAllAppraisals();
                        break;
                    case NotifyCollectionChangedAction.Add:
                        UpdateAllAppraisals();
                        foreach (var newItem in e.NewItems) {
                            var function = newItem as Function;
                            if (function == null) continue;
                            function.PropertyChanged += OnFunctionPropertyChanged;
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldItem in e.OldItems) {
                            var function = oldItem as Function;
                            if (function == null) continue;
                            function.PropertyChanged -= OnFunctionPropertyChanged;
                        }
                        foreach (var appraisal in SelectedAppraisals) {
                            foreach (Function function in e.OldItems)
                                appraisal.Criteria.RemoveCriterion(function);
                        }
                        break;
                }
            };

            TabViewModel = (AppraisalTabViewModel)IoC.GetInstance(typeof(IAppraisalTab), "");
            TabViewModel.Plugin = this;

            AppraisalTabItem = new StartPanelTabItem
            {
                Name = "Appraisals",
                ModelInstance = TabViewModel,
            };

            FunctionsViewModel = (FunctionsTabViewModel)IoC.GetInstance(typeof(IFunctionsTab), "");
            FunctionsViewModel.Plugin = this;

            FunctionsTabItem = new StartPanelTabItem
            {
                Name = "Functions",
                ModelInstance = FunctionsViewModel
            };

            Application.Current.MainWindow.PreviewKeyUp += MainWindowPreviewKeyUp;
            SelectedAppraisals.CollectionChanged += SelectedAppraisalsCollectionChanged;
           
            AppState.AddStartPanelTabItem(AppraisalTabItem);
            AppState.AddStartPanelTabItem(FunctionsTabItem);
        }

        public void Start() {
            IsRunning = true;
            //isTimelineVisible = AppState.TimelineVisible;
            UpdateTabs();

            var avm = AppState.Container.GetExportedValue<IAppraisal>();
            if (avm == null) throw new SystemException("Couldn't create AppraisalViewModel");
            avm.Plugin = this;
            if (menu != null) AppState.CircularMenus.Add(menu);
            // Since the AppraisalPlugin is not part of the lifecycle, I need to activate the AppraisalViewModel myself.
            ScreenExtensions.TryActivate(avm);
            Screen = avm as IPluginScreen;
        }

        public void Pause()
        {
            IsRunning = false;
        }

        private CircularMenuItem menu;
        public void Stop() {
            //AppState.RemoveStartPanelTabItem(AppraisalTabItem);
            //AppState.RemoveStartPanelTabItem(FunctionsTabItem);
            menu = AppState.CircularMenus.FirstOrDefault(m => string.Equals(m.Id, "AppraisalMenu"));
            if (menu != null) AppState.CircularMenus.Remove(menu);
            IsRunning = false;
            Active = false;
            UpdateTabs();
            Save();
        }

        private void UpdateTabs() {
            AppState.DockedFloatingElementsVisible = !Active;
            AppState.BottomTabMenuVisible = true;
            AppState.BotomPanelVisible = true;
            if (Active)
            {
                AppState.LeftTabMenuVisible = false;
                AppState.ExcludedStartTabItems.Clear();
                if (!AppState.FilteredStartTabItems.Contains("Appraisals")) AppState.FilteredStartTabItems.Add("Appraisals");
                if (!AppState.FilteredStartTabItems.Contains("Functions")) AppState.FilteredStartTabItems.Add("Functions");
                //isTimelineVisible = AppState.TimelineVisible;
                //AppState.TimelineVisible = false;
            }
            else {
                AppState.LeftTabMenuVisible = true;
                AppState.FilteredStartTabItems.Clear();
                if (!AppState.ExcludedStartTabItems.Contains("Appraisals")) AppState.ExcludedStartTabItems.Add("Appraisals");
                if (!AppState.ExcludedStartTabItems.Contains("Functions")) AppState.ExcludedStartTabItems.Add("Functions");
                //AppState.TimelineVisible = isTimelineVisible;
            }
        }

        private void OnActiveChanged()
        {
            var handler = ActiveChanged;
            if (handler != null) ActiveChanged(this, null);
        }

        public event EventHandler ActiveChanged;

        public event EventHandler AppraisalsUpdated;

        public void TriggerAppraisalsUpdated()
        {
            if (AppraisalsUpdated != null) AppraisalsUpdated(this, null);
        }

        //public List<Guid> DefaultFunctions { get; set; }

        private void UpdateAllAppraisals() {
            foreach (var appraisal in SelectedAppraisals)
                appraisal.Criteria.Update(functions);
        }

        private void OnFunctionPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName.Equals("Name") || e.PropertyName.Equals("IsSelected"))
                UpdateAllAppraisals();
        }

        /// <summary>
        ///     Load all functions and appraisals from disk
        /// </summary>
        public void Load() {
            try {
                if (File.Exists(SavedFunctionsFileName)) {
                    var xmlSerializer = new XmlSerializer(typeof (FunctionList));
                    using (var stream = new StreamReader(SavedFunctionsFileName)) {
                        var ff = xmlSerializer.Deserialize(stream) as FunctionList;
                        if (ff != null) {
                            foreach (var function in ff)
                                function.PropertyChanged += OnFunctionPropertyChanged;
                            Functions.AddRange(ff);
                            
                        }
                    }
                }
                if (!File.Exists(SavedAppraisalsFileName)) return;
                var xmlSerializer2 = new XmlSerializer(typeof(AppraisalList));
                using (var stream = new StreamReader(SavedAppraisalsFileName))
                {
                    var a = xmlSerializer2.Deserialize(stream) as AppraisalList;
                    if (a == null) return;
                    foreach (var appraisal in a)
                    {
                        // Adjust path filename as they may have moved to a different location
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(appraisal.FileName);
                        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension)) continue;
                        appraisal.FileName = FullFileName(fileNameWithoutExtension);
                    }
                    Appraisals.AddRange(a);
                    SelectedAppraisals.AddRange(a);
                }
            }
            catch (Exception e) {
                Logger.Log("AppraisalPlugin", "Load", e.Message, Logger.Level.Error);
            }
        }

        /// <summary>
        ///     Save all functions and appraisals to disk
        /// </summary>
        public void Save() {
            try
            {

            
            var xmlSerializer = new XmlSerializer(typeof (FunctionList));
            using (var stream = new StreamWriter(SavedFunctionsFileName))
                xmlSerializer.Serialize(stream, Functions);
            var xmlSerializer2 = new XmlSerializer(typeof (AppraisalList));
            using (var stream = new StreamWriter(SavedAppraisalsFileName))
                xmlSerializer2.Serialize(stream, Appraisals);
            }
            catch (Exception e)
            {
                Logger.Log("AppraisalPlugin","Error saving xml file",e.Message,Logger.Level.Error);
            }
        }


        private void AppStateDrop(object sender, DropEventArgs e) {
            if (!IsRunning || !(e.EventArgs.Cursor.Data is Appraisal)) return;
            //var a = (Appraisal) e.EventArgs.Cursor.Data;
            var avm = AppState.Container.GetExportedValue<IAppraisal>();

            avm.Plugin = this;
            var fe = new FloatingElement {
                ModelInstance = avm,
                OpacityDragging = 0.5,
                OpacityNormal = 1.0,
                CanDrag = true,
                CanMove = true,
                CanRotate = true,
                CanScale = true,
                Background = Brushes.DarkOrange,
                //MaxSize = new Size(500, (500.0 / pf.Width) * pf.Height),                                 
                StartPosition = e.Pos,
                StartSize = new Size(200, 200),
                Width = 300,
                Height = 300,
                ShowsActivationEffects = false,
                RemoveOnEdge = true,
                Contained = true,
                CanFullScreen = true,
                Title = "Appraisal",
                Foreground = Brushes.White,
                DockingStyle = DockingStyles.None,
            };
            AppState.FloatingItems.Add(fe);
        }

        private void SelectedAppraisalsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            foreach (var appraisal in SelectedAppraisals) {
                if (appraisal.Criteria == null || appraisal.Criteria.Count == 0) appraisal.Criteria = new CriteriaList(functions, 0, 10);
                else appraisal.Criteria.Update(functions);
            }
        }

        #endregion

        public void GoTo() {}

        private void MainWindowPreviewKeyUp(object sender, KeyEventArgs e) {
            // Key.Snapshot is the PrintScreen key. However, when ALT is pressed, I have to check the SystemKey for Key.Snapshot.
            // NOTE The PrintScreen key is not captured in the KeyDown event!
            if (e.Key != Key.Snapshot && e.SystemKey != Key.Snapshot) return;
            //if (e.Key != Key.LeftCtrl) return;
            e.Handled = true;

            CreateNewAppraisal();
        }

        public ImageSource CreateNewAppraisal() {
            var appraisal = new Appraisal {
                Title = "New Appraisal",
                Criteria = new CriteriaList(functions, 0, 10)
            };
            appraisal.FileName = FullFileName(appraisal.Id.ToString());
            var r = Screenshots.SaveImageOfControl(Application.Current.MainWindow, appraisal.FileName);
            if (r == null) return null;
            Appraisals.Add(appraisal);
            SelectedAppraisals.Add(appraisal);
            return r;
        }

        public void Export()
        {
            try
            {
                var ap = Appraisals.Where(a => a.IsSelected && File.Exists(a.FileName)).Select(a => a).ToList();
                if (!ap.Any())
                {
                    AppStateSettings.Instance.TriggerNotification(string.Format("No appraisals created or selected", "export"));
                    return;
                }

                var imagePaths = ap.Select(a => a.FileName).ToList();



                var titles = ap.Select(a => a.Title).ToList();

                var at = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(imagePaths[0]), "export - " + DateTime.Now.Ticks.ToString()), "pptx");
                var pptFactory = new PowerPointFactory(at);
                pptFactory.CreateTitleAndImageSlides(imagePaths, titles);

                var ne = new NotificationEventArgs()
                {
                    Header = "Export",
                    Text = "Finished creating PowerPoint"
                };
                ne.Foreground = Brushes.Black;
                ne.Background = Brushes.LightBlue;
                ne.Options = new List<string> { "Open" };
                ne.OptionClicked += (f, b) =>
                {
                    Process.Start(at);
                };
                AppStateSettings.Instance.TriggerNotification(ne);
            }
            catch (Exception)
            {
                AppStateSettings.Instance.TriggerNotification(string.Format("Error creating PowerPoint, check if folder exist", "export"));
            }

        }

        public ImageSource CreateNewMapAppraisal() {
            var appraisal = new Appraisal {
                Title = "New Appraisal",
                Criteria = new CriteriaList(functions, 0, 10),
                IsSelected = true
            };
            appraisal.FileName = FullFileName(appraisal.Id.ToString());
            var r = Screenshots.SaveImageOfControl(AppState.ViewDef.MapControl, appraisal.FileName);
            if (r == null) return null;
            Appraisals.Add(appraisal);
            if (SelectedAppraisals.IndexOf(appraisal)==-1) SelectedAppraisals.Add(appraisal);
            SelectedAppraisal = appraisal;
            return r;
        }

        private string FullFileName(string screenshotName) {
            return Path.ChangeExtension(Path.Combine(ScreenshotFolder, screenshotName), "png");
        }

        #region ImageHandling

        ///// <summary>
        /////     Convert any control to a PngBitmapEncoder
        ///// </summary>
        ///// <param name="controlToConvert"> The control to convert to an ImageSource </param>
        ///// <returns> The returned ImageSource of the controlToConvert </returns>
        ///// <see cref="http://www.dreamincode.net/code/snippet4326.htm" />
        //private static PngBitmapEncoder GetImageFromControl(FrameworkElement controlToConvert) {
        //    //var printDialog = new PrintDialog();
        //    //if (printDialog.ShowDialog() == true) printDialog.PrintVisual(controlToConvert, "ActivityInspector diagram");

        //    // save current canvas transform
        //    // var transform = controlToConvert.LayoutTransform;

        //    // get size of control
        //    var sizeOfControl = new Size(controlToConvert.ActualWidth, controlToConvert.ActualHeight);
        //    // measure and arrange the control
        //    controlToConvert.Measure(sizeOfControl);
        //    // arrange the surface
        //    controlToConvert.Arrange(new Rect(sizeOfControl));

        //    // craete and render surface and push bitmap to it
        //    var renderBitmap = new RenderTargetBitmap((Int32) sizeOfControl.Width, (Int32) sizeOfControl.Height, 96d, 96d,
        //                                              PixelFormats.Pbgra32);
        //    // now render surface to bitmap
        //    renderBitmap.Render(controlToConvert);

        //    // encode png data
        //    var pngEncoder = new PngBitmapEncoder();
        //    // puch rendered bitmap into it
        //    pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
        //    //pngEncoder.Metadata = new BitmapMetadata("png") {
        //    //                                                    ApplicationName = "Marvelous",
        //    //                                                    DateTaken = DateTime.Now.ToString(CultureInfo.InvariantCulture),
        //    //                                                    Subject = "Casual Loop Analysis using Marvel",
        //    //                                                    Title = "Marvelous screenshot",
        //    //                                                    Author = new ReadOnlyCollection<string>(new List<string> {
        //    //                                                                                                                 Properties.Settings.Default.UserName
        //    //                                                                                                             })
        //    //                                                };

        //    // return encoder
        //    return pngEncoder;
        //}

        ///// <summary>
        /////     Get an ImageSource of a control
        ///// </summary>
        ///// <param name="controlToConvert"> The control to convert to an ImageSource </param>
        ///// <returns> The returned ImageSource of the controlToConvert </returns>
        //public static ImageSource GetImageOfControl(Control controlToConvert) {
        //    // return first frame of image 
        //    return GetImageFromControl(controlToConvert).Frames[0];
        //}

        ///// <summary>
        /////     Save an image of a control
        ///// </summary>
        ///// <param name="controlToConvert"> The control to convert to an ImageSource </param>
        ///// ///
        ///// <param name="fileName"> The location to save the image to </param>
        ///// <returns> The returned ImageSource of the controlToConvert </returns>
        //public static ImageSource SaveImageOfControl(Control controlToConvert, string fileName) {
        //    ImageSource result = null;
        //    try {
        //        // create a file stream for saving image
        //        using (var outStream = new FileStream(fileName, FileMode.Create)) {
        //            PngBitmapEncoder r = GetImageFromControl(controlToConvert);
        //            r.Save(outStream);
        //            return r.Frames[0];
        //        } // save encoded data to stream
        //    }
        //    catch (Exception e) {
        //        // display for debugging
        //        MessageBox.Show(String.Format("Exception caught saving stream: {0}", e.Message), "Error saving image", MessageBoxButton.OK,
        //                        MessageBoxImage.Error);
        //        // return fail
        //        return null;
        //    }
        //}

        #endregion
    }
}