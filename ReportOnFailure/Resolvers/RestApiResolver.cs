using ReportOnFailure.Enums;
using ReportOnFailure.Factories;
using ReportOnFailure.Interfaces.Resolvers;

namespace ReportOnFailure.Resolvers;

public class RestApiResolver : BaseApiResolver<IRestApiReporter>, IRestApiResolver
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    public RestApiResolver(IResultFormatterFactory formatterFactory, HttpClient? httpClient = null)
        : base(formatterFactory)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _disposeHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _disposeHttpClient = true;
        }
    }

    public override async Task<string> ResolveAsync(IRestApiReporter reporter, CancellationToken cancellationToken = default)
    {
        await HandleAuthenticationAsync(reporter, cancellationToken);

        var results = await ExecuteRequestWithRetryAsync(reporter, cancellationToken);
        return FormatResults(results, reporter.ResultsFormat);
    }

    protected override async Task<List<Dictionary<string, object?>>> ExecuteRequestAsync(IRestApiReporter reporter, CancellationToken cancellationToken)
    {
        var request = CreateHttpRequestMessage(reporter);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(reporter.TimeoutSeconds));

        using var response = await _httpClient.SendAsync(request, cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

        return ProcessApiResponse(response, responseContent);
    }

    private HttpRequestMessage CreateHttpRequestMessage(IRestApiReporter reporter)
    {
        var fullUrl = reporter.BuildFullUrl();
        var httpMethod = ConvertToHttpMethod(reporter.Method);
        var request = new HttpRequestMessage(httpMethod, fullUrl);


        foreach (var header in reporter.Headers)
        {
            if (!IsContentHeader(header.Key))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }


        if (ShouldIncludeContent(reporter.Method))
        {
            if (reporter.FormData.Count > 0)
            {
                var formContent = new FormUrlEncodedContent(reporter.FormData);
                request.Content = formContent;
            }
            else if (!string.IsNullOrEmpty(reporter.RequestBody))
            {
                var content = new StringContent(
                    reporter.RequestBody,
                    reporter.ContentEncoding,
                    reporter.GetContentTypeString());

                request.Content = content;
            }


            if (request.Content != null)
            {
                foreach (var header in reporter.Headers.Where(h => IsContentHeader(h.Key)))
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return request;
    }

    private static List<Dictionary<string, object?>> ProcessApiResponse(HttpResponseMessage response, string content)
    {
        var result = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["StatusCode"] = (int)response.StatusCode,
                ["Status"] = response.StatusCode.ToString(),
                ["IsSuccess"] = response.IsSuccessStatusCode,
                ["Content"] = content,
                ["ContentLength"] = content.Length,
                ["ContentType"] = response.Content.Headers.ContentType?.ToString(),
                ["Url"] = response.RequestMessage?.RequestUri?.ToString(),
                ["Timestamp"] = DateTime.UtcNow
            }
        };


        foreach (var header in response.Headers)
        {
            result[0][$"Header_{header.Key}"] = string.Join(", ", header.Value);
        }


        if (response.Content?.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                result[0][$"Header_{header.Key}"] = string.Join(", ", header.Value);
            }
        }


        if (response.TrailingHeaders.Any())
        {
            foreach (var header in response.TrailingHeaders)
            {
                result[0][$"TrailingHeader_{header.Key}"] = string.Join(", ", header.Value);
            }
        }

        return result;
    }

    private static HttpMethod ConvertToHttpMethod(ApiHttpMethod method)
    {
        return method switch
        {
            ApiHttpMethod.GET => HttpMethod.Get,
            ApiHttpMethod.POST => HttpMethod.Post,
            ApiHttpMethod.PUT => HttpMethod.Put,
            ApiHttpMethod.PATCH => HttpMethod.Patch,
            ApiHttpMethod.DELETE => HttpMethod.Delete,
            ApiHttpMethod.HEAD => HttpMethod.Head,
            ApiHttpMethod.OPTIONS => HttpMethod.Options,
            _ => HttpMethod.Get
        };
    }

    private static bool ShouldIncludeContent(ApiHttpMethod method)
    {
        return method is ApiHttpMethod.POST or ApiHttpMethod.PUT or ApiHttpMethod.PATCH;
    }

    private static bool IsContentHeader(string headerName)
    {
        return headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Language", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Range", StringComparison.OrdinalIgnoreCase);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}