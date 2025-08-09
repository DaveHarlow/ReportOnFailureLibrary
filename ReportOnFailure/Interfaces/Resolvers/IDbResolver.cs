using ReportOnFailure.Interfaces.Reporters;

namespace ReportOnFailure.Interfaces.Resolvers;

public interface IDbResolver : IReportResolverAsync<IDbReporter>, IReportResolverSync<IDbReporter>
{
}