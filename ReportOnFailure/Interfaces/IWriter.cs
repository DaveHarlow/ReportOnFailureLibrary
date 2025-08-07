namespace ReportOnFailure.Interfaces;

public interface IWriter
{
    Task WriteAsync(string content, string fileName, CancellationToken cancellationToken = default);
    void Write(string content, string fileName);
}