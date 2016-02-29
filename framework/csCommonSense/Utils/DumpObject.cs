using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Utils
{
    public class DumpObject
    {
        public static bool DumpToXml(object @object, string file)
        {
            if (@object == null) return false;
            System.Xml.Serialization.XmlSerializer xmlWriter =
                new System.Xml.Serialization.XmlSerializer(@object.GetType());
            StreamWriter streamWriter = new StreamWriter(file);
            xmlWriter.Serialize(streamWriter, @object);
            streamWriter.Close();
            return true; // Dump succeeded.
        }

    }
}
