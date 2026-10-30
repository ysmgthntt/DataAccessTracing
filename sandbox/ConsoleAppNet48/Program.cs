using DataAccessTracing;
using System.Configuration;
using System.Data.Common;

Console.WriteLine("Hello, World!");


DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();

var connectionSettings = ConfigurationManager.ConnectionStrings["DefaultDatabase"];
var factory = DbProviderFactories.GetFactory(connectionSettings.ProviderName);
Console.WriteLine(factory.GetType().FullName);

var dummyFactory = DbProviderFactories.GetFactory("DummyFactory");
Console.WriteLine(dummyFactory.GetType().FullName);


class DummyFactory : DbProviderFactory
{
    public static readonly DummyFactory Instance = new();

    public DummyFactory()
    {
        Console.WriteLine("DummyFactory.ctor");
    }
}
