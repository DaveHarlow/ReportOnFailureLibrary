using ReportOnFailure.Interfaces.Reporters;

namespace ReportOnFailure.Interfaces.Resolvers;

public interface IReportResolverSync<in T> where T : IReporter
{
    string ResolveSync(T reporter);
}