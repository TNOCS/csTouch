//using PoiConvertor.Models;

using BagDataAccess;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Types.DataServer.PoI.Templates;
using csCommon.Utils.IO;
using csCommon.Views.Dialogs;
using csShared;
using csShared.Utils;
using CsvToDataService;
using CsvToDataService.Model;
using DataServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoiConvertor.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WPFFolderBrowser;
using Label = PoiConvertor.Model.Label;

namespace PoiConvertor.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            MergeIncludeSecondaryPois = true;
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += (sender, args) => Settings.Default.Save();
        }

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        //        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        //        {
        //            OnPropertyChangedExplicit(propertyName);
        //        }

        protected void OnPropertyChanged()
        {
            OnPropertyChangedExplicit(string.Empty); // Change all properties.
        }

        // Allows strongly typed property change syntax such as OnPropertyChange(() => PropertyName), instead of OnPropertyChange("PropertyName").
        protected void OnPropertyChanged<TProperty>(Expression<Func<TProperty>> projection)
        {
            var memberExpression = (MemberExpression)projection.Body;
            OnPropertyChangedExplicit(memberExpression.Member.Name);
        }

        private void OnPropertyChangedExplicit(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion PropertyChanged

        #region Check BAG

        private void OnConfigureBagButtonClick(object sender, RoutedEventArgs e)
        {
            var connectionString = string.IsNullOrEmpty(Settings.Default.UserConnectionString)
                ? Settings.Default.ConnectionString
                : Settings.Default.UserConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = Settings.Default.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = "Server=127.0.0.1;Port=[PORT];User Id=[USER];Password=[PWD];Database=bag;SearchPath=bag8jan2014,public";
                }
            }
            var newConnectionString = Microsoft.VisualBasic.Interaction.InputBox("Enter the connection string",
                    "Configure BAG", connectionString);
            if (string.IsNullOrEmpty(newConnectionString))
            {
                return;
            }

            Exception exception;
            if (BagAccessible.IsAccessible(newConnectionString, out exception))
            {
                MessageBox.Show(this, "BAG can be used to look up addresses!", "Success", MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Settings.Default.UserConnectionString = newConnectionString;
                Settings.Default.Save();
            }
            else
            {
                MessageBox.Show(this,
                    "BAG cannot be used to look up addresses!\n\nConnection string: " + connectionString + "\n\nMake sure the database is installed, the service is running, and the connection string is correct.\n\nException:" + exception.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Check BAG

        #region CSV to DS

        private string _csvFile, _originalFile;
        private string _descriptionFormatString  = Settings.Default.DescriptionFormatString;
        private bool _isInRd                     = Settings.Default.IsInRD;
        private bool _isStaticLayer              = Settings.Default.IsStaticLayer;
        private string _nameFormatString         = Settings.Default.NameFormatString;
        private int _numEntriesInCurrentFile     = -1;
        private CsvHeader _selectedCsvHeader;
        private char _separator                  = Settings.Default.DefaultSeparator;
        private bool _useDescriptionFormatString = Settings.Default.UseDescriptionFormatString;
        private bool _useNameFormatString        = Settings.Default.UseNameFormatString;
        private bool _useGoogleForLocation       = true;
        private bool _includeMetaData            = true;
        private TemplateStore<MetaInfo> _templateStore;

        public string CsvFile
        {
            get { return _csvFile; }
            set
            {
                if (string.Equals(_csvFile, value)) return;
                _csvFile = value;
                ConvertCsv.LoadCsvHeaders(_csvFile, _separator, CsvHeaders, ProcessingErrors, out _numEntriesInCurrentFile);

                FileLocation templateFileRoot = new FileLocation(AppStateSettings.TemplateFolder);
                _templateStore = new TemplateStore<MetaInfo>(templateFileRoot);

                Indexes = new List<int>(CsvHeaders.Count);
                for (int i = 0; i < CsvHeaders.Count; i++)
                {
                    Indexes.Add(i);
                    CsvHeaders[i].TemplateStore = _templateStore;
                }
                Indexes.Add(CsvHeaders.Count);

                OnPropertyChanged();
                OnPropertyChanged(() => CanStart); // TODO REVIEW was "CanStart"
            }
        }

        public string OriginalFile
        {
            get { return _originalFile; }
            set
            {
                if (_originalFile == value) return;
                _originalFile = value;
                OnPropertyChanged(() => OriginalFile);
            }
        }

        public ObservableCollection<CsvHeader> CsvHeaders
        {
            get { return _csvHeaders; }
            set
            {
                if ((_csvHeaders == null && value == null)) return;
                if (_csvHeaders != null && _csvHeaders.Equals(value)) return;
                _csvHeaders = value;
            }
        }

        public CsvHeader SelectedCsvHeader
        {
            get { return _selectedCsvHeader; }
            set
            {
                _selectedCsvHeader = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProcessingError> ProcessingErrors { get; set; }

        public bool IsStaticLayer
        {
            get { return _isStaticLayer; }
            set
            {
                if (_isStaticLayer == value) return;
                Settings.Default.IsStaticLayer = _isStaticLayer = value;
                OnPropertyChanged();
            }
        }

        public CsvSeparator Separator
        {
            get { return _separator == ';' ? CsvSeparator.SemiColon : CsvSeparator.Comma; }
            set
            {
                Settings.Default.DefaultSeparator = _separator = (value == CsvSeparator.SemiColon ? ';' : ',');
                Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public bool DotIsThousandSeparator
        {
            get { return CsvToDataService.Properties.Settings.Default.DotIsThousandSeparator; }
            set
            {
                CsvToDataService.Properties.Settings.Default.DotIsThousandSeparator = value;
                CsvToDataService.Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public bool CommaIsThousandSeparator
        {
            get { return CsvToDataService.Properties.Settings.Default.CommaIsThousandSeparator; }
            set
            {
                CsvToDataService.Properties.Settings.Default.CommaIsThousandSeparator = value;
                CsvToDataService.Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public bool CanStart
        {
            get { return !string.IsNullOrEmpty(_csvFile); } //  || !string.IsNullOrEmpty(_shpFile)
        }

        public bool UseNameFormatString
        {
            get { return _useNameFormatString; }
            set
            {
                if (_useNameFormatString == value) return;
                Settings.Default.UseNameFormatString = _useNameFormatString = value;
                OnPropertyChanged();
            }
        }

        public string NameFormatString
        {
            get { return _nameFormatString; }
            set
            {
                if (string.Equals(_nameFormatString, value)) return;
                Settings.Default.NameFormatString = _nameFormatString = value;
                OnPropertyChanged();
            }
        }

        public bool UseDescriptionFormatString
        {
            get { return _useDescriptionFormatString; }
            set
            {
                if (_useDescriptionFormatString == value) return;
                Settings.Default.UseDescriptionFormatString = _useDescriptionFormatString = value;
                OnPropertyChanged();
            }
        }

        public bool UseGoogleForLocation
        {
            get { return _useGoogleForLocation; }
            set
            {
                if (_useGoogleForLocation == value) return;
                _useGoogleForLocation = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeMetaData
        {
            get { return _includeMetaData; }
            set
            {
                if (_includeMetaData == value) return;
                _includeMetaData = value;
                OnPropertyChanged();
            }
        }

        public string DescriptionFormatString
        {
            get { return _descriptionFormatString; }
            set
            {
                if (string.Equals(_descriptionFormatString, value)) return;
                Settings.Default.DescriptionFormatString = _descriptionFormatString = value;
                OnPropertyChanged();
            }
        }

        public List<int> Indexes { get; private set; }

        public bool IsInRd
        {
            get { return _isInRd; }
            set
            {
                if (_isInRd == value) return;
                Settings.Default.IsInRD = _isInRd = value;
                OnPropertyChanged();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            DataContext = this;
            CsvHeaders = new ObservableCollection<CsvHeader>();
            ProcessingErrors = new ObservableCollection<ProcessingError>();
        }

        private void OnOpenCsvButtonClick(object sender, RoutedEventArgs e)
        {
            FileLocation file = OpenSupportedFileDialog.BrowseFile(this);
            if (file == null)
            {
                return;
            }
            string extension = Path.GetExtension(file.LocationString);
            if (!string.Equals(extension, ".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                IOResult<PoiService> import = PoiServiceImporters.Instance.Import(file);
                if (import != null && import.Successful)
                {
                    if (!import.Result.ContentLoaded)
                    {
                        try
                        {
                            DataServiceIO.LoadPoiServiceData(import.Result, file);
                        }
                        catch (Exception err) { } // TODO Quite ugly, this is done in quite a few places.
                    }
                    FileLocation tempCsvFile = new FileLocation(Path.Combine(Path.GetTempPath(), "TEMP-" + Guid.NewGuid() + ".csv"));
                    IOResult<FileLocation> export = PoiServiceExporters.Instance.Export(import.Result, tempCsvFile, true); // true, although we know we cannot export meta data for now.
                    if (export.Successful)
                    {
                        OriginalFile = file.LocationString; // Remember where to save!
                        CsvFile = export.Result.LocationString;
                        // Delete the temp file.
                        // File.Delete(tempCsvFile.LocationString); // Cannot do this.
                        return;
                    }
                    else
                    {
                        MessageBox.Show(this, export.Exception.Message, "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }
                else if (import != null && import.Exception != null)
                {
                    MessageBox.Show(this, import.Exception.Message, "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                CsvFile = file.LocationString;
                OriginalFile = file.LocationString; // Not really needed, but used for the label.
            }
            //            var ofd = new OpenFileDialog
            //            {
            //                AddExtension = true,
            //                CheckFileExists = true,
            //                CheckPathExists = true,
            //                DefaultExt = ".csv",
            //                Filter = "CSV files|*.csv"
            //            };
            //            if (ofd.ShowDialog() != true) return;
            //            CsvFile = ofd.FileName;
        }

        private void OnShowInPopupCheckBoxClick(object sender, RoutedEventArgs e)
        {
            bool isChecked = CheckUncheck.IsChecked == true;
            foreach (CsvHeader header in CsvHeaders)
            {
                header.VisibleInCallOut = isChecked;
            }
        }

        private void OnAddSectionButtonClick(object sender, RoutedEventArgs e)
        {
            string sectionName = Microsoft.VisualBasic.Interaction.InputBox("Please enter a section name",
                "New section",
                CsvHeader.NO_SECTION);
            if (sender.GetType() != typeof(DataButton))
            {
                return;
            }
            DataButton dataButton = (DataButton)sender;
            if (dataButton.Data == null || dataButton.Data.GetType() != typeof(CsvHeader))
            {
                return;
            }
            if (string.IsNullOrEmpty(sectionName))
            {
                return;
            }
            CsvHeader csvHeader = (CsvHeader)dataButton.Data;
            csvHeader.Section = sectionName;
        }

        private void OnSaveTemplatesClick(object sender, RoutedEventArgs e)
        {
            Dictionary<MetaInfo, string> metaInfos = new Dictionary<MetaInfo, string>();
            foreach (CsvHeader csvHeader in CsvHeaders) // Ugly, but required conversion from CsvHeader to MetaInfo.
            {
                MetaInfo newMetaInfo = new MetaInfo();
                newMetaInfo.FromXml(csvHeader.ToXml());
                metaInfos[newMetaInfo] = csvHeader.Template;
            }

            Dictionary<MetaInfo, string> selectedTemplates = SaveTemplateDialog.ShowDialog(metaInfos);
            _templateStore.Refresh(); // Easiest way to reload everything.

            foreach (KeyValuePair<MetaInfo, string> selectedTemplate in selectedTemplates)
            {
                KeyValuePair<MetaInfo, string> template = selectedTemplate;
                CsvHeader affectedHeader = CsvHeaders.First(header => ((MetaInfo)header).Id ==
                    template.Key.Id);
                if (!affectedHeader.Templates.Contains(selectedTemplate.Value))
                {
                    List<string> newTemplates = new List<string>(affectedHeader.Templates);
                    newTemplates.Add(selectedTemplate.Value);
                    affectedHeader.Templates = newTemplates;
                }
                affectedHeader.Template = selectedTemplate.Value;
            }
        }

        private void OnStartExportCsvToDsButtonClick(object sender, RoutedEventArgs e)
        {
            bool saveDefault = (Equals(sender, SaveDsButton));
            if (string.IsNullOrEmpty(CsvFile)) return;
            BindingOperations.EnableCollectionSynchronization(ProcessingErrors, this);

            var bw = new BackgroundWorker();
            var progressStruct = new ProgressStruct { NumTotal = _numEntriesInCurrentFile };

            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += delegate(object o, DoWorkEventArgs args)
            {
                var service = ConvertCsv.LoadCsvData(CsvFile, OriginalFile, _separator, _isStaticLayer,
                    CsvHeaders, progressStruct, ProcessingErrors, bw,
                    UseNameFormatString ? NameFormatString : string.Empty,
                    UseDescriptionFormatString ? DescriptionFormatString : string.Empty, saveDefault, // save DS by default
                    UseGoogleForLocation,
                    string.IsNullOrEmpty(Settings.Default.UserConnectionString) 
                        ? Settings.Default.ConnectionString
                        : Settings.Default.UserConnectionString);
                if (service == null) {
                    bw.CancelAsync();
                }
                args.Result = service;
            };
            bw.RunWorkerCompleted += (o, args) =>
            {
                if (args.Cancelled)
                {
                    MessageBox.Show(this, "The operation was cancelled!", "Cancelled", MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                    return;
                }

                var service = (PoiService)args.Result;
                var icon = MessageBoxImage.Information;
                string myMessage;
                string title;
                if (!saveDefault)
                {
                    IOResult<FileLocation> result = null;
                    if (service != null)
                    {
                        result = SaveSupportedFileDialog.BrowseAndSaveFile(service, IncludeMetaData, this, null, UseNameFormatString ? NameFormatString : string.Empty);
                    }
                    if (result != null)
                    {
                        string errorLogFileLocation = null;
                        if (ProcessingErrors.Count > 0)
                        {
                            string locationString = result.Result != null ? result.Result.LocationString : "unnamed";
                            errorLogFileLocation = locationString + ".errors.csv";
                            LogProcessingErrors(errorLogFileLocation);
                        }
                        if (result.Successful)
                        {
                            title = "Saved successfully";
                            myMessage = ProcessingErrors.Count == 0
                                ? "Output saved successfully with no errors."
                                : string.Format("Output saved successfully with {0} errors!", ProcessingErrors.Count);
                            myMessage += "\n" + result.Result.LocationString;
                        }
                        else
                        {
                            title = "Error";
                            icon = MessageBoxImage.Error;
                            myMessage = string.Format("Failed to process file: {0} errors found!\nError message: {1}",
                                ProcessingErrors.Count, result.Exception.Message);
                            if (errorLogFileLocation != null)
                            {
                                myMessage += "\nError log saved to: " + errorLogFileLocation;
                            }
                        }
                    }
                    else
                    {
                        // User cancelled. Do nothing.
                        SaveDsButton.IsEnabled = true; // TODO REVIEW: these en/disable operations should be bound.
                        SaveAsButton.IsEnabled = true;
                        OpenCsv.IsEnabled = true;
                        return;
                    }
                }
                else
                {
                    if (null != service)
                    {
                        title = "Saved successfully";
                        myMessage = ProcessingErrors.Count == 0
                            ? "Output saved successfully with no errors."
                            : string.Format("Output saved successfully with {0} errors!", ProcessingErrors.Count);
                        myMessage += "\n" + service.FileName;
                    }
                    else
                    {
                        title = "Error";
                        icon = MessageBoxImage.Error;
                        myMessage = string.Format("Failed to process CSV file: {0} errors found!",
                            ProcessingErrors.Count);
                    }
                    if (ProcessingErrors.Count > 0)
                    {
                        string fileName = service != null ? service.FileName : "anonymous";
                        string errorLogFileLocation = fileName + ".errors.csv";
                        LogProcessingErrors(errorLogFileLocation);
                        myMessage += "\nError log saved to: " + errorLogFileLocation;
                    }
                }
                MessageBox.Show(myMessage, title, MessageBoxButton.OK, icon);
                SaveDsButton.IsEnabled = true; // TODO REVIEW: these en/disable operations should be bound.
                SaveAsButton.IsEnabled = true;
                OpenCsv.IsEnabled = true;
            };

            var progressPopup = new BackgroundWorkerProgressPopup(this, bw, progressStruct);
            progressPopup.StartWorking();
        }

        private void LogProcessingErrors(string errorLogFileLocation)
        {
            using (var sw = new StreamWriter(errorLogFileLocation))
            {
                foreach (var processingError in ProcessingErrors)
                {
                    sw.Write(processingError.ToCsv(';'));
                }
            }
        }

        #endregion CSV to DS

        #region SHP to CSV

        //        private string _shpFile;
        //        public string ShpFile
        //        {
        //            get { return _shpFile; }
        //            set
        //            {
        //                if (string.Equals(_shpFile, value))
        //                {
        //                    return;
        //                }
        //                _shpFile = value;
        //
        //                var shpConvertor = new ShapeService();
        //                IOResult<PoiService> importData = shpConvertor.ImportData(new FileLocation(_shpFile));
        //                if (importData.Successful)
        //                {
        //                    importData.Result.SaveXml(Path.ChangeExtension(_shpFile, "ds"));
        //                    // TODO Message box that the file was saved.
        //                }
        //            }
        //        }

        //        private void OnOpenShapeButtonClick(object sender, RoutedEventArgs e)
        //        {
        //            var ofd = new OpenFileDialog
        //            {
        //                AddExtension = true,
        //                CheckFileExists = true,
        //                CheckPathExists = true,
        //                DefaultExt = ".shp",
        //                Filter = "SHP files|*.shp"
        //            };
        //            if (ofd.ShowDialog() != true)
        //            {
        //                return;
        //            }
        //            ShpFile = ofd.FileName;
        //        }
        //
        #endregion SHP to CSV

        #region Export DS

        public bool CanExport
        {
            get { return SelectedPoiServiceExporter != null && SelectedFileToExport != null; }
        }

        private IExporter<PoiService, FileLocation> _selectedPoiServiceExporter;
        public IExporter<PoiService, FileLocation> SelectedPoiServiceExporter
        {
            get { return _selectedPoiServiceExporter; }
            set
            {
                _selectedPoiServiceExporter = value;
                OnPropertyChanged();
                OnPropertyChanged(() => CanExport);
            }
        }

        private string _selectedFileToExport;
        public string SelectedFileToExport
        {
            get { return _selectedFileToExport; }
            set
            {
                _selectedFileToExport = value;
                OnPropertyChanged();
                OnPropertyChanged(() => CanExport);
            }
        }

        private void OnBrowseDsToExportButtonClick(object sender, RoutedEventArgs e)
        {
            FileLocation selectedFile = OpenSupportedFileDialog.BrowseFile(this);
            if (selectedFile != null)
            {
                SelectedFileToExport = selectedFile.LocationString;
            }
        }

        private void OnExportDsButtonClick(object sender, RoutedEventArgs e)
        {
            // Import.
            FileLocation inputFile = new FileLocation(SelectedFileToExport);
            IOResult<PoiService> import = PoiServiceImporters.Instance.Import(inputFile);
            if (!import.Successful)
            {
                MessageBox.Show("Error importing " + SelectedFileToExport + ".\n" + import.Exception, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load the data for the PoiService as well.
            if (!import.Result.ContentLoaded)
            {
                try
                {
                    DataServiceIO.LoadPoiServiceData(import.Result, inputFile);
                }
                catch (Exception er) // Will throw exception if we read from any source but a DS.                    
                {
                    MessageBox.Show("Error importing " + SelectedFileToExport + ".\n" + er, "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            // Export.
            SelectedPoiServiceExporter.IncludeMetaData = IncludeMetaData;
            IOResult<FileLocation> exportResult = SelectedPoiServiceExporter.ExportData(import.Result, null); // No template; use default.

            // Inform the user.
            if (exportResult.Successful)
            {
                MessageBox.Show("Ready converting to " + SelectedPoiServiceExporter.DataFormatExtension + ".\n" + exportResult.Result, "Ready", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error converting to " + SelectedPoiServiceExporter.DataFormatExtension + ".\n" + exportResult.Exception, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Export DS

        #region Convert templates

        private string _templateConversionPath;
        private SupportedTemplateExtensions _templateConversionFormat = SupportedTemplateExtensions.tjson;

        private void OnChangeConversionTab(object sender, RoutedEventArgs e)
        {
            if (TemplatesTab.IsSelected)
            {
                // TODO The first time we select this, the folder is wrong.
                TemplateConversionPath = AppStateSettings.TemplateFolder;
            }
        }

        public string TemplateConversionPath
        {
            get { return _templateConversionPath; }
            set
            {
                _templateConversionPath = value;
                OnPropertyChanged(() => TemplateConversionPath);
            }
        }

        public SupportedTemplateExtensions TemplateConversionFormat
        {
            get { return _templateConversionFormat; }
            set { _templateConversionFormat = value; }
        }

        private void OnTemplatePathBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog folderBrowserDialog = new WPFFolderBrowserDialog()
            {
                InitialDirectory = _templateConversionPath,
                ShowPlacesList = false,
                Title = "Browse for template folder"
            };
            bool? userSelectedFolder = folderBrowserDialog.ShowDialog(this);
            if (userSelectedFolder ?? false)
            {
                TemplateConversionPath = folderBrowserDialog.FileName;
            }
        }

        private void OnTemplateConvertButtonClick(object sender, RoutedEventArgs e)
        {
            TemplateStore<MetaInfo> templateStore = new TemplateStore<MetaInfo>(new FileLocation(_templateConversionPath));
            templateStore.ConvertTemplates(_templateConversionFormat);
            MessageBox.Show(this, "Templates converted!", "Information", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OnOpenTemplateFolderButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start(_templateConversionPath);
        }

        #endregion Convert templates

        #region Merging DS files

        private PoiService _mergeMainDataService;
        private string _mergeMainDataServiceFile = "<select a file>";
        private ObservableCollection<Label> _mergeMainDataServiceLabels;
        private PoiService _mergeSecondaryDataService;
        private string _mergeSecondaryDataServiceFile = "<select a file>";
        private ObservableCollection<Label> _mergeSecondaryDataServiceLabels;
        private Label _mergeSelectedMainLabel;
        private Label _mergeSelectedSecondaryLabel;

        public string MergeMainDataServiceFile
        {
            get { return _mergeMainDataServiceFile; }
            set
            {
                if (string.Equals(_mergeMainDataServiceFile, value)) return;
                _mergeMainDataServiceFile = value;
                OnPropertyChanged();
                // OnPropertyChanged("CanMergeDataService");
                OnPropertyChanged(() => CanMergeDataService);
            }
        }

        public string MergeSecondaryDataServiceFile
        {
            get { return _mergeSecondaryDataServiceFile; }
            set
            {
                if (string.Equals(_mergeSecondaryDataServiceFile, value)) return;
                _mergeSecondaryDataServiceFile = value;
                OnPropertyChanged();
                OnPropertyChanged(() => CanMergeDataService);
            }
        }

        public Label MergeSelectedMainLabel
        {
            get { return _mergeSelectedMainLabel; }
            set
            {
                _mergeSelectedMainLabel = value;
                // OnPropertyChanged();
                OnPropertyChanged(() => CanMergeDataService);
            }
        }

        public ObservableCollection<Label> MergeMainDataServiceLabels
        {
            get { return _mergeMainDataServiceLabels; }
            set
            {
                _mergeMainDataServiceLabels = value;
                OnPropertyChanged();
            }
        }

        public Label MergeSelectedSecondaryLabel
        {
            get { return _mergeSelectedSecondaryLabel; }
            set
            {
                _mergeSelectedSecondaryLabel = value;
                // OnPropertyChanged();
                // OnPropertyChanged("CanMergeDataService");
                OnPropertyChanged(() => CanMergeDataService);
            }
        }

        public ObservableCollection<Label> MergeSecondaryDataServiceLabels
        {
            get { return _mergeSecondaryDataServiceLabels; }
            set
            {
                _mergeSecondaryDataServiceLabels = value;
                OnPropertyChanged();
            }
        }

        public bool CanMergeDataService
        {
            get { return _mergeMainDataService != null && _mergeSecondaryDataService != null && MergeSelectedMainLabel != null && MergeSelectedSecondaryLabel != null; }
        }

        public bool MergeIncludeSecondaryPois { get; set; }

        public bool MergeExcludeNonExistentSecondaryPois { get; set; }

        public bool MergeStopOnFirstHit { get; set; }

        public bool MergeOverwriteDuplicateLabels { get; set; }

        private bool _mergeIncludeRightSideLabels = true;
        public bool MergeIncludeRightSideLabels
        {
            get { return _mergeIncludeRightSideLabels; }
            set { _mergeIncludeRightSideLabels = value; }
        }

        public bool GenerateGuid
        {
            get { return _generateGuid; }
            set
            {
                if (_generateGuid == value) return;
                _generateGuid = value;
                OnPropertyChanged(() => GenerateGuid);
            }
        }

        private void OnMergeOpenMainFileButtonClick(object sender, RoutedEventArgs e)
        {
            FileLocation file;
            IOResult<PoiService> ioResult = OpenSupportedFileDialog.BrowseAndOpenFile(out file, this);
            if (file == null || ioResult == null)
            {
                return;
            }
            if (!ioResult.Successful)
            {
                Logger.Log("", ioResult.Exception.StackTrace, "", Logger.Level.Error);
                MessageBox.Show(this, ioResult.Exception.Message, "Error opening data service", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            _mergeMainDataService = ioResult.Result;
            MergeMainDataServiceFile = file.LocationString;
            MergeMainDataServiceLabels = MergePopulateLabels(_mergeMainDataService);
        }

        private void MergeOpenSecondaryFileButtonClick(object sender, RoutedEventArgs e)
        {
            FileLocation file;
            IOResult<PoiService> ioResult =
                OpenSupportedFileDialog.BrowseAndOpenFile(out file, this);
            if (file == null || ioResult == null)
            {
                return;
            }
            if (!ioResult.Successful)
            {
                Logger.Log("", ioResult.Exception.StackTrace, "", Logger.Level.Error);
                MessageBox.Show(this, ioResult.Exception.Message, "Error opening data service", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            _mergeSecondaryDataService = ioResult.Result;
            MergeSecondaryDataServiceFile = file.LocationString;
            MergeSecondaryDataServiceLabels = MergePopulateLabels(_mergeSecondaryDataService);
        }

        private static ObservableCollection<Label> MergePopulateLabels(PoiService ps)
        {
            var labelDictionary = new Dictionary<string, Label>();
            foreach (var label in ps.PoIs.SelectMany(poi => poi.Labels))
            {
                Label value;
                if (labelDictionary.TryGetValue(label.Key, out value)) value.Count++;
                else labelDictionary[label.Key] = new Label(label.Key);
            }

            var labels = new ObservableCollection<Label>();
            int count = ps.PoIs.Count;
            foreach (Label label in labelDictionary.Values)
            {
                label.SetPoisCounter(count);
                labels.Add(label);
            }
            return labels;
        }

        private void OnMergeDataServiceButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBoxImage icon; // = MessageBoxImage.None;

            // TODO We cannot deal with files that have multiple poitypes.
            if (_mergeMainDataService.PoITypes.Count > 1 && _mergeSecondaryDataService.PoITypes.Count > 1)
            {
                icon = MessageBoxImage.Error;
                MessageBox.Show(
                    "Cannot merge files. The header of one of the files defines multiple PoI types. We currently support at most one PoI type per header.",
                    "Error", MessageBoxButton.OK, icon);
                return;
            }

            // First ask the user to select a file.
            FileLocation destination = null;
            if (GenerateGuid)
            {
                string filename = PromptFilename();
                if (filename == null)
                {
                    return;
                }
                _mergeMainDataService.Name = filename;
            }
            else
            {
                IOResult<FileLocation> result = SaveSupportedFileDialog.BrowseFile();
                if (result == null || !result.Successful)
                {
                    return;
                }
                destination = result.Result;
                _mergeMainDataService.Name = Path.GetFileNameWithoutExtension(destination.LocationString);
            }

            // Initialize.
            string mainKey = _mergeSelectedMainLabel.Name;
            string secKey = _mergeSelectedSecondaryLabel.Name;

            CsvToDataService.MergeDataService.Merge(_mergeMainDataService, mainKey, _mergeSecondaryDataService, secKey,
                MergeIncludeSecondaryPois, MergeExcludeNonExistentSecondaryPois,
                MergeIncludeRightSideLabels, MergeOverwriteDuplicateLabels,
                MergeStopOnFirstHit, IncludeMetaData, destination, TxtMergeDebugOutput);

            // Inform the user.
            icon = MessageBoxImage.Information;
            string fileName = destination == null ? _mergeMainDataService.FileName : destination.LocationString;
            MessageBox.Show("Ready merging files.\n" + fileName, "Ready", MessageBoxButton.OK, icon);
        }

        // Ask for a file name! Insanely enough, WPF has no input box.
        private static string PromptFilename()
        {
            string filename = null;
            while (filename == null || filename.Trim() == "")
                filename = Microsoft.VisualBasic.Interaction.InputBox("Please enter a file name",
                    "Filename",
                    "MergedFile");
            return filename;
        }

        #endregion Merging DS files

        #region Hierarchic merging

        private ObservableCollection<LayerFileDescription> _hierarchicLayers = new ObservableCollection<LayerFileDescription>();
        private ObservableCollection<CsvHeader> _csvHeaders;
        private bool _generateGuid;

        public ObservableCollection<LayerFileDescription> HierarchicLayers
        {
            get { return _hierarchicLayers; }
            private set { _hierarchicLayers = value; }
        }

        public bool CanStartHierarchicLayerMerge { get { return _hierarchicLayers.Count > 1; } }

        private void OnAddSublayerButtonClick(object sender, RoutedEventArgs e)
        {
            IEnumerable<FileLocation> fileLocations = OpenSupportedFileDialog.BrowseFiles(this);
            if (fileLocations == null || !fileLocations.Any())
            {
                return;
            }
            foreach (var newDescription in fileLocations.Select(fileLocation => new LayerFileDescription(fileLocation)))
            {
                HierarchicLayers.Add(newDescription);
            }

            OnPropertyChanged(() => CanStartHierarchicLayerMerge);
        }

        private void OnRemoveSublayerButtonClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListHierarchicLayers.SelectedIndex;
            if (ListHierarchicLayers.SelectedItem != null)
            {
                HierarchicLayers.Remove((LayerFileDescription)ListHierarchicLayers.SelectedItem);
            }
            if (HierarchicLayers.Count > selectedIndex)
            {
                ListHierarchicLayers.SelectedIndex = selectedIndex;
            }
            else if (HierarchicLayers.Count > 0)
            {
                ListHierarchicLayers.SelectedIndex = 0;
            }
            OnPropertyChanged(() => CanStartHierarchicLayerMerge);
        }

        private void OnMoveSublayerUpButtonClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListHierarchicLayers.SelectedIndex;
            if (selectedIndex > 0 && selectedIndex < HierarchicLayers.Count - 1)
            {
                LayerFileDescription currentLfDatIndex = HierarchicLayers[selectedIndex];
                HierarchicLayers[selectedIndex] = HierarchicLayers[selectedIndex - 1];
                HierarchicLayers[selectedIndex - 1] = currentLfDatIndex;
                ListHierarchicLayers.SelectedIndex = selectedIndex - 1;
            }
        }

        private void OnMoveSublayerDownButtonClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ListHierarchicLayers.SelectedIndex;
            if (selectedIndex < HierarchicLayers.Count - 1 && selectedIndex > -1)
            {
                LayerFileDescription currentLfDatIndex = HierarchicLayers[selectedIndex];
                HierarchicLayers[selectedIndex] = HierarchicLayers[selectedIndex + 1];
                HierarchicLayers[selectedIndex + 1] = currentLfDatIndex;
                ListHierarchicLayers.SelectedIndex = selectedIndex + 1;
            }
        }

        private void OnStartHierarchicalMergeClick(object sender, RoutedEventArgs e)
        {
            if (HierarchicLayers.Count < 2)
            {
                MessageBox.Show("Merging requires at least two sublayers.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            //            var sfd = new SaveFileDialog()
            //            {
            //                Title = "Select or type a file name (NOTE: the layer will be saved in the same folder as the first sublayer)",
            //                Filter = "Data Service Files (.ds)|*.ds",
            //                FilterIndex = 1,
            //                OverwritePrompt = false // There will be a new Guid, so the file will not be overwritten anyway.
            //            };
            //
            //            bool? userClickedOk = sfd.ShowDialog();
            //            if (userClickedOk != true)
            //            {
            //                return;
            //            }
            //
            //            string filename = sfd.FileName;

            // First ask the user to select a file.
            string filename;
            if (GenerateGuid)
            {
                filename = PromptFilename();

            }
            else
            {
                IOResult<FileLocation> result = SaveSupportedFileDialog.BrowseFile();
                if (result == null || !result.Successful || result.Result == null)
                {
                    return;
                }
                filename = result.Result.LocationString;
            }
            if (filename == null)
            {
                return;
            }
            bool createSuperTypePoi = CreateSuperTypePoiOnHierarchicalMergeCheckBox.IsChecked ?? false;

            ProcessingErrors.Clear();

            string finalFilename = HierarchicalMergeDataService.Merge(HierarchicLayers, filename, GenerateGuid, createSuperTypePoi, IncludeMetaData, ProcessingErrors);

            // Inform the user.
            if (finalFilename != null)
            {
                MessageBox.Show("Ready merging files.\n" + finalFilename, "Ready", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error merging files.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion Hierarchic merging

        #region Inspect JSON

        private bool prettifying = false;

        private void OnJsonContainerTextChanged(object sender, TextChangedEventArgs e)
        {
            if (prettifying) return;
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;
            string text = textBox.Text;
            try
            {
                JObject json = JObject.Parse(text);
                prettifying = true;
                textBox.Text = json.ToString(Formatting.Indented);
                prettifying = false;
            }
            catch (Exception err)
            {
                prettifying = true;
                textBox.Text = "That's not valid JSON.\n" + err.Message;
                prettifying = false;
            }
        }

        private void OnClearJsonButtonClick(object sender, RoutedEventArgs e)
        {
            JsonTextBox.Text = "";
        }

        #endregion Inspect JSON

        #region Aggregate files

        private readonly AggregationPolicy _protoTypeNumericAggregationPolicy = new AggregationPolicy { DataIsNumeric = true };
        private readonly AggregationPolicy _prototypeNonNumericAggregationPolicy = new AggregationPolicy { DataIsNumeric = false };

        private string _aggregationDataFile;
        private PoiService _aggregationDataService;

        private bool _aggregateByLabel = true;

        private string _aggregationShapeFile;
        private PoiService _aggregationShapeService;

        private AggregationPolicy.NumericAggregation _aggregationPolicyForAllNumericLabels = AggregationPolicy.NumericAggregation.Sum;
        private AggregationPolicy.NonNumericAggregation _aggregationPolicyForAllNonNumericLabels = AggregationPolicy.NonNumericAggregation.Omit;

        private Dictionary<string, AggregationPolicy> _aggregationPolicyDictionary;

        private string _selectedAggregationLabelDescription;
        private string _selectedAggregationLabelValueExamples;

        public string AggregationDataFile
        {
            get { return _aggregationDataFile; }
            set
            {
                if (Equals(_aggregationDataFile, value)) return;
                _aggregationDataFile = value;

                IOResult<PoiService> ioResult = PoiServiceImporters.Instance.Import(new FileLocation(_aggregationDataFile));
                if (ioResult.Successful)
                {
                    _aggregationDataService = ioResult.Result;
                }
                else
                {
                    MessageBox.Show(this,
                        string.Format("Cannot load file {0}:\n{1}", _aggregationDataFile, ioResult.Exception),
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AggregationDataFile = null; // Remove the reference again.
                    _aggregationDataService = null;
                    _aggregationPolicyDictionary = null; // Also remove the reference here.
                    return;
                }

                if (_aggregationDataService == null)
                {
                    return;
                }

                if (!_aggregationDataService.PoITypes.Any())
                {
                    MessageBox.Show(this,
                        "No feature types are defined in this file.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_aggregationDataService.PoITypes.Count > 1)
                {
                    MessageBox.Show(this,
                        "Multiple feature types are defined in this file. We will work only with the first one.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                BaseContent poiType = _aggregationDataService.PoITypes.First();
                Dictionary<string, AggregationPolicy> aggregationPolicies = new Dictionary<string, AggregationPolicy>();
                foreach (MetaInfo metaInfo in poiType.MetaInfo)
                {
                    AggregationPolicy aggregationPolicy = new AggregationPolicy { Label = metaInfo.Label };
                    switch (metaInfo.Type)
                    {
                        case MetaTypes.number:
                            aggregationPolicy.DataIsNumeric = true;
                            break;
                        default:
                            aggregationPolicy.DataIsNumeric = false;
                            break;
                    }
                    aggregationPolicies[aggregationPolicy.Label] = aggregationPolicy;
                }

                foreach (BaseContent baseContent in _aggregationDataService.PoIs)
                {
                    foreach (KeyValuePair<string, string> keyValuePair in baseContent.Labels)
                    {
                        AggregationPolicy policy;
                        if (aggregationPolicies.TryGetValue(keyValuePair.Key, out policy)) // The label is defined in the MetaInfo
                        {
                            policy.AddValueIfUnique(keyValuePair.Value); // Add the label value to the possible values.
                        }
                    }
                }

                _aggregationPolicyDictionary = aggregationPolicies;
                SelectedAggregationLabelDescription = AggregationLabelDescriptions.Any() ? AggregationLabelDescriptions.First() : null;

                OnPropertyChanged(); // Many properties get changed. Apparently this is the way to notify a change to ALL of them.
            }
        }

        public bool AggregationDataFileChosen
        {
            get { return AggregationDataFile != null; }
        }

        public IEnumerable<string> AggregationLabelDescriptions
        {
            get
            {
                if (_aggregationPolicyDictionary == null) return null;
                return _aggregationPolicyDictionary.Values.Select(aggregationPolicy => aggregationPolicy.Label + " (" + aggregationPolicy.Count + ")");
            }
        }

        public string SelectedAggregationLabelDescription
        {
            get { return _selectedAggregationLabelDescription; }
            set
            {
                _selectedAggregationLabelDescription = value;
                RefreshAggregationLabelValueExample(SelectedAggregationLabel);
            }
        }

        private string SelectedAggregationLabel
        {
            get
            {
                return SelectedAggregationLabelDescription.Substring(0,
                    SelectedAggregationLabelDescription.LastIndexOf(" ", StringComparison.InvariantCulture));
            }
        }

        private void OnAggregateLabelsComboBoxMouseMove(object sender, MouseEventArgs e)
        {
            ComboBoxItem item = sender as ComboBoxItem;
            if (item == null)
            {
                return;
            }

            string aggregationLabelDescription = item.Content.ToString();
            string aggregationLabel = aggregationLabelDescription.Substring(0,
                    aggregationLabelDescription.LastIndexOf(" ", StringComparison.InvariantCulture));

            RefreshAggregationLabelValueExample(aggregationLabel);
        }

        private void RefreshAggregationLabelValueExample(string aggregationLabel)
        {
            if (_aggregationPolicyDictionary == null)
            {
                SelectedAggregationLabelValueExamples = "Unknown label";
                return;
            }
            AggregationPolicy policy;
            SelectedAggregationLabelValueExamples = _aggregationPolicyDictionary.TryGetValue(aggregationLabel, out policy)
                ? policy.ExampleValues
                : "Unknown label";
        }

        public string SelectedAggregationLabelValueExamples
        {
            get { return _selectedAggregationLabelValueExamples; }
            set
            {
                if (value == _selectedAggregationLabelValueExamples) return;
                _selectedAggregationLabelValueExamples = value;
                OnPropertyChanged(() => SelectedAggregationLabelValueExamples);
            }
        }

        public bool AggregateByLabel
        {
            get { return _aggregateByLabel; }
            set
            {
                _aggregateByLabel = value;
                OnPropertyChanged();
            }
        }

        public bool AggregateByGeography
        {
            get { return !AggregateByLabel; }
            set { AggregateByLabel = !value; }
        }

        public string AggregationShapeFile
        {
            get { return _aggregationShapeFile; }
            set
            {
                // Import the file
                IOResult<PoiService> import = PoiServiceImporters.Instance.Import(new FileLocation(value));
                if (!import.Successful)
                {
                    MessageBox.Show(this, "Could not read the regions file.\nError: " + import.Exception, "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                PoiService ass = import.Result;

                // Test whether the file has regions
                bool hasRegions = ass.PoIs.Any(poi => poi.Geometry != null && poi.Geometry.IsRegion);
                if (!hasRegions)
                {
                    MessageBox.Show(this, "This data source file does not contain any regions.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Show a list of labels that could ID the region
                List<string> aggregationShapeFileLabels = new List<string>();
                foreach (KeyValuePair<string, string> kv in ass.PoIs.SelectMany(poi => poi.Labels))
                {
                    if (aggregationShapeFileLabels.Contains(kv.Key))
                    {
                        continue;
                    }
                    aggregationShapeFileLabels.Add(kv.Key);
                }
                AggregationShapeFileLabels = aggregationShapeFileLabels;
                SelectedAggregationShapeFileLabelAsId = AggregationShapeFileLabels.First();

                // At the end.
                _aggregationShapeFile = value;
                _aggregationShapeService = ass;

                OnPropertyChanged();
            }
        }

        public PoiService AggregationShapeService
        {
            get { return _aggregationShapeService; }
        }

        public bool AggregationShapeFileChosen
        {
            get
            {
                return AggregationShapeService != null;
            }
        }

        public IEnumerable<string> AggregationShapeFileLabels { get; set; }

        public string SelectedAggregationShapeFileLabelAsId { get; set; }

        public bool AggregateAddLabelsFromShapeFile { get; set; }

        public IEnumerable<string> NumericAggregationPolicyNames
        {
            get
            {
                return _protoTypeNumericAggregationPolicy.Policies;
            }
        }

        public string AggregationPolicyNameForAllNumericLabels
        {
            get { return _aggregationPolicyForAllNumericLabels.ToString(); }
            set
            {
                AggregationPolicy.NumericAggregation aggregation;
                if (!Enum.TryParse(value, out aggregation)) return;
                if (Equals(aggregation, _aggregationPolicyForAllNumericLabels)) return;
                _aggregationPolicyForAllNumericLabels = aggregation;
                if (AggregationPolicies == null) return;
                foreach (AggregationPolicy aggregationPolicy in AggregationPolicies.Where(aggregationPolicy => aggregationPolicy.DataIsNumeric))
                {
                    aggregationPolicy.NumericAggregationPolicy = aggregation;
                }
            }
        }

        public IEnumerable<string> NonNumericAggregationPolicyNames
        {
            get
            {
                return _prototypeNonNumericAggregationPolicy.Policies;
            }
        }

        public string AggregationPolicyNameForAllNonNumericLabels
        {
            get { return _aggregationPolicyForAllNonNumericLabels.ToString(); }
            set
            {
                AggregationPolicy.NonNumericAggregation aggregation;
                if (!Enum.TryParse(value, out aggregation)) return;
                if (Equals(aggregation, _aggregationPolicyForAllNonNumericLabels)) return;
                _aggregationPolicyForAllNonNumericLabels = aggregation;
                if (AggregationPolicies == null) return;
                foreach (AggregationPolicy aggregationPolicy in AggregationPolicies.Where(aggregationPolicy => !aggregationPolicy.DataIsNumeric))
                {
                    aggregationPolicy.NonNumericAggregationPolicy = aggregation;
                }
            }
        }

        public IEnumerable<AggregationPolicy> AggregationPolicies
        {
            get { return _aggregationPolicyDictionary != null ? _aggregationPolicyDictionary.Values : null; }
        }

        public bool AggregationCanStart
        {
            get
            {
                return (AggregateByLabel && AggregationDataFileChosen && SelectedAggregationLabelDescription != null) ||
                       (AggregateByGeography && AggregationDataFileChosen && AggregationShapeFileChosen);
            }
        }

        private void OnAggregateDataBrowseFileButtonClick(object sender, RoutedEventArgs e)
        {
            FileLocation location;
            IOResult<PoiService> result = OpenSupportedFileDialog.BrowseAndOpenFile(out location, this);
            if (result == null || !result.Successful)
            {
                return;
            }

            AggregationDataFile = location.LocationString;
        }

        private void OnAggregateDataBrowseShapeFileButtonClick(object sender, RoutedEventArgs e)
        {
            FileLocation location = OpenSupportedFileDialog.BrowseFile(this);
            AggregationShapeFile = location != null ? location.LocationString : AggregationShapeFile;
        }

        private void OnStartAggregateRowsButtonClick(object sender, RoutedEventArgs e)
        {
            PoiService aggregate;
            if (AggregateByLabel)
            {
                aggregate = AggregateDataService.Aggregate(_aggregationDataService, SelectedAggregationLabel,
                    _aggregationPolicyDictionary);
            }
            else
            {
                aggregate = AggregateDataService.Aggregate(_aggregationDataService, _aggregationShapeService, SelectedAggregationShapeFileLabelAsId,
                    _aggregationPolicyDictionary, AggregateAddLabelsFromShapeFile);
            }
            IOResult<FileLocation> ioResult = SaveSupportedFileDialog.BrowseAndSaveFile(aggregate, IncludeMetaData,
                this);
            if (ioResult != null && ioResult.Successful)
            {
                MessageBox.Show(this, "The grouped file was successfully saved", "Information", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (ioResult != null && !ioResult.Successful)
            {
                MessageBox.Show(this, "The grouped file was not saved.\nError: " + ioResult.Exception, "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
