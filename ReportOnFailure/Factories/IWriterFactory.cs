namespace ReportOnFailure.Factories;

using Enums;
using ReportOnFailure.Interfaces.Writers;

public interface IWriterFactory
{
    IWriter CreateWriter(DestinationType destinationType, string destinationLocation, bool compressResults = false);
}