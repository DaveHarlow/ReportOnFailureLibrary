namespace ReportOnFailure.Interfaces;

public interface IRegistry
{
    void RegisterReporter(IReporter reporter);
    void UnRegisterReporter(IReporter reporter);

    List<string> GetAllReports();
}