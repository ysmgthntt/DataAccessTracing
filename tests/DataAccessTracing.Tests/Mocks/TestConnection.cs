using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestConnection : DbConnection
    {
        public TestConnection() { }

        public TestConnection(bool open)
        {
            if (open)
                _state = ConnectionState.Open;
        }

#if NET
        [AllowNull]
#endif
        public override string ConnectionString { get; set; } = "";

        public override string Database => "";

        public override string DataSource => "";

        public override string ServerVersion => "";

        private ConnectionState _state = ConnectionState.Closed;
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close()
        {
            var oldState = _state;
            _state = ConnectionState.Closed;
            if (_state != oldState)
                OnStateChange(new StateChangeEventArgs(oldState, _state));
        }

        public override void Open()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException();
            if (_state != ConnectionState.Closed)
                throw new InvalidOperationException();

            var oldState = _state;
            _state = ConnectionState.Open;
            if (_state != oldState)
                OnStateChange(new StateChangeEventArgs(oldState, _state));
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            if (_state != ConnectionState.Open)
                throw new InvalidOperationException();

            return new TestTransaction(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand() => new TestCommand() { Connection = this };

        //

        protected override DbProviderFactory? DbProviderFactory => TestProviderFactory.Instance;

        //

#if !NET35
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            await base.OpenAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }

#if NET
        protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var trans = await base.BeginDbTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return trans;
        }

        public override async Task CloseAsync()
        {
            await base.CloseAsync().ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync().ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false);
        }
#endif
#endif

        //

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
                IsDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
