using System;
using System.Data;

namespace Neurotoxin.Soulshot
{
    public class DbContext : IDisposable
    {
        protected readonly IDataEngine DataEngine;

        public string ConnectionString
        {
            get { return DataEngine.ConnectionString; }
            set { DataEngine.ConnectionString = value; }
        }

        public IDbConnection Connection => DataEngine.Connection;

        public DbContext(IDbConnection connection)
        {
            //TODO: config
            DataEngine = new SqlServerDataEngine(connection);
        }

        public void Dispose()
        {
            DataEngine.Dispose();
        }
    }
}