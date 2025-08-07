namespace ReportOnFailure.Writers.Strategies;

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Interfaces;

public class ZipFileStrategy : ICompressionStrategy
{
    private readonly object _lockObject = new();

    public async Task WriteAsync(string basePath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        var zipFileName = Path.ChangeExtension(fileName, ".zip");
        var zipPath = Path.Combine(basePath, zipFileName);
        var directory = Path.GetDirectoryName(zipPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var contentBytes = Encoding.UTF8.GetBytes(content);

        await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await entryStream.WriteAsync(contentBytes, cancellationToken);
    }

    public void Write(string basePath, string fileName, string content)
    {
        var zipFileName = Path.ChangeExtension(fileName, ".zip");
        var zipPath = Path.Combine(basePath, zipFileName);
        var directory = Path.GetDirectoryName(zipPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        lock (_lockObject)
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);

            using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(contentBytes);
        }
    }
}