using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using csShared.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Globalization;
using csShared;

namespace BagDataAccess
{
    /// <summary>
    /// Contains functions to lookup an address and receive the lat/lon location in WGS84.
    /// </summary>
    public class LatLongLookup // TODO Perhaps rename this class to better show that it works only for NL addresses. (This goes for the entire BagDataAccess.)
    {
        private static readonly Regex DigitMatcher = new Regex("[^0-9]");
        public static string ConnectionString = AppStateSettings.Instance.Config.Get("BAG.ConnectionString", "Server=127.0.0.1;Port=5432;User Id=bag_user;Password=bag4all;Database=bag;SearchPath=bag8mrt2014,public"); // TODO Providing a default DB connection string in code is quite ugly. 
        private const string BagLatLongLookupCommand = @"SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location FROM adres a, verblijfsobjectpandactueel v WHERE v.identificatie=a.adresseerbaarobject AND textsearchable_adres @@ to_tsquery('{0}') LIMIT 10;";

        private const bool USE_CACHE = false; // TODO Configuration setting.

        private static Dictionary<string, double[]> latLongCache;
        private static string latLongCacheFile = Path.Combine(AppStateSettings.CacheFolder, "csv_latlong_addresses.xml");

        private static void ReadLatLongCache()
        {
            if (latLongCache != null)
            {
                return;
            }
            if (!USE_CACHE)
            {
                latLongCache = new Dictionary<string, double[]>(); // Saves on the number of checks in code below. All cache queries will be misses.
                return;
            }
            using (var reader = new StreamReader(latLongCacheFile))
            {
                try
                {
                    latLongCache = JSONHelper.Deserialize<Dictionary<string, double[]>>(reader.ReadToEnd());
                }
                catch (Exception e)
                {
                    latLongCache = new Dictionary<string, double[]>();
                }
            }
        }

        private static void WriteLatLongCache()
        {
            if (! USE_CACHE) return;
            using (var writer = new StreamWriter(latLongCacheFile))
            {
                writer.Write(JSONHelper.Serialize(latLongCache));
            }
        }

        /// <summary>
        /// Lookup an address in The Netherlands by zip code and house number, and return the longitude and latitude in WGS84.
        /// </summary>
        /// <param name="zipCode">Zip code without spaces</param>
        /// <param name="houseNumber">House number with possible additions</param>
        /// <param name="useFirstPart">If true (default), use the first part of the house number, otherwise the second, 
        /// e.g. houseNumber=2-10 would normally use 2, but if false, use 10 to lookup the location.</param>
        /// <returns>Longitude and latitude in WGS84</returns>
        public static double[] FromZipCode(string zipCode, string houseNumber, bool useFirstPart = true)
        {
            ReadLatLongCache();

            var index = houseNumber.IndexOf('-');
            if (index > 0) 
                houseNumber = useFirstPart 
                    ? houseNumber.Substring(0, index)
                    : houseNumber.Substring(index+1, houseNumber.Length-index-1);
            houseNumber = DigitMatcher.Replace(houseNumber, string.Empty); // Remove huisnummer toevoegingen

            string key = zipCode + houseNumber;
            if (latLongCache.ContainsKey(key)) return latLongCache[key];

            int houseNr;
            if (!int.TryParse(houseNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out houseNr))
                return null;
            var result = FromZipCode(zipCode, houseNr);
            if (result == null && index > 0 && useFirstPart) 
                // Try to resolve the second part of house number to find the location.
                FromZipCode(zipCode, houseNumber, false);

            if (USE_CACHE) latLongCache[key] = result;
            WriteLatLongCache();

            return result;
        }

