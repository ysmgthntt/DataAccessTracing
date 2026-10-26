using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace DataAccessTracing
{
    public static class DataAccessTrace
    {
        public static void ReplaceDbProviderFactoriesToTracingProvider()
        {
#if NETFRAMEWORK
            // Force Creation
            _ = DbProviderFactories.GetFactoryClasses();

            DataTable providerTable;
            // for .NET 4.x
            var fi = typeof(DbProviderFactories).GetField("_providerTable", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi is not null)
            {
                providerTable = (DataTable)fi.GetValue(null);
            }
            else
            {
                // for .NET 2.x, 3.x
                fi = typeof(DbProviderFactories).GetField("_configTable", BindingFlags.NonPublic | BindingFlags.Static);
                if (fi is not null)
                {
                    providerTable = ((DataSet)fi.GetValue(null)).Tables[nameof(DbProviderFactories)];
                }
                else
                {
                    Trace.WriteLine($"{nameof(DataAccessTracing)}: Provider table not found.");
                    return;
                }
            }

            const string typeColumnName = nameof(Type.AssemblyQualifiedName);
            providerTable.Columns[typeColumnName].ReadOnly = false;
            try
            {
                foreach (DataRow row in providerTable.Rows)
                {
                    DbProviderFactory factory;
                    try
                    {
                        factory = DbProviderFactories.GetFactory(row);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{nameof(DataAccessTracing)}: DbProviderFactories.GetFactory error - {row["Name"]} {ex.Message}");
                        continue;
                    }

                    if (factory is ITracingProviderFactory)
                    {
                        Trace.WriteLine($"{nameof(DataAccessTracing)}: Already replaced - {row["Name"]}");
                        continue;
                    }

                    var proxyType = typeof(TracingProviderFactory<>).MakeGenericType(factory.GetType());
                    row[typeColumnName] = proxyType.AssemblyQualifiedName;
                }
            }
            finally
            {
                providerTable.Columns[typeColumnName].ReadOnly = true;
            }
#else
            foreach (var providerName in DbProviderFactories.GetProviderInvariantNames())
            {
                if (!DbProviderFactories.TryGetFactory(providerName, out var factory))
                    continue;

                if (factory is ITracingProviderFactory)
                {
                    Trace.WriteLine($"{nameof(DataAccessTracing)}: Already replaced - {providerName}");
                    continue;
                }

                var proxyType = typeof(TracingProviderFactory<>).MakeGenericType(factory.GetType());

                DbProviderFactories.UnregisterFactory(providerName);
                DbProviderFactories.RegisterFactory(providerName, proxyType);
            }
#endif
        }
    }
}
