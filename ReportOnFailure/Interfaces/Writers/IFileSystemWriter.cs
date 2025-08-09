namespace ReportOnFailure.Interfaces.Writers;

public interface IFileSystemWriter : IWriter
{
    string BasePath { get; }
    bool CompressResults { get; }
}