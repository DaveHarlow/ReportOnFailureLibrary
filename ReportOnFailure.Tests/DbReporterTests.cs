namespace ReportOnFailure.Tests;

using ReportOnFailure.Reporters;
using ReportOnFailure.Enums;
using System.Data.Common;
using Moq;

public class DbReporterTests
{
    [Fact]
    public void TestWithDatabaseType()
    {
        // Arrange
        var reporter = new DbReporter().WithDatabaseType(DbType.SqlServer);

        // Assert
        Assert.Equal(DbType.SqlServer, reporter.DatabaseType);
    }

    [Fact]
    public void TestWithConnectionString()
    {
        // Arrange
        var reporter = new DbReporter().WithConnectionString("Server=myServer;Database=myDB;User Id=myUser;Password=myPass;");

        // Assert
        Assert.Equal("Server=myServer;Database=myDB;User Id=myUser;Password=myPass;", reporter.ConnectionString);
    }

    [Fact]
    public void TestWithQuery()
    {
        // Arrange
        var reporter = new DbReporter().WithQuery("SELECT * FROM Users");

        // Assert
        Assert.Equal("SELECT * FROM Users", reporter.Query);
    }

    [Fact]
    public void TestWithCommandTimeout()
    {
        // Arrange
        var reporter = new DbReporter().WithCommandTimeout(60);

        // Assert
        Assert.Equal(60, reporter.CommandTimeout);
    }

    [Fact]
    public void TestAddParameter()
    {
        // Arrange
        var reporter = new DbReporter();
        var parameter = new Mock<DbParameter>().Object; // Create a mock or real DbParameter

        // Act
        reporter.AddParameter(parameter);

        // Assert
        Assert.Contains(parameter, reporter.Parameters);
    }

    [Fact]
    public void TestAddParameters()
    {
        // Arrange
        var reporter = new DbReporter();
        var parameters = new List<DbParameter>
        {
            new Mock<DbParameter>().Object, // Create mock or real DbParameters
            new Mock<DbParameter>().Object
        };

        // Act
        reporter.AddParameters(parameters);

        // Assert
        Assert.Equal(2, reporter.Parameters.Count);
    }
}