        /// <summary>
        /// Lookup an address in The Netherlands by zip code and house number, and return the longitude and latitude in WGS84.
        /// </summary>
        /// <param name="zipCode">Zip code</param>
        /// <param name="houseNumber">House number with possible additions</param>
        /// <returns>Longitude and latitude in WGS84</returns>
        public static double[] FromZipCode(string zipCode, int houseNumber)
        {
            ReadLatLongCache();

            if (string.IsNullOrEmpty(zipCode) || houseNumber < 0) return null;
            zipCode = zipCode.Trim().Replace(" ", string.Empty);

            string key = zipCode + houseNumber;
            if (latLongCache.ContainsKey(key)) return latLongCache[key];

            using (var conn = new NpgsqlConnection(BagAccessible.ConnectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand(
                    "SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location FROM adres a, verblijfsobjectpandactueel v " +
                    "WHERE v.identificatie=a.adresseerbaarobject AND a.postcode = :zipcode AND a.huisnummer = :huisnummer",
                    conn))
                {
                    // Now add the parameter to the parameter collection of the command specifying its type.
                    command.Parameters.Add(new NpgsqlParameter("zipcode", NpgsqlDbType.Varchar, 6));
                    command.Parameters.Add(new NpgsqlParameter("huisnummer", NpgsqlDbType.Integer));

                    // Now, add a value to it and later execute the command as usual.
                    command.Parameters[0].Value = zipCode.Trim().Replace(" ", string.Empty);
                    command.Parameters[1].Value = houseNumber;

                    double[] fromZipCode;
                    if (ExecuteAddressLookupQuery(command, out fromZipCode))
                    {
                        if (USE_CACHE) latLongCache[key] = fromZipCode;
                        WriteLatLongCache();

                        return fromZipCode;
                    }
                }
            }
            return null;
        }

