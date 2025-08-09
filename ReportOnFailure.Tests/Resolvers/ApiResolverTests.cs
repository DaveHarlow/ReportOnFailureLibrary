using Moq;
using ReportOnFailure.Authentication;
using ReportOnFailure.Enums;
using ReportOnFailure.Factories;
using ReportOnFailure.Reporters;
using ReportOnFailure.Resolvers;
using System.Text;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace ReportOnFailure.Tests.Resolvers;

public class ApiResolverTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;
    private readonly RestApiResolver _apiResolver;
    private readonly Mock<IResultFormatterFactory> _mockFormatterFactory;

    public ApiResolverTests()
    {
        _mockServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = false
        });

        _httpClient = new HttpClient();

        _mockFormatterFactory = new Mock<IResultFormatterFactory>();
        var mockFormatter = new Mock<Formatters.IResultFormatter>();
        mockFormatter.Setup(f => f.Format(It.IsAny<IReadOnlyCollection<Dictionary<string, object?>>>()))
            .Returns((IReadOnlyCollection<Dictionary<string, object?>> data) => JsonSerializer.Serialize(data));

        _mockFormatterFactory.Setup(f => f.CreateFormatter(It.IsAny<ResultsFormat>()))
            .Returns(mockFormatter.Object);

        _apiResolver = new RestApiResolver(_mockFormatterFactory.Object, _httpClient);
    }

    public void Dispose()
    {
        _apiResolver?.Dispose();
        _httpClient?.Dispose();
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }

    #region HTTP Method Tests

    [Fact]
    public async Task ResolveAsync_WithGetMethod_SendsGetRequest()
    {

        SetupSuccessfulResponse("/api/test", "GET", "Test GET response");

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/test")
            .WithMethod(ApiHttpMethod.GET)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Test GET response", result);
        VerifyRequestMade("/api/test", "GET");
    }

    [Fact]
    public async Task ResolveAsync_WithPostMethod_SendsPostRequest()
    {

        const string requestBody = "{\"name\": \"test\", \"value\": \"data\"}";
        SetupSuccessfulResponse("/api/create", "POST", "Created successfully", requestBody);

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/create")
            .WithMethod(ApiHttpMethod.POST)
            .WithJsonBody(requestBody)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Created successfully", result);
        VerifyRequestMade("/api/create", "POST");
    }

    [Fact]
    public async Task ResolveAsync_WithPutMethod_SendsPutRequest()
    {

        const string requestBody = "{\"id\": 1, \"name\": \"updated\"}";
        SetupSuccessfulResponse("/api/update/1", "PUT", "Updated successfully", requestBody);

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/update/1")
            .WithMethod(ApiHttpMethod.PUT)
            .WithJsonBody(requestBody)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Updated successfully", result);
        VerifyRequestMade("/api/update/1", "PUT");
    }

    [Fact]
    public async Task ResolveAsync_WithPatchMethod_SendsPatchRequest()
    {

        const string requestBody = "{\"name\": \"patched\"}";
        SetupSuccessfulResponse("/api/patch/1", "PATCH", "Patched successfully", requestBody);

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/patch/1")
            .WithMethod(ApiHttpMethod.PATCH)
            .WithJsonBody(requestBody)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Patched successfully", result);
        VerifyRequestMade("/api/patch/1", "PATCH");
    }

    [Fact]
    public async Task ResolveAsync_WithDeleteMethod_SendsDeleteRequest()
    {

        SetupSuccessfulResponse("/api/delete/1", "DELETE", "Deleted successfully");

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/delete/1")
            .WithMethod(ApiHttpMethod.DELETE)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Deleted successfully", result);
        VerifyRequestMade("/api/delete/1", "DELETE");
    }

    [Fact]
    public async Task ResolveAsync_WithHeadMethod_SendsHeadRequest()
    {

        _mockServer
            .Given(Request.Create().WithPath("/api/head").UsingHead())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Length", "100")
                .WithHeader("Content-Type", "application/json"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/head")
            .WithMethod(ApiHttpMethod.HEAD)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("200", result);
        VerifyRequestMade("/api/head", "HEAD");
    }

    [Fact]
    public async Task ResolveAsync_WithOptionsMethod_SendsOptionsRequest()
    {

        _mockServer
            .Given(Request.Create().WithPath("/api/options").UsingOptions())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Allow", "GET, POST, PUT, DELETE")
                .WithBody(""));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/options")
            .WithMethod(ApiHttpMethod.OPTIONS)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("200", result);
        Assert.Contains("GET, POST, PUT, DELETE", result);
        VerifyRequestMade("/api/options", "OPTIONS");
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task ResolveAsync_WithBearerToken_IncludesAuthorizationHeader()
    {

        const string bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/protected")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {bearerToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Protected resource accessed"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/protected")
            .WithMethod(ApiHttpMethod.GET)
            .WithBearerToken(bearerToken)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Protected resource accessed", result);
        VerifyAuthorizationHeader($"Bearer {bearerToken}");
    }

    [Fact]
    public async Task ResolveAsync_WithBasicAuth_IncludesBasicAuthHeader()
    {

        const string username = "testuser";
        const string password = "testpass";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/basic-auth")
                .UsingGet()
                .WithHeader("Authorization", $"Basic {credentials}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Basic auth successful"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/basic-auth")
            .WithMethod(ApiHttpMethod.GET)
            .WithBasicAuth(username, password)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Basic auth successful", result);
        VerifyAuthorizationHeader($"Basic {credentials}");
    }

    [Fact]
    public async Task ResolveAsync_WithApiKeyInHeader_IncludesApiKeyHeader()
    {

        const string apiKey = "test-api-key-12345";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/key-protected")
                .UsingGet()
                .WithHeader("X-API-Key", apiKey))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("API key auth successful"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/key-protected")
            .WithMethod(ApiHttpMethod.GET)
            .WithApiKey("X-API-Key", apiKey, inHeader: true)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("API key auth successful", result);
        VerifyHeaderExists("X-API-Key", apiKey);
    }

    [Fact]
    public async Task ResolveAsync_WithApiKeyInQuery_IncludesApiKeyInQueryString()
    {

        const string apiKey = "test-api-key-query";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/query-auth")
                .UsingGet()
                .WithParam("api_key", apiKey))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Query API key auth successful"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/query-auth")
            .WithMethod(ApiHttpMethod.GET)
            .WithApiKey("api_key", apiKey, inHeader: false)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Query API key auth successful", result);
        VerifyQueryParameter("api_key", apiKey);
    }

    [Fact]
    public async Task ResolveAsync_WithJwtTokenProvider_FetchesAndUsesToken()
    {

        const string jwtToken = "jwt.from.provider.token";
        var mockJwtProvider = new Mock<IJwtTokenProvider>();
        mockJwtProvider.Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/jwt-protected")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {jwtToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("JWT auth successful"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/jwt-protected")
            .WithMethod(ApiHttpMethod.GET)
            .WithJwtProvider(mockJwtProvider.Object)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("JWT auth successful", result);
        mockJwtProvider.Verify(p => p.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyAuthorizationHeader($"Bearer {jwtToken}");
    }

    [Fact]
    public async Task ResolveAsync_WithExpiredJwtToken_RefreshesAndRetries()
    {

        const string expiredToken = "expired.jwt.token";
        const string refreshedToken = "refreshed.jwt.token";

        var mockJwtProvider = new Mock<IJwtTokenProvider>();
        mockJwtProvider.SetupSequence(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken)
            .ReturnsAsync(refreshedToken);

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/jwt-retry")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {expiredToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized"));

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/jwt-retry")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {refreshedToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Access granted after refresh"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/jwt-retry")
            .WithMethod(ApiHttpMethod.GET)
            .WithJwtProvider(mockJwtProvider.Object)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Access granted after refresh", result);
        mockJwtProvider.Verify(p => p.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockJwtProvider.Verify(p => p.RefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public async Task ResolveAsync_WithJsonBody_SendsJsonContent()
    {

        const string jsonBody = "{\"name\": \"test\", \"value\": 123}";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/json")
                .UsingPost()
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(jsonBody))
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBody("JSON received"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/json")
            .WithEncoding(Encoding.UTF8)
            .WithMethod(ApiHttpMethod.POST)
            .WithJsonBody(jsonBody)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("JSON received", result);
        VerifyContentType("application/json");
    }

    [Fact]
    public async Task ResolveAsync_WithXmlBody_SendsXmlContent()
    {

        const string xmlBody = "<root><name>test</name><value>123</value></root>";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/xml")
                .UsingPost()
                .WithHeader("Content-Type", "application/xml; charset=utf-8")
                .WithBody(xmlBody))
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBody("XML received"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/xml")
            .WithMethod(ApiHttpMethod.POST)
            .WithEncoding(Encoding.UTF8)
            .WithXmlBody(xmlBody)
            .WithResultsFormat(ResultsFormat.Xml);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("XML received", result);
        VerifyContentType("application/xml");
    }

    [Fact]
    public async Task ResolveAsync_WithFormData_SendsFormUrlEncodedContent()
    {

        var formData = new Dictionary<string, string>
        {
            ["username"] = "testuser",
            ["password"] = "testpass",
            ["remember"] = "true"
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/form")
                .UsingPost()
                .WithHeader("Content-Type", "application/x-www-form-urlencoded"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Form data received"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/form")
            .WithMethod(ApiHttpMethod.POST)
            .WithFormData(formData)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Form data received", result);
        VerifyContentType("application/x-www-form-urlencoded");
    }

    [Fact]
    public async Task ResolveAsync_WithTextBody_SendsTextContent()
    {

        const string textBody = "Plain text content for the API";

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/text")
                .UsingPost()
                .WithHeader("Content-Type", "text/plain; charset=utf-8")
                .WithBody(textBody))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Text received"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/text")
            .WithMethod(ApiHttpMethod.POST)
            .WithEncoding(Encoding.UTF8)
            .WithTextBody(textBody)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Text received", result);
        VerifyContentType("text/plain");
    }

    #endregion

    #region Query Parameters and Headers Tests

    [Fact]
    public async Task ResolveAsync_WithQueryParameters_IncludesParametersInUrl()
    {

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/search")
                .UsingGet()
                .WithParam("q", "test query")
                .WithParam("limit", "10")
                .WithParam("offset", "0"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Search results"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/search")
            .WithMethod(ApiHttpMethod.GET)
            .WithQueryParameter("q", "test query")
            .WithQueryParameter("limit", 10)
            .WithQueryParameter("offset", 0)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Search results", result);
        VerifyQueryParameter("q", "test query");
        VerifyQueryParameter("limit", "10");
        VerifyQueryParameter("offset", "0");
    }

    [Fact]
    public async Task ResolveAsync_WithCustomHeaders_IncludesHeaders()
    {

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/headers")
                .UsingGet()
                .WithHeader("User-Agent", "ReportOnFailure/1.0")
                .WithHeader("Accept", "application/json")
                .WithHeader("X-Custom-Header", "custom-value"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Headers received"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/headers")
            .WithMethod(ApiHttpMethod.GET)
            .WithHeader("User-Agent", "ReportOnFailure/1.0")
            .WithHeader("Accept", "application/json")
            .WithHeader("X-Custom-Header", "custom-value")
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("Headers received", result);
        VerifyHeaderExists("User-Agent", "ReportOnFailure/1.0");
        VerifyHeaderExists("Accept", "application/json");
        VerifyHeaderExists("X-Custom-Header", "custom-value");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ResolveAsync_With404Response_ReturnsErrorResponse()
    {

        _mockServer
            .Given(Request.Create().WithPath("/api/notfound").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody("Not Found"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/notfound")
            .WithMethod(ApiHttpMethod.GET)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("404", result);
        Assert.Contains("Not Found", result);
        Assert.Contains("\"IsSuccess\":false", result);
    }

    [Fact]
    public async Task ResolveAsync_With500Response_ReturnsServerErrorResponse()
    {

        _mockServer
            .Given(Request.Create().WithPath("/api/error").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/error")
            .WithMethod(ApiHttpMethod.GET)
            .WithResultsFormat(ResultsFormat.Json);


        var result = await _apiResolver.ResolveAsync(reporter);


        Assert.Contains("500", result);
        Assert.Contains("Internal Server Error", result);
        Assert.Contains("\"IsSuccess\":false", result);
    }

    [Fact]
    public async Task ResolveAsync_WithTimeout_CancelsAfterTimeoutReached()
    {

        _mockServer
            .Given(Request.Create().WithPath("/api/slow").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(10))
                .WithBody("Slow response"));

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/slow")
            .WithMethod(ApiHttpMethod.GET)
            .WithTimeout(2)
            .WithResultsFormat(ResultsFormat.Json);


        await Assert.ThrowsAsync<TaskCanceledException>(() => _apiResolver.ResolveAsync(reporter));
    }

    [Fact]
    public async Task ResolveAsync_WithCancellationToken_CancelsRequest()
    {

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/test")
            .WithMethod(ApiHttpMethod.GET)
            .WithResultsFormat(ResultsFormat.Json);


        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _apiResolver.ResolveAsync(reporter, cts.Token));
    }

    #endregion

    #region Sync Method Tests

    [Fact]
    public void ResolveSync_WithGetMethod_ReturnsResponse()
    {

        SetupSuccessfulResponse("/api/sync", "GET", "Sync response");

        var reporter = new RestApiReporter()
            .WithBaseUrl(_mockServer.Urls[0])
            .WithEndpoint("/api/sync")
            .WithMethod(ApiHttpMethod.GET)
            .WithResultsFormat(ResultsFormat.Json);


        var result = _apiResolver.ResolveSync(reporter);


        Assert.Contains("Sync response", result);
        VerifyRequestMade("/api/sync", "GET");
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulResponse(string path, string method, string responseBody, string? requestBody = null)
    {
        var requestBuilder = Request.Create().WithPath(path);

        requestBuilder = method.ToUpper() switch
        {
            "GET" => requestBuilder.UsingGet(),
            "POST" => requestBuilder.UsingPost(),
            "PUT" => requestBuilder.UsingPut(),
            "PATCH" => requestBuilder.UsingPatch(),
            "DELETE" => requestBuilder.UsingDelete(),
            "HEAD" => requestBuilder.UsingHead(),
            "OPTIONS" => requestBuilder.UsingOptions(),
            _ => requestBuilder.UsingGet()
        };

        if (!string.IsNullOrEmpty(requestBody))
        {
            requestBuilder = requestBuilder.WithBody(requestBody);
        }

        _mockServer
            .Given(requestBuilder)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseBody));
    }

    private void VerifyRequestMade(string path, string method)
    {
        var requests = _mockServer.LogEntries;
        Assert.Contains(requests, r =>
            r.RequestMessage.Path.Equals(path, StringComparison.OrdinalIgnoreCase) &&
            r.RequestMessage.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
    }

    private void VerifyAuthorizationHeader(string expectedValue)
    {
        var requests = _mockServer.LogEntries;
        Assert.Contains(requests, r =>
            r.RequestMessage.Headers != null &&
            r.RequestMessage.Headers.ContainsKey("Authorization") &&
            r.RequestMessage.Headers["Authorization"].FirstOrDefault() == expectedValue);
    }

    private void VerifyHeaderExists(string headerName, string expectedValue)
    {
        var requests = _mockServer.LogEntries;
        Assert.Contains(requests, r =>
            r.RequestMessage.Headers != null &&
            r.RequestMessage.Headers.ContainsKey(headerName) &&
            r.RequestMessage.Headers[headerName].FirstOrDefault() == expectedValue);
    }

    private void VerifyQueryParameter(string paramName, string expectedValue)
    {
        var requests = _mockServer.LogEntries;
        Assert.Contains(requests, r =>
            r.RequestMessage.Query != null &&
            r.RequestMessage.Query.ContainsKey(paramName) &&
            r.RequestMessage.Query[paramName].FirstOrDefault() == expectedValue);
    }

    private void VerifyContentType(string expectedContentType)
    {
        var requests = _mockServer.LogEntries;
        Assert.Contains(requests, r =>
            r.RequestMessage.Headers != null &&
            r.RequestMessage.Headers.ContainsKey("Content-Type") &&
            r.RequestMessage.Headers["Content-Type"].FirstOrDefault()?.StartsWith(expectedContentType) == true);
    }

    #endregion
}