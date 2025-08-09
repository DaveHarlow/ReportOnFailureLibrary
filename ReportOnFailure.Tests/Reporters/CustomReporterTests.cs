using ReportOnFailure.Enums;
using ReportOnFailure.Interfaces.Reporters;
using ReportOnFailure.Reporters;

namespace ReportOnFailure.Tests.Reporters;

public class CustomReporterTests
{
    #region Custom Reporter Implementation Tests

    [Fact]
    public void CustomFileReporter_SetsPropertiesCorrectly()
    {

        var reporter = new CustomFileReporter()
            .WithDirectoryPath(@"C:\logs")
            .WithSearchPattern("*.log")
            .WithRecursive(true)
            .WithResultsFormat(ResultsFormat.Json)
            .WithFileNamePrefix("FileListing")
            .WithExecutionModeOverride(ExecutionMode.Asynchronous);


        Assert.Equal(@"C:\logs", reporter.DirectoryPath);
        Assert.Equal("*.log", reporter.SearchPattern);
        Assert.True(reporter.Recursive);
        Assert.Equal(ResultsFormat.Json, reporter.ResultsFormat);
        Assert.Equal("FileListing", reporter.FileNamePrefix);
        Assert.Equal(ExecutionMode.Asynchronous, reporter.ExecutionModeOverride);
    }

    [Fact]
    public void CustomFileReporter_FluentApi_ReturnsCorrectInstance()
    {

        var reporter = new CustomFileReporter();


        var result1 = reporter.WithDirectoryPath(@"C:\temp");
        var result2 = reporter.WithSearchPattern("*.txt");
        var result3 = reporter.WithRecursive(false);


        Assert.Same(reporter, result1);
        Assert.Same(reporter, result2);
        Assert.Same(reporter, result3);
    }

    [Fact]
    public void CustomFileReporter_DefaultValues_AreSetCorrectly()
    {

        var reporter = new CustomFileReporter();


        Assert.Equal(string.Empty, reporter.DirectoryPath);
        Assert.Equal("*.*", reporter.SearchPattern);
        Assert.False(reporter.Recursive);
        Assert.Equal(ResultsFormat.Json, reporter.ResultsFormat);
        Assert.Equal(string.Empty, reporter.FileNamePrefix);
        Assert.Null(reporter.ExecutionModeOverride);
    }

    #endregion

    #region Custom Memory Reporter Tests

    [Fact]
    public void CustomMemoryReporter_SetsPropertiesCorrectly()
    {

        var data = new Dictionary<string, object>
        {
            ["TotalMemory"] = 1024000,
            ["AvailableMemory"] = 512000,
            ["UsedMemory"] = 512000
        };


        var reporter = new CustomMemoryReporter()
            .WithMemoryData(data)
            .WithIncludeGCInfo(true)
            .WithResultsFormat(ResultsFormat.Xml)
            .WithFileNamePrefix("MemoryInfo");


        Assert.Equal(data, reporter.MemoryData);
        Assert.True(reporter.IncludeGCInfo);
        Assert.Equal(ResultsFormat.Xml, reporter.ResultsFormat);
        Assert.Equal("MemoryInfo", reporter.FileNamePrefix);
    }

    [Fact]
    public void CustomMemoryReporter_WithEmptyData_ThrowsArgumentException()
    {

        var reporter = new CustomMemoryReporter();
        var emptyData = new Dictionary<string, object>();


        Assert.Throws<ArgumentException>(() => reporter.WithMemoryData(emptyData));
    }

    [Fact]
    public void CustomMemoryReporter_WithNullData_ThrowsArgumentNullException()
    {

        var reporter = new CustomMemoryReporter();


        Assert.Throws<ArgumentNullException>(() => reporter.WithMemoryData(null!));
    }

    #endregion

    #region Custom Process Reporter Tests

    [Fact]
    public void CustomProcessReporter_SetsPropertiesCorrectly()
    {

        var reporter = new CustomProcessReporter()
            .WithProcessName("notepad")
            .WithIncludeChildProcesses(true)
            .WithIncludeThreads(false)
            .WithResultsFormat(ResultsFormat.Csv)
            .WithFileNamePrefix("ProcessInfo");


        Assert.Equal("notepad", reporter.ProcessName);
        Assert.True(reporter.IncludeChildProcesses);
        Assert.False(reporter.IncludeThreads);
        Assert.Equal(ResultsFormat.Csv, reporter.ResultsFormat);
        Assert.Equal("ProcessInfo", reporter.FileNamePrefix);
    }

    [Theory]
    [InlineData(data: null)]
    [InlineData(data: "")]
    [InlineData(data: "   ")]
    public void CustomProcessReporter_WithInvalidProcessName_ThrowsArgumentException(string? processName)
    {

        var reporter = new CustomProcessReporter();


        Assert.Throws<ArgumentException>(testCode: () => reporter.WithProcessName(processName: processName));
    }

    #endregion

    #region Custom Network Reporter Tests

