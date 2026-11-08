using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    /// <summary>
    /// Provides global configuration and initialization methods for DataAccessTracing.
    /// </summary>
    public static class DataAccessTrace
    {
        private static bool _replaced;

        /// <summary>
        /// Replaces all database provider factories registered in <see cref="System.Data.Common.DbProviderFactories"/>
        /// with tracing provider factories.
        /// </summary>
        /// <remarks>
        /// This method can be called multiple times to replace database provider factories
        /// registered since the previous call.
        /// </remarks>
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

        /// <summary>
        /// Sets the default trace handler.
        /// </summary>
        /// <param name="traceHandler">The trace handler to use by default.</param>
        /// <remarks>
        /// This method must be called before
        /// <see cref="ReplaceDbProviderFactoriesWithTracingProviders"/>.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// The method is called after <see cref="ReplaceDbProviderFactoriesWithTracingProviders"/> has been called.
        /// </exception>
        public static void SetDefaultTraceHandler(DataAccessTraceHandler traceHandler)
        {
            ArgumentNullException.ThrowIfNull(traceHandler);
            if (_replaced)
                throw new InvalidOperationException($"{nameof(SetDefaultTraceHandler)} must be called before {nameof(ReplaceDbProviderFactoriesWithTracingProviders)}.");
            DefaultTraceHandler = traceHandler;
        }

        /// <summary>
        /// Gets or sets a value indicating whether parameter validation is enabled by default.
        /// </summary>
        /// <remarks>
        /// This value is used as the default setting for tracing provider factories.
        /// This property must be set before calling
        /// <see cref="ReplaceDbProviderFactoriesWithTracingProviders"/>.
        /// </remarks>
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

        private static bool _excludePrefixesChanged;

        /// <summary>
        /// Gets or sets the namespace prefixes to exclude when determining the caller's method from a stack trace.
        /// </summary>
        /// <remarks>
        /// Types whose full names start with one of the specified prefixes are excluded.
        /// The comparison is ordinal and case-sensitive.
        /// If this property is <see langword="null"/>, no namespace prefixes are excluded.
        /// </remarks>
        public static IEnumerable<string>? StackTraceExcludedNamespacePrefixes
        {
            get;
            set
            {
                field = value;
                _excludePrefixesChanged = true;
            }
        } = new List<string>() { "System.", "Microsoft." }.AsReadOnly();

        internal static MethodBase? GetCallerMethod(int startFrameCount)
        {
            var thisAssembly = typeof(DataAccessTrace).Assembly;
            var trace = new StackTrace();
            for (int index = startFrameCount; index < trace.FrameCount; index++)
            {
                var method = trace.GetFrame(index)?.GetMethod();
                if (method is null or { DeclaringType: null })
                    continue;
                var declaringType = method.DeclaringType;
                if (declaringType.Assembly == thisAssembly)
                    continue;
                //if (method.IsDefined(typeof(CompilerGeneratedAttribute), false) || declaringType.IsDefined(typeof(CompilerGeneratedAttribute), false))
                //    continue;
                var typeName = declaringType.FullName;
                if (typeName is null || typeName.Contains('<'))
                    continue;
                if (!_excludePrefixesChanged)
                {
                    if (!typeName.StartsWith("System.", StringComparison.Ordinal) &&
                        !typeName.StartsWith("Microsoft.", StringComparison.Ordinal))
                    {
                        return method;
                    }
                }
                else
                {
                    bool exclude = false;
                    if (StackTraceExcludedNamespacePrefixes is not null and var prefixes)
                    {
                        foreach (var excludePrefix in prefixes)
                        {
                            if (typeName.StartsWith(excludePrefix, StringComparison.Ordinal))
                            {
                                exclude = true;
                                break;
                            }
                        }
                    }
                    if (!exclude)
                        return method;
                }
            }
            return null;
        }
    }
}
