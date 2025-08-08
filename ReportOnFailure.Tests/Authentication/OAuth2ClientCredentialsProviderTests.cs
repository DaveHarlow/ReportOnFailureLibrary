namespace ReportOnFailure.Tests.AuthenticationTests;

using ReportOnFailure.Authentication;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

public class OAuth2ClientCredentialsProviderTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;

    public OAuth2ClientCredentialsProviderTests()
    {
        _mockServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = false
        });

        _httpClient = new HttpClient();

        SetupTokenEndpoint();
    }

    private void SetupTokenEndpoint()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/oauth/token")
                .UsingPost()
                .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                .WithBody("grant_type=client_credentials&client_id=test-client&client_secret=test-secret"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    access_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token",
                    expires_in = 3600,
                    token_type = "Bearer"
                }));
    }

    [Fact]
    public async Task GetTokenAsync_FirstCall_FetchesNewToken()
    {

        var tokenEndpoint = $"{_mockServer.Urls[0]}/oauth/token";
        var provider = new OAuth2ClientCredentialsProvider(
            tokenEndpoint,
            "test-client",
            "test-secret",
            httpClient: _httpClient);

        var result = await provider.GetTokenAsync();


        Assert.Equal("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token", result);
    }


    #region Enhanced IsTokenValidAsync Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsTokenValidAsync_WithNullOrEmptyToken_ReturnsFalse(string? token)
    {

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        var result = await provider.IsTokenValidAsync(token!);


        Assert.False(result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid.jwt")]
    [InlineData("invalid.jwt.token.toomanyparts")]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("part1..part3")]
    [InlineData(".part2.part3")]
    [InlineData("part1.part2.")]
    public async Task IsTokenValidAsync_WithInvalidJwtStructure_ReturnsFalse(string invalidToken)
    {

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        var result = await provider.IsTokenValidAsync(invalidToken);


        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithValidStructureButNotCached_ReturnsFalse()
    {

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);

        var validStructureToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";


        var result = await provider.IsTokenValidAsync(validStructureToken);


        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithCachedValidToken_ReturnsTrue()
    {

        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";

        SetupTokenEndpointWithToken(token, 3600);

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        var cachedToken = await provider.GetTokenAsync();


        var result = await provider.IsTokenValidAsync(cachedToken);


        Assert.True(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithCachedExpiredToken_ReturnsFalse()
    {

        const string expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.expired.token";


        SetupTokenEndpointWithToken(expiredToken, 1);

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        var cachedToken = await provider.GetTokenAsync();


        await Task.Delay(TimeSpan.FromSeconds(2));


        var result = await provider.IsTokenValidAsync(cachedToken);


        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithDifferentTokenThanCached_ReturnsFalse()
    {

        const string cachedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.cached.token";
        const string differentToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.different.token";

        SetupTokenEndpointWithToken(cachedToken, 3600);

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        await provider.GetTokenAsync();

        var result = await provider.IsTokenValidAsync(differentToken);


        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithCancellationToken_PropagatesToken()
    {

        using var cts = new CancellationTokenSource();
        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);

        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";

        cts.Cancel();
        var result = await provider.IsTokenValidAsync(token, cts.Token);

        Assert.False(result);
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c")]
    [InlineData("eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUuY29tIiwic3ViIjoidXNlcjEyMyIsImF1ZCI6ImFwaS5leGFtcGxlLmNvbSIsImV4cCI6MTcwMDAwMDAwMCwiaWF0IjoxNjAwMDAwMDAwfQ.signature")]
    [InlineData("header.payload.signature")]
    public async Task IsTokenValidAsync_WithValidJwtStructure_ValidatesStructure(string validJwtStructure)
    {

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        var result = await provider.IsTokenValidAsync(validJwtStructure);

        Assert.False(result);
    }


    [Fact]
    public async Task IsTokenValidAsync_WithTokenContainingSpecialCharacters_HandlesGracefully()
    {

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);

        const string tokenWithSpecialChars = "header+special/chars.payload-with_underscores.signature=with=equals";


        var result = await provider.IsTokenValidAsync(tokenWithSpecialChars);


        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithVeryLongToken_HandlesGracefully()
    {

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        var longPart = new string('a', 10000);
        var veryLongToken = $"{longPart}.{longPart}.{longPart}";


        var result = await provider.IsTokenValidAsync(veryLongToken);


        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_ConcurrentCalls_AreThreadSafe()
    {

        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.concurrent.token";

        SetupTokenEndpointWithToken(token, 3600);

        var provider = new OAuth2ClientCredentialsProvider(
            $"{_mockServer.Urls[0]}/oauth/token",
            "test-client",
            "test-secret",
            httpClient: _httpClient);


        await provider.GetTokenAsync();

        var tasks = new Task<bool>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = provider.IsTokenValidAsync(token);
        }

        var results = await Task.WhenAll(tasks);


        Assert.All(results, result => Assert.True(result));
    }

    #endregion


    private void SetupTokenEndpointWithToken(string token, int expiresIn)
    {
        _mockServer.Reset();

        _mockServer
            .Given(Request.Create()
                .WithPath("/oauth/token")
                .UsingPost()
                .WithHeader("Content-Type", "application/x-www-form-urlencoded"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    access_token = token,
                    expires_in = expiresIn,
                    token_type = "Bearer"
                }));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }
}