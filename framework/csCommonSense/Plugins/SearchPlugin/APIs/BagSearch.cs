using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using csCommon.MapPlugins.Search;
using csGeoLayers;
using csShared;
using csShared.Utils;
using DataServer;
using Npgsql;

namespace csCommon.Plugins.SearchPlugin.APIs
{
    public class BagSearch : BaseSearch
    {
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;
        private static readonly string ConnectionString = AppState.Config.Get("BAG.ConnectionSettings", "Server=127.0.0.1;Port=5433;User Id=bag_user;Password=bag4all;Database=bag;");

        public override bool IsOnline { get { return false; } }

        public override string Title { get { return "BAG"; } }

        public override void DoSearch(MapPlugins.Search.SearchPlugin plugin)
        {
            base.DoSearch(plugin);
            if (string.IsNullOrEmpty(Key) || Key.Length <= 3) return;
            DoRequest();
        }

        /*
 * https://github.com/opengeogroep/NLExtract/issues/84
 * 
select * from
adres a,
verblijfsobjectpandactueel v
where
v.identificatie=a.adresseerbaarobject and
textsearchable_adres @@ to_tsquery('Enschedepad            & 76 & Almere')
 * 
 * https://github.com/opengeogroep/NLExtract/issues/117
 * 
SELECT to_tsvector('dutch', 'laan van oceanië'), 
to_tsvector('dutch', 'laan van oceanië') 
@@ to_tsquery('dutch','laan&van&oceanie');
 */
        private const string AddresLookupCommand           = @"SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location, openbareruimtenaam, huisnummer, huisletter, huisnummertoevoeging, postcode, woonplaatsnaam, gemeentenaam, provincienaam FROM adres a, verblijfsobjectpandactueel v WHERE v.identificatie=a.adresseerbaarobject AND textsearchable_adres @@ to_tsquery('{0}') LIMIT 10;";
        private const string ZipCodeAndNumberLookupCommand = @"SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location, openbareruimtenaam, huisnummer, huisletter, huisnummertoevoeging, postcode, woonplaatsnaam, gemeentenaam, provincienaam FROM adres a, verblijfsobjectpandactueel v WHERE v.identificatie=a.adresseerbaarobject AND a.postcode='{0}{1}' AND a.huisnummer={2} LIMIT 10;";
        private const string ZipCodeLookupCommand          = @"SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location, openbareruimtenaam, huisnummer, huisletter, huisnummertoevoeging, postcode, woonplaatsnaam, gemeentenaam, provincienaam FROM adres a, verblijfsobjectpandactueel v WHERE v.identificatie=a.adresseerbaarobject AND a.postcode='{0}{1}' LIMIT 10;";

        private readonly Dictionary<string, string> substitutions = new Dictionary<string, string> {
            {"den haag", "gravenhage"},
            {"den bosch", "hertogenbosch"},
        };

