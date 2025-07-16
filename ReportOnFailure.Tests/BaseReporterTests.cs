namespace ReportOnFailure.Tests;

using ReportOnFailure.Reporters;
using ReportOnFailure.Enums;

public class BaseReporterTests
{
    [Fact]
    public void TestWithResultsFormat()
    {
        // Arrange
        var reporter = new BaseReporter().WithResultsFormat(ResultsFormat.Json);

        // Assert
        Assert.Equal(ResultsFormat.Json, reporter.ResultsFormat);
    }


}