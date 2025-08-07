namespace ReportOnFailure.Interfaces;

using Enums;

public interface IReporter
{
    public ResultsFormat ResultsFormat { get; set; }
    public ExecutionMode? ExecutionModeOverride { get; set; }

    public string FileNamePrefix { get; set; }
}