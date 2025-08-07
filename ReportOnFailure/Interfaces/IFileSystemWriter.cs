namespace ReportOnFailure.Interfaces;

public interface IFileSystemWriter : IWriter
{
    string BasePath { get; }
    bool CompressResults { get; }
}