using Moq;

namespace ReportOnFailure.Tests;

public class RegistryTests
{
    [Fact]
    public void RegistryShouldAllowReporterRegistration()
    {
        var registry = new ReportOnFailure.Registry();
        var reporter = new Mock<ReportOnFailure.Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters);
        Assert.Contains(reporter.Object, registry.reporters);
    }

    [Fact]
    public void RegistryShouldNotAllowNullReporterRegistration()
    {
        var registry = new ReportOnFailure.Registry();
        Assert.Throws<ArgumentNullException>(() => registry.RegisterReporter(null));
        Assert.Empty(registry.reporters);
    }

    [Fact]
    public void RegistryShouldAllowReporterUnregistration()
    {
        var registry = new ReportOnFailure.Registry();
        var reporter = new Mock<ReportOnFailure.Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters);

        registry.UnRegisterReporter(reporter.Object);
        Assert.Empty(registry.reporters);
    }

    [Fact]
    public void RegistryConstructorCreatesEmptyReporterList()
    {
        var registry = new ReportOnFailure.Registry();
        Assert.NotNull(registry.reporters);
        Assert.Empty(registry.reporters);
    }

    [Fact]
    public void RegistryShouldNotAllowDuplicateReporterRegistration()
    {
        var registry = new ReportOnFailure.Registry();
        var reporter = new Mock<ReportOnFailure.Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters);

        // Attempt to register the same reporter again
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters); // Should still be one
    }

    [Fact]
    public void GetAllReportsShouldMakeEachReporterReport()
    {
        var registry = new ReportOnFailure.Registry();
        var reporter1 = new Mock<ReportOnFailure.Interfaces.IReporter>();
        var reporter2 = new Mock<ReportOnFailure.Interfaces.IReporter>();
        registry.RegisterReporter(reporter1.Object);
        registry.RegisterReporter(reporter2.Object);
        reporter1.Setup(r => r.OutPutData()).Returns("Report 1");
        reporter2.Setup(r => r.OutPutData()).Returns("Report 2");
        var reports = registry.GetAllReports();
        Assert.Equal(2, reports.Count);
        Assert.Contains("Report 1", reports);
        Assert.Contains("Report 2", reports);
    }
}


