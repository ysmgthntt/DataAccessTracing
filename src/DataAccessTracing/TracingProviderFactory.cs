using System.Data.Common;
using System.Reflection;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    /// <summary>
    /// A provider factory that wraps another provider factory to add tracing functionality.
    /// </summary>
    public class TracingProviderFactory : DbProviderFactory
    {
        private readonly DbProviderFactory _innerFactory;

        internal DbProviderFactory InnerFactory => _innerFactory;

        private protected DataAccessTraceHandler TraceHandler { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether parameter validation is enabled.
        /// </summary>
        /// <remarks>
        /// The initial value is determined by
        /// <see cref="DataAccessTrace.IsParameterValidationEnabledDefault"/>.
        /// </remarks>
        public bool IsParameterValidationEnabled { get; set; } = DataAccessTrace.IsParameterValidationEnabledDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingProviderFactory"/> class.
        /// </summary>
        /// <param name="innerFactory">The provider factory to wrap.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="innerFactory"/> is <see langword="null"/>.
        /// </exception>
        public TracingProviderFactory(DbProviderFactory innerFactory) : this(innerFactory, DataAccessTrace.DefaultTraceHandler)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingProviderFactory"/> class
        /// with the specified trace handler.
        /// </summary>
        /// <param name="innerFactory">The provider factory to wrap.</param>
        /// <param name="traceHandler">The trace handler to use for tracing.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="innerFactory"/> or <paramref name="traceHandler"/> is <see langword="null"/>.
        /// </exception>
        public TracingProviderFactory(DbProviderFactory innerFactory, DataAccessTraceHandler traceHandler)
        {
            ArgumentNullException.ThrowIfNull(innerFactory);
            ArgumentNullException.ThrowIfNull(traceHandler);
            if (innerFactory is TracingProviderFactory)
                throw new ArgumentException($"{nameof(innerFactory)} must not be {nameof(TracingProviderFactory)}.", nameof(innerFactory));
            _innerFactory = innerFactory;
            TraceHandler = traceHandler;
        }

        private protected TracingProviderFactory()
        {
            var t = GetInnerFactoryType();
            var fi = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!;
            _innerFactory = (DbProviderFactory)fi.GetValue(null)!;
            TraceHandler = DataAccessTrace.DefaultTraceHandler;
        }

        private protected virtual Type GetInnerFactoryType() => throw new NotImplementedException();

        /// <inheritdoc />
        public override string ToString() => base.ToString() + ", InnerFactory: " + _innerFactory.ToString();

        //

        /// <inheritdoc />
        public override bool CanCreateDataSourceEnumerator => _innerFactory.CanCreateDataSourceEnumerator;

        /// <inheritdoc />
        public override DbCommand? CreateCommand()
        {
            var innerCommand = _innerFactory.CreateCommand();
            return innerCommand is null ? null : new TracingCommand(innerCommand, TraceHandler, null, IsParameterValidationEnabled);
        }

        private bool? _noDeriveParametersMethod;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override DbConnection? CreateConnection()
        {
            var innerConnection = _innerFactory.CreateConnection();
            return innerConnection is null ? null : new TracingConnection(innerConnection, TraceHandler, this);
        }

        /// <inheritdoc />
        public override DbConnectionStringBuilder? CreateConnectionStringBuilder() => _innerFactory.CreateConnectionStringBuilder();

        /// <inheritdoc />
        public override DbDataAdapter? CreateDataAdapter()
        {
            var innerAdapter = _innerFactory.CreateDataAdapter();
            return innerAdapter is null ? null : new TracingDataAdapter(innerAdapter, TraceHandler);
        }

        /// <inheritdoc />
        public override DbDataSourceEnumerator? CreateDataSourceEnumerator() => _innerFactory.CreateDataSourceEnumerator();

        /// <inheritdoc />
        public override DbParameter? CreateParameter() => _innerFactory.CreateParameter();

#if !NETFRAMEWORK
        /// <inheritdoc />
        public override bool CanCreateCommandBuilder => _innerFactory.CanCreateCommandBuilder;
        /// <inheritdoc />
        public override bool CanCreateDataAdapter => _innerFactory.CanCreateDataAdapter;
#endif

#if NET
        /// <inheritdoc />
        public override bool CanCreateBatch => _innerFactory.CanCreateBatch;

        /// <inheritdoc />
        public override DbBatch CreateBatch() => _innerFactory.CreateBatch();

        /// <inheritdoc />
        public override DbBatchCommand CreateBatchCommand() => _innerFactory.CreateBatchCommand();

        /// <inheritdoc />
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
