namespace ReportOnFailure.Tests.Registry;

using ReportOnFailure;
using ReportOnFailure.Interfaces;
using ReportOnFailure.Enums;
using Moq;

public class RegistryTests
{
    [Fact]
    public void CanRegisterReporter()
    {
        var registry = new Registry();
        var reporter = new Mock<IReporter>().Object;

        registry.RegisterReporter(reporter);

        Assert.Contains(reporter, registry.Reporters);
    }

    [Fact]
    public void CanUnregisterReporter()
    {
        var registry = new Registry();
        var reporter = new Mock<IReporter>().Object;

        registry.RegisterReporter(reporter);
        registry.UnRegisterReporter(reporter);

        Assert.DoesNotContain(reporter, registry.Reporters);
    }

    [Fact]
    public void CanSetExecutionMode()
    {
        var registry = new Registry();
        registry.WithExecutionMode(ExecutionMode.Asynchronous);

        Assert.Equal(ExecutionMode.Asynchronous, registry.ExecutionMode);
    }

    [Fact]
    public void CanSetDestinationType()
    {
        var registry = new Registry();
        registry.WithDestinationType(DestinationType.AzureBlobStorage);

        Assert.Equal(DestinationType.AzureBlobStorage, registry.DestinationType);
    }

    [Fact]
    public void CanSetDestinationLocation()
    {
        var registry = new Registry();
        const string location = "https://example.com/storage";

        registry.WithDestinationLocation(location);

        Assert.Equal(location, registry.DestinationLocation);
    }

    [Fact]
    public void CanEnableCompression()
    {
        var registry = new Registry();
        registry.WithCompression();

        Assert.True(registry.CompressResults);


    }
}