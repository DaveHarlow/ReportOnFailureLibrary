namespace ReportOnFailure.Authentication;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IJwtTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(string token, CancellationToken cancellationToken = default);
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}