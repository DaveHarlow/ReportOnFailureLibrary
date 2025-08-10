# Report On Failure Library (ROFL)

A .NET library for collecting and outputting contextual information when tests or operations fail. Designed to help with debugging by automatically gathering relevant data from databases, APIs, and other sources when something goes wrong.

## Purpose

ROFL helps developers and testers gather debugging information automatically when failures occur on E2E and integration tests. Instead of manually collecting logs, database states, or API responses after a failure, this library allows you to pre-configure data collection that executes only when needed.

**Common use cases:**
- Capturing database state when integration tests fail
- Collecting API responses for debugging failed service calls  
- Gathering configuration data when deployment verification fails
- Saving system metrics when performance tests don't meet thresholds

## Installation

dotnet package add ReportOnFailureLibrary

## Usage

1. Create a registry in your E2E or integration test. Configure the output location.
2. Add reporters that are relevant to the test as a whole.
3. Add reporters that are relevant for the next part of the test.
4. When part of the test passes, remove any reporters you no longer care about and add new ones for upcoming parts of the Test.
5. If the test fails, call the registry to execute in the teardown.

## Quick Start

```csharp
using ReportOnFailure;
using ReportOnFailure.Reporters;
using ReportOnFailure.Enums;

// Set up the registry
var registry = new Registry()
    .WithDestinationLocation(@"C:\FailureReports")
    .WithCompression()
    .WithExecutionMode(ExecutionMode.Asynchronous);

// Add a database reporter
var dbReporter = new DbReporter()
    .WithDatabaseType(DatabaseType.SqlServer)
    .WithConnectionString("Server=localhost;Database=TestDb;Trusted_Connection=true;")
    .WithQuery("SELECT TOP 10 * FROM Users ORDER BY CreatedDate DESC")
    .WithResultsFormat(ResultsFormat.Json)
    .WithFileNamePrefix("UserState");

registry.RegisterReporter(dbReporter);

// Add an API reporter
var apiReporter = new ApiReporter()
    .WithBaseUrl("https://api.myservice.com")
    .WithEndpoint("/health")
    .WithMethod(HttpMethod.GET)
    .WithBearerToken("your-api-token")
    .WithResultsFormat(ResultsFormat.Json)
    .WithFileNamePrefix("ServiceHealth");

registry.RegisterReporter(apiReporter);

// Execute when failure occurs
try 
{
    // Your test or operation that might fail
    await SomeOperationThatMightFail();
}
catch (Exception)
{
    // Collect failure context
    await registry.ExecuteAsync();
    throw; // Re-throw the original exception
}

//Test passed? Remove the reporters, add ones for the next assert
registry.UnregisterReporter(apiReporter);
registry.UnregisterReporter(dbReporter);
```

## Setting Up the Registry

The `Registry` is the main orchestrator that manages reporters and handles output configuration.

### Basic Setup

```csharp
var registry = new Registry();
```

### Configuration Options

#### Destination Location
Set where reports will be written:

```csharp
registry.WithDestinationLocation(@"C:\Reports");           // Local folder
registry.WithDestinationLocation(@"\\server\share\logs"); // Network share
```

#### Compression
Enable compression to reduce file sizes:

```csharp
registry.WithCompression(); // Creates .zip files instead of loose files
```

#### Execution Mode
Control how reporters are executed:

```csharp
registry.WithExecutionMode(ExecutionMode.Synchronous);  // Default: one after another
registry.WithExecutionMode(ExecutionMode.Asynchronous); // Parallel execution
```

#### Destination Type
Currently supports file system output:

```csharp
registry.WithDestinationType(DestinationType.FileSystem); // Default
// Future: AzureBlobStorage, AmazonS3, GoogleCloudStorage
```

### Fluent Configuration

Chain configuration methods together:

```csharp
var registry = new Registry()
    .WithDestinationLocation(@"C:\FailureReports")
    .WithCompression()
    .WithExecutionMode(ExecutionMode.Asynchronous);
```

## Adding Reporters

