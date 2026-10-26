using System.Data.Common;
using System.Reflection;

namespace DataAccessTracing
{
    internal sealed class TracingProviderFactory<TDbProviderFactory> : DbProviderFactory, ITracingProviderFactory
        where TDbProviderFactory : DbProviderFactory
    {
        public static readonly TracingProviderFactory<TDbProviderFactory> Instance = new();

        private readonly TDbProviderFactory _innerFactory;

        public TracingProviderFactory()
        {
            var fi = typeof(TDbProviderFactory).GetField(nameof(Instance), BindingFlags.Public | BindingFlags.Static);
            _innerFactory = (TDbProviderFactory)fi.GetValue(null);
        }
    }
}
