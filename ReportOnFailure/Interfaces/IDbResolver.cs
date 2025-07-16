namespace ReportOnFailure.Interfaces;

public interface IDbResolver : IReportResolverAsync<IDbReporter>, IReportResolverSync<IDbReporter>
{
}