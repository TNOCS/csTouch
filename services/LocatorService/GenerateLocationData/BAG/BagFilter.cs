using System;
using System.Collections.Generic;
using GenerateLocationData.Model;
using Npgsql;

namespace GenerateLocationData.BAG
{
    public class BagFilter
    {
        private const string Query =
@"SELECT DISTINCT openbareruimtenaam as straat
FROM (SELECT ST_GeomFromText('{0}', 28992) as buurt) as foo, adres
WHERE ST_Contains(buurt, geopunt)";
        private readonly string connectionString;

        public BagFilter(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Get all CBS neighbourhoods, specifically its name, boundary (in WKT), and center.
        /// </summary>
        public void Enhance(IEnumerable<LocationDescription> locationDescriptions)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var streets = new List<string>();
                foreach (var locationDescription in locationDescriptions)
                {
                    var query = string.Format(Query, locationDescription.RdBoundary);
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        try
                        {
                            using (var dr = command.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    streets.Add(dr["straat"].ToString());
                                }
                            }
                        }
                        catch (SystemException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    locationDescription.Features.Add("straten", string.Join(";", streets));
                }
            }
        }
    }
}