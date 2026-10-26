using DataAccessTracing;
using Microsoft.Data.SqlClient;
using System.Data.Common;

Console.WriteLine("Hello, World!");


const string providerName = "Microsoft.Data.SqlClient";
DbProviderFactories.RegisterFactory(providerName, typeof(SqlClientFactory));

DataAccessTrace.ReplaceDbProviderFactoriesToTracingProvider();
DataAccessTrace.ReplaceDbProviderFactoriesToTracingProvider();

var factory = DbProviderFactories.GetFactory(providerName);
Console.WriteLine(factory.GetType().FullName);
