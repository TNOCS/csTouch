using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using ESRI.ArcGIS.Client;

namespace csShared.Utils
{
  public static class Extensions
  {
    private static readonly Action EmptyDelegate = delegate { };

    public static void BinarySerializeObject(this object obj, string fileName)
    {
        IFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        formatter.Serialize(stream, obj);
        stream.Close();
    }

    public static long Time(this Stopwatch sw, Action action)
    {
        sw.Reset();
        sw.Start();
        action();
        
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
      
      public static void GetAllSubLayers(this Layer layer, string path, ref Dictionary<string, Layer> results)
      {
          string p = path + @"\" + layer.ID;
          if (!results.ContainsKey(p)) results.Add(p, layer);
          if (layer is GroupLayer)
          {
              foreach (var sl in ((GroupLayer)layer).ChildLayers)
              {
                  sl.GetAllSubLayers(path + @"\" + sl.ID, ref results);
              }
          }
      }

    public static void Refresh(this UIElement uiElement)
    {
      uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
    }

    public static IEnumerable<T> Values<T>(this T en) where T : struct
    {
      if (!typeof(T).IsEnum)
			throw new InvalidOperationException();
      return Enum.GetValues(typeof(T)).Cast<T>();
    }
  }

  public static class SHA1Hash
  {
    private static object _lock = new object();

    private static SHA1CryptoServiceProvider cryptoTransformSHA1;

    public static SHA1CryptoServiceProvider CryptoProvider
    {
      get { return cryptoTransformSHA1 ?? (cryptoTransformSHA1 = new SHA1CryptoServiceProvider()); }
    }

    public static long GetSHA1Hash(this string stringToHash)
    {
      return string.IsNullOrWhiteSpace(stringToHash) ? 0 : (Hash(stringToHash, Encoding.Default));
    }

    public static long Hash(string stringToHash, Encoding enc)
    {
      try
      {
        lock (_lock)
        {
          return BitConverter.ToInt64(CryptoProvider.ComputeHash(enc.GetBytes(stringToHash)), 0);
        }
      }
      catch (Exception)
      {

        return 0;
      }
      
    }
  }
}
