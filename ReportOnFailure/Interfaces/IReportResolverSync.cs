namespace ReportOnFailure.Interfaces;

public interface IReportResolverSync<in T> where T : IReporter
{
    string ResolveSync(T reporter);
}