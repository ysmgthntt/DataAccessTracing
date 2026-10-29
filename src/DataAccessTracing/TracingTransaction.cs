using System.Data;
using System.Data.Common;

namespace DataAccessTracing
{
    using Diagnostics;

    internal sealed class TracingTransaction : DbTransaction
    {
        private readonly DbTransaction _innerTransaction;
        private readonly TracingConnection _connection;
        private readonly TransactionInfo _info;

        public TracingTransaction(DbTransaction innerTransaction, TracingConnection connnection)
        {
            _innerTransaction = innerTransaction;
            _connection = connnection;
            _info = new TransactionInfo(connnection.Info);

            // OracleTransaction の IsolationLevel を取得すると NullReferenceException が発生する。
            if (_innerTransaction.GetType().FullName != "System.Data.OracleClient.OracleTransaction")
                _info.IsolationLevel = _innerTransaction.IsolationLevel;

            DataAccessTrace.TraceHandler.OnBeginTransaction(_info);
            _info.Begin();
        }

        internal DbTransaction InnerTransaction => _innerTransaction;

        //

        public override IsolationLevel IsolationLevel => _innerTransaction.IsolationLevel;

        protected override DbConnection DbConnection => _connection;

        public override void Commit()
        {
            _innerTransaction.Commit();
            _info.End();
            DataAccessTrace.TraceHandler.OnCommitTransaction(_info);
        }

        public override void Rollback()
        {
            _innerTransaction.Rollback();
            _info.End();
            DataAccessTrace.TraceHandler.OnRollbackTransaction(_info);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _innerTransaction.Dispose();

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnused(_info);
        }

        //

#if !NETFRAMEWORK
        public override async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _innerTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _info.End();
            DataAccessTrace.TraceHandler.OnCommitTransaction(_info);
        }

        public override async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _innerTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _info.End();
            DataAccessTrace.TraceHandler.OnRollbackTransaction(_info);
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerTransaction.DisposeAsync().ConfigureAwait(false);
            base.Dispose(true);
            // dispose check
            GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnused(_info);
        }
#endif

        //

        // dispose check
        ~TracingTransaction()
        {
            try
            {
                DataAccessTrace.TraceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
