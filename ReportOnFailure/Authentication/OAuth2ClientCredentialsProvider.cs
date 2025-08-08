namespace ReportOnFailure.Authentication;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public class OAuth2ClientCredentialsProvider : IJwtTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string? _scope;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public OAuth2ClientCredentialsProvider(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope = null,
        HttpClient? httpClient = null)
    {
        _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _scope = scope;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return _cachedToken;
            }

            await RefreshTokenAsync(cancellationToken);
            return _cachedToken!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<bool> IsTokenValidAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(false);

        var parts = token.Split('.');
        if (parts.Length != 3)
            return Task.FromResult(false);

        return Task.FromResult(token == _cachedToken && DateTime.UtcNow < _tokenExpiry);
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var request = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        };

        if (!string.IsNullOrEmpty(_scope))
        {
            request["scope"] = _scope;
        }

        var content = new FormUrlEncodedContent(request);
        var response = await _httpClient.PostAsync(_tokenEndpoint, content, cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

        if (tokenResponse?.AccessToken == null)
            throw new InvalidOperationException("Failed to retrieve access token from OAuth2 endpoint");

        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 3600);
    }

    private record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}