    [Fact]
    public void CustomNetworkReporter_SetsPropertiesCorrectly()
    {

        var reporter = new CustomNetworkReporter()
            .WithHostname("example.com")
            .WithPort(443)
            .WithTimeout(5000)
            .WithIncludePingTest(true)
            .WithResultsFormat(ResultsFormat.Html)
            .WithFileNamePrefix("NetworkCheck");


        Assert.Equal("example.com", reporter.Hostname);
        Assert.Equal(443, reporter.Port);
        Assert.Equal(5000, reporter.Timeout);
        Assert.True(reporter.IncludePingTest);
        Assert.Equal(ResultsFormat.Html, reporter.ResultsFormat);
        Assert.Equal("NetworkCheck", reporter.FileNamePrefix);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    public void CustomNetworkReporter_WithInvalidPort_ThrowsArgumentOutOfRangeException(int port)
    {

        var reporter = new CustomNetworkReporter();


        Assert.Throws<ArgumentOutOfRangeException>(() => reporter.WithPort(port));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CustomNetworkReporter_WithInvalidTimeout_ThrowsArgumentOutOfRangeException(int timeout)
    {

        var reporter = new CustomNetworkReporter();


        Assert.Throws<ArgumentOutOfRangeException>(() => reporter.WithTimeout(timeout));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void CustomReporters_ImplementIReporter()
    {

        Assert.True(typeof(IReporter).IsAssignableFrom(typeof(CustomFileReporter)));
        Assert.True(typeof(IReporter).IsAssignableFrom(typeof(CustomMemoryReporter)));
        Assert.True(typeof(IReporter).IsAssignableFrom(typeof(CustomProcessReporter)));
        Assert.True(typeof(IReporter).IsAssignableFrom(typeof(CustomNetworkReporter)));
    }

    [Fact]
    public void CustomReporters_InheritFromBaseReporter()
    {

        Assert.True(typeof(BaseReporter<CustomFileReporter>).IsAssignableFrom(typeof(CustomFileReporter)));
        Assert.True(typeof(BaseReporter<CustomMemoryReporter>).IsAssignableFrom(typeof(CustomMemoryReporter)));
        Assert.True(typeof(BaseReporter<CustomProcessReporter>).IsAssignableFrom(typeof(CustomProcessReporter)));
        Assert.True(typeof(BaseReporter<CustomNetworkReporter>).IsAssignableFrom(typeof(CustomNetworkReporter)));
    }

    #endregion

    #region Test Helper Classes - Custom Reporter Implementations

    public interface ICustomFileReporter : IReporter
    {
        string DirectoryPath { get; set; }
        string SearchPattern { get; set; }
        bool Recursive { get; set; }
    }

    public class CustomFileReporter : BaseReporter<CustomFileReporter>, ICustomFileReporter
    {
        public string DirectoryPath { get; set; } = string.Empty;
        public string SearchPattern { get; set; } = "*.*";
        public bool Recursive { get; set; } = false;

        public CustomFileReporter WithDirectoryPath(string directoryPath)
        {
            DirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
            return this;
        }

        public CustomFileReporter WithSearchPattern(string searchPattern)
        {
            SearchPattern = searchPattern ?? throw new ArgumentNullException(nameof(searchPattern));
            return this;
        }

        public CustomFileReporter WithRecursive(bool recursive)
        {
            Recursive = recursive;
            return this;
        }
    }

    public interface ICustomMemoryReporter : IReporter
    {
        Dictionary<string, object> MemoryData { get; set; }
        bool IncludeGCInfo { get; set; }
    }

    public class CustomMemoryReporter : BaseReporter<CustomMemoryReporter>, ICustomMemoryReporter
    {
        public Dictionary<string, object> MemoryData { get; set; } = new();
        public bool IncludeGCInfo { get; set; } = false;

        public CustomMemoryReporter WithMemoryData(Dictionary<string, object> memoryData)
        {
            ArgumentNullException.ThrowIfNull(memoryData);
            if (memoryData.Count == 0)
                throw new ArgumentException("Memory data cannot be empty", nameof(memoryData));

            MemoryData = memoryData;
            return this;
        }

        public CustomMemoryReporter WithIncludeGCInfo(bool includeGCInfo)
        {
            IncludeGCInfo = includeGCInfo;
            return this;
        }
    }

    public interface ICustomProcessReporter : IReporter
    {
        string ProcessName { get; set; }
        bool IncludeChildProcesses { get; set; }
        bool IncludeThreads { get; set; }
    }

    public class CustomProcessReporter : BaseReporter<CustomProcessReporter>, ICustomProcessReporter
    {
        public string ProcessName { get; set; } = string.Empty;
        public bool IncludeChildProcesses { get; set; } = false;
        public bool IncludeThreads { get; set; } = true;

        public CustomProcessReporter WithProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

            ProcessName = processName;
            return this;
        }

        public CustomProcessReporter WithIncludeChildProcesses(bool includeChildProcesses)
        {
            IncludeChildProcesses = includeChildProcesses;
            return this;
        }

        public CustomProcessReporter WithIncludeThreads(bool includeThreads)
        {
            IncludeThreads = includeThreads;
            return this;
        }
    }

    public interface ICustomNetworkReporter : IReporter
    {
        string Hostname { get; set; }
        int Port { get; set; }
        int Timeout { get; set; }
        bool IncludePingTest { get; set; }
    }

    public class CustomNetworkReporter : BaseReporter<CustomNetworkReporter>, ICustomNetworkReporter
    {
        public string Hostname { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public int Timeout { get; set; } = 3000;
        public bool IncludePingTest { get; set; } = false;

        public CustomNetworkReporter WithHostname(string hostname)
        {
            Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
            return this;
        }

        public CustomNetworkReporter WithPort(int port)
        {
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");

            Port = port;
            return this;
        }

        public CustomNetworkReporter WithTimeout(int timeout)
        {
            if (timeout < 1)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than 0");

            Timeout = timeout;
            return this;
        }

        public CustomNetworkReporter WithIncludePingTest(bool includePingTest)
        {
            IncludePingTest = includePingTest;
            return this;
        }
    }

    #endregion
}
