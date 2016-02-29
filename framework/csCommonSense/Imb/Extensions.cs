
namespace csImb
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    namespace csShared.Utils
    {
        public static class XmlExtensions
        {
            public static XElement GetElement(this XElement source, string name)
            {
                IEnumerable<XElement> result = source.Elements().Where(k => k.Name.LocalName == name);
                if (result.Count() > 0) return result.First();
                return null;
            }

            public static List<XElement> GetElements(this XElement source, string name)
            {
                IEnumerable<XElement> result = source.Elements().Where(k => k.Name.LocalName == name);
                if (result.Count() > 0) return result.ToList();
                return new List<XElement>();
            }

            #region Private fields
            private static readonly Dictionary<RuntimeTypeHandle, XmlSerializer> ms_serializers = new Dictionary<RuntimeTypeHandle, XmlSerializer>();
            #endregion
            #region Public methods
            /// <summary>
            ///   Serialize object to xml string by <see cref = "XmlSerializer" />
            /// </summary>
            /// <typeparam name = "T"></typeparam>
            /// <param name = "value"></param>
            /// <returns></returns>
            public static string ToXml<T>(this T value)
                where T : new()
            {
                var _serializer = GetValue(typeof(T));
                using (var _stream = new MemoryStream())
                {
                    using (var _writer = new XmlTextWriter(_stream, new UTF8Encoding()))
                    {
                        _serializer.Serialize(_writer, value);
                        return Encoding.UTF8.GetString(_stream.ToArray());
                    }
                }
            }
            /// <summary>
            ///   Serialize object to stream by <see cref = "XmlSerializer" />
            /// </summary>
            /// <typeparam name = "T"></typeparam>
            /// <param name = "value"></param>
            /// <param name = "stream"></param>
            public static void ToXml<T>(this T value, Stream stream)
                where T : new()
            {
                var _serializer = GetValue(typeof(T));
                _serializer.Serialize(stream, value);
            }

            /// <summary>
            ///   Deserialize object from string
            /// </summary>
            /// <typeparam name = "T">Type of deserialized object</typeparam>
            /// <param name = "srcString">Xml source</param>
            /// <returns></returns>
            public static T FromXml<T>(this string srcString)
                where T : new()
            {
                var serializer = GetValue(typeof(T));
                using (var stringReader = new StringReader(srcString))
                {
                    try
                    {
                        return (T)serializer.Deserialize(stringReader);
                    }
                    catch (Exception es)
                    {
                        Console.WriteLine(es.Message);
                        return new T();
                    }
                }
            }

            /// <summary>
            ///   Deserialize object from stream
            /// </summary>
            /// <typeparam name = "T">Type of deserialized object</typeparam>
            /// <param name = "source">Xml source</param>
            /// <returns></returns>
            public static T FromXml<T>(this Stream source)
                where T : new()
            {
                var _serializer = GetValue(typeof(T));
                return (T)_serializer.Deserialize(source);
            }
            #endregion
            #region Private methods
            private static XmlSerializer GetValue(Type type)
            {
                XmlSerializer _serializer;
                if (!ms_serializers.TryGetValue(type.TypeHandle, out _serializer))
                {
                    lock (ms_serializers)
                    {
                        if (!ms_serializers.TryGetValue(type.TypeHandle, out _serializer))
                        {
                            _serializer = new XmlSerializer(type);
                            ms_serializers.Add(type.TypeHandle, _serializer);
                        }
                    }
                }
                return _serializer;
            }
            #endregion
        }



    }
}
