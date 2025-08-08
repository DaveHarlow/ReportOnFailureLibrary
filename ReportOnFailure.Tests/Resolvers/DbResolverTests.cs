using System.Text;

namespace ReportOnFailure.Tests.Resolvers;

using Enums;
using Factories;
using Microsoft.Data.Sqlite;
using Reporters;
using ReportOnFailure.Resolvers;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;


public class DbResolverTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private const string SharedConnectionString = "DataSource=TestDb;Mode=Memory;Cache=Shared";
    private ResultFormatterFactory _formatterFactory = new ResultFormatterFactory();
    private DbProviderFactoryFactory _dbProviderFactoryFactory = new DbProviderFactoryFactory();


    public DbResolverTests()
    {
        _connection = new SqliteConnection(SharedConnectionString);
        _connection.Open();

        try
        {

            using var transaction = _connection.BeginTransaction();
            var createCommand = _connection.CreateCommand();
            createCommand.Transaction = transaction;

            createCommand.CommandText = @"
                    CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL,
                        IsActive INTEGER NOT NULL
                    );
                    INSERT INTO Users (Id, Name, IsActive) VALUES (1, 'Alice', 1);
                    INSERT INTO Users (Id, Name, IsActive) VALUES (2, 'Bob', 0);
                    INSERT INTO Users (Id, Name, IsActive) VALUES (3, 'Charlie', 1);
                ";
            createCommand.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (SqliteException ex)
        {
            throw new Exception("Error setting up test database: " + ex.Message, ex);
        }
    }

    [Fact]
    public async Task ResolveAsync_WithValidQuery_ReturnsFormattedJson()
    {


        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);


        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT Id, Name, IsActive FROM Users WHERE IsActive = 1 ORDER BY Id")
            .WithResultsFormat(ResultsFormat.Json);

        var result = await resolver.ResolveAsync(reporter);

        Assert.NotNull(result);
        using var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());

        var firstUser = root[0];
        Assert.Equal(1, firstUser.GetProperty("Id").GetInt64());
        Assert.Equal("Alice", firstUser.GetProperty("Name").GetString());
        Assert.Equal(1, firstUser.GetProperty("IsActive").GetInt64());

        var secondUser = root[1];
        Assert.Equal(3, secondUser.GetProperty("Id").GetInt64());
        Assert.Equal("Charlie", secondUser.GetProperty("Name").GetString());
    }

    [Fact]
    public void ResolveSync_WithValidQuery_ReturnsFormattedCsv()
    {

        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);

        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT * FROM Users ORDER BY Id")
            .WithResultsFormat(ResultsFormat.Csv);

        var result = resolver.ResolveSync(reporter);

        var expectedCsv = new StringBuilder()
            .AppendLine("\"Id\",\"Name\",\"IsActive\"")
            .AppendLine("\"1\",\"Alice\",\"1\"")
            .AppendLine("\"2\",\"Bob\",\"0\"")
            .AppendLine("\"3\",\"Charlie\",\"1\"")
            .ToString();
        Assert.Equal(expectedCsv, result);
    }

    [Fact]
    public void ResolveSync_WithValidQuery_ReturnsFormattedText()
    {
        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);

        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT * FROM Users ORDER BY Id")
            .WithResultsFormat(ResultsFormat.Text);

        var result = resolver.ResolveSync(reporter);

        var expected = new StringBuilder()
            .AppendLine("Id: 1 | Name: Alice | IsActive: 1")
            .AppendLine("--------------------")
            .AppendLine("Id: 2 | Name: Bob | IsActive: 0")
            .AppendLine("--------------------")
            .AppendLine("Id: 3 | Name: Charlie | IsActive: 1")
            .AppendLine("--------------------")
            .ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ResolveAsync_WithXmlFormat_ReturnsNotImplementedMessage()
    {
        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);

        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT * FROM Users ORDER BY Id")
            .WithResultsFormat(ResultsFormat.Xml);

        var result = await resolver.ResolveAsync(reporter);

        Assert.Equal("Result formatting for 'Xml' is not yet implemented.", result);
    }

    [Fact]
    public void ResolveSync_WithHtmlFormat_ReturnsNotImplementedMessage()
    {

        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);

        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT * FROM Users ORDER BY Id")
            .WithResultsFormat(ResultsFormat.Html);

        var result = resolver.ResolveSync(reporter);

        Assert.Equal("Result formatting for 'Html' is not yet implemented.", result);
    }




    [Fact]
    public void ResolveSync_WithNoResults_ReturnsEmptyMessage()
    {

        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);

        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT * FROM Users WHERE Id = 999");

        var result = resolver.ResolveSync(reporter);

        Assert.Equal("Query executed successfully. No records were returned.", result);
    }

    [Fact]
    public void ResolveSync_WithParameters_ReturnsFilteredResults()
    {

        var resolver = new DbResolver(_formatterFactory, _dbProviderFactoryFactory);

        var reporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(SharedConnectionString)
            .WithQuery("SELECT * FROM Users WHERE IsActive = @IsActive AND Name LIKE @Name")
            .AddParameter(new SqliteParameter("@IsActive", 1))
            .AddParameter(new SqliteParameter("@Name", "A%"))
            .WithResultsFormat(ResultsFormat.Json);


        var result = resolver.ResolveSync(reporter);


        Assert.NotNull(result);
        using var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var firstUser = root[0];
        Assert.Equal(1, firstUser.GetProperty("Id").GetInt64());
        Assert.Equal("Alice", firstUser.GetProperty("Name").GetString());
        Assert.Equal(1, firstUser.GetProperty("IsActive").GetInt64());
    }




    public void Dispose()
    {
        _connection.Dispose();
    }
}