        public static double[] FromAddress(string street, int houseNumber, string gemeente)
        {
            ReadLatLongCache();

            if (string.IsNullOrEmpty(gemeente) || string.IsNullOrEmpty(street) || houseNumber < 0) return null;

            string key = street + houseNumber + gemeente;
            if (latLongCache.ContainsKey(key)) return latLongCache[key];

            using (var conn = new NpgsqlConnection(BagAccessible.ConnectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand(
                    "SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location FROM adres a, verblijfsobjectpandactueel v WHERE v.identificatie=a.adresseerbaarobject AND a.openbareruimtenaam = :street AND a.huisnummer = :huisnummer AND a.gemeentenaam = :gemeente",
                    //"SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location FROM geo_adres WHERE postcode = :zipcode AND huisnummer = :huisnummer",
                    conn))
                {
                    // Now add the parameter to the parameter collection of the command specifying its type.
                    command.Parameters.Add(new NpgsqlParameter("street", NpgsqlDbType.Varchar));
                    command.Parameters.Add(new NpgsqlParameter("huisnummer", NpgsqlDbType.Integer));
                    command.Parameters.Add(new NpgsqlParameter("gemeente", NpgsqlDbType.Varchar));

                    // Now, add a value to it and later execute the command as usual.
                    command.Parameters[0].Value = street;
                    command.Parameters[1].Value = houseNumber;
                    command.Parameters[2].Value = gemeente;

                    double[] fromAddress;
                    if (ExecuteAddressLookupQuery(command, out fromAddress))
                    {
                        if (USE_CACHE) latLongCache[key] = fromAddress;
                        WriteLatLongCache();

                        return fromAddress;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Lookup an address in The Netherlands by zip code, and return the longitude and latitude in WGS84.
        /// </summary>
        /// <param name="zipCode">Zip code without spaces</param>
        /// <returns>Longitude and latitude in WGS84</returns>
        public static double[] FromZipCode(string zipCode)
        {
            ReadLatLongCache();

            string key = zipCode;
            if (latLongCache.ContainsKey(key)) return latLongCache[key];

            using (var conn = new NpgsqlConnection(BagAccessible.ConnectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand(
                    "SELECT ST_AsText(ST_TRANSFORM(geopunt, 4326)) AS location FROM adres a, verblijfsobjectpandactueel v " +
                    "WHERE v.identificatie=a.adresseerbaarobject AND a.postcode = :zipcode",
                    conn))
                {
                    // Now add the parameter to the parameter collection of the command specifying its type.
                    command.Parameters.Add(new NpgsqlParameter("zipcode", NpgsqlDbType.Varchar, 6));

                    // Now, add a value to it and later execute the command as usual.
                    command.Parameters[0].Value = zipCode;

                    double[] fromZipCode;
                    if (ExecuteAddressLookupQuery(command, out fromZipCode))
                    {
                        if (USE_CACHE) latLongCache[key] = fromZipCode;
                        WriteLatLongCache();

                        return fromZipCode;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Look up using the street name, house number and city.
        /// </summary>
        /// <param name="streetAndHouseNumber">Street and number, in that order</param>
        /// <param name="city">City</param>
        /// <returns>Lat-long in WGS84, or null</returns>
        public double[] FromAddress(string streetAndHouseNumber, string city)
        {
            ReadLatLongCache();

            string key = streetAndHouseNumber + city;
            if (latLongCache.ContainsKey(key)) return latLongCache[key];

            using (var conn = new NpgsqlConnection(BagAccessible.ConnectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand(string.Format(BagLatLongLookupCommand, string.Format("{0} & {1}", streetAndHouseNumber, city)), conn))
                {
                    double[] fromZipCode;
                    if (ExecuteAddressLookupQuery(command, out fromZipCode))
                    {
                        if (USE_CACHE) latLongCache[key] = fromZipCode;
                        WriteLatLongCache();

                        return fromZipCode;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Look up using the street name, house number and city.
        /// </summary>
        /// <param name="street">Street</param>
        /// <param name="houseNumber">House number</param>
        /// <param name="city">City</param>
        /// <returns></returns>
        public double[] FromAddress(string street, string houseNumber, string city)
        {
            ReadLatLongCache();

            string key = street + houseNumber + city;
            double[] value;
            if (latLongCache.TryGetValue(key, out value)) return value;

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open(); // TODO REVIEW: Merged code correct?
                var tsQuery = string.Format("{0}&{1}&{2}", street, houseNumber, city);
                //replace spaces with amersands
                tsQuery = tsQuery.Replace(" ", "&");
                // add ampersand spacing
                tsQuery = tsQuery.Replace("&", " & ");
                using (var command = new NpgsqlCommand(string.Format(BagLatLongLookupCommand, tsQuery), conn))
                {
                    double[] fromZipCode;
                    if (ExecuteAddressLookupQuery(command, out fromZipCode))
                    {
                        if (USE_CACHE) latLongCache[key] = fromZipCode;
                        WriteLatLongCache();

                        return fromZipCode;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Execute the query and try to parse the result into a double[] with longitude and latitude.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="fromZipCode"></param>
        /// <returns></returns>
        private static bool ExecuteAddressLookupQuery(NpgsqlCommand command, out double[] fromZipCode)
        {
            fromZipCode = null;
            try
            {
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var position = dr[0].ToString();
                        if (string.IsNullOrEmpty(position)) continue;
                        var positionString = position.Substring(9).Split(new[] { ' ', ')' }, 4, StringSplitOptions.RemoveEmptyEntries);
                        double longitude;
                        if (!double.TryParse(positionString[0], NumberStyles.Number, CultureInfo.InvariantCulture, out longitude))
                        {
                            return false;
                        }
                        double latitude;
                        if (!double.TryParse(positionString[1], NumberStyles.Number, CultureInfo.InvariantCulture, out latitude))
                        {
                            return false;
                        }
                        {
                            fromZipCode = new[] { longitude, latitude };
                            return true;
                        }
                    }
                }
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message); // TODO Reroute the exception elsewhere.
                {
                    return false;
                }
            }
            return false;
        }


        /*
         * https://github.com/opengeogroep/NLExtract/issues/84
         * 
            select * from
            adres a,
            verblijfsobjectpandactueel v
            where
            v.identificatie=a.adresseerbaarobject and
            textsearchable_adres @@ to_tsquery('Enschedepad & 76 & Almere')
         * 
         * https://github.com/opengeogroep/NLExtract/issues/117
         * 
            SELECT to_tsvector('dutch', 'laan van oceanië'), 
               to_tsvector('dutch', 'laan van oceanië') 
               @@ to_tsquery('dutch','laan&van&oceanie');
         */
    }
}