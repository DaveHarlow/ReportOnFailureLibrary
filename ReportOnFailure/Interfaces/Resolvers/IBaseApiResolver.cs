using ReportOnFailure.Interfaces.Reporters;

namespace ReportOnFailure.Interfaces.Resolvers;

public interface IBaseApiResolver<T> : IReportResolverAsync<T>, IReportResolverSync<T>
    where T : class, IBaseApiReporter
{

}