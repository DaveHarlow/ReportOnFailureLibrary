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
        
        var reporter = new DbReporter().WithDatabaseType(DbType.SqlServer);

        
        Assert.Equal(DbType.SqlServer, reporter.DatabaseType);
    }

    [Fact]
    public void TestWithConnectionString()
    {
        
        var reporter = new DbReporter().WithConnectionString("Server=myServer;Database=myDB;User Id=myUser;Password=myPass;");

        
        Assert.Equal("Server=myServer;Database=myDB;User Id=myUser;Password=myPass;", reporter.ConnectionString);
    }

    [Fact]
    public void TestWithQuery()
    {
        
        var reporter = new DbReporter().WithQuery("SELECT * FROM Users");

        
        Assert.Equal("SELECT * FROM Users", reporter.Query);
    }

    [Fact]
    public void TestWithCommandTimeout()
    {
        
        var reporter = new DbReporter().WithCommandTimeout(60);

        
        Assert.Equal(60, reporter.CommandTimeout);
    }

    [Fact]
    public void TestAddParameter()
    {
        
        var reporter = new DbReporter();
        var parameter = new Mock<DbParameter>().Object; // Create a mock or real DbParameter

        
        reporter.AddParameter(parameter);

        
        Assert.Contains(parameter, reporter.Parameters);
    }

    [Fact]
    public void TestAddParameters()
    {
        
        var reporter = new DbReporter();
        var parameters = new List<DbParameter>
        {
            new Mock<DbParameter>().Object, // Create mock or real DbParameters
            new Mock<DbParameter>().Object
        };

        
        reporter.AddParameters(parameters);

        
        Assert.Equal(2, reporter.Parameters.Count);
    }

    [Fact]
    public void TestWithExecutionModeOverride()
    {
        
        var reporter = new DbReporter().WithExecutionModeOverride(ExecutionMode.Asynchronous);

        
        Assert.Equal(ExecutionMode.Asynchronous, reporter.ExecutionModeOverride);
    }

    [Fact]
    public void TestWithResultsFormat()
    {
        
        var reporter = new DbReporter().WithResultsFormat(ResultsFormat.Json);

        
        Assert.Equal(ResultsFormat.Json, reporter.ResultsFormat);
    }
}

