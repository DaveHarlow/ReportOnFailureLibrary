using ReportOnFailure.Interfaces.Reporters;

namespace ReportOnFailure.Interfaces.Resolvers
{
    public interface IApiResolver : IReportResolverAsync<IApiReporter>, IReportResolverSync<IApiReporter>
    {
    }
}
