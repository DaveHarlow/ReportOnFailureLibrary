namespace ReportOnFailure.Interfaces.Reporters;

using Authentication;
using Enums;
using System.Net.Http.Headers;
using System.Text;

public interface IBaseApiReporter : IReporter
{

    string BaseUrl { get; set; }
    string Endpoint { get; set; }
    int TimeoutSeconds { get; set; }
    bool FollowRedirects { get; set; }
    Encoding ContentEncoding { get; set; }


    Dictionary<string, string> Headers { get; set; }
    string? AuthorizationToken { get; set; }
    string? BasicAuthUsername { get; set; }
    string? BasicAuthPassword { get; set; }
    IJwtTokenProvider? JwtTokenProvider { get; set; }


    string? RequestBody { get; set; }
    ContentType? RequestContentType { get; set; }


    MediaTypeHeaderValue GetContentTypeString();
}

