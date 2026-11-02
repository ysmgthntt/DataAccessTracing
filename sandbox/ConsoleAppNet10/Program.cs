using System.Data.Common;
using DataAccessTracing;
using Microsoft.Data.SqlClient;

Console.WriteLine("Hello, World!");


const string providerName = "Microsoft.Data.SqlClient";
const string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=DataAccessTracingTestDB;Integrated Security=true";
const string dummyName = nameof(DummyFactory);

// Type を渡すとインスタンス化されてしまう。
DbProviderFactories.RegisterFactory(providerName, typeof(SqlClientFactory));
DbProviderFactories.RegisterFactory(dummyName, typeof(DummyFactory));

DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();

var factory = DbProviderFactories.GetFactory(providerName);
Console.WriteLine(factory.ToString());

var dummyFactory = DbProviderFactories.GetFactory(dummyName);
Console.WriteLine(dummyFactory.ToString());


using var connection = factory.CreateConnection()!;
connection.ConnectionString = connectionString;
connection.Open();
using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM TestTable";
using var da = factory.CreateDataAdapter()!;
da.SelectCommand = command;
using var commandBuilder = factory.CreateCommandBuilder()!;
commandBuilder.DataAdapter = da;
using var updateCommand = commandBuilder.GetUpdateCommand();

Console.WriteLine(updateCommand.CommandText);


class DummyFactory : DbProviderFactory
{
    public static readonly DummyFactory Instance = new();

    public DummyFactory()
    {
        Console.WriteLine("DummyFactory.ctor");
    }
}
