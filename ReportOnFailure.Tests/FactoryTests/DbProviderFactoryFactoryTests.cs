namespace ReportOnFailure.Tests.FactoryTests;

using System.Data;
using ReportOnFailure.Factories;
using ReportOnFailure.Interfaces;
using ReportOnFailure.Enums;

public class dbProviderFactoryFactoryTests
{
    [Fact]
    public void Create_ReturnsSqlServerProviderFactory()
    {

        var factoryfactory = new DbProviderFactoryFactory();
        var factory = factoryfactory.GetFactory(DatabaseType.SqlServer);

        Assert.NotNull(factory);
        Assert.True(factory.GetType() == typeof(Microsoft.Data.SqlClient.SqlClientFactory));
    }

    [Fact]
    public void Create_ReturnsOdbcProviderFactory()
    {

        var factoryfactory = new DbProviderFactoryFactory();
        var factory = factoryfactory.GetFactory(DatabaseType.Odbc);
 
        Assert.NotNull(factory);
        Assert.True(factory.GetType() == typeof(System.Data.Odbc.OdbcFactory));
    }

    [Fact]
            public void Create_ReturnsSqliteProviderFactory()
    {

        var factoryfactory = new DbProviderFactoryFactory();
        var factory = factoryfactory.GetFactory(DatabaseType.Sqlite);

        Assert.NotNull(factory);
        Assert.True(factory.GetType() == typeof(Microsoft.Data.Sqlite.SqliteFactory));
    }
}