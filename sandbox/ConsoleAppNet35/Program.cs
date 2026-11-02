using DataAccessTracing;
using System.Configuration;
using System.Data.Common;

Console.WriteLine("Hello, World!");


DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();

var connectionSettings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
var factory = DbProviderFactories.GetFactory(connectionSettings.ProviderName);
Console.WriteLine(factory.GetType().FullName);

var dummyFactory = DbProviderFactories.GetFactory("DummyFactory");
Console.WriteLine(dummyFactory.GetType().FullName);


using var connection = factory.CreateConnection();
connection.ConnectionString = connectionSettings.ConnectionString;
connection.Open();
using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM TestTable";
using var da = factory.CreateDataAdapter();
da.SelectCommand = command;
using var commandBuilder = factory.CreateCommandBuilder();
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
