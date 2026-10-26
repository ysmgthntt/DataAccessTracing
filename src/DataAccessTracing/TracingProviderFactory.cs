using System.Data.Common;
using System.Reflection;

namespace DataAccessTracing
{
    internal class TracingProviderFactory : DbProviderFactory
    {
        private readonly DbProviderFactory _innerFactory;

        public TracingProviderFactory(DbProviderFactory innerFactory)
        {
            _innerFactory = innerFactory;
        }

        protected TracingProviderFactory()
        {
            _innerFactory = GetInnerFactory();
        }

        protected virtual DbProviderFactory GetInnerFactory() => throw new NotImplementedException();

        public override string ToString()
        {
            return base.ToString() + ", InnerFactory: " + _innerFactory.ToString();
        }
    }

    internal sealed class TracingProviderFactory<TDbProviderFactory> : TracingProviderFactory
        where TDbProviderFactory : DbProviderFactory
    {
        public static readonly TracingProviderFactory<TDbProviderFactory> Instance = new();

        protected override DbProviderFactory GetInnerFactory()
        {
            var fi = typeof(TDbProviderFactory).GetField(nameof(Instance), BindingFlags.Public | BindingFlags.Static);
            var factory = (DbProviderFactory)fi.GetValue(null);
            return factory;
        }
    }
}
