namespace ReportOnFailure.Reporters;

using ReportOnFailure.Interfaces;
using ReportOnFailure.Enums;

public class BaseReporter : IReporter
{

    public ResultsFormat ResultsFormat { get; set; }

    public BaseReporter WithResultsFormat(ResultsFormat format)
    {
        ResultsFormat = format;
        return this;
    }
}