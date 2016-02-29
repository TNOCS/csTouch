using Npgsql;
using System;

namespace BagDataAccess
{
    /// <summary>
    /// Tests whether the BAG (Postgress + Postgis Database is accessible)
    /// </summary>
    public static class BagAccessible
    {
        public static string ConnectionString { get; private set; }

        public static bool IsAccessible(string connectionString, out Exception exception)
        {
            ConnectionString = connectionString;
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                }
                exception = null;
                return true;
            }
            catch (SystemException e)
            {
                exception = e;
                return false;
            }            
        }

        public static bool IsAccessible(string connectionString)
        {
            Exception e;
            return IsAccessible(connectionString, out e);
        }
    }
}