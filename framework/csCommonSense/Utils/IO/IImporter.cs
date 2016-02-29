using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csCommon.Utils.IO;

namespace csCommon.Utils.IO
{
    public interface IImporter<in TIn, TOut> : IIO where TIn : class where TOut : class
    {
        /// <summary>
        /// Import the given data in the format supported by this importer, into the required data.
        /// </summary>
        /// <param name="source">The data to read. Commonly, this is probably a FileLocation.</param>
        /// <returns>The import result; either a successful one with whatever you convert to, or an error with a message.</returns>
        IOResult<TOut> ImportData(TIn source);

        /// <summary>
        /// Some file formats, e.g. CSV, can only store the actual data, no data about the data. Others, such as XML or JSON, may have headers.
        /// </summary>
        /// <returns>Whether metadata is supported.</returns>
        bool SupportsMetaData { get; }
    }
}