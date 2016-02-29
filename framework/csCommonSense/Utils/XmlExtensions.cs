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
        public static XmlDocument ToXmlDocument(this XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }

        public static XDocument ToXDocument(this XmlDocument xmlDocument)
        {
            using (var nodeReader = new XmlNodeReader(xmlDocument))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);
            }
        }

        public static XElement ToXElement<T>(this T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (TextWriter streamWriter = new StreamWriter(memoryStream))
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    var ns = new XmlSerializerNamespaces();
                    ns.Add(string.Empty, string.Empty);
                    xmlSerializer.Serialize(streamWriter, obj, ns);
                    return XElement.Parse(Encoding.UTF8.GetString(memoryStream.ToArray()));
                }
            }
        }

        public static T FromXElement<T>(this XElement xElement)
        {
            using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(xElement.ToString())))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                return (T)xmlSerializer.Deserialize(memoryStream);
            }
        }

        public static XElement GetElement(this XElement source, string name) {
            var result = source.Elements().Where(k => k.Name.LocalName == name);
            return result.Any() ? result.First() : null;
        }

        public static List<XElement> GetElements(this XElement source, string name) {
            var result = source.Elements().Where(k => k.Name.LocalName == name);
            return result.Any() ? result.ToList() : new List<XElement>();
        }

        #region Private fields

        private static readonly Dictionary<RuntimeTypeHandle, XmlSerializer> ms_serializers =
            new Dictionary<RuntimeTypeHandle, XmlSerializer>();

        #endregion

        #region Public methods

        /// <summary>
        ///  Serialize object to xml string by <see cref = "XmlSerializer" />
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "value"></param>
        /// <returns></returns>
        public static string ToXml<T>(this T value)
            where T : new() {
            var _serializer = GetValue(typeof (T));
            using (var _stream = new MemoryStream()) {
                using (var _writer = new XmlTextWriter(_stream, new UTF8Encoding())) {
                    _serializer.Serialize(_writer, value);
                    return Encoding.UTF8.GetString(_stream.ToArray());
                }
            }
        }

        /// <summary>
        ///  Serialize object to stream by <see cref = "XmlSerializer" />
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "value"></param>
        /// <param name = "stream"></param>
        public static void ToXml<T>(this T value, Stream stream)
            where T : new() {
            var _serializer = GetValue(typeof (T));
            _serializer.Serialize(stream, value);
        }

        /// <summary>
        ///  Deserialize object from string
        /// </summary>
        /// <typeparam name = "T">Type of deserialized object</typeparam>
        /// <param name = "srcString">Xml source</param>
        /// <returns></returns>
        public static T FromXml<T>(this string srcString)
            where T : new() {

            var _serializer = GetValue(typeof (T));
            using (var _stringReader = new StringReader(srcString)) {
                using (XmlReader _reader = new XmlTextReader(_stringReader)) {
                    return (T) _serializer.Deserialize(_reader);
                }
            }
        }

        /// <summary>
        ///  Deserialize object from stream
        /// </summary>
        /// <typeparam name = "T">Type of deserialized object</typeparam>
        /// <param name = "source">Xml source</param>
        /// <returns></returns>
        public static T FromXml<T>(this Stream source)
            where T : new() {
            var _serializer = GetValue(typeof (T));
            return (T) _serializer.Deserialize(source);
        }

        #endregion

        #region Private methods

        private static XmlSerializer GetValue(Type type) {
            XmlSerializer _serializer;
            if (!ms_serializers.TryGetValue(type.TypeHandle, out _serializer)) {
                lock (ms_serializers) {
                    if (!ms_serializers.TryGetValue(type.TypeHandle, out _serializer)) {
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