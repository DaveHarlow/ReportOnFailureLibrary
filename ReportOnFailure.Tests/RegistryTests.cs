using Moq;
using System.IO.Compression;


namespace ReportOnFailure.Tests;

public class RegistryTests
{
    [Fact]
    public void RegistryShouldAllowReporterRegistration()
    {
        var registry = new Registry();
        var reporter = new Mock<Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters);
        Assert.Contains(reporter.Object, registry.reporters);
    }

    [Fact]
    public void RegistryShouldNotAllowNullReporterRegistration()
    {
        var registry = new Registry();
        Assert.Throws<ArgumentNullException>(() => registry.RegisterReporter(null));
        Assert.Empty(registry.reporters);
    }

    [Fact]
    public void RegistryShouldAllowReporterUnregistration()
    {
        var registry = new Registry();
        var reporter = new Mock<Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters);

        registry.UnRegisterReporter(reporter.Object);
        Assert.Empty(registry.reporters);
    }

    [Fact]
    public void RegistryConstructorCreatesEmptyReporterList()
    {
        var registry = new Registry();
        Assert.NotNull(registry.reporters);
        Assert.Empty(registry.reporters);
    }

    [Fact]
    public void RegistryShouldNotAllowDuplicateReporterRegistration()
    {
        var registry = new Registry();
        var reporter = new Mock<Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters);

        // Attempt to register the same reporter again
        registry.RegisterReporter(reporter.Object);
        Assert.Single(registry.reporters); // Should still be one
    }

    [Fact]
    public void GetAllReportsShouldMakeEachReporterReport()
    {
        var registry = new Registry();
        var reporter1 = new Mock<Interfaces.IReporter>();
        var reporter2 = new Mock<Interfaces.IReporter>();
        registry.RegisterReporter(reporter1.Object);
        registry.RegisterReporter(reporter2.Object);
        reporter1.Setup(r => r.OutPutData()).Returns("Report 1");
        reporter2.Setup(r => r.OutPutData()).Returns("Report 2");
        var reports = registry.GetAllReports();
        Assert.Equal(2, reports.Count);
        Assert.Contains("Report 1", reports);
        Assert.Contains("Report 2", reports);
    }

    [Fact]
    public void SaveAllReportsShouldWriteReportsToFile()
    {
        var registry = new Registry();
        var reporter = new Mock<Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        reporter.Setup(r => r.OutPutData()).Returns("Report Content");
        var filePath = Path.GetTempFileName();
        registry.SaveAllReports(filePath);
        var savedReports = File.ReadAllLines(filePath);
        Assert.Single(savedReports);
        Assert.Equal("Report Content", savedReports[0]);
        File.Delete(filePath); // Clean up
    }

    [Fact]
    public void SaveAllReportsAsZipShouldCreateZipWithReports()
    {
        var registry = new Registry();
        var reporter = new Mock<Interfaces.IReporter>();
        registry.RegisterReporter(reporter.Object);
        reporter.Setup(r => r.OutPutData()).Returns("Report Content");
        var zipFilePath = Path.GetTempFileName() + ".zip";
        registry.SaveAllReportsAsZip(zipFilePath);
        
        using (var zip = ZipFile.OpenRead(zipFilePath))
        {
            Assert.Single(zip.Entries);
            using (var entryStream = zip.Entries.First().Open())
            using (var reader = new StreamReader(entryStream))
            {
                var content = reader.ReadToEnd();
                Assert.Equal("Report Content", content);
            }
        }
        
        File.Delete(zipFilePath); // Clean up
    }
}


