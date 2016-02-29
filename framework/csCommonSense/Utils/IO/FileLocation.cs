using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Utils.IO
{
    /// <summary>
    /// C# uses strings for files everywhere. We need to be able to distinguish between strings and files, however. Therefore, we created this class.
    /// TODO This may be replacable by, e.g., an URI or URL class that is already present. Or, we could add whether files are local or remote.
    /// </summary>
    public class FileLocation
    {
        public String LocationString { get; private set; }

        public FileLocation(string locationString) 
        {
            LocationString = locationString;
        }

        public override string ToString()
        {
            return LocationString;
        }
    }
}
