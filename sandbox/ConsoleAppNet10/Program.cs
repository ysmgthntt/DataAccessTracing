using DataAccessTracing;
using Microsoft.Data.SqlClient;
using System.Data.Common;

Console.WriteLine("Hello, World!");


const string providerName = "Microsoft.Data.SqlClient";
const string dummyName = nameof(DummyFactory);

// Type を渡すとインスタンス化されてしまう。
DbProviderFactories.RegisterFactory(providerName, typeof(SqlClientFactory));
DbProviderFactories.RegisterFactory(dummyName, typeof(DummyFactory));

DataAccessTrace.ReplaceDbProviderFactoriesToTracingProvider();
DataAccessTrace.ReplaceDbProviderFactoriesToTracingProvider();

var factory = DbProviderFactories.GetFactory(providerName);
Console.WriteLine(factory.GetType().FullName);

var dummyFactory = DbProviderFactories.GetFactory(dummyName);
Console.WriteLine(dummyFactory.GetType().FullName);


class DummyFactory : DbProviderFactory
{
    public static readonly DummyFactory Instance = new();

    public DummyFactory()
    {
        Console.WriteLine("DummyFactory.ctor");
    }
}
