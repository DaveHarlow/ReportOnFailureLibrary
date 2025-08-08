namespace ReportOnFailure.Interfaces
{
    public interface IApiResolver : IReportResolverAsync<IApiReporter>, IReportResolverSync<IApiReporter>
    {
    }
}
