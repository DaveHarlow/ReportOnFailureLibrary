namespace ReportOnFailure.Factories;

using Enums;
using Interfaces;

public interface IWriterFactory
{
    IWriter CreateWriter(DestinationType destinationType, string destinationLocation, bool compressResults = false);
}