Reporters define what data to collect and how to format it. Each reporter focuses on a specific data source.

### Database Reporter

Collect data from SQL databases:

```csharp
var dbReporter = new DbReporter()
    .WithDatabaseType(DatabaseType.SqlServer)
    .WithConnectionString("your-connection-string")
    .WithQuery("SELECT * FROM ErrorLogs WHERE CreatedDate > @Since")
    .WithCommandTimeout(60)
    .AddParameter(new SqlParameter("@Since", DateTime.Now.AddHours(-1)))
    .WithResultsFormat(ResultsFormat.Json)
    .WithFileNamePrefix("ErrorLogs");

registry.RegisterReporter(dbReporter);
```

**Supported databases:**
- `DatabaseType.SqlServer`
- `DatabaseType.Sqlite` 
- `DatabaseType.Odbc`

**Result formats:**
- `ResultsFormat.Json` - Structured JSON output
- `ResultsFormat.Csv` - Comma-separated values
- `ResultsFormat.Text` - Human-readable text
- `ResultsFormat.Xml` - XML format (planned)
- `ResultsFormat.Html` - HTML format (planned)

### API Reporter

Collect data from REST APIs:

```csharp
var restApiReporter = new restApiReporter()
    .WithBaseUrl("https://api.example.com")
    .WithEndpoint("/v1/status")
    .WithMethod(HttpMethod.GET)
    .WithHeader("Accept", "application/json")
    .WithQueryParameter("detailed", "true")
    .WithBearerToken("your-jwt-token")
    .WithTimeout(30)
    .WithResultsFormat(ResultsFormat.Json)
    .WithFileNamePrefix("ApiStatus");

registry.RegisterReporter(restApiReporter);
```

**Authentication options:**
```csharp
// Bearer token
.WithBearerToken("your-jwt-token")

// Basic authentication  
.WithBasicAuth("username", "password")

// API key in header
.WithApiKey("X-API-Key", "your-api-key", inHeader: true)

// API key in query string
.WithApiKey("api_key", "your-api-key", inHeader: false)

// OAuth2 client credentials (automatic token management)
.WithOAuth2ClientCredentials(
    tokenEndpoint: "https://auth.example.com/oauth/token",
    clientId: "your-client-id", 
    clientSecret: "your-client-secret",
    scope: "read:data")
```

### Multiple Reporters

Register multiple reporters to collect different types of data:

```csharp
// Database state
registry.RegisterReporter(userStateReporter);
registry.RegisterReporter(orderHistoryReporter);

// API health checks  
registry.RegisterReporter(authServiceReporter);
registry.RegisterReporter(paymentServiceReporter);

// Configuration data
registry.RegisterReporter(appConfigReporter);
```

### Unregister Reporters

```csharp
// Database state
registry.UnregisterReporter(userStateReporter);
```

### Reporter-Specific Settings

Override global settings per reporter:

```csharp
var urgentReporter = new DbReporter()
    .WithQuery("SELECT * FROM CriticalErrors")
    .WithExecutionModeOverride(ExecutionMode.Synchronous) // Force sync even if registry is async
    .WithResultsFormat(ResultsFormat.Json);
```

## Triggering Report Generation

### Synchronous Execution

```csharp
try 
{
    // Your operation
    PerformOperation();
}
catch (Exception)
{
    registry.Execute(); // Blocks until all reports are generated
    throw;
}
```

### Asynchronous Execution (Recommended)

```csharp
try 
{
    await PerformOperationAsync();
}
catch (Exception)
{
    await registry.ExecuteAsync(); // Non-blocking, better performance
    throw;
}
```

### With Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try 
{
    await PerformOperationAsync();
}
catch (Exception)
{
    await registry.ExecuteAsync(cts.Token); // Respect cancellation
    throw;
}
```

## Output Format

Reports are saved with descriptive filenames:

```
{FileNamePrefix}_{timestamp}_{guid}.{extension}
```

**Examples:**
- `UserState_20241208_143052_a1b2c3d4.json`
- `ApiHealth_20241208_143053_e5f6g7h8.csv`  
- `ErrorLogs_20241208_143054_i9j0k1l2.txt`

**With compression enabled:**
- `UserState_20241208_143052_a1b2c3d4.zip` (contains .json file inside)

## Integration Examples

### xUnit Test Integration

```csharp
public class IntegrationTests : IAsyncLifetime
{
    private readonly Registry _failureReporter;

