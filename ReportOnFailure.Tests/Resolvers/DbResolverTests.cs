namespace ReportOnFailure.Tests.Resolvers;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ReportOnFailure.Enums;
using ReportOnFailure.Reporters;
using ReportOnFailure.Resolvers;
using Xunit;


    public class DbResolverTests : IDisposable
    {
        private readonly DbResolver _resolver;
        private readonly SqliteConnection _connection;
        private const string SharedConnectionString = "DataSource=TestDb;Mode=Memory;Cache=Shared";

        public DbResolverTests()
        {
            _resolver = new DbResolver();

            
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
            var reporter = new DbReporter()
                .WithDatabaseType(DbType.Sqlite)
                .WithConnectionString(SharedConnectionString)
                .WithQuery("SELECT Id, Name, IsActive FROM Users WHERE IsActive = 1 ORDER BY Id")
                .WithResultsFormat(ResultsFormat.Json);

            var result = await _resolver.ResolveAsync(reporter);

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
            var reporter = new DbReporter()
                .WithDatabaseType(DbType.Sqlite)
                .WithConnectionString(SharedConnectionString)
                .WithQuery("SELECT * FROM Users ORDER BY Id")
                .WithResultsFormat(ResultsFormat.Csv);
            
            var result = _resolver.ResolveSync(reporter);

            var lines = result.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(4, lines.Length);
            Assert.Equal("\"Id\",\"Name\",\"IsActive\"", lines[0]);
            Assert.Equal("\"1\",\"Alice\",\"1\"", lines[1]);
            Assert.Equal("\"2\",\"Bob\",\"0\"", lines[2]);
        }

        [Fact]
        public void ResolveSync_WithNoResults_ReturnsEmptyMessage()
        {
            var reporter = new DbReporter()
                .WithDatabaseType(DbType.Sqlite)
                .WithConnectionString(SharedConnectionString)
                .WithQuery("SELECT * FROM Users WHERE Id = 999");

            var result = _resolver.ResolveSync(reporter);

            Assert.Equal("Query executed successfully. No records were returned.", result);
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
