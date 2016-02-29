using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace csCommon.Utils
{
    public static class ColorExtensions
    {
        public static String ToHex(this System.Windows.Media.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }

    public class ColorTemplate
    {
        //ColorName;NumOfColors;Type;CritVal;ColorNum;ColorLetter;R;G;B;SchemeType;;;;;;
        public string Name;
        public int NumberOfColors;
        public string Type;
        public Dictionary<int, Color> Colors = new Dictionary<int, Color>();
    }

    public class ColorBrewer
    {

        #region singleton
        private static ColorBrewer _instance;

        public static ColorBrewer GetInstance()
        {
            if (_instance != null) return _instance;
            _instance = new ColorBrewer();
            return _instance;
        }

        #endregion

        public static readonly List<ColorTemplate> ColorTemplates = new List<ColorTemplate>();

        /// <summary>
        /// Read all lines from an embedded resource.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        /// <see cref="http://www.dottodotnet.com/2010/10/read-embedded-resource-text-file-in-c.html"/>
        private string[] ReadAllLinesFromEmbeddedResource(string resourceName)
        {
            var assem = GetType().Assembly;
            using (var stream = assem.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        private ColorBrewer()
        {
            //var colorlines = File.ReadAllLines(Directory.GetCurrentDirectory() + "/resources/data/ColorBrewer.csv");
            var colorlines = ReadAllLinesFromEmbeddedResource("csCommon.Resources.Data.ColorBrewer.csv");
            var name = string.Empty;
            var type = string.Empty;
            var count = 0;
            for (var i = 1; i < colorlines.Count(); i++)
            {
                try
                {
                    var split = colorlines[i].Split(';');
                    if (split.Count() != 16) continue;
                    if (!String.IsNullOrEmpty(split[0]))
                        name = split[0];
                    if (!String.IsNullOrEmpty(split[9]))
                        type = split[9];
                    if (!String.IsNullOrEmpty(split[1]))
                        count = Convert.ToInt16(split[1]);
                    //ColorName;NumOfColors;Type;CritVal;ColorNum;ColorLetter;R;G;B;SchemeType;;;;;;
                    var ct = ColorTemplates.FirstOrDefault(k => k.Name == name && k.NumberOfColors == count && k.Type == type);
                    if (ct == null)
                    {
                        ct = new ColorTemplate
                        {
                            Name = split[0],
                            NumberOfColors = Convert.ToInt16(split[1]),
                            Type = type
                        };
                        ColorTemplates.Add(ct);
                    }
                    var colorIndex = Convert.ToInt16(split[4]);
                    var r = Convert.ToByte(split[6]);
                    var g = Convert.ToByte(split[7]);
                    var b = Convert.ToByte(split[8]);

                    ct.Colors[colorIndex] = Color.FromArgb(255, r, g, b);
                }
                catch (Exception)
                {

                }
            }
        }

    }
}
