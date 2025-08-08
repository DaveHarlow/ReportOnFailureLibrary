namespace ReportOnFailure.Writers.Strategies;

using Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class UncompressedFileStrategy : ICompressionStrategy
{
    public async Task WriteAsync(string basePath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(basePath, fileName);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
    }

    public void Write(string basePath, string fileName, string content)
    {
        var fullPath = Path.Combine(basePath, fileName);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }
}