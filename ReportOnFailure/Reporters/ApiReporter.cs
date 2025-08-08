using System.Net.Http.Headers;

namespace ReportOnFailure.Reporters;

using Enums;
using Interfaces;
using ReportOnFailure.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

public class ApiReporter : BaseReporter<ApiReporter>, IApiReporter
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public ApiHttpMethod Method { get; set; } = ApiHttpMethod.GET;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, object> QueryParameters { get; set; } = new();
    public string? RequestBody { get; set; }
    public ContentType? RequestContentType { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool FollowRedirects { get; set; } = true;
    public Dictionary<string, string> FormData { get; set; } = new();
    public string? AuthorizationToken { get; set; }
    public string? BasicAuthUsername { get; set; }
    public string? BasicAuthPassword { get; set; }
    public Encoding ContentEncoding { get; set; } = Encoding.UTF8;

    public IJwtTokenProvider? JwtTokenProvider { get; set; }

    
    public ApiReporter WithJwtProvider(IJwtTokenProvider tokenProvider)
    {
        JwtTokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        return this;
    }

    public ApiReporter WithOAuth2ClientCredentials(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope = null)
    {
        var provider = new OAuth2ClientCredentialsProvider(tokenEndpoint, clientId, clientSecret, scope);
        return WithJwtProvider(provider);
    }

    public ApiReporter WithStaticBearerToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        Headers["Authorization"] = $"Bearer {token}";
        return this;
    }

    
    public ApiReporter WithBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        return this;
    }

    public ApiReporter WithEndpoint(string endpoint)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        return this;
    }

    public ApiReporter WithMethod(ApiHttpMethod method)
    {
        Method = method;
        return this;
    }

    public ApiReporter WithTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        TimeoutSeconds = timeoutSeconds;
        return this;
    }

    public ApiReporter WithFollowRedirects(bool followRedirects = true)
    {
        FollowRedirects = followRedirects;
        return this;
    }

    
    public ApiReporter WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        Headers[name] = value;
        return this;
    }

    public ApiReporter WithHeaders(Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (var header in headers)
        {
            Headers[header.Key] = header.Value;
        }
        return this;
    }

    
    public ApiReporter WithQueryParameter(string name, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        QueryParameters[name] = value;
        return this;
    }

    public ApiReporter WithQueryParameters(Dictionary<string, object> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var param in parameters)
        {
            QueryParameters[param.Key] = param.Value;
        }
        return this;
    }

    
    public ApiReporter WithJsonBody(string jsonBody, Encoding? encoding = null)
    {
        RequestBody = jsonBody;
        RequestContentType = ContentType.ApplicationJson;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return this;
    }

    public ApiReporter WithXmlBody(string xmlBody, Encoding? encoding = null)
    {
        RequestBody = xmlBody;
        RequestContentType = ContentType.ApplicationXml;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return this;
    }

    public ApiReporter WithTextBody(string textBody, Encoding? encoding = null)
    {
        RequestBody = textBody;
        RequestContentType = ContentType.TextPlain;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return this;
    }

    public ApiReporter WithBody(string body, ContentType contentType, Encoding? encoding = null)
    {
        RequestBody = body;
        RequestContentType = contentType;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return this;
    }

    public ApiReporter WithEncoding(Encoding encoding)
    {
        ContentEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        return this;
    }

    
    public ApiReporter WithFormData(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        FormData[name] = value;
        RequestContentType = ContentType.ApplicationFormUrlEncoded;
        return this;
    }

    public ApiReporter WithFormData(Dictionary<string, string> formData)
    {
        ArgumentNullException.ThrowIfNull(formData);

        foreach (var data in formData)
        {
            FormData[data.Key] = data.Value;
        }
        RequestContentType = ContentType.ApplicationFormUrlEncoded;
        return this;
    }

    
    public ApiReporter WithBearerToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        AuthorizationToken = token;
        Headers["Authorization"] = $"Bearer {token}";
        return this;
    }

    public ApiReporter WithApiKey(string keyName, string keyValue, bool inHeader = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);
        ArgumentException.ThrowIfNullOrEmpty(keyValue);

        if (inHeader)
        {
            Headers[keyName] = keyValue;
        }
        else
        {
            QueryParameters[keyName] = keyValue;
        }
        return this;
    }

    public ApiReporter WithBasicAuth(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);

        BasicAuthUsername = username;
        BasicAuthPassword = password;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        Headers["Authorization"] = $"Basic {credentials}";
        return this;
    }

    
    public string BuildFullUrl()
    {
        var baseUrl = BaseUrl.TrimEnd('/');
        var endpoint = Endpoint.TrimStart('/');
        var fullUrl = $"{baseUrl}/{endpoint}";

        if (QueryParameters.Count == 0)
            return fullUrl;

        var queryString = string.Join("&",
            QueryParameters.Select(kvp =>
                $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value.ToString())}"));

        return $"{fullUrl}?{queryString}";
    }

    public MediaTypeHeaderValue GetContentTypeString()
    {
        var mediaType = RequestContentType switch
        {
            ContentType.ApplicationJson => "application/json",
            ContentType.ApplicationXml => "application/xml",
            ContentType.ApplicationFormUrlEncoded => "application/x-www-form-urlencoded",
            ContentType.TextPlain => "text/plain",
            ContentType.TextHtml => "text/html",
            ContentType.ApplicationOctetStream => "application/octet-stream",
            ContentType.MultipartFormData => "multipart/form-data",
            _ => "application/json"
        };

        var result = MediaTypeHeaderValue.Parse(mediaType);

        if (ShouldIncludeCharset(RequestContentType))
        {
            result.CharSet = ContentEncoding.WebName;
        }

        return result;
    }

    private static bool ShouldIncludeCharset(ContentType? contentType)
    {
        return contentType is ContentType.ApplicationJson
            or ContentType.ApplicationXml
            or ContentType.TextPlain
            or ContentType.TextHtml
            or ContentType.ApplicationFormUrlEncoded;
    }
}