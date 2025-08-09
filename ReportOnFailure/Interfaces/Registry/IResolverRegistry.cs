using ReportOnFailure.Interfaces.Reporters;
using ReportOnFailure.Interfaces.Resolvers;

namespace ReportOnFailure.Interfaces.Registry;

public interface IResolverRegistry
{
    void RegisterResolver<TReporter, TResolver>(TResolver resolver)
        where TReporter : class, IReporter
        where TResolver : class, IReportResolverAsync<TReporter>, IReportResolverSync<TReporter>;

    void RegisterResolver<TReporter>(Func<TReporter, CancellationToken, Task<string>> asyncResolver, Func<TReporter, string> syncResolver)
        where TReporter : class, IReporter;

    Task<string> ResolveAsync<TReporter>(TReporter reporter, CancellationToken cancellationToken = default)
        where TReporter : class, IReporter;

    string ResolveSync<TReporter>(TReporter reporter)
        where TReporter : class, IReporter;

    bool CanResolve<TReporter>() where TReporter : class, IReporter;
    bool CanResolve(Type reporterType);
}