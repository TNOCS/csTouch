using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csCommon.Utils.IO;

namespace csCommon.Utils.IO
{
    public interface IExporter<in TIn, TOut> : IIO where TIn : class where TOut : class
    {
        /// <summary>
        /// Some exporters may contain capabilities to validate the export format. Others may not. In any case, validation can be en- or disabled.
        /// </summary>
        bool EnableValidation { get; set; }

        /// <summary>
        /// Export the given data to the format supported by this exporter.
        /// </summary>
        /// <param name="source">The file to read.</param>
        /// <param name="template">Optional template; for example, should the exporter export a file, you can indicate here where it should be saved.</param>
        /// <param name="options">Optional options that may vary for each exporter (which is quite ugly).</param>
        /// <returns>The export result; either a successful one with whatever you export to, or an error with a message.</returns>
        IOResult<TOut> ExportData(TIn source, TOut template); // , Dictionary<string, object> options

        /// <summary>
        /// Some file formats, e.g. CSV, can only store the actual data, no data about the data. Others, such as XML or JSON, may have headers.
        /// </summary>
        /// <returns>Whether metadata is supported.</returns>
        bool SupportsMetaData { get; }

        /// <summary>
        /// Some file formats support storing metadata about the data (e.g. formatting information). 
        /// </summary>
        bool IncludeMetaData { set; }
    }
}