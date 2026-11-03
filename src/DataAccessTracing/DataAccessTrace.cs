using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    public static class DataAccessTrace
    {
        private static bool _replaced;

        public static void ReplaceDbProviderFactoriesWithTracingProviders()
        {
            // DbProviderFactories.RegisterFactory で追加できるので、 何度でも実行できるように _replaced はチェックしない。

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

            var colTypeName = providerTable.Columns[nameof(Type.AssemblyQualifiedName)];
            colTypeName.ReadOnly = false;
            try
            {
                foreach (DataRow row in providerTable.Rows)
                {
                    string factoryTypeName = (string)row[colTypeName];
                    if (factoryTypeName.StartsWith("DataAccessTracing.TracingProviderFactory", StringComparison.Ordinal))
                    {
                        Trace.WriteLine($"{nameof(DataAccessTracing)}: Already replaced - {row["Name"]}");
                        continue;
                    }

                    Type factoryType;
                    try
                    {
                        factoryType = Type.GetType(factoryTypeName);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{nameof(DataAccessTracing)}: Failed to get type for - {row["Name"]} {ex.Message}");
                        continue;
                    }

                    var proxyType = typeof(TracingProviderFactory<>).MakeGenericType(factoryType);
                    row[colTypeName] = proxyType.AssemblyQualifiedName;
                }
            }
            finally
            {
                colTypeName.ReadOnly = true;
            }
#else
            // DbProviderFactories.RegisterFactory は Type を渡すとインスタンス化される。
            // すでにインスタンス化されている可能性が高いため、そのまま取得して処理する。
            foreach (var providerName in DbProviderFactories.GetProviderInvariantNames())
            {
                if (!DbProviderFactories.TryGetFactory(providerName, out var factory))
                    continue;

                if (factory is TracingProviderFactory)
                {
                    Trace.WriteLine($"{nameof(DataAccessTracing)}: Already replaced - {providerName}");
                    continue;
                }

                var tracingFactory = new TracingProviderFactory(factory);

                DbProviderFactories.UnregisterFactory(providerName);
                DbProviderFactories.RegisterFactory(providerName, tracingFactory);
            }
#endif

            _replaced = true;
        }

        internal static DataAccessTraceHandler DefaultTraceHandler
        {
            get => field ??= new DefaultDataAccessTraceHandler();
            private set;
        }

        public static void SetDefaultTraceHandler(DataAccessTraceHandler traceHandler)
        {
            ArgumentNullException.ThrowIfNull(traceHandler);
            if (_replaced)
                throw new InvalidOperationException($"{nameof(SetDefaultTraceHandler)} must be called before {nameof(ReplaceDbProviderFactoriesWithTracingProviders)}.");
            DefaultTraceHandler = traceHandler;
        }

        public static bool IsParameterValidationEnabledDefault
        {
            get;
            set
            {
                if (_replaced)
                    throw new InvalidOperationException($"{nameof(IsParameterValidationEnabledDefault)} must be set before {nameof(ReplaceDbProviderFactoriesWithTracingProviders)}.");
                field = value;
            }
        }

        internal static MethodBase? GetCallerMethod(int startFrameCount)
        {
            var trace = new StackTrace();
            for (int index = startFrameCount; index < trace.FrameCount; index++)
            {
                var method = trace.GetFrame(index)?.GetMethod();
                if (method is null)
                    continue;
                var typeName = method.DeclaringType?.FullName;
                if (typeName is not null &&
                    !typeName.StartsWith("DataAccessTracing.", StringComparison.Ordinal) &&
                    !typeName.StartsWith("System.", StringComparison.Ordinal) &&
                    !typeName.StartsWith("Microsoft.", StringComparison.Ordinal))
                {
                    return method;
                }
            }
            return null;
        }
    }
}
