using ReportOnFailure.Interfaces;
using ReportOnFailure.Enums;
using ReportOnFailure.Factories;
using ReportOnFailure.Resolvers;

namespace ReportOnFailure;

public class Registry : IRegistry
{
    private readonly IWriterFactory _writerFactory;
    private readonly IDbResolver _dbResolver;

    public Registry() : this(null, null)
    {
    }

    public Registry(IWriterFactory? writerFactory = null, IDbResolver? dbResolver = null)
    {
        _writerFactory = writerFactory ?? new WriterFactory();
        _dbResolver = dbResolver ?? new DbResolver(new ResultFormatterFactory(), new DbProviderFactoryFactory());
        
        Reporters = new List<IReporter>();
        DestinationType = DestinationType.FileSystem; 
        DestinationLocation = string.Empty; 
        CompressResults = false; 
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

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        ValidateForExecution();

        var writer = _writerFactory.CreateWriter(DestinationType, DestinationLocation, CompressResults);
        
        if (ExecutionMode == ExecutionMode.Asynchronous)
        {
            await ExecuteAsyncMode(writer, cancellationToken);
        }
        else
        {
            await ExecuteSyncModeAsync(writer, cancellationToken);
        }
    }

    public void Execute()
    {
        ValidateForExecution();

        var writer = _writerFactory.CreateWriter(DestinationType, DestinationLocation, CompressResults);
        ExecuteSyncMode(writer);
    }

    private void ValidateForExecution()
    {
        if (string.IsNullOrEmpty(DestinationLocation))
            throw new InvalidOperationException("Destination location must be set before execution.");
        
        if (Reporters.Count == 0)
            throw new InvalidOperationException("At least one reporter must be registered before execution.");
    }

    private async Task ExecuteAsyncMode(IWriter writer, CancellationToken cancellationToken)
    {
        var tasks = Reporters.Select(async reporter =>
        {
            var content = await ResolveReporterAsync(reporter, cancellationToken);
            var fileName = GenerateFileName(reporter);
            await writer.WriteAsync(content, fileName, cancellationToken);
        });

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteSyncModeAsync(IWriter writer, CancellationToken cancellationToken)
    {
        foreach (var reporter in Reporters)
        {
            var content = await ResolveReporterAsync(reporter, cancellationToken);
            var fileName = GenerateFileName(reporter);
            await writer.WriteAsync(content, fileName, cancellationToken);
        }
    }

    private void ExecuteSyncMode(IWriter writer)
    {
        foreach (var reporter in Reporters)
        {
            var content = ResolveReporterSync(reporter);
            var fileName = GenerateFileName(reporter);
            writer.Write(content, fileName);
        }
    }

    private async Task<string> ResolveReporterAsync(IReporter reporter, CancellationToken cancellationToken)
    {
        var effectiveExecutionMode = reporter.ExecutionModeOverride ?? ExecutionMode;
        
        return reporter switch
        {
            IDbReporter dbReporter => effectiveExecutionMode == ExecutionMode.Asynchronous 
                ? await _dbResolver.ResolveAsync(dbReporter, cancellationToken)
                : _dbResolver.ResolveSync(dbReporter),
            _ => throw new NotSupportedException($"Reporter type {reporter.GetType().Name} is not supported.")
        };
    }

    private string ResolveReporterSync(IReporter reporter)
    {
        return reporter switch
        {
            IDbReporter dbReporter => _dbResolver.ResolveSync(dbReporter),
            
            _ => throw new NotSupportedException($"Reporter type {reporter.GetType().Name} is not supported.")
        };
    }

    private static string GenerateFileName(IReporter reporter)
    {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = GetFileExtension(reporter.ResultsFormat);
            var guid = Guid.NewGuid().ToString("N")[..8]; 


            return $"{reporter.FileNamePrefix}_{timestamp}_{guid}.{extension}";
    }

    private static string GetFileExtension(ResultsFormat format)
    {
        return format switch
        {
            ResultsFormat.Json => "json",
            ResultsFormat.Csv => "csv",
            ResultsFormat.Xml => "xml",
            ResultsFormat.Html => "html",
            ResultsFormat.Text => "txt",
            _ => "txt"
        };
    }
}