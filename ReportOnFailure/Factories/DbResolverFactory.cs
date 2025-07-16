using ReportOnFailure.Interfaces;
using ReportOnFailure.Resolvers;

namespace ReportOnFailure.Factories;

public class DbResolverFactory
{
    private readonly IResultFormatterFactory _formatterFactory;
    private readonly IDbProviderFactoryFactory _dbProviderFactoryFactory;

    public DbResolverFactory(IResultFormatterFactory formatterFactory, IDbProviderFactoryFactory dbProviderFactoryFactory)
    {
        _formatterFactory = formatterFactory;
        _dbProviderFactoryFactory = dbProviderFactoryFactory;
    }

    public IDbResolver Create() => new DbResolver(_formatterFactory, _dbProviderFactoryFactory);
}