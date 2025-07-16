namespace ReportOnFailure.Interfaces;

using ReportOnFailure.Enums;

public interface IReporter
{
    public ResultsFormat ResultsFormat { get; set; }
}