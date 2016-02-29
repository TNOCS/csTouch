using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csCommon.Utils;
using csCommon.Utils.IO;

namespace CsvToDataService.Model
{
    /// <summary>
    /// Object holding a reference to a layer file (i.e. a DS file) and a readable name or description for this file.
    /// </summary>
    public class LayerFileDescription
    {
        private FileLocation _file;
        private String _description;

        /// <summary>
        /// Construct the object based on a file. Automatically derives a default description.
        /// </summary>
        /// <param name="file">The file. Note that this file is not accessed in any way.</param>
        public LayerFileDescription(FileLocation file)
        {
            _file = file;
            _description = GetDefaultDescription(file);
        }

        /// <summary>
        /// Derive default description from the file name.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        private string GetDefaultDescription(FileLocation file)
        {
            string descr = Path.GetFileNameWithoutExtension(file.LocationString);
            if (descr == null)
            {
                descr = "Layer";
            }
            if (descr.StartsWith("~"))
            {
                descr = descr.Substring(1);
            }
            return StripGuid.Strip(descr, '.', ' ');
        }

        /// <summary>
        /// Get or set the layer file.
        /// </summary>
        public FileLocation File
        {
            get { return _file; }
        }

        /// <summary>
        /// Get or set the description of the layer file.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
    }
}
