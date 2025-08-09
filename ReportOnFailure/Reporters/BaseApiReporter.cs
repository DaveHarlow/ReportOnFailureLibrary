using ReportOnFailure.Authentication;
using ReportOnFailure.Enums;
using ReportOnFailure.Interfaces.Reporters;
using System.Net.Http.Headers;
using System.Text;

namespace ReportOnFailure.Reporters;

public abstract class BaseApiReporter<T> : BaseReporter<T>, IBaseApiReporter
    where T : BaseApiReporter<T>
{

    public string BaseUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool FollowRedirects { get; set; } = true;
    public Encoding ContentEncoding { get; set; } = Encoding.UTF8;


    public Dictionary<string, string> Headers { get; set; } = new();
    public string? AuthorizationToken { get; set; }
    public string? BasicAuthUsername { get; set; }
    public string? BasicAuthPassword { get; set; }
    public IJwtTokenProvider? JwtTokenProvider { get; set; }


    public string? RequestBody { get; set; }
    public ContentType? RequestContentType { get; set; }


    public T WithBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        return (T)this;
    }

    public T WithEndpoint(string endpoint)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        return (T)this;
    }

    public T WithTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));
        TimeoutSeconds = timeoutSeconds;
        return (T)this;
    }

    public T WithFollowRedirects(bool followRedirects = true)
    {
        FollowRedirects = followRedirects;
        return (T)this;
    }

    public T WithEncoding(Encoding encoding)
    {
        ContentEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        return (T)this;
    }


    public T WithJwtProvider(IJwtTokenProvider tokenProvider)
    {
        JwtTokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        return (T)this;
    }

    public T WithOAuth2ClientCredentials(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope = null)
    {
        var provider = new OAuth2ClientCredentialsProvider(tokenEndpoint, clientId, clientSecret, scope);
        return WithJwtProvider(provider);
    }

    public T WithStaticBearerToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        Headers["Authorization"] = $"Bearer {token}";
        return (T)this;
    }

    public T WithBearerToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        AuthorizationToken = token;
        Headers["Authorization"] = $"Bearer {token}";
        return (T)this;
    }

    public T WithBasicAuth(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);

        BasicAuthUsername = username;
        BasicAuthPassword = password;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        Headers["Authorization"] = $"Basic {credentials}";
        return (T)this;
    }

    public T WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        Headers[name] = value;
        return (T)this;
    }

    public T WithHeaders(Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        foreach (var header in headers)
        {
            Headers[header.Key] = header.Value;
        }
        return (T)this;
    }

    public T WithApiKey(string keyName, string keyValue, bool inHeader = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);
        ArgumentException.ThrowIfNullOrEmpty(keyValue);

        if (inHeader)
        {
            Headers[keyName] = keyValue;
        }
        else
        {


            HandleApiKeyInQuery(keyName, keyValue);
        }
        return (T)this;
    }


    public T WithJsonBody(string jsonBody, Encoding? encoding = null)
    {
        RequestBody = jsonBody;
        RequestContentType = ContentType.ApplicationJson;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return (T)this;
    }

    public T WithXmlBody(string xmlBody, Encoding? encoding = null)
    {
        RequestBody = xmlBody;
        RequestContentType = ContentType.ApplicationXml;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return (T)this;
    }

    public T WithTextBody(string textBody, Encoding? encoding = null)
    {
        RequestBody = textBody;
        RequestContentType = ContentType.TextPlain;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return (T)this;
    }

    public T WithBody(string body, ContentType contentType, Encoding? encoding = null)
    {
        RequestBody = body;
        RequestContentType = contentType;
        ContentEncoding = encoding ?? Encoding.UTF8;
        return (T)this;
    }


    protected virtual void HandleApiKeyInQuery(string keyName, string keyValue)
    {

        throw new NotSupportedException($"Query parameter API keys are not supported by {GetType().Name}");
    }


    public virtual MediaTypeHeaderValue GetContentTypeString()
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