namespace ReportOnFailure.Tests;

using Reporters;
using Microsoft.Data.Sqlite;
using System;

public class SqliteReporterTests
{
    [Fact]
    public void OutPutData_Should_ReturnCorrectlyFormattedData_FromDatabase()
    {
        // Using a shared, in-memory database allows the test to set up the DB
        // with one connection and the reporter to query it with another.
        const string connectionString = "DataSource=TestDb;Mode=Memory;Cache=Shared";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Arrange: Create a schema and seed it with data
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT
                );
                INSERT INTO Users (Id, Name, Email) VALUES (1, 'Alice', 'alice@example.com');
                INSERT INTO Users (Id, Name, Email) VALUES (2, 'Bob', NULL);
            ";
            command.ExecuteNonQuery();
        }

        var reporter = new SqlLiteReporter();
        reporter.SetConnectionString(connectionString);
        reporter.SetTableName("Users");

        // Act
        var result = reporter.OutPutData();

        // Assert
        var expected = "Id\tName\tEmail" + Environment.NewLine +
                       "1\tAlice\talice@example.com" + Environment.NewLine +
                       "2\tBob\tNULL" + Environment.NewLine;

        Assert.Equal(expected, result);

        // Clean up the connection so the in-memory database is destroyed.
        connection.Close();
    }

    [Fact]
    public void OutPutData_Should_ApplyWhereClause_WhenProvided()
    {
        const string connectionString = "DataSource=TestDbFiltered;Mode=Memory;Cache=Shared";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Arrange: Create a schema and seed it with data
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE Products (
                    SKU TEXT PRIMARY KEY,
                    Category TEXT NOT NULL,
                    Price REAL
                );
                INSERT INTO Products VALUES ('A123', 'Books', 9.99);
                INSERT INTO Products VALUES ('B456', 'Electronics', 199.99);
                INSERT INTO Products VALUES ('C789', 'Books', 14.50);
            ";
            command.ExecuteNonQuery();
        }

        var reporter = new SqlLiteReporter();
        reporter.SetConnectionString(connectionString);
        reporter.SetTableName("Products");
        reporter.SetWhereClause("Category = 'Books'"); // Apply the filter

        // Act
        var result = reporter.OutPutData();

        // Assert
        Assert.Contains("A123\tBooks\t9.99", result);
        Assert.Contains("C789\tBooks\t14.5", result); // Note: REAL type might format differently
        Assert.DoesNotContain("B456", result);
    }
}