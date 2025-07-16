namespace ReportOnFailure.Tests.FactoryTests;

using ReportOnFailure.Factories;
using ReportOnFailure.Interfaces;
using ReportOnFailure.Resolvers;
using Moq;

public class DbResolverFactoryTests
{
    [Fact]
    public void Create_ReturnsDbResolver()
    {
        var formatterFactoryMock = new Mock<IResultFormatterFactory>();
        var dbProviderFactoryFactoryMock = new Mock<IDbProviderFactoryFactory>();

        var factory = new DbResolverFactory(formatterFactoryMock.Object, dbProviderFactoryFactoryMock.Object);
        var resolver = factory.Create();

        Assert.NotNull(resolver);
        Assert.IsType<DbResolver>(resolver);
    }
}