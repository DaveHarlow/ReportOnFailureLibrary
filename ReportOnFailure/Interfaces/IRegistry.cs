namespace ReportOnFailure.Interfaces;

using Enums;

public interface IRegistry
{
    void RegisterReporter(IReporter reporter);
    void UnRegisterReporter(IReporter reporter);

    List<IReporter> Reporters { get; }
    ExecutionMode ExecutionMode { get; set; }


}