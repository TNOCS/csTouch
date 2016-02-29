using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Utils.IO;
using DataServer;
using Microsoft.Win32;

namespace csCommon.Views.Dialogs
{
    public class OpenSupportedFileDialog
    // TODO Touch table mode.
    {
        public static IEnumerable<FileLocation> BrowseFiles(Window owner = null, IEnumerable<string> excludedExtensions = null)
        {
            var ofd = Browse(owner, true, excludedExtensions); // Multiple files.
            if (ofd == null) return null;
            return ofd.FileNames.Select(file => new FileLocation(file));
        }

        public static FileLocation BrowseFile(Window owner = null, IEnumerable<string> excludedExtensions = null)
        {
            var ofd = Browse(owner, false, excludedExtensions); // One file.
            if (ofd == null) return null;
            return new FileLocation(ofd.FileName);
        }

        private static OpenFileDialog Browse(Window owner, bool multiSelect, IEnumerable<string> excludedExtensions)
        {
            StringBuilder allFileFilterBuilder = new StringBuilder("All supported formats|");
            StringBuilder fileFilterBuilder = new StringBuilder();
            IEnumerable<string> supportedExtensions = PoiServiceImporters.Instance.GetSupportedExtensions(excludedExtensions);
            bool first = true;
            string defaultExt = "";
            foreach (var supportedExtension in supportedExtensions)
            {
                IImporter<FileLocation, PoiService> importer = PoiServiceImporters.Instance.GetImporter(supportedExtension);
                if (first)
                {
                    defaultExt = "." + supportedExtension;
                }
                else
                {
                    fileFilterBuilder.Append('|');
                    allFileFilterBuilder.Append(';');
                }
                fileFilterBuilder.Append(importer.DataFormat)
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
                MessageBox.Show(owner, "Cannot find any importers in the assembly!", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }

            if (owner == null)
            {
                owner = Application.Current.MainWindow;
            }

            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = defaultExt,
                Multiselect = multiSelect, 
                Filter = allFileFilterBuilder.Append('|').Append(fileFilterBuilder).ToString()
            };
            if (ofd.ShowDialog(owner) != true)
            {
                return ofd;
            }
            return ofd;
        }

        public static IOResult<PoiService> BrowseAndOpenFile(out FileLocation file, Window owner = null, IEnumerable<string> excludedExtensions = null)
        {
            file = BrowseFile(owner, excludedExtensions);
            if (file == null)
            {
                return null;
            }

            string selectedExtension = Path.GetExtension(file.LocationString);
            IImporter<FileLocation, PoiService> selectedImporter = PoiServiceImporters.Instance.GetImporter(selectedExtension);
            if (selectedImporter == null)
            {
                return null;
            }
            IOResult<PoiService> importData = selectedImporter.ImportData(file);
            if (importData.Successful)
            {
                if (!importData.Result.ContentLoaded && selectedImporter is DataServiceIO)
                {
                    // This only works with a DS file, which should be the only file format that needs this. TODO Quite ugly hack though.
                    try
                    {
                        DataServiceIO.LoadPoiServiceData(importData.Result, file);
                    }
                    catch (Exception e)
                    {
                        return new IOResult<PoiService>(new Exception("Cannot load PoI Service data.", e));
                    } 
                }
            }
            return importData;
        }
    }
}
