using System.IO.Compression;
using Microsoft.Data.Sqlite;
using ReportOnFailure;
using ReportOnFailure.Enums;
using ReportOnFailure.Reporters;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace ReportOnFailure.Tests.E2E;

public class EndToEndTest : IDisposable
{
    private readonly WireMockServer _mockApiServer;
    private readonly SqliteConnection _inMemoryDatabase;
    private readonly string _testOutputDirectory;
    private readonly string _connectionString;

    public EndToEndTest()
    {
        
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "ReportOnFailure_E2E", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDirectory);

        
        _mockApiServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = false
        });

        SetupMockApiEndpoints();

        var cacheId = Guid.NewGuid().ToString("N")[..8];
        _connectionString = $"DataSource=TestDb_{cacheId};Mode=Memory;Cache=Shared";

        
        _inMemoryDatabase = new SqliteConnection(_connectionString);
        _inMemoryDatabase.Open();

        SetupInMemoryDatabase();
    }

    [Fact]
    public async Task EndToEnd_FailureScenario_CreatesCompressedReportsWithCorrectContent()
    {
        
        var registry = new ReportOnFailure.Registry()
            .WithDestinationLocation(_testOutputDirectory)
            .WithCompression()
            .WithExecutionMode(ExecutionMode.Asynchronous);

        
        var dbReporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(_connectionString)
            .WithQuery("SELECT * FROM Users WHERE IsActive = 1 ORDER BY CreatedDate DESC")
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("ActiveUsers");

        
        var apiReporter = new ApiReporter()
            .WithBaseUrl(_mockApiServer.Urls[0])
            .WithEndpoint("/api/system/status")
            .WithMethod(ApiHttpMethod.GET)
            .WithHeader("Accept", "application/json")
            .WithQueryParameter("detailed", "true")
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("SystemStatus");

        
        registry.RegisterReporter(dbReporter);
        registry.RegisterReporter(apiReporter);

        
        var testFailed = false;
        try
        {
            
            SimulateFailingOperation();
        }
        catch (InvalidOperationException)
        {
            testFailed = true;

            
            await registry.ExecuteAsync();
        }

        Assert.True(testFailed, "Test should have failed to trigger report generation");


        var createdFiles = Directory.GetFiles(_testOutputDirectory, "*.zip");
        Assert.Equal(2, createdFiles.Length); 

        var dbReportFile = Array.Find(createdFiles, f => Path.GetFileName(f).StartsWith("ActiveUsers_"));
        var apiReportFile = Array.Find(createdFiles, f => Path.GetFileName(f).StartsWith("SystemStatus_"));

        Assert.NotNull(dbReportFile);
        Assert.NotNull(apiReportFile);

        await VerifyDbReportContent(dbReportFile);

        await VerifyApiReportContent(apiReportFile);

        VerifyFileNamingConvention(dbReportFile, "ActiveUsers");
        VerifyFileNamingConvention(apiReportFile, "SystemStatus");
    }

    [Fact]
    public async Task EndToEnd_SynchronousExecution_CreatesReports()
    {
        var registry = new ReportOnFailure.Registry()
            .WithDestinationLocation(_testOutputDirectory)
            .WithCompression()
            .WithExecutionMode(ExecutionMode.Synchronous);

        var dbReporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(_connectionString)
            .WithQuery("SELECT COUNT(*) as UserCount FROM Users")
            .WithResultsFormat(ResultsFormat.Csv)
            .WithFileNamePrefix("UserCount");

        registry.RegisterReporter(dbReporter);

        try
        {
            SimulateFailingOperation();
        }
        catch (InvalidOperationException)
        {
            registry.Execute(); 
        }

        var createdFiles = Directory.GetFiles(_testOutputDirectory, "UserCount_*.zip");
        Assert.Single(createdFiles);

        using var archive = ZipFile.OpenRead(createdFiles[0]);
        var entry = Assert.Single(archive.Entries);
        Assert.True(entry.Name.EndsWith(".csv"));

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var csvContent = await reader.ReadToEndAsync();

        Assert.Contains("UserCount", csvContent);
        Assert.Contains("3", csvContent);
    }

    [Fact]
    public async Task EndToEnd_WithParameterizedDbQuery_CreatesFilteredReport()
    {
        var registry = new ReportOnFailure.Registry()
            .WithDestinationLocation(_testOutputDirectory)
            .WithCompression();

        var dbReporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(_connectionString)
            .WithQuery("SELECT * FROM Users WHERE IsActive = @IsActive AND Name LIKE @NamePattern")
            .AddParameter(new SqliteParameter("@IsActive", 1))
            .AddParameter(new SqliteParameter("@NamePattern", "Alice%"))
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("FilteredUsers");

        registry.RegisterReporter(dbReporter);

        try
        {
            SimulateFailingOperation();
        }
        catch (InvalidOperationException)
        {
            await registry.ExecuteAsync();
        }

        var createdFiles = Directory.GetFiles(_testOutputDirectory, "FilteredUsers_*.zip");
        Assert.Single(createdFiles);

        var jsonContent = await ExtractJsonFromZip(createdFiles[0]);
        Assert.Contains("Alice", jsonContent);
        Assert.DoesNotContain("Bob", jsonContent);
        Assert.DoesNotContain("Charlie", jsonContent);
    }

    [Fact]
    public async Task EndToEnd_ApiWithAuthentication_CreatesAuthenticatedReport()
    {
        _mockApiServer
            .Given(Request.Create()
                .WithPath("/api/protected")
                .UsingGet()
                .WithHeader("Authorization", "Bearer test-token-12345"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\": \"authenticated\", \"user\": \"test-user\", \"permissions\": [\"read\", \"write\"]}"));

        var registry = new ReportOnFailure.Registry()
            .WithDestinationLocation(_testOutputDirectory)
            .WithCompression();

        var apiReporter = new ApiReporter()
            .WithBaseUrl(_mockApiServer.Urls[0])
            .WithEndpoint("/api/protected")
            .WithMethod(ApiHttpMethod.GET)
            .WithBearerToken("test-token-12345")
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("ProtectedApi");

        registry.RegisterReporter(apiReporter);

        try
        {
            SimulateFailingOperation();
        }
        catch (InvalidOperationException)
        {
            await registry.ExecuteAsync();
        }

        var createdFiles = Directory.GetFiles(_testOutputDirectory, "ProtectedApi_*.zip");
        Assert.Single(createdFiles);

        var jsonContent = await ExtractJsonFromZip(createdFiles[0]);
        Assert.Contains("authenticated", jsonContent);
        Assert.Contains("test-user", jsonContent);
        Assert.Contains("200", jsonContent); 
    }

    [Fact]
    public async Task EndToEnd_MixedExecutionModes_RespectsReporterOverrides()
    {
        var registry = new ReportOnFailure.Registry()
            .WithDestinationLocation(_testOutputDirectory)
            .WithCompression()
            .WithExecutionMode(ExecutionMode.Asynchronous);

        var syncDbReporter = new DbReporter()
            .WithDatabaseType(DatabaseType.Sqlite)
            .WithConnectionString(_connectionString)
            .WithQuery("SELECT 'sync' as ExecutionType")
            .WithExecutionModeOverride(ExecutionMode.Synchronous)
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("SyncReport");

        var asyncApiReporter = new ApiReporter()
            .WithBaseUrl(_mockApiServer.Urls[0])
            .WithEndpoint("/api/system/status")
            .WithMethod(ApiHttpMethod.GET)
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("AsyncReport");

        registry.RegisterReporter(syncDbReporter);
        registry.RegisterReporter(asyncApiReporter);

        try
        {
            SimulateFailingOperation();
        }
        catch (InvalidOperationException)
        {
            await registry.ExecuteAsync();
        }

        
        var createdFiles = Directory.GetFiles(_testOutputDirectory, "*.zip");
        Assert.Equal(2, createdFiles.Length);

        var syncReport = Array.Find(createdFiles, f => Path.GetFileName(f).StartsWith("SyncReport_"));
        var asyncReport = Array.Find(createdFiles, f => Path.GetFileName(f).StartsWith("AsyncReport_"));

        Assert.NotNull(syncReport);
        Assert.NotNull(asyncReport);

        var syncContent = await ExtractJsonFromZip(syncReport);
        var asyncContent = await ExtractJsonFromZip(asyncReport);

        Assert.Contains("sync", syncContent);
        Assert.Contains("operational", asyncContent);
    }

    #region Helper Methods

    private void SetupMockApiEndpoints()
    {
        
        _mockApiServer
            .Given(Request.Create()
                .WithPath("/api/system/status")
                .UsingGet()
                .WithParam("detailed", "true"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\": \"operational\", \"version\": \"1.2.3\", \"uptime\": \"72:30:15\", \"services\": {\"database\": \"healthy\", \"cache\": \"healthy\"}}"));

        
        _mockApiServer
            .Given(Request.Create()
                .WithPath("/api/system/status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\": \"operational\"}"));
    }

    private void SetupInMemoryDatabase()
    {
        using var transaction = _inMemoryDatabase.BeginTransaction();

        var createTableCommand = _inMemoryDatabase.CreateCommand();
        createTableCommand.Transaction = transaction;
        createTableCommand.CommandText = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedDate TEXT NOT NULL
            );

            INSERT INTO Users (Name, Email, IsActive, CreatedDate) VALUES 
                ('Alice Johnson', 'alice@example.com', 1, '2024-12-01 10:00:00'),
                ('Bob Smith', 'bob@example.com', 0, '2024-12-02 11:30:00'),
                ('Charlie Brown', 'charlie@example.com', 1, '2024-12-03 09:15:00');
        ";

        createTableCommand.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void SimulateFailingOperation()
    {
        
        throw new InvalidOperationException("Simulated test failure - this triggers failure report collection");
    }

    private async Task VerifyDbReportContent(string zipFilePath)
    {
        var jsonContent = await ExtractJsonFromZip(zipFilePath);

        
        Assert.Contains("Alice Johnson", jsonContent);
        Assert.Contains("Charlie Brown", jsonContent);
        Assert.DoesNotContain("Bob Smith", jsonContent); 

        
        Assert.Contains("\"Id\":", jsonContent);
        Assert.Contains("\"Name\":", jsonContent);
        Assert.Contains("\"Email\":", jsonContent);
        Assert.Contains("\"IsActive\":", jsonContent);
        Assert.Contains("\"CreatedDate\":", jsonContent);
    }

    private async Task VerifyApiReportContent(string zipFilePath)
    {
        var jsonContent = await ExtractJsonFromZip(zipFilePath);

        
        Assert.Contains("\"StatusCode\": 200", jsonContent);
        Assert.Contains("\"IsSuccess\": true", jsonContent);

        
        Assert.Contains("operational", jsonContent);
        Assert.Contains("version", jsonContent);
        Assert.Contains("uptime", jsonContent);
        Assert.Contains("database", jsonContent);
    }

    private static async Task<string> ExtractJsonFromZip(string zipFilePath)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        var entry = Assert.Single(archive.Entries);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static void VerifyFileNamingConvention(string filePath, string expectedPrefix)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        var parts = fileName.Split('_');

        Assert.True(parts.Length >= 3, $"File name should have at least 3 parts: {fileName}");
        Assert.Equal(expectedPrefix, parts[0]);

        Assert.True(parts[1].Length == 8, "Date part should be 8 characters (yyyyMMdd)");
        Assert.True(parts[2].Length == 6, "Time part should be 6 characters (HHmmss)");

        Assert.True(parts[3].Length == 8, "GUID part should be 8 characters");

        Assert.True(filePath.EndsWith(".zip"), "File should be a zip file");
    }

    #endregion

    public void Dispose()
    {
        _inMemoryDatabase?.Dispose();
        _mockApiServer?.Stop();
        _mockApiServer?.Dispose();

        
        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, recursive: true);
        }
    }
}