namespace ReportOnFailure.Reporters;

using ReportOnFailure.Enums;
using ReportOnFailure.Interfaces.Reporters;

public abstract class BaseReporter<T> : IReporter where T : BaseReporter<T>
{

    public ResultsFormat ResultsFormat { get; set; }
    public ExecutionMode? ExecutionModeOverride { get; set; }

    public string FileNamePrefix { get; set; } = "";


    public T WithResultsFormat(ResultsFormat format)
    {
        ResultsFormat = format;
        return (T)this;
    }

    public T WithExecutionModeOverride(ExecutionMode mode)
    {
        ExecutionModeOverride = mode;
        return (T)this;
    }

    public T WithFileNamePrefix(string fileName)
    {
        FileNamePrefix = fileName;
        return (T)this;
    }

}