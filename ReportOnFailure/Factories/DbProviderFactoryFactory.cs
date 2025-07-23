namespace ReportOnFailure.Factories;

using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data.Odbc;
using ReportOnFailure.Enums;


public class DbProviderFactoryFactory : IDbProviderFactoryFactory
{
    public DbProviderFactory GetFactory(DatabaseType dbType)
    {
        switch (dbType)
        {
            case DatabaseType.SqlServer:
                return SqlClientFactory.Instance;
            case DatabaseType.Sqlite:
                return SqliteFactory.Instance;
            case DatabaseType.Odbc:
                return OdbcFactory.Instance;
            default:
                throw new ArgumentOutOfRangeException(nameof(dbType), $"No provider factory is configured for the database type '{dbType}'.");
        }
    }
}