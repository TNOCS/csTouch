using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace csShared.Utils
{
    // TODO REVIEW Can we move the two methods to ConvertGeoJson?
  public class JSONHelper
  {
    public static string Serialize<T>(T obj)
    {
      var serializer = new DataContractJsonSerializer(obj.GetType(), new DataContractJsonSerializerSettings() {UseSimpleDictionaryFormat = true});
      var ms = new MemoryStream();
      serializer.WriteObject(ms, obj);
      string retVal = Encoding.Default.GetString(ms.ToArray());
      ms.Dispose();
      return retVal;
    }

    public static T Deserialize<T>(string json)
    {
      var obj = Activator.CreateInstance<T>();
      var ms = new MemoryStream(Encoding.Unicode.GetBytes(json));
      var serializer = new DataContractJsonSerializer(obj.GetType(), new DataContractJsonSerializerSettings() { UseSimpleDictionaryFormat = true });
      obj = (T) serializer.ReadObject(ms);
      ms.Close();
      ms.Dispose();
      return obj;
    }
  }
}