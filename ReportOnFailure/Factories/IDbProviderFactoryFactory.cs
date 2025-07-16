namespace ReportOnFailure.Factories;

using System.Data.Common;
using ReportOnFailure.Enums;

public interface IDbProviderFactoryFactory
{
    DbProviderFactory GetFactory(DbType dbType);
}