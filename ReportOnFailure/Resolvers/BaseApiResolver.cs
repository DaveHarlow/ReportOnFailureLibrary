using ReportOnFailure.Enums;
using ReportOnFailure.Factories;
using ReportOnFailure.Interfaces.Reporters;
using ReportOnFailure.Interfaces.Resolvers;

namespace ReportOnFailure.Resolvers;

public abstract class BaseApiResolver<T> : IBaseApiResolver<T>, IDisposable
    where T : class, IBaseApiReporter
{
    protected readonly IResultFormatterFactory _formatterFactory;
    private bool _disposed = false;

    protected BaseApiResolver(IResultFormatterFactory formatterFactory)
    {
        _formatterFactory = formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
    }

    public abstract Task<string> ResolveAsync(T reporter, CancellationToken cancellationToken = default);

    public virtual string ResolveSync(T reporter)
    {
        return ResolveAsync(reporter).GetAwaiter().GetResult();
    }


    protected virtual async Task<string> HandleAuthenticationAsync(T reporter, CancellationToken cancellationToken)
    {
        if (reporter.JwtTokenProvider != null)
        {
            var token = await reporter.JwtTokenProvider.GetTokenAsync(cancellationToken);
            reporter.Headers["Authorization"] = $"Bearer {token}";
            return token;
        }
        return string.Empty;
    }

    protected virtual async Task<string> HandleAuthenticationRetryAsync(T reporter, CancellationToken cancellationToken)
    {
        if (reporter.JwtTokenProvider != null)
        {
            try
            {
                await reporter.JwtTokenProvider.RefreshTokenAsync(cancellationToken);
                var newToken = await reporter.JwtTokenProvider.GetTokenAsync(cancellationToken);
                reporter.Headers["Authorization"] = $"Bearer {newToken}";
                return newToken;
            }
            catch
            {

                return string.Empty;
            }
        }
        return string.Empty;
    }

    protected virtual string FormatResults(IReadOnlyCollection<Dictionary<string, object?>> data, ResultsFormat format)
    {
        if (data == null || data.Count == 0)
        {
            return "No data returned from API call.";
        }
        return _formatterFactory.CreateFormatter(format).Format(data);
    }


    protected abstract Task<List<Dictionary<string, object?>>> ExecuteRequestAsync(T reporter, CancellationToken cancellationToken);


    protected virtual async Task<List<Dictionary<string, object?>>> ExecuteRequestWithRetryAsync(T reporter, CancellationToken cancellationToken)
    {
        var result = await ExecuteRequestAsync(reporter, cancellationToken);


        if (ShouldRetryForAuthentication(result) && reporter.JwtTokenProvider != null)
        {
            var newToken = await HandleAuthenticationRetryAsync(reporter, cancellationToken);
            if (!string.IsNullOrEmpty(newToken))
            {

                return await ExecuteRequestAsync(reporter, cancellationToken);
            }
        }

        return result;
    }


    protected virtual bool ShouldRetryForAuthentication(List<Dictionary<string, object?>> result)
    {

        if (result?.Count > 0 && result[0].ContainsKey("StatusCode"))
        {
            return result[0]["StatusCode"]?.ToString() == "401";
        }
        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {


            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}