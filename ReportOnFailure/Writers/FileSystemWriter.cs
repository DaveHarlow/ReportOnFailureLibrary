namespace ReportOnFailure.Writers;

using Interfaces;
using Strategies;
using System;
using System.Threading;
using System.Threading.Tasks;

public class FileSystemWriter : IFileSystemWriter
{
    private readonly ICompressionStrategy _compressionStrategy;

    public FileSystemWriter(string basePath, bool compressResults = false)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be null or empty.", nameof(basePath));

        BasePath = basePath;
        CompressResults = compressResults;
        _compressionStrategy = compressResults
            ? new ZipFileStrategy()
            : new UncompressedFileStrategy();
    }

    public string BasePath { get; }
    public bool CompressResults { get; }

    public async Task WriteAsync(string content, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        await _compressionStrategy.WriteAsync(BasePath, fileName, content, cancellationToken);
    }

    public void Write(string content, string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        _compressionStrategy.Write(BasePath, fileName, content);
    }
}