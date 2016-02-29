using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CbsShapeToPoiConverter
{
    // TODO This class seems to be unused.
    [DebuggerDisplay("{Description}")]
    public class PurposeAreaYearBin
    {
//        private readonly Dictionary<string, int> purposes = new Dictionary<string, int>();
//        private readonly int[] areas = new int[8];
//        private readonly int[] years = new int[6];

//        public PurposeAreaYearBin(string name)
//        {
//            Name = name;
//        }
//
//        public string Name { get; private set; }
//
//        /// <summary>
//        /// Returns the combined result.
//        /// </summary>
//        public Dictionary<string, int> Result
//        {
//            get
//            {
//                var dict = new Dictionary<string, int> {
//                                                              {"Panden", years.Sum()},
//                                                              {"OppTot50m2", areas[0]},
//                                                              {"OppTot75m2", areas[1]},
//                                                              {"OppTot100m2", areas[2]},
//                                                              {"OppTot125m2", areas[3]},
//                                                              {"OppTot150m2", areas[4]},
//                                                              {"OppTot175m2", areas[5]},
//                                                              {"OppTot200m2", areas[6]},
//                                                              {"OppBoven200m2", areas[7]},
//                                                              {"BouwjaarVoor1900", years[0]},
//                                                              {"BouwjaarVoor1925", years[1]},
//                                                              {"BouwjaarVoor1950", years[2]},
//                                                              {"BouwjaarVoor1975", years[3]},
//                                                              {"BouwjaarVoor2003", years[4]},
//                                                              {"BouwjaarNa2003", years[5]},
//                                                          };
//                foreach (var purpose in purposes)
//                    dict.Add(purpose.Key.Replace(' ', '_'), purpose.Value);
//
//                return dict;
//            }
//        }
//
//        private void AddArea(int area)
//        {
//            if (area < 50) areas[0]++;
//            else if (area < 75) areas[1]++;
//            else if (area < 100) areas[2]++;
//            else if (area < 125) areas[3]++;
//            else if (area < 150) areas[4]++;
//            else if (area < 175) areas[5]++;
//            else if (area < 200) areas[6]++;
//            else areas[7]++;
//        }
//
//        private void AddYear(int year)
//        {
//            if (year < 1900) years[0]++;
//            else if (year < 1925) years[1]++;
//            else if (year < 1950) years[2]++;
//            else if (year < 1975) years[3]++;
//            else if (year < 2003) years[4]++;
//            else years[5]++;
//        }
//
//        private void AddPurpose(string purpose)
//        {
//            if (purposes.ContainsKey(purpose)) purposes[purpose]++;
//            else purposes[purpose] = 1;
//        }
//
//        /// <summary>
//        /// Add a new purpose/area/year entry to the bin
//        /// </summary>
//        /// <param name="purpose"></param>
//        /// <param name="area"></param>
//        /// <param name="year"></param>
//        public void AddPurposeAreaYear(string purpose, decimal area, decimal year)
//        {
//            AddPurpose(purpose);
//            AddArea((int)area);
//            AddYear((int)year);
//        }
//
//        public string Description
//        {
//            get { 
//                var sb = new StringBuilder("opp<50; <75; <100; <125; <150; <175; <200; >=200; jaar<1900; <1925; <1950; <1975; <2003; >=2003");
//                sb.AppendLine(ToString());
//                return sb.ToString();
//            }
//        }
//
//        public override string ToString()
//        {
//            if (purposes.Values.Count == 0) return string.Format("{1}{0} is leeg.{1}", Name, Environment.NewLine);
//
//            var sb = new StringBuilder(string.Format("{1}Naam:\t{0}{1}", Name, Environment.NewLine));
//            sb.AppendLine("Oppervlaktes:\t<50;\t<75;\t<100;\t<125;\t<150;\t<175;\t<200;\t>=200;");
//            sb.Append("\t\t");
//            foreach (var area in areas) sb.Append(area + ";\t");
//            sb.AppendLine("Jaren:\t\t<1900;\t<1925;\t<1950;\t<1975;\t<2003;\t>=2003");
//            sb.Append("\t\t");
//            foreach (var year in years) sb.Append(year + ";\t");
//            sb.Append(Environment.NewLine);
//            if (purposes.Values.Count > 0) sb.AppendLine("Gebruiksdoelen:");
//            foreach (var purpose in purposes)
//                sb.AppendFormat("\t{0}: {1}{2}", purpose.Key, purpose.Value, Environment.NewLine);
//            return sb.ToString();
//        }
    }
}