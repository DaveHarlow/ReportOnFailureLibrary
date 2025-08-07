namespace ReportOnFailure.Interfaces;

public interface ICompressionStrategy
{
    Task WriteAsync(string basePath, string fileName, string content, CancellationToken cancellationToken = default);
    void Write(string basePath, string fileName, string content);
}