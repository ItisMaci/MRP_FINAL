using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MyMediaList_HttpServer.Database
{
    public class PostgresConnectionFactory : IDatabaseConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            return DatabaseConnection.GetConnection();
        }
    }
}