        private void DoRequest()
        {
            Plugin.IsLoading = true;
            var bagResult = Plugin.CreatePoiTypeResult("BAG", Colors.CornflowerBlue);
            bagResult.Style.InnerTextColor = Colors.Black;
            bagResult.AddMetaInfo("Adres", "Adres");
            bagResult.AddMetaInfo("Postcode", "Postcode");
            bagResult.AddMetaInfo("Woonplaats", "Woonplaats");
            bagResult.AddMetaInfo("Gemeente", "Gemeente");
            bagResult.AddMetaInfo("Provincie", "Provincie");
            Plugin.SearchService.PoITypes.Add(bagResult);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string cmdString;
                    if (ZipCodeAndHouseNumberRegex.IsMatch(Key))
                    {
                        // Search for a zip code
                        var ms = ZipCodeAndHouseNumberRegex.Match(Key);

                        var zipNumbers  = ms.Groups["zipNumber"]       .Value.Trim();
                        var zipLetters  = ms.Groups["zipLetter"]       .Value.Trim().ToUpper();
                        var houseNumber = ms.Groups["houseNumberStart"].Value.Trim();

                        if (string.IsNullOrEmpty(houseNumber)) houseNumber = ms.Groups["houseNumberEnd"].Value.Trim();
                        cmdString = string.IsNullOrEmpty(houseNumber)
                            ? string.Format(ZipCodeLookupCommand,          zipNumbers, zipLetters)
                            : string.Format(ZipCodeAndNumberLookupCommand, zipNumbers, zipLetters, houseNumber);
                    }
                    else
                    {
                        // Search for a generic address
                        var input = substitutions.Keys.Aggregate(Key, (current, key) => current.Replace(key, substitutions[key]));
                        var keywords = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        cmdString = string.Format(AddresLookupCommand, string.Join("&", keywords));
                    }

                    var pois = new ContentList();
                    using (var conn = new NpgsqlConnection(ConnectionString))
                    {
                        conn.Open();

                        using (var command = new NpgsqlCommand(cmdString, conn))
                        {
                            using (var dr = command.ExecuteReader())
                            {
                                var count = 0;
                                while (dr.Read())
                                {
                                    count++;

                                    var position = ConvertPointZToPosition(dr.GetString(dr.GetOrdinal("location")));

                                    var p = new PoI
                                    {
                                        Service = Plugin.SearchService,
                                        InnerText = count.ToString(CultureInfo.InvariantCulture),
                                        PoiTypeId = bagResult.ContentId,
                                        PoiType = bagResult,
                                        Position = position
                                    };
                                    AddAddress(p, dr);
                                    p.UpdateEffectiveStyle();
                                    pois.Add(p);
                                }
                            }
                        }
                    }

                    Application.Current.Dispatcher.Invoke(
                        delegate
                        {
                            lock (Plugin.ServiceLock)
                            {
                                pois.ForEach(p => Plugin.SearchService.PoIs.Add(p));
                            }
                            Plugin.IsLoading = false;
                        });
                }
                catch (NpgsqlException e)
                {
                    Logger.Log("BAG Geocoding", "Error finding location", e.Message, Logger.Level.Error, true);
                    Plugin.IsLoading = false;
                }
            });
        }

        private static Position ConvertPointZToPosition(string position)
        {
            if (string.IsNullOrEmpty(position)) return null;
            var positionString = position.Substring(9).Split(new[] { ' ', ')' }, 4, StringSplitOptions.RemoveEmptyEntries);
            double longitude;
            if (!double.TryParse(positionString[0], NumberStyles.Number, CultureInfo.InvariantCulture, out longitude))
                return null;
            double latitude;
            if (!double.TryParse(positionString[1], NumberStyles.Number, CultureInfo.InvariantCulture, out latitude))
                return null;
            return new Position(longitude, latitude);
        }

        private static void AddAddress(BaseContent poi, IDataRecord dr)
        {
            var huisnummer = dr.GetValue(dr.GetOrdinal("huisnummer"));
            var adres = string.Format("{0} {1}{2}{3}",
                dr.GetValue(dr.GetOrdinal("openbareruimtenaam")) as string ?? string.Empty,
                huisnummer is Decimal ? ((Decimal)huisnummer).ToString(CultureInfo.InvariantCulture) : string.Empty,
                dr.GetValue(dr.GetOrdinal("huisletter")) as string ?? string.Empty,
                dr.GetValue(dr.GetOrdinal("huisnummertoevoeging")) as string ?? string.Empty);
            var postcode = dr.GetValue(dr.GetOrdinal("postcode")) as string ?? string.Empty;
            var woonplaats = dr.GetValue(dr.GetOrdinal("woonplaatsnaam")) as string ?? string.Empty;
            var gemeente = dr.GetValue(dr.GetOrdinal("gemeentenaam")) as string ?? string.Empty;
            var provincie = dr.GetValue(dr.GetOrdinal("provincienaam")) as string ?? string.Empty;

            poi.Name = string.Format("{0}, {1}, {2}", adres, postcode, woonplaats);

            poi.Labels["Adres"] = adres;
            poi.Labels["Postcode"] = postcode;
            poi.Labels["Woonplaats"] = woonplaats;
            poi.Labels["Gemeente"] = gemeente;
            poi.Labels["Provincie"] = provincie;
        }

        //  using System.Text.RegularExpressions;

        /// <summary>
        ///  Regular expression built for C# on: Wed, Mar 5, 2014, 01:08:22 PM
        ///  Using Expresso Version: 3.0.4750, http://www.ultrapico.com
        ///  
        ///  A description of the regular expression:
        ///  
        ///  Whitespace, any number of repetitions
        ///  Match expression but don't capture it. [^\s*|(?<houseNumberStart>\d{1,5})|.*]
        ///      Select from 3 alternatives
        ///          ^\s*
        ///              Beginning of line or string
        ///              Whitespace, any number of repetitions
        ///          [houseNumberStart]: A named capture group. [\d{1,5}]
        ///              Any digit, between 1 and 5 repetitions
        ///          Any character, any number of repetitions
        ///  ,?\s*
        ///      ,, zero or one repetitions
        ///      Whitespace, any number of repetitions
        ///  [zipNumber]: A named capture group. [\d{4}]
        ///      Any digit, exactly 4 repetitions
        ///  Whitespace, any number of repetitions
        ///  [zipLetter]: A named capture group. [[a-zA-Z]{2}]
        ///      Any character in this class: [a-zA-Z], exactly 2 repetitions
        ///  ,, zero or one repetitions
        ///  [houseNumberEnd]: A named capture group. [\d{1,5}.*|.*]
        ///      Select from 2 alternatives
        ///          \d{1,5}.*
        ///              Any digit, between 1 and 5 repetitions
        ///              Any character, any number of repetitions
        ///          Any character, any number of repetitions
        ///  End of line or string
        ///  
        /// </summary>
        private static readonly Regex ZipCodeAndHouseNumberRegex = new Regex(
            "\\s*(?:^\\s*|(?<houseNumberStart>\\d{1,5})|.*),?\\s*(?<zipNumber>\\d{4})\\s*(?<zipLetter>[a-zA-Z]{2}),?(?<houseNumberEnd>\\d{1,5}.*|.*)$",
            RegexOptions.Multiline
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );

        //// Replace the matched text in the InputText using the replacement pattern
        // string result = regex.Replace(InputText,regexReplace);

        //// Split the InputText wherever the regex matches
        // string[] results = regex.Split(InputText);

        //// Capture the first Match, if any, in the InputText
        // Match m = regex.Match(InputText);

        //// Capture all Matches in the InputText
        // MatchCollection ms = regex.Matches(InputText);

        //// Test to see if there is a match in the InputText
        // bool IsMatch = regex.IsMatch(InputText);

        //// Get the names of all the named and numbered capture groups
        // string[] GroupNames = regex.GetGroupNames();

        //// Get the numbers of all the named and numbered capture groups
        // int[] GroupNumbers = regex.GetGroupNumbers();



    }
}
