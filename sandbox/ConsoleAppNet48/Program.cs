using DataAccessTracing;
using System.Configuration;
using System.Data.Common;

Console.WriteLine("Hello, World!");


DataAccessTrace.ReplaceDbProviderFactoriesToTracingProvider();
DataAccessTrace.ReplaceDbProviderFactoriesToTracingProvider();

var connectionSettings = ConfigurationManager.ConnectionStrings["DefaultDatabase"];
var factory = DbProviderFactories.GetFactory(connectionSettings.ProviderName);

Console.WriteLine(factory.GetType().FullName);