    public IntegrationTests()
    {
        _failureReporter = new Registry()
            .WithDestinationLocation(@"C:\TestFailures")
            .WithExecutionMode(ExecutionMode.Asynchronous);

        // Add relevant reporters
        _failureReporter.RegisterReporter(CreateDatabaseReporter());
        _failureReporter.RegisterReporter(CreateApiReporter());
    }

    [Fact]
    public async Task UserRegistration_ShouldCreateUser()
    {
        try
        {
            // Test logic here
            var result = await _userService.RegisterAsync(newUser);
            Assert.NotNull(result.UserId);
        }
        catch (Exception)
        {
            await _failureReporter.ExecuteAsync();
            throw;
        }
    }
}
```

### ASP.NET Core Integration

```csharp
public class HealthCheckService
{
    private readonly Registry _failureReporter;

    public HealthCheckService()
    {
        _failureReporter = new Registry()
            .WithDestinationLocation(@"C:\HealthCheckFailures")
            .WithCompression();

        // Add health check reporters
        _failureReporter.RegisterReporter(CreateDatabaseHealthReporter());
        _failureReporter.RegisterReporter(CreateExternalServiceReporter());
    }

    public async Task<HealthCheckResult> CheckSystemHealthAsync()
    {
        try
        {
            // Perform health checks
            return await PerformHealthChecksAsync();
        }
        catch (Exception ex)
        {
            await _failureReporter.ExecuteAsync();
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
```

## Advanced Configuration

### Custom File Naming

```csharp
var reporter = new DbReporter()
    .WithFileNamePrefix("CriticalUserData")
    .WithQuery("SELECT * FROM Users WHERE Status = 'Critical'");
    
// Generates: CriticalUserData_20241208_143052_a1b2c3d4.json
```

### Execution Mode Mixing

```csharp
var registry = new Registry()
    .WithExecutionMode(ExecutionMode.Asynchronous); // Default for all reporters

// But override for specific reporters
var criticalReporter = new DbReporter()
    .WithExecutionModeOverride(ExecutionMode.Synchronous) // Force sync for this one
    .WithQuery("SELECT * FROM CriticalTable");

var normalReporter = new ApiReporter()
    .WithBaseUrl("https://api.example.com")
    .WithEndpoint("/status");
    // Uses async mode from registry

registry.RegisterReporter(criticalReporter);
registry.RegisterReporter(normalReporter);

await registry.ExecuteAsync(); 
// criticalReporter runs synchronously, normalReporter runs async
```

## Best Practices

1. **Configure Once**: Set up the registry and reporters during application startup or test initialization.

2. **Use Meaningful Prefixes**: Choose `FileNamePrefix` values that clearly identify the data being collected.

3. **Limit Data Volume**: Use appropriate WHERE clauses and LIMIT/TOP statements to avoid collecting excessive data.

4. **Handle Sensitive Data**: Be careful not to collect passwords, API keys, or other sensitive information in your queries.

5. **Use Compression**: Enable compression for production use to reduce storage requirements.

6. **Set Appropriate Timeouts**: Configure reasonable timeouts for database queries and API calls.

7. **Async When Possible**: Use `ExecuteAsync()` for better performance, especially when collecting from multiple sources.

## Error Handling

The library handles common errors gracefully:

- **Database connection failures**: Logged but don't prevent other reporters from running
- **API timeouts**: Configurable timeouts with proper error reporting  
- **File system issues**: Directory creation and permission handling
- **Invalid queries**: SQL errors are captured in the output

Failed reports generate error files with diagnostic information rather than throwing exceptions that could mask the original failure.

## Requirements

- .NET 8.0 or .NET 9.0

## License

MIT License
