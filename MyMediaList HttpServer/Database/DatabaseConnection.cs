using Npgsql;

namespace MyMediaList_HttpServer.Database
{
    public class DatabaseConnection
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Connection string to docker postgres container</summary>
        private const string CONNECTION_STRING =
            "Host=host.docker.internal;Port=5432;Database=mymedialist_db;Username=postgres;Password=1234";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // pubilc properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Returns NpgsqlConnection with Connection string</summary>
        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(CONNECTION_STRING);
        }
    }
}
