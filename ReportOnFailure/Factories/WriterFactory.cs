namespace ReportOnFailure.Factories;

using Enums;
using ReportOnFailure.Interfaces.Writers;
using System;
using Writers;

public class WriterFactory : IWriterFactory
{
    public IWriter CreateWriter(DestinationType destinationType, string destinationLocation, bool compressResults = false)
    {
        return destinationType switch
        {
            DestinationType.FileSystem => new FileSystemWriter(destinationLocation, compressResults),
            DestinationType.AzureBlobStorage => throw new NotImplementedException("Azure Blob Storage writer not implemented yet."),
            DestinationType.AmazonS3 => throw new NotImplementedException("Amazon S3 writer not implemented yet."),
            DestinationType.GoogleCloudStorage => throw new NotImplementedException("Google Cloud Storage writer not implemented yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(destinationType), $"Unknown destination type: {destinationType}")
        };
    }
}