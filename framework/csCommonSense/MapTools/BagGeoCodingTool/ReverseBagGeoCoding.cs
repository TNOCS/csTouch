using System;
using System.Globalization;
using System.Threading;
using csCommon.MapTools.GeoCodingTool;
using csShared;
using ESRI.ArcGIS.Client.Geometry;
using csShared.Utils;
using Npgsql;

namespace csCommon.MapTools.BagGeoCodingTool
{
    public class ReverseBagGeoCoding
    {
        private static readonly AppStateSettings AppState = AppStateSettings.Instance;
        private static readonly string ConnectionString = AppState.Config.Get("BAG.ConnectionSettings", "Server=127.0.0.1;Port=5433;User Id=bag_user;Password=bag4all;Database=bag;");

        private const string Lookup =
@"SELECT ST_Distance(geopunt,ST_Transform(ST_SetSRID(ST_MakePoint({0},{1}),4326),28992)) as d, openbareruimtenaam, huisnummer, huisletter, huisnummertoevoeging, postcode, woonplaatsnaam, gemeentenaam, provincienaam 
FROM adres a, verblijfsobjectpandactueel v 
WHERE v.identificatie=a.adresseerbaarobject AND 
ST_DWithin(geopunt, ST_Transform(ST_SetSRID(ST_MakePoint({0},{1}),4326),28992), {2}) ORDER BY d LIMIT 1;";

        //private bool _busy;
        public event EventHandler<ReverseGeocodingCompletedEventArgs> Result;

        private bool isBusy;
    
        public bool RetrieveFormatedAddress(MapPoint pos, bool overule) {
            if (isBusy) return false;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try {
                    isBusy = true;
                    var cmdString = string.Format(Lookup, pos.X, pos.Y, 100); // 100m withing point of interest
                    using (var conn = new NpgsqlConnection(ConnectionString)) {
                        conn.Open();

                        using (var command = new NpgsqlCommand(cmdString, conn)) {
                            using (var dr = command.ExecuteReader()) {
                                while (dr.Read()) {
                                    var postcode = dr.GetValue(dr.GetOrdinal("postcode")) as string ?? string.Empty;
                                    var woonplaats = dr.GetValue(dr.GetOrdinal("woonplaatsnaam")) as string ?? string.Empty;
                                    //var gemeente = dr.GetValue(dr.GetOrdinal("gemeentenaam")) as string ?? string.Empty;
                                    //var provincie = dr.GetValue(dr.GetOrdinal("provincienaam")) as string ?? string.Empty;
                                    var huisnummer = dr.GetValue(dr.GetOrdinal("huisnummer"));
                                    var adres = string.Format("{0} {1}{2}{3}",
                                        dr.GetValue(dr.GetOrdinal("openbareruimtenaam")) as string ?? string.Empty,
                                        huisnummer is Decimal
                                            ? ((Decimal) huisnummer).ToString(CultureInfo.InvariantCulture)
                                            : string.Empty,
                                        dr.GetValue(dr.GetOrdinal("huisletter")) as string ?? string.Empty,
                                        dr.GetValue(dr.GetOrdinal("huisnummertoevoeging")) as string ?? string.Empty);

                                    var a = new Address {
                                        Country          = "Nederland",
                                        Locality         = woonplaats,
                                        PostalCode       = postcode,
                                        FormattedAddress = adres,
                                        StreetNumber     = huisnummer.ToString(),
                                        Position         = pos
                                    };
                                    OnResult(a);
                                }
                            }
                        }
                    }
                    isBusy = false;
                }
                catch (NpgsqlException e) {
                    Logger.Log("Geocoding", "Error retrieving geocoding data from BAG", e.Message, Logger.Level.Error, true);
                }
            });
            return true;
        }

        private void OnResult(Address address) {
            var handler = Result;
            if (handler != null) handler(this, new ReverseGeocodingCompletedEventArgs(address.FormattedAddress, string.Empty, address));
        }

    }
}
