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

        private bool? _isParameterValidationEnabled;

        public bool IsParameterValidationEnabled
        {
            get => _isParameterValidationEnabled ??= DataAccessTrace.IsParameterValidationEnabledDefault;
            set => _isParameterValidationEnabled = value;
        }

        public TracingProviderFactory(DbProviderFactory innerFactory)
        {
            ArgumentNullException.ThrowIfNull(innerFactory);
            if (innerFactory is TracingProviderFactory)
                throw new ArgumentException("innerFactory must not be TracingProviderFactory.", nameof(innerFactory));
            _innerFactory = innerFactory;
        }

        public TracingProviderFactory(DbProviderFactory innerFactory, DataAccessTraceHandler traceHandler) : this(innerFactory)
        {
            ArgumentNullException.ThrowIfNull(traceHandler);
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
            return innerCommand is null ? null : new TracingCommand(innerCommand, TraceHandler, null, IsParameterValidationEnabled);
        }

        private bool? _noDeriveParametersMethod;

        public override DbCommandBuilder? CreateCommandBuilder()
        {
            var innerBuilder = _innerFactory.CreateCommandBuilder();
            if (innerBuilder is null)
                return null;

            if (_noDeriveParametersMethod is null)
                _noDeriveParametersMethod = innerBuilder.GetType().GetMethod("DeriveParameters", BindingFlags.Public | BindingFlags.Static) is null;

            if (_noDeriveParametersMethod.Value)
                return new TracingCommandBuilder(innerBuilder, TraceHandler);

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
