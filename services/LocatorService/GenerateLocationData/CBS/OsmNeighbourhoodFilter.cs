using System;
using System.Collections.Generic;
using GenerateLocationData.Model;
using Npgsql;

namespace GenerateLocationData.CBS
{
    public class OsmNeighbourhoodFilter
    {
        private const string Query =
@"SELECT bu_code, bu_naam, b.wk_code, w.wk_naam, b.gm_naam, postcode, 
	ST_AsText(ST_Transform(ST_Centroid(b.geom), 4326)) as center,
	ST_AsText(b.geom) as wktRD, 
	ST_AsText(ST_Transform(b.geom, 4326)) as wkt
FROM buurt_2013_v1 as b, wijk_2013_v1 as w
WHERE w.wk_code = b.wk_code
LIMIT 100;";
        private readonly string connectionString;

        public OsmNeighbourhoodFilter(string connectionString)
        {
            this.connectionString = connectionString;

            Initialize();
        }

        /// <summary>
        /// Get all CBS neighbourhoods, specifically its name, boundary (in WKT), and center.
        /// </summary>
        private void Initialize()
        {
            LocationDescriptions = new List<LocationDescription>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand(Query, conn))
                {
                    try
                    {
                        using (var dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var center      = dr["center"].ToString();
                                var id          = dr["bu_code"].ToString();
                                var name        = dr["bu_naam"].ToString();
                                var wijk        = dr["wk_naam"].ToString();
                                var gemeente    = dr["gm_naam"].ToString();
                                var postcode    = dr["postcode"].ToString();
                                var rdBoundary  = dr["wktRD"].ToString();
                                var wgsBoundary = dr["wkt"].ToString();

                                var locationDescription = new LocationDescription(id, name, center, rdBoundary, wgsBoundary);
                                locationDescription.Features["wijk"    ] = wijk.Substring(7).Trim();
                                locationDescription.Features["gemeente"] = gemeente;
                                locationDescription.Features["postcode"] = postcode;

                                LocationDescriptions.Add(locationDescription);
                            }
                        }
                    }
                    catch (SystemException e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }
            }
        }

        public List<LocationDescription> LocationDescriptions { get; set; }
    }
}