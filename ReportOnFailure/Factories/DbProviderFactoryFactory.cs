namespace ReportOnFailure.Factories;

using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data.Odbc;
using ReportOnFailure.Enums;


public static class DbProviderFactoryFactory
{
    public static DbProviderFactory GetFactory(DbType dbType)
    {
        switch (dbType)
        {
            case DbType.SqlServer:
                return SqlClientFactory.Instance;

            case DbType.Sqlite:
                return SqliteFactory.Instance;
            case DbType.ODBC:
                return OdbcFactory.Instance;
            default:
                throw new ArgumentOutOfRangeException(nameof(dbType), $"No provider factory is configured for the database type '{dbType}'.");
        }
    }
}