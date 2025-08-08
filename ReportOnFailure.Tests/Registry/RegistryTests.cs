namespace ReportOnFailure.Tests.Registry;

using Moq;
using ReportOnFailure;
using ReportOnFailure.Enums;
using ReportOnFailure.Factories;
using ReportOnFailure.Interfaces;

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


    [Fact]
    public void RegisterReporter_ThrowsArgumentNullException_WhenReporterIsNull()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterReporter(null!));
    }

    [Fact]
    public void UnRegisterReporter_ThrowsArgumentNullException_WhenReporterIsNull()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentNullException>(() => registry.UnRegisterReporter(null!));
    }

    [Fact]
    public void RegisterReporter_DoesNotAddDuplicate_WhenSameReporterAddedTwice()
    {
        var registry = new Registry();
        var reporter = new Mock<IReporter>().Object;

        registry.RegisterReporter(reporter);
        registry.RegisterReporter(reporter);

        Assert.Single(registry.Reporters);
        Assert.Contains(reporter, registry.Reporters);
    }

    [Fact]
    public void WithDestinationLocation_ThrowsArgumentException_WhenLocationIsNullOrEmpty()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentException>(() => registry.WithDestinationLocation(null!));
        Assert.Throws<ArgumentException>(() => registry.WithDestinationLocation(string.Empty));
        Assert.Throws<ArgumentException>(() => registry.WithDestinationLocation("   "));
    }

    [Fact]
    public void WithDestinationLocation_ReturnsRegistry_ForFluentChaining()
    {
        var registry = new Registry();
        const string location = "/path/to/destination";

        var result = registry.WithDestinationLocation(location);

        Assert.Same(registry, result);
    }

    [Fact]
    public void WithExecutionMode_ReturnsRegistry_ForFluentChaining()
    {
        var registry = new Registry();

        var result = registry.WithExecutionMode(ExecutionMode.Asynchronous);

        Assert.Same(registry, result);
    }

    [Fact]
    public void WithDestinationType_ReturnsRegistry_ForFluentChaining()
    {
        var registry = new Registry();

        var result = registry.WithDestinationType(DestinationType.AmazonS3);

        Assert.Same(registry, result);
    }

    [Fact]
    public void WithCompression_ReturnsRegistry_ForFluentChaining()
    {
        var registry = new Registry();

        var result = registry.WithCompression();

        Assert.Same(registry, result);
    }

    [Fact]
    public void Execute_ThrowsInvalidOperationException_WhenDestinationLocationNotSet()
    {
        var registry = new Registry();
        var reporter = new Mock<IDbReporter>().Object;
        registry.RegisterReporter(reporter);

        var exception = Assert.Throws<InvalidOperationException>(() => registry.Execute());
        Assert.Equal("Destination location must be set before execution.", exception.Message);
    }

    [Fact]
    public void Execute_ThrowsInvalidOperationException_WhenNoReportersRegistered()
    {
        var registry = new Registry();
        registry.WithDestinationLocation("/test/path");

        var exception = Assert.Throws<InvalidOperationException>(() => registry.Execute());
        Assert.Equal("At least one reporter must be registered before execution.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenDestinationLocationNotSet()
    {
        var registry = new Registry();
        var reporter = new Mock<IDbReporter>().Object;
        registry.RegisterReporter(reporter);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => registry.ExecuteAsync());
        Assert.Equal("Destination location must be set before execution.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenNoReportersRegistered()
    {
        var registry = new Registry();
        registry.WithDestinationLocation("/test/path");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => registry.ExecuteAsync());
        Assert.Equal("At least one reporter must be registered before execution.", exception.Message);
    }

    [Fact]
    public void Execute_CallsWriterFactory_WithCorrectParameters()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveSync(It.IsAny<IDbReporter>()))
            .Returns("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationType(DestinationType.FileSystem)
            .WithDestinationLocation("/test/path")
            .WithCompression();

        registry.RegisterReporter(mockDbReporter.Object);


        registry.Execute();


        mockWriterFactory.Verify(f => f.CreateWriter(
            DestinationType.FileSystem,
            "/test/path",
            true), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CallsWriterFactory_WithCorrectParameters()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveAsync(It.IsAny<IDbReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);
        mockDbReporter.Setup(r => r.ExecutionModeOverride).Returns((ExecutionMode?)null);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationType(DestinationType.AmazonS3)
            .WithDestinationLocation("s3://bucket/path")
            .WithExecutionMode(ExecutionMode.Synchronous);

        registry.RegisterReporter(mockDbReporter.Object);


        await registry.ExecuteAsync();


        mockWriterFactory.Verify(f => f.CreateWriter(
            DestinationType.AmazonS3,
            "s3://bucket/path",
            false), Times.Once);
    }

    [Fact]
    public void Execute_CallsResolverSync_ForDbReporter()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveSync(It.IsAny<IDbReporter>()))
            .Returns("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path");

        registry.RegisterReporter(mockDbReporter.Object);


        registry.Execute();


        mockDbResolver.Verify(r => r.ResolveSync(mockDbReporter.Object), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CallsResolverAsync_WhenExecutionModeIsAsynchronous()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveAsync(It.IsAny<IDbReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);
        mockDbReporter.Setup(r => r.ExecutionModeOverride).Returns((ExecutionMode?)null);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path")
            .WithExecutionMode(ExecutionMode.Asynchronous);

        registry.RegisterReporter(mockDbReporter.Object);


        await registry.ExecuteAsync();


        mockDbResolver.Verify(r => r.ResolveAsync(mockDbReporter.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsReporterExecutionModeOverride()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveSync(It.IsAny<IDbReporter>()))
            .Returns("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);
        mockDbReporter.Setup(r => r.ExecutionModeOverride).Returns(ExecutionMode.Synchronous);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path")
            .WithExecutionMode(ExecutionMode.Asynchronous);

        registry.RegisterReporter(mockDbReporter.Object);


        await registry.ExecuteAsync();


        mockDbResolver.Verify(r => r.ResolveSync(mockDbReporter.Object), Times.Once);
        mockDbResolver.Verify(r => r.ResolveAsync(It.IsAny<IDbReporter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Execute_CallsWriterWrite_WithCorrectFileName()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveSync(It.IsAny<IDbReporter>()))
            .Returns("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);
        mockDbReporter.Setup(r => r.FileNamePrefix).Returns("MockReporter");


        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path");

        registry.RegisterReporter(mockDbReporter.Object);


        registry.Execute();


        mockWriter.Verify(w => w.Write(
            "test content",
            It.Is<string>(fileName =>
                fileName.StartsWith("MockReporter") &&
                fileName.EndsWith(".json"))),
            Times.Once);
    }

    [Fact]
    public void Execute_ThrowsNotSupportedException_ForUnsupportedReporterType()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockUnsupportedReporter = new Mock<IReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockUnsupportedReporter.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path");

        registry.RegisterReporter(mockUnsupportedReporter.Object);


        var exception = Assert.Throws<NotSupportedException>(() => registry.Execute());
        Assert.Contains("Reporter type IReporterProxy is not supported.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesMultipleReporters()
    {

        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter1 = new Mock<IDbReporter>();
        var mockDbReporter2 = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveAsync(mockDbReporter1.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync("content1");

        mockDbResolver
            .Setup(r => r.ResolveAsync(mockDbReporter2.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync("content2");

        mockDbReporter1.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Json);
        mockDbReporter1.Setup(r => r.ExecutionModeOverride).Returns((ExecutionMode?)null);

        mockDbReporter2.Setup(r => r.ResultsFormat).Returns(ResultsFormat.Csv);
        mockDbReporter2.Setup(r => r.ExecutionModeOverride).Returns((ExecutionMode?)null);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path")
            .WithExecutionMode(ExecutionMode.Asynchronous);

        registry.RegisterReporter(mockDbReporter1.Object);
        registry.RegisterReporter(mockDbReporter2.Object);


        await registry.ExecuteAsync();


        mockWriter.Verify(w => w.WriteAsync("content1", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockWriter.Verify(w => w.WriteAsync("content2", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ResultsFormat.Json, "json")]
    [InlineData(ResultsFormat.Csv, "csv")]
    [InlineData(ResultsFormat.Xml, "xml")]
    [InlineData(ResultsFormat.Html, "html")]
    [InlineData(ResultsFormat.Text, "txt")]
    public void GenerateFileName_CreatesCorrectExtension_ForResultsFormat(ResultsFormat format, string expectedExtension)
    {
        var mockWriterFactory = new Mock<IWriterFactory>();
        var mockWriter = new Mock<IWriter>();
        var mockDbResolver = new Mock<IDbResolver>();
        var mockDbReporter = new Mock<IDbReporter>();

        mockWriterFactory
            .Setup(f => f.CreateWriter(It.IsAny<DestinationType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockWriter.Object);

        mockDbResolver
            .Setup(r => r.ResolveSync(It.IsAny<IDbReporter>()))
            .Returns("test content");

        mockDbReporter.Setup(r => r.ResultsFormat).Returns(format);

        var registry = new Registry(mockWriterFactory.Object, mockDbResolver.Object)
            .WithDestinationLocation("/test/path");

        registry.RegisterReporter(mockDbReporter.Object);


        registry.Execute();


        mockWriter.Verify(w => w.Write(
            It.IsAny<string>(),
            It.Is<string>(fileName => fileName.EndsWith($".{expectedExtension}"))),
            Times.Once);
    }

    [Fact]
    public void Constructor_UsesDefaultImplementations_WhenDependenciesNotProvided()
    {

        var registry = new Registry();

        Assert.NotNull(registry.Reporters);
        Assert.Empty(registry.Reporters);
        Assert.Equal(DestinationType.FileSystem, registry.DestinationType);
        Assert.Equal(string.Empty, registry.DestinationLocation);
        Assert.False(registry.CompressResults);
        Assert.Equal(ExecutionMode.Synchronous, registry.ExecutionMode);
    }
}
