using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Utils.IO;
using csGeoLayers;
using DataServer;
using Microsoft.Win32;

namespace csCommon.Views.Dialogs
{
    public class SaveSupportedFileDialog
    // TODO Touch table mode.
    {
        public static IOResult<FileLocation> BrowseAndSaveFile(PoiService content, bool saveMetaData = true, Window owner = null, IEnumerable<string> excludedExtensions = null, string nameFormatString = "")
        {
            IOResult<FileLocation> browseFile = BrowseFile(content, owner, excludedExtensions);
            if (browseFile == null || !browseFile.Successful)
            {
                return browseFile;
            }
            var extension = Path.GetExtension(browseFile.Result.LocationString);
            if (extension == null ||
                (string.IsNullOrEmpty(nameFormatString) ||
                 !extension.Equals(".json", StringComparison.InvariantCultureIgnoreCase)))
                return PoiServiceExporters.Instance.Export(content, browseFile.Result, saveMetaData);
            if (string.IsNullOrEmpty(nameFormatString))
                return PoiServiceExporters.Instance.Export(content, browseFile.Result, saveMetaData);
            var poiType = content.PoITypes.FirstOrDefault();
            if (poiType != null)
            {
                poiType.MetaInfo.Insert(0, new MetaInfo
                {
                    Label            = "Name",
                    Title            = "Name",
                    IsEditable       = false,
                    IsSearchable     = true,
                    VisibleInCallOut = true,
                    Type             = MetaTypes.stringFormat,
                    StringFormat     = nameFormatString
                });
            }
            content.PoIs.ForEach(p => p.Labels.Remove("Name"));
            return PoiServiceExporters.Instance.Export(content, browseFile.Result, saveMetaData);
        }

        public static IOResult<FileLocation> BrowseFile(PoiService content = null, Window owner = null, IEnumerable<string> excludedExtensions = null)
        {
            if (owner == null)
            {
                owner = Application.Current.MainWindow;
            }

            StringBuilder allFileFilterBuilder = new StringBuilder("All supported formats|");
            StringBuilder fileFilterBuilder = new StringBuilder();
            IEnumerable<string> supportedExtensions = PoiServiceExporters.Instance.GetSupportedExtensions(excludedExtensions);
            bool first = true;
            string defaultExt = "";
            foreach (var supportedExtension in supportedExtensions)
            {
                IExporter<PoiService, FileLocation> exporter = PoiServiceExporters.Instance.GetExporter(supportedExtension);
                if (first)
                {
                    defaultExt = "." + supportedExtension;
                }
                else
                {
                    fileFilterBuilder.Append('|');
                    allFileFilterBuilder.Append(';');
                }
                fileFilterBuilder.Append(exporter.DataFormat)
                    .Append(" (*.")
                    .Append(supportedExtension)
                    .Append(")")
                    .Append("|*.")
                    .Append(supportedExtension);
                allFileFilterBuilder.Append("*.").Append(supportedExtension);
                first = false;
            }
            if (fileFilterBuilder.Length == 0)
            {
                MessageBox.Show(owner, "Cannot find any exporters in the assembly!", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }

            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = defaultExt,
                Filter = allFileFilterBuilder.Append('|').Append(fileFilterBuilder).ToString()
            };

            if (content != null && !string.IsNullOrEmpty(content.Folder))
            {
                sfd.InitialDirectory = content.Folder;
            }

            if (sfd.ShowDialog(owner) == true)
            {
                return new IOResult<FileLocation>(new FileLocation(sfd.FileName));
            }

            return null;
        }
    }
}
