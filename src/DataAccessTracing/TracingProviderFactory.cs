using System.Data.Common;
using System.Reflection;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    public class TracingProviderFactory : DbProviderFactory
    {
        private readonly DbProviderFactory _innerFactory;

        internal DbProviderFactory InnerFactory => _innerFactory;

        private protected DataAccessTraceHandler TraceHandler
        {
            get => field ??= DataAccessTrace.DefaultTraceHandler;
            init;
        }

        public TracingProviderFactory(DbProviderFactory innerFactory)
        {
            ArgumentNullException.ThrowIfNull(innerFactory);
            _innerFactory = innerFactory;
        }

        public TracingProviderFactory(DbProviderFactory innerFactory, DataAccessTraceHandler traceHandler)
        {
            ArgumentNullException.ThrowIfNull(innerFactory);
            ArgumentNullException.ThrowIfNull(traceHandler);
            _innerFactory = innerFactory;
            TraceHandler = traceHandler;
        }

        private protected TracingProviderFactory()
        {
            var t = GetInnerFactoryType();
            var fi = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
            _innerFactory = (DbProviderFactory)fi.GetValue(null)!;
        }

        private protected virtual Type GetInnerFactoryType() => throw new NotImplementedException();

        public override string ToString() => base.ToString() + ", InnerFactory: " + _innerFactory.ToString();

        //

        public override bool CanCreateDataSourceEnumerator => _innerFactory.CanCreateDataSourceEnumerator;

        public override DbCommand? CreateCommand()
        {
            var innerCommand = _innerFactory.CreateCommand();
            return innerCommand is null ? null : new TracingCommand(innerCommand, TraceHandler, null);
        }

        public override DbCommandBuilder? CreateCommandBuilder()
        {
            var innerBuilder = _innerFactory.CreateCommandBuilder();
            if (innerBuilder is null)
                return null;
            var t = typeof(TracingCommandBuilder<>).MakeGenericType(_innerFactory.GetType());
            var cb = (DbCommandBuilder?)Activator.CreateInstance(t, innerBuilder, TraceHandler);
            return cb;
        }

        public override DbConnection? CreateConnection()
        {
            var innerConnection = _innerFactory.CreateConnection();
            return innerConnection is null ? null : new TracingConnection(innerConnection, TraceHandler, this);
        }

        public override DbConnectionStringBuilder? CreateConnectionStringBuilder() => _innerFactory.CreateConnectionStringBuilder();

        public override DbDataAdapter? CreateDataAdapter()
        {
            var innerAdapter = _innerFactory.CreateDataAdapter();
            return innerAdapter is null ? null : new TracingDataAdapter(innerAdapter, TraceHandler);
        }

        public override DbDataSourceEnumerator? CreateDataSourceEnumerator() => _innerFactory.CreateDataSourceEnumerator();

        public override DbParameter? CreateParameter() => _innerFactory.CreateParameter();

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

        public override DbCommandBuilder? CreateCommandBuilder()
        {
            var innerBuilder = InnerFactory.CreateCommandBuilder();
            return innerBuilder is null ? null : new TracingCommandBuilder<TDbProviderFactory>(innerBuilder, TraceHandler);
        }
    }
}
