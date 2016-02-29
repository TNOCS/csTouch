using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Types.DataServer.Interfaces
{
    public interface IConvertibleWkt
    {
        /// <summary>
        /// Convert the object to a well-known-text representation.
        /// </summary>
        /// <returns>The well-known-text string.</returns>
        string ToWkt();

        /// <summary>
        /// Read the WKT string and set the appropriate attributes of this object (or a new object).
        /// </summary>
        /// <param name="wkt"></param>
        /// <param name="newObject">Whether to change the object at hand, or to return a new one.</param>
        /// <returns>The object initialized from the string.</returns>
        IConvertibleWkt FromWkt(string wkt, bool newObject = true);
    }
}
