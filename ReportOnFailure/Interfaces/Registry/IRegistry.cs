namespace ReportOnFailure.Interfaces.Registry;

using Enums;
using ReportOnFailure.Interfaces.Reporters;

public interface IRegistry
{
    void RegisterReporter(IReporter reporter);
    void UnRegisterReporter(IReporter reporter);

    List<IReporter> Reporters { get; }
    ExecutionMode ExecutionMode { get; set; }
    DestinationType DestinationType { get; set; }

    string DestinationLocation { get; set; }


    bool CompressResults { get; set; }


}