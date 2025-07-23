using ReportOnFailure.Interfaces;
using ReportOnFailure.Enums;


namespace ReportOnFailure;

public class Registry : IRegistry
{

    public Registry()
    {
        Reporters = new List<IReporter>();
        DestinationType = DestinationType.FileSystem; // Default write type
        DestinationLocation = string.Empty; // Default location
        CompressResults = false; // Default compression setting
    }

    public List<IReporter> Reporters { get; set; }

    public DestinationType DestinationType { get; set; }
    public bool CompressResults { get; set; }

    public string DestinationLocation { get; set; } = string.Empty;

    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Synchronous;

    public void RegisterReporter(IReporter reporter)
    {
        if (reporter == null) throw new ArgumentNullException(nameof(reporter));
        if (!Reporters.Contains(reporter))
        {
            Reporters.Add(reporter);
        }
    }

    public void UnRegisterReporter(IReporter reporter)
    {
        if (reporter == null) throw new ArgumentNullException(nameof(reporter));
        Reporters.Remove(reporter);
    }

    public Registry WithExecutionMode(ExecutionMode executionMode)
    {
        ExecutionMode = executionMode;
        return this;
    }

    public Registry WithDestinationType(DestinationType destinationType)
    {
        DestinationType = destinationType;
        return this;
    }

    public Registry WithDestinationLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Destination location cannot be null or empty.", nameof(location));
        }
        DestinationLocation = location;
        return this;
    }

    public Registry WithCompression()
    {
        CompressResults = true;
        return this;
    }


}