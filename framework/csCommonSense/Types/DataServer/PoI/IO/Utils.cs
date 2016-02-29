using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace csCommon.Types.DataServer.PoI.IO
{
    public static class StringCleanupExtensions
    {
        private const string DelimiterReplace = "[[D]]";
        private const string EscapeCharacterReplace = "[BSLASH]";
        private const string NewLineCharacterReplace = "[RET]";
        private const string TabCharacterReplace = "[T]";
        private const string ReplaceWithEmpty = "";

        public static string RemoveInvalidCharacters(this string input)
        {
            if (input == null) return null;
            return input.Replace("\n", NewLineCharacterReplace).Replace("\\", EscapeCharacterReplace).Replace("\t", TabCharacterReplace).Replace("\"", ReplaceWithEmpty);
        }

        public static string RestoreInvalidCharacters(this string input)
        {
            if (input == null) return null;
            return input.Replace(NewLineCharacterReplace, "\n").Replace(EscapeCharacterReplace, "\\").Replace(TabCharacterReplace, "\t");
        }

        public static string RemoveDelimiters(string input, char delimiter)
        {
            if (input == null) return null;
            return input.Replace("" + delimiter, DelimiterReplace).RemoveInvalidCharacters();
        }

        public static string RestoreDelimiters(string input, char delimiter)
        {
            if (input == null) return null;
            return input.Replace(DelimiterReplace, "" + delimiter).RemoveInvalidCharacters();
        }
    }

    /// <summary>
    /// Source: http://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp
    /// </summary>
    public static class StringCompressor
    {
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }
    }

}
