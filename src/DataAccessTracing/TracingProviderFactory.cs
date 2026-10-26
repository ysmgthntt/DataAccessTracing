using System.Data.Common;
using System.Diagnostics;

namespace DataAccessTracing
{
    internal sealed class TracingProviderFactory<TDbProviderFactory> : DbProviderFactory, ITracingProviderFactory
        where TDbProviderFactory : DbProviderFactory
    {
        public static readonly TracingProviderFactory<TDbProviderFactory> Instance = new();

        public TracingProviderFactory()
        {
            Trace.WriteLine(GetType().FullName + " ctor");
        }
    }
}
