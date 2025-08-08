using System.Text;
using ReportOnFailure.Authentication;

namespace ReportOnFailure.Interfaces;

using System.Collections.Generic;
using System.Net.Http.Headers;
using Enums;

public interface IApiReporter : IReporter
{
    string BaseUrl { get; set; }
    string Endpoint { get; set; }
    ApiHttpMethod Method { get; set; }
    Dictionary<string, string> Headers { get; set; }
    Dictionary<string, object> QueryParameters { get; set; }
    string? RequestBody { get; set; }
    ContentType? RequestContentType { get; set; }

    Encoding ContentEncoding { get; set; }

    int TimeoutSeconds { get; set; }
    bool FollowRedirects { get; set; }
    Dictionary<string, string> FormData { get; set; }
    string? AuthorizationToken { get; set; }
    string? BasicAuthUsername { get; set; }
    string? BasicAuthPassword { get; set; }

    IJwtTokenProvider? JwtTokenProvider { get; set; }

    string BuildFullUrl();

    MediaTypeHeaderValue GetContentTypeString();
}