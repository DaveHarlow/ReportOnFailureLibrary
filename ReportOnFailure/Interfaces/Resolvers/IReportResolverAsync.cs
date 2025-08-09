namespace ReportOnFailure.Interfaces.Resolvers;

using ReportOnFailure.Interfaces.Reporters;
using System.Threading;
using System.Threading.Tasks;

public interface IReportResolverAsync<in T> where T : IReporter
{
    Task<string> ResolveAsync(T reporter, CancellationToken cancellationToken = default);
}