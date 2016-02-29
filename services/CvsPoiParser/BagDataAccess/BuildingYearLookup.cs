using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace BagDataAccess
{
    /*

  SELECT pandactueelbestaand.bouwjaar, 
    verblijfsobjectgebruiksdoel.gebruiksdoelverblijfsobject
  FROM adres, verblijfsobjectactueelbestaand, 
    verblijfsobjectgebruiksdoel, verblijfsobjectpandactueel, 
    pandactueelbestaand
  WHERE adres.postcode='9981CJ' and adres.huisnummer=100 AND adres.adresseerbaarobject = verblijfsobjectactueelbestaand.identificatie AND verblijfsobjectgebruiksdoel.identificatie = verblijfsobjectactueelbestaand.identificatie AND verblijfsobjectpandactueel.identificatie = verblijfsobjectactueelbestaand.identificatie AND verblijfsobjectpandactueel.gerelateerdpand = pandactueelbestaand.identificatie;

     */

    public class BuildingYearLookup
    {
        private static string connectionString;

        public BuildingYearLookup(string connectionString)
        {
            BuildingYearLookup.connectionString = connectionString;            
        }

        public decimal FromZipCode(string zipCode, int houseNumber)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var command = new NpgsqlCommand(
                                "SELECT pandactueelbestaand.bouwjaar, verblijfsobjectgebruiksdoel.gebruiksdoelverblijfsobject FROM adres, verblijfsobjectactueelbestaand, verblijfsobjectgebruiksdoel, verblijfsobjectpandactueel, pandactueelbestaand WHERE adres.postcode=:zipcode and adres.huisnummer=:huisnummer AND adres.adresseerbaarobject = verblijfsobjectactueelbestaand.identificatie AND verblijfsobjectgebruiksdoel.identificatie = verblijfsobjectactueelbestaand.identificatie AND verblijfsobjectpandactueel.identificatie = verblijfsobjectactueelbestaand.identificatie AND verblijfsobjectpandactueel.gerelateerdpand = pandactueelbestaand.identificatie;",
                                conn))
                    {
                        // Now add the parameter to the parameter collection of the command specifying its type.
                        command.Parameters.Add(new NpgsqlParameter("zipcode", NpgsqlDbType.Varchar, 6));
                        command.Parameters.Add(new NpgsqlParameter("huisnummer", NpgsqlDbType.Integer));

                        // Now, add a value to it and later execute the command as usual.
                        command.Parameters[0].Value = zipCode;
                        command.Parameters[1].Value = houseNumber;
                        using (var dr = command.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                return (decimal)dr[0];
                                //Console.WriteLine(dr[0]);
                                //Console.WriteLine(dr[1]);
                                // if (int.TryParse(dr[0],))
                            }

                        }
                        return -1;
                    }
                }
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
    }
}
