namespace ReportOnFailure.Tests.Registry;

using ReportOnFailure;
using ReportOnFailure.Interfaces;
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
}