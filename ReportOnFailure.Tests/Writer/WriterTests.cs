using ReportOnFailure.Enums;
using ReportOnFailure.Factories;
using ReportOnFailure.Writers;
using ReportOnFailure.Writers.Strategies;
using System.IO.Compression;
using System.Text;

namespace ReportOnFailure.Tests.Writer;

public class WriterTests : IDisposable
{
    private readonly string _testDirectory;

    public WriterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ReportOnFailureTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region FileSystemWriter Tests

    [Fact]
    public void FileSystemWriter_Constructor_ThrowsArgumentException_WhenBasePathIsNull()
    {
        Assert.Throws<ArgumentException>(() => new FileSystemWriter(null!));
    }

    [Fact]
    public void FileSystemWriter_Constructor_ThrowsArgumentException_WhenBasePathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new FileSystemWriter(string.Empty));
    }

    [Fact]
    public void FileSystemWriter_Constructor_ThrowsArgumentException_WhenBasePathIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new FileSystemWriter("   "));
    }

    [Fact]
    public void FileSystemWriter_Constructor_SetsProperties_WithoutCompression()
    {
        var basePath = "/test/path";
        var writer = new FileSystemWriter(basePath, compressResults: false);

        Assert.Equal(basePath, writer.BasePath);
        Assert.False(writer.CompressResults);
    }

    [Fact]
    public void FileSystemWriter_Constructor_SetsProperties_WithCompression()
    {
        var basePath = "/test/path";
        var writer = new FileSystemWriter(basePath, compressResults: true);

        Assert.Equal(basePath, writer.BasePath);
        Assert.True(writer.CompressResults);
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenContentIsNull()
    {
        var writer = new FileSystemWriter(_testDirectory);

        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, "test.txt"));
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenContentIsEmpty()
    {
        var writer = new FileSystemWriter(_testDirectory);

        Assert.Throws<ArgumentException>(() => writer.Write(string.Empty, "test.txt"));
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenFileNameIsNull()
    {
        var writer = new FileSystemWriter(_testDirectory);

        Assert.Throws<ArgumentNullException>(() => writer.Write("content", null!));
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenFileNameIsEmpty()
    {
        var writer = new FileSystemWriter(_testDirectory);

        Assert.Throws<ArgumentException>(() => writer.Write("content", string.Empty));
    }

    [Fact]
    public async Task WriteAsync_ThrowsArgumentException_WhenContentIsNull()
    {
        var writer = new FileSystemWriter(_testDirectory);

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteAsync(null!, "test.txt"));
    }

    [Fact]
    public async Task WriteAsync_ThrowsArgumentException_WhenContentIsEmpty()
    {
        var writer = new FileSystemWriter(_testDirectory);

        await Assert.ThrowsAsync<ArgumentException>(() => writer.WriteAsync(string.Empty, "test.txt"));
    }

    [Fact]
    public async Task WriteAsync_ThrowsArgumentException_WhenFileNameIsNull()
    {
        var writer = new FileSystemWriter(_testDirectory);

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteAsync("content", null!));
    }

    [Fact]
    public async Task WriteAsync_ThrowsArgumentException_WhenFileNameIsEmpty()
    {
        var writer = new FileSystemWriter(_testDirectory);

        await Assert.ThrowsAsync<ArgumentException>(() => writer.WriteAsync("content", string.Empty));
    }

    [Fact]
    public void Write_CreatesUncompressedFile_WhenCompressionDisabled()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: false);
        const string content = "Test content";
        const string fileName = "test.txt";

        writer.Write(content, fileName);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));

        var writtenContent = File.ReadAllText(filePath);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public async Task WriteAsync_CreatesUncompressedFile_WhenCompressionDisabled()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: false);
        const string content = "Test content async";
        const string fileName = "test_async.txt";

        await writer.WriteAsync(content, fileName);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));

        var writtenContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public void Write_CreatesCompressedFile_WhenCompressionEnabled()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: true);
        const string content = "Test compressed content";
        const string fileName = "test.txt";

        writer.Write(content, fileName);

        var zipPath = Path.Combine(_testDirectory, "test.zip");
        Assert.True(File.Exists(zipPath));
        Assert.False(File.Exists(Path.Combine(_testDirectory, fileName)));


        using var archive = ZipFile.OpenRead(zipPath);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(fileName, entry.Name);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var extractedContent = reader.ReadToEnd();
        Assert.Equal(content, extractedContent);
    }

    [Fact]
    public async Task WriteAsync_CreatesCompressedFile_WhenCompressionEnabled()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: true);
        const string content = "Test compressed content async";
        const string fileName = "test_async.txt";

        await writer.WriteAsync(content, fileName);

        var zipPath = Path.Combine(_testDirectory, "test_async.zip");
        Assert.True(File.Exists(zipPath));


        using var archive = ZipFile.OpenRead(zipPath);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(fileName, entry.Name);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var extractedContent = reader.ReadToEnd();
        Assert.Equal(content, extractedContent);
    }

    [Fact]
    public void Write_CreatesDirectoryStructure_WhenFileNameContainsSubdirectories()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: false);
        const string content = "Test content with subdirs";
        const string fileName = "subdir1/subdir2/test.txt";

        writer.Write(content, fileName);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));

        var writtenContent = File.ReadAllText(filePath);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public async Task WriteAsync_CreatesDirectoryStructure_WhenFileNameContainsSubdirectories()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: false);
        const string content = "Test content with subdirs async";
        const string fileName = "async_subdir1/async_subdir2/test_async.txt";

        await writer.WriteAsync(content, fileName);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));

        var writtenContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public async Task WriteAsync_SupportsCancellation()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            writer.WriteAsync("content", "test.txt", cts.Token));
    }

    #endregion

    #region UncompressedFileStrategy Tests

    [Fact]
    public void UncompressedFileStrategy_Write_CreatesFile()
    {
        var strategy = new UncompressedFileStrategy();
        const string content = "Test strategy content";
        const string fileName = "strategy_test.txt";

        strategy.Write(_testDirectory, fileName, content);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));

        var writtenContent = File.ReadAllText(filePath);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public async Task UncompressedFileStrategy_WriteAsync_CreatesFile()
    {
        var strategy = new UncompressedFileStrategy();
        const string content = "Test strategy content async";
        const string fileName = "strategy_test_async.txt";

        await strategy.WriteAsync(_testDirectory, fileName, content);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));

        var writtenContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, writtenContent);
    }

    [Fact]
    public void UncompressedFileStrategy_Write_CreatesDirectoryStructure()
    {
        var strategy = new UncompressedFileStrategy();
        const string content = "Test content";
        const string fileName = "strategy_sub1/strategy_sub2/test.txt";

        strategy.Write(_testDirectory, fileName, content);

        var filePath = Path.Combine(_testDirectory, fileName);
        Assert.True(File.Exists(filePath));
        Assert.Equal(content, File.ReadAllText(filePath));
    }

    #endregion

    #region ZipFileStrategy Tests

    [Fact]
    public void ZipFileStrategy_Write_CreatesZipFile()
    {
        var strategy = new ZipFileStrategy();
        const string content = "Test zip strategy content";
        const string fileName = "zip_strategy_test.txt";

        strategy.Write(_testDirectory, fileName, content);

        var zipPath = Path.Combine(_testDirectory, "zip_strategy_test.zip");
        Assert.True(File.Exists(zipPath));


        using var archive = ZipFile.OpenRead(zipPath);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(fileName, entry.Name);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var extractedContent = reader.ReadToEnd();
        Assert.Equal(content, extractedContent);
    }

    [Fact]
    public async Task ZipFileStrategy_WriteAsync_CreatesZipFile()
    {
        var strategy = new ZipFileStrategy();
        const string content = "Test zip strategy content async";
        const string fileName = "zip_strategy_test_async.txt";

        await strategy.WriteAsync(_testDirectory, fileName, content);

        var zipPath = Path.Combine(_testDirectory, "zip_strategy_test_async.zip");
        Assert.True(File.Exists(zipPath));


        using var archive = ZipFile.OpenRead(zipPath);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(fileName, entry.Name);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var extractedContent = reader.ReadToEnd();
        Assert.Equal(content, extractedContent);
    }

    [Fact]
    public void ZipFileStrategy_Write_CreatesDirectoryStructure()
    {
        var strategy = new ZipFileStrategy();
        const string content = "Test zip content with subdirs";
        const string fileName = "zip_sub1/zip_sub2/test.txt";

        strategy.Write(_testDirectory, fileName, content);

        var zipPath = Path.Combine(_testDirectory, "zip_sub1", "zip_sub2", "test.zip");
        Assert.True(File.Exists(zipPath));
    }

    [Fact]
    public void ZipFileStrategy_Write_HandlesSpecialCharacters()
    {
        var strategy = new ZipFileStrategy();
        const string content = "Test content with special chars: áéíóú ñü 中文 🎉";
        const string fileName = "special_chars.txt";

        strategy.Write(_testDirectory, fileName, content);

        var zipPath = Path.Combine(_testDirectory, "special_chars.zip");
        Assert.True(File.Exists(zipPath));


        using var archive = ZipFile.OpenRead(zipPath);
        var entry = Assert.Single(archive.Entries);
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var extractedContent = reader.ReadToEnd();
        Assert.Equal(content, extractedContent);
    }

    #endregion

    #region WriterFactory Tests

    [Fact]
    public void WriterFactory_CreateWriter_ReturnsFileSystemWriter_WhenDestinationTypeIsFileSystem()
    {
        var factory = new WriterFactory();
        const string location = "/test/location";

        var writer = factory.CreateWriter(DestinationType.FileSystem, location, compressResults: false);

        Assert.IsType<FileSystemWriter>(writer);
        var fileSystemWriter = (FileSystemWriter)writer;
        Assert.Equal(location, fileSystemWriter.BasePath);
        Assert.False(fileSystemWriter.CompressResults);
    }

    [Fact]
    public void WriterFactory_CreateWriter_ReturnsCompressedFileSystemWriter_WhenCompressionEnabled()
    {
        var factory = new WriterFactory();
        const string location = "/test/location";

        var writer = factory.CreateWriter(DestinationType.FileSystem, location, compressResults: true);

        Assert.IsType<FileSystemWriter>(writer);
        var fileSystemWriter = (FileSystemWriter)writer;
        Assert.Equal(location, fileSystemWriter.BasePath);
        Assert.True(fileSystemWriter.CompressResults);
    }

    [Theory]
    [InlineData(DestinationType.AzureBlobStorage)]
    [InlineData(DestinationType.AmazonS3)]
    [InlineData(DestinationType.GoogleCloudStorage)]
    public void WriterFactory_CreateWriter_ThrowsNotImplementedException_ForUnsupportedDestinationTypes(
        DestinationType destinationType)
    {
        var factory = new WriterFactory();

        Assert.Throws<NotImplementedException>(() =>
            factory.CreateWriter(destinationType, "/test/location"));
    }

    [Fact]
    public void WriterFactory_CreateWriter_ThrowsArgumentOutOfRangeException_ForInvalidDestinationType()
    {
        var factory = new WriterFactory();
        const DestinationType invalidType = (DestinationType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factory.CreateWriter(invalidType, "/test/location"));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FileSystemWriter_WriteMultipleFiles_WithMixedCompression()
    {

        var uncompressedWriter = new FileSystemWriter(_testDirectory, compressResults: false);
        var compressedWriter = new FileSystemWriter(_testDirectory, compressResults: true);

        const string content1 = "Uncompressed content";
        const string content2 = "Compressed content";
        const string fileName1 = "uncompressed.txt";
        const string fileName2 = "compressed.txt";


        await uncompressedWriter.WriteAsync(content1, fileName1);
        await compressedWriter.WriteAsync(content2, fileName2);


        var uncompressedPath = Path.Combine(_testDirectory, fileName1);
        Assert.True(File.Exists(uncompressedPath));
        Assert.Equal(content1, await File.ReadAllTextAsync(uncompressedPath));


        var compressedPath = Path.Combine(_testDirectory, "compressed.zip");
        Assert.True(File.Exists(compressedPath));

        using var archive = ZipFile.OpenRead(compressedPath);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(fileName2, entry.Name);

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var extractedContent = reader.ReadToEnd();
        Assert.Equal(content2, extractedContent);
    }

    [Fact]
    public void FileSystemWriter_Write_OverwritesExistingFile()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: false);
        const string fileName = "overwrite_test.txt";
        const string originalContent = "Original content";
        const string newContent = "New content";


        writer.Write(originalContent, fileName);


        writer.Write(newContent, fileName);

        var filePath = Path.Combine(_testDirectory, fileName);
        var finalContent = File.ReadAllText(filePath);
        Assert.Equal(newContent, finalContent);
    }

    [Fact]
    public void FileSystemWriter_WriteLargeContent_HandlesCorrectly()
    {
        var writer = new FileSystemWriter(_testDirectory, compressResults: true);

        var largeContent = new string('A', 1024 * 1024);
        const string fileName = "large_file.txt";

        writer.Write(largeContent, fileName);

        var zipPath = Path.Combine(_testDirectory, "large_file.zip");
        Assert.True(File.Exists(zipPath));


        using var archive = ZipFile.OpenRead(zipPath);
        var entry = Assert.Single(archive.Entries);
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var extractedContent = reader.ReadToEnd();

        Assert.Equal(largeContent.Length, extractedContent.Length);
        Assert.Equal(largeContent, extractedContent);
    }

    #endregion
}
