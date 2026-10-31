using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Transactions;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    internal sealed class TracingConnection : DbConnection
    {
        private readonly DbConnection _innerConnection;
        private readonly TracingProviderFactory? _factory;

        private readonly ConnectionInfo _info = new();

        internal ConnectionInfo Info => _info;

        public TracingConnection(DbConnection innerConnection, TracingProviderFactory? factory)
        {
            _innerConnection = innerConnection;
            _factory = factory;

            if (innerConnection.State == ConnectionState.Open)
                OpenConnection();

            innerConnection.StateChange += InnerConnection_StateChange;
        }

        internal DbConnection InnerConnection => _innerConnection;

        //

        public override event StateChangeEventHandler? StateChange
        {
            add => _innerConnection.StateChange += value;
            remove => _innerConnection.StateChange -= value;
        }

#if NET
        [AllowNull]
#endif
        public override string ConnectionString
        {
            get => _innerConnection.ConnectionString;
            set => _innerConnection.ConnectionString = value;
        }

        public override int ConnectionTimeout => _innerConnection.ConnectionTimeout;

        public override string Database => _innerConnection.Database;

        public override string DataSource => _innerConnection.DataSource;

        public override string ServerVersion => _innerConnection.ServerVersion;

        public override ISite? Site
        {
            get => _innerConnection.Site;
            set => _innerConnection.Site = value;
        }

        public override ConnectionState State => _innerConnection.State;

        protected override DbProviderFactory? DbProviderFactory => _factory;

        public override void ChangeDatabase(string databaseName) => _innerConnection.ChangeDatabase(databaseName);

        public override void Close() => _innerConnection.Close();

        public override void Open() => _innerConnection.Open();

        public override void EnlistTransaction(Transaction? transaction)
        {
            _innerConnection.EnlistTransaction(transaction);
            // TODO
        }

        public override DataTable GetSchema() => _innerConnection.GetSchema();

        public override DataTable GetSchema(string collectionName) => _innerConnection.GetSchema(collectionName);

        public override DataTable GetSchema(string collectionName, string?[] restrictionValues) => _innerConnection.GetSchema(collectionName, restrictionValues);

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            var innerTransaction = _innerConnection.BeginTransaction(isolationLevel);
            return new TracingTransaction(innerTransaction, this);
        }

        protected override DbCommand CreateDbCommand()
        {
            var innerCommand = _innerConnection.CreateCommand();
            return new TracingCommand(innerCommand, this);
        }

        protected override object? GetService(Type service) => ((IServiceProvider)_innerConnection).GetService(service);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerConnection.Dispose();
                _innerConnection.StateChange -= InnerConnection_StateChange;
            }

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnusedObjectDetected(_info);
        }

#if !NET35
        public override Task OpenAsync(CancellationToken cancellationToken) => _innerConnection.OpenAsync(cancellationToken);

#if !NETFRAMEWORK
        public override Task CloseAsync() => _innerConnection.CloseAsync();

        protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var innerTransaction = await _innerConnection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
            return new TracingTransaction(innerTransaction, this);
        }

        public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
            => _innerConnection.ChangeDatabaseAsync(databaseName, cancellationToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2215")]
        public override async ValueTask DisposeAsync()
        {
            await _innerConnection.DisposeAsync().ConfigureAwait(false);
            _innerConnection.StateChange -= InnerConnection_StateChange;
            // base.DisposeAsync() を呼ぶと base.Dispose() -> this.Dispose(true) が呼ばれるため、 base.Dispose(true) だけ呼んでおく。
            base.Dispose(true);
            // dispose check
            GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnusedObjectDetected(_info);
        }
#endif
#endif

#if NET
        public override bool CanCreateBatch => _innerConnection.CanCreateBatch;

        private Func<DbBatch>? _createDbBatch;

        protected override DbBatch CreateDbBatch()
        {
            if (_createDbBatch is null)
            {
                var mi = _innerConnection.GetType().GetMethod(nameof(CreateDbBatch), BindingFlags.NonPublic | BindingFlags.Instance);
                _createDbBatch = mi!.CreateDelegate<Func<DbBatch>>();
            }
            var innerBatch = _createDbBatch();
            // TODO: TracingBatch
            return innerBatch;
        }

        public override Task<DataTable> GetSchemaAsync(CancellationToken cancellationToken = default)
            => _innerConnection.GetSchemaAsync(cancellationToken);

        public override Task<DataTable> GetSchemaAsync(string collectionName, CancellationToken cancellationToken = default)
            => _innerConnection.GetSchemaAsync(collectionName, cancellationToken);

        public override Task<DataTable> GetSchemaAsync(string collectionName, string?[] restrictionValues, CancellationToken cancellationToken = default)
            => _innerConnection.GetSchemaAsync(collectionName, restrictionValues, cancellationToken);
#endif

        //

        private void InnerConnection_StateChange(object sender, StateChangeEventArgs e)
        {
            if (e.CurrentState == ConnectionState.Open)
                OpenConnection();
            else if (e.CurrentState == ConnectionState.Closed)
                CloseConnection();
        }

        private void OpenConnection()
        {
            _info.Open();
            DataAccessTrace.TraceHandler.OnConnectionOpened(_info);
            _info.Start();
        }

        private void CloseConnection()
        {
            _info.Close();
            DataAccessTrace.TraceHandler.OnConnectionClosed(_info);
        }

        // dispose check
        ~TracingConnection()
        {
            try
            {
                DataAccessTrace.TraceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
