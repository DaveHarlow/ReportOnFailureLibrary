using ReportOnFailure.Enums;

namespace ReportOnFailure.Resolvers;

using Factories;
using ReportOnFailure.Interfaces.Reporters;
using ReportOnFailure.Interfaces.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ApiResolver : IApiResolver
{
    private readonly IResultFormatterFactory _formatterFactory;
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    public ApiResolver(IResultFormatterFactory formatterFactory, HttpClient? httpClient = null)
    {
        _formatterFactory = formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));

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

    public async Task<string> ResolveAsync(IApiReporter reporter, CancellationToken cancellationToken = default)
    {
        if (reporter.JwtTokenProvider != null)
        {
            var token = await reporter.JwtTokenProvider.GetTokenAsync(cancellationToken);
            reporter.Headers["Authorization"] = $"Bearer {token}";
        }

        var request = CreateHttpRequestMessage(reporter);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(reporter.TimeoutSeconds));

        using var response = await _httpClient.SendAsync(request, cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && reporter.JwtTokenProvider != null)
        {
            try
            {
                await reporter.JwtTokenProvider.RefreshTokenAsync(cancellationToken);
                var newToken = await reporter.JwtTokenProvider.GetTokenAsync(cancellationToken);
                reporter.Headers["Authorization"] = $"Bearer {newToken}";

                var retryRequest = CreateHttpRequestMessage(reporter);
                using var retryResponse = await _httpClient.SendAsync(retryRequest, cts.Token);
                var retryContent = await retryResponse.Content.ReadAsStringAsync(cts.Token);

                var retryResults = ProcessApiResponse(retryResponse, retryContent);
                return FormatResults(retryResults, reporter.ResultsFormat);
            }
            catch
            {
                var results = ProcessApiResponse(response, responseContent);
                return FormatResults(results, reporter.ResultsFormat);
            }
        }

        var normalResults = ProcessApiResponse(response, responseContent);
        return FormatResults(normalResults, reporter.ResultsFormat);
    }

    public string ResolveSync(IApiReporter reporter)
    {
        return ResolveAsync(reporter).GetAwaiter().GetResult();
    }

    private HttpRequestMessage CreateHttpRequestMessage(IApiReporter reporter)
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
                    Encoding.UTF8,
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
                ["Url"] = response.RequestMessage?.RequestUri?.ToString()
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

    private string FormatResults(IReadOnlyCollection<Dictionary<string, object?>> data, ResultsFormat format)
    {
        return _formatterFactory.CreateFormatter(format).Format(data);
    }

    private static System.Net.Http.HttpMethod ConvertToHttpMethod(Enums.ApiHttpMethod method)
    {
        return method switch
        {
            Enums.ApiHttpMethod.GET => System.Net.Http.HttpMethod.Get,
            Enums.ApiHttpMethod.POST => System.Net.Http.HttpMethod.Post,
            Enums.ApiHttpMethod.PUT => System.Net.Http.HttpMethod.Put,
            Enums.ApiHttpMethod.PATCH => System.Net.Http.HttpMethod.Patch,
            Enums.ApiHttpMethod.DELETE => System.Net.Http.HttpMethod.Delete,
            Enums.ApiHttpMethod.HEAD => System.Net.Http.HttpMethod.Head,
            Enums.ApiHttpMethod.OPTIONS => System.Net.Http.HttpMethod.Options,
            _ => System.Net.Http.HttpMethod.Get
        };
    }

    private static bool ShouldIncludeContent(Enums.ApiHttpMethod method)
    {
        return method is Enums.ApiHttpMethod.POST or Enums.ApiHttpMethod.PUT or Enums.ApiHttpMethod.PATCH;
    }

    private static bool IsContentHeader(string headerName)
    {
        return headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Language", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}