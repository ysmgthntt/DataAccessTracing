using System.Data.Common;
using System.Reflection;

namespace DataAccessTracing
{
    internal class TracingProviderFactory : DbProviderFactory
    {
        private readonly DbProviderFactory _innerFactory;

        internal DbProviderFactory InnerFactory => _innerFactory;

        public TracingProviderFactory(DbProviderFactory innerFactory)
        {
            _innerFactory = innerFactory;
        }

        protected TracingProviderFactory()
        {
            var t = GetInnerFactoryType();
            var fi = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            _innerFactory = (DbProviderFactory)fi.GetValue(null);
        }

        protected virtual Type GetInnerFactoryType() => throw new NotImplementedException();

        public override string ToString()
        {
            return base.ToString() + ", InnerFactory: " + _innerFactory.ToString();
        }

#if !NETFRAMEWORK
        public override bool CanCreateCommandBuilder => _innerFactory.CanCreateCommandBuilder;
#endif

        public override DbCommandBuilder CreateCommandBuilder()
        {
            var innerBuilder = _innerFactory.CreateCommandBuilder();
            var t = typeof(TracingCommandBuilder<>).MakeGenericType(_innerFactory.GetType());
            var cb = (DbCommandBuilder)Activator.CreateInstance(t, innerBuilder);
            return cb;
        }
    }

    internal sealed class TracingProviderFactory<TDbProviderFactory> : TracingProviderFactory
        where TDbProviderFactory : DbProviderFactory
    {
        public static readonly TracingProviderFactory<TDbProviderFactory> Instance = new();

        protected override Type GetInnerFactoryType() => typeof(TDbProviderFactory);

        public override DbCommandBuilder CreateCommandBuilder()
        {
            var innerBuilder = InnerFactory.CreateCommandBuilder();
            return new TracingCommandBuilder<TDbProviderFactory>(innerBuilder);
        }
    }
}
