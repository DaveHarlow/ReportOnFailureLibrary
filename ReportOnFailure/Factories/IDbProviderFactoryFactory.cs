namespace ReportOnFailure.Factories;

using ReportOnFailure.Enums;
using System.Data.Common;

public interface IDbProviderFactoryFactory
{
    DbProviderFactory GetFactory(DatabaseType dbType);
}