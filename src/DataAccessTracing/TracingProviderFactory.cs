using System.Data.Common;
using System.Reflection;

namespace DataAccessTracing
{
    public class TracingProviderFactory : DbProviderFactory
    {
        private readonly DbProviderFactory _innerFactory;

        internal DbProviderFactory InnerFactory => _innerFactory;

        public TracingProviderFactory(DbProviderFactory innerFactory)
        {
            ArgumentNullException.ThrowIfNull(innerFactory);
            _innerFactory = innerFactory;
        }

        private protected TracingProviderFactory()
        {
            var t = GetInnerFactoryType();
            var fi = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            _innerFactory = (DbProviderFactory)fi.GetValue(null);
        }

        private protected virtual Type GetInnerFactoryType() => throw new NotImplementedException();

        public override string ToString() => base.ToString() + ", InnerFactory: " + _innerFactory.ToString();

        //

        public override bool CanCreateDataSourceEnumerator => _innerFactory.CanCreateDataSourceEnumerator;

        public override DbCommand CreateCommand()
        {
            var innerCommand = _innerFactory.CreateCommand();
            return new TracingCommand(innerCommand, null);
        }

        public override DbCommandBuilder CreateCommandBuilder()
        {
            var innerBuilder = _innerFactory.CreateCommandBuilder();
            var t = typeof(TracingCommandBuilder<>).MakeGenericType(_innerFactory.GetType());
            var cb = (DbCommandBuilder)Activator.CreateInstance(t, innerBuilder);
            return cb;
        }

        public override DbConnection CreateConnection()
        {
            var innerConnection = _innerFactory.CreateConnection();
            return new TracingConnection(innerConnection, this);
        }

        public override DbConnectionStringBuilder CreateConnectionStringBuilder() => _innerFactory.CreateConnectionStringBuilder();

        public override DbDataAdapter CreateDataAdapter()
        {
            var innerAdapter = _innerFactory.CreateDataAdapter();
            return new TracingDataAdapter(innerAdapter);
        }

        public override DbDataSourceEnumerator CreateDataSourceEnumerator() => _innerFactory.CreateDataSourceEnumerator();

        public override DbParameter CreateParameter() => _innerFactory.CreateParameter();

#if !NETFRAMEWORK
        public override bool CanCreateCommandBuilder => _innerFactory.CanCreateCommandBuilder;
        public override bool CanCreateDataAdapter => _innerFactory.CanCreateDataAdapter;
#endif

#if NET
        public override bool CanCreateBatch => _innerFactory.CanCreateBatch;

        public override DbBatch CreateBatch() => _innerFactory.CreateBatch();

        public override DbBatchCommand CreateBatchCommand() => _innerFactory.CreateBatchCommand();

        public override DbDataSource CreateDataSource(string connectionString)
        {
            var innerDataSource = _innerFactory.CreateDataSource(connectionString);
            // TODO
            return innerDataSource;
        }
#endif
    }

    internal sealed class TracingProviderFactory<TDbProviderFactory> : TracingProviderFactory
        where TDbProviderFactory : DbProviderFactory
    {
        public static readonly TracingProviderFactory<TDbProviderFactory> Instance = new();

        private protected override Type GetInnerFactoryType() => typeof(TDbProviderFactory);

        public override DbCommandBuilder CreateCommandBuilder()
        {
            var innerBuilder = InnerFactory.CreateCommandBuilder();
            return new TracingCommandBuilder<TDbProviderFactory>(innerBuilder);
        }
    }
}
