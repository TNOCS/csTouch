using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml.Serialization;
using Caliburn.Micro;

namespace TasksPlugin.DAL {
    [Serializable]
    public class TaskSettings : PropertyChangedBase {
        private const string TasksXml = "TasksPluginSettings.xml";
        private static TaskSettings instance;
        private bool hasTargetCollectionChanged;

        private TaskSettings() {
            RecipientsTop = new ObservableCollection<string>();
            RecipientsBottom = new ObservableCollection<string>();
            RecipientsLeft = new ObservableCollection<string>();
            RecipientsRight = new ObservableCollection<string>();
            if (string.IsNullOrEmpty(Signature)) Signature = "My signature";
        }

        private TaskSettings(Store store) {
            IsTasksServer = store.IsTasksServer;
            Signature = store.Signature;
            if (string.IsNullOrEmpty(Signature)) Signature = "My signature";
            IsSavingTasksToOutputFolder = store.IsSavingTasksToOutputFolder;
            OutputFolderForTasks = store.OutputFolderForTasks;
            SaveAsHtml = store.SaveAsHtml;

            RecipientsTop = new ObservableCollection<string>(store.RecipientsTop);
            RecipientsBottom = new ObservableCollection<string>(store.RecipientsBottom);
            RecipientsLeft = new ObservableCollection<string>(store.RecipientsLeft);
            RecipientsRight = new ObservableCollection<string>(store.RecipientsRight);
        }

        public bool HasTargetCollectionChanged {
            get { return hasTargetCollectionChanged; }
            set {
                if (hasTargetCollectionChanged == value) return;
                hasTargetCollectionChanged = value;
                NotifyOfPropertyChange(() => HasTargetCollectionChanged);
            }
        }

        public ObservableCollection<string> RecipientsTop { get; set; }
        public ObservableCollection<string> RecipientsBottom { get; set; }
        public ObservableCollection<string> RecipientsLeft { get; set; }
        public ObservableCollection<string> RecipientsRight { get; set; }

        public string Signature { get; set; }

        public bool IsTasksServer { get; set; }

        public bool IsSavingTasksToOutputFolder { get; set; }

        public bool SaveAsHtml { get; set; }

        public string OutputFolderForTasks { get; set; }

        /// <summary>
        /// Retreive the settings from isolated storage
        /// </summary>
        [XmlIgnore]
        public static TaskSettings Instance {
            get {
                if (instance != null) return instance;
                var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
                //isoStore.DeleteFile(TasksXml);
                if (isoStore.FileExists(TasksXml)) {
                    using (var isoStream = new IsolatedStorageFileStream(TasksXml, FileMode.Open, isoStore)) {
                        using (var reader = new StreamReader(isoStream)) {
                            var xmlSerializer = new XmlSerializer(typeof (Store));
                            var store = xmlSerializer.Deserialize(reader) as Store;
                            instance = new TaskSettings(store);
                        }
                    }
                }
                else
                    instance = new TaskSettings();
                return instance;
            }
        }

        /// <summary>
        /// Save the settings to isolated storage.
        /// </summary>
        public void Save() {
            HasTargetCollectionChanged = true;

            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (isoStore.FileExists(TasksXml)) isoStore.DeleteFile(TasksXml);
            using (var isoStream = new IsolatedStorageFileStream(TasksXml, FileMode.CreateNew, isoStore)) {
                using (var writer = new StreamWriter(isoStream)) {
                    var xmlSerializer = new XmlSerializer(typeof (Store));
                    var store = new Store(this);
                    xmlSerializer.Serialize(writer, store);
                }
            }
        }
    }

    [Serializable]
    public class Store
    {
        public Store() {}

        public Store(TaskSettings taskSettings) {
            IsTasksServer = taskSettings.IsTasksServer;
            Signature = taskSettings.Signature;
            IsSavingTasksToOutputFolder = taskSettings.IsSavingTasksToOutputFolder;
            OutputFolderForTasks = taskSettings.OutputFolderForTasks;
            SaveAsHtml = taskSettings.SaveAsHtml;

            RecipientsTop = taskSettings.RecipientsTop.ToList();
            RecipientsBottom = taskSettings.RecipientsBottom.ToList();
            RecipientsLeft = taskSettings.RecipientsLeft.ToList();
            RecipientsRight = taskSettings.RecipientsRight.ToList();
        }

        [XmlAttribute]
        public bool IsTasksServer { get; set; }
        [XmlAttribute]
        public string Signature { get; set; }
        [XmlAttribute]
        public bool IsSavingTasksToOutputFolder { get; set; }
        [XmlAttribute]
        public string OutputFolderForTasks { get; set; }
        [XmlAttribute]
        public bool SaveAsHtml { get; set; }

        [XmlArray, XmlArrayItem(ElementName = "RecipientTop")]
        public List<string> RecipientsTop { get; set; }
        [XmlArray, XmlArrayItem(ElementName = "RecipientBottom")]
        public List<string> RecipientsBottom { get; set; }
        [XmlArray, XmlArrayItem(ElementName = "RecipientLeft")]
        public List<string> RecipientsLeft { get; set; }
        [XmlArray, XmlArrayItem(ElementName = "RecipientRight")]
        public List<string> RecipientsRight { get; set; }
    }

}