using System.Data;
using System.Data.Common;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    internal sealed class TracingTransaction : DbTransaction
    {
        private readonly DbTransaction _innerTransaction;
        private readonly DataAccessTraceHandler _traceHandler;
        private readonly TracingConnection _connection;
        private readonly TransactionInfo _info;

        internal TransactionInfo Info => _info;

        internal TracingTransaction(DbTransaction innerTransaction, DataAccessTraceHandler traceHandler, TracingConnection connnection)
        {
            _innerTransaction = innerTransaction;
            _traceHandler = traceHandler;
            _connection = connnection;

            // OracleTransaction の IsolationLevel を取得すると NullReferenceException が発生する。
            IsolationLevel isolationLevel
                = _innerTransaction.GetType().FullName == "System.Data.OracleClient.OracleTransaction" ? IsolationLevel.ReadCommitted
                : _innerTransaction.IsolationLevel;

            _info = new TransactionInfo(connnection.Info, isolationLevel);

            _traceHandler.OnBeginTransaction(_info);
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
            _traceHandler.OnCommitTransaction(_info);
        }

        public override void Rollback()
        {
            _innerTransaction.Rollback();
            _info.End();
            _traceHandler.OnRollbackTransaction(_info);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerTransaction.Dispose();
                _info.End();
            }

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                _traceHandler.OnUnusedObjectDetected(_info);
        }

        //

#if !NETFRAMEWORK
        public override async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _innerTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _info.End();
            _traceHandler.OnCommitTransaction(_info);
        }

        public override async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _innerTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _info.End();
            _traceHandler.OnRollbackTransaction(_info);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2215")]
        public override async ValueTask DisposeAsync()
        {
            await _innerTransaction.DisposeAsync().ConfigureAwait(false);
            // base.DisposeAsync() を呼ぶと base.Dispose() -> this.Dispose(true) が呼ばれるため、 base.Dispose(true) だけ呼んでおく。
            base.Dispose(true);
            // dispose check
            GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                _traceHandler.OnUnusedObjectDetected(_info);
        }
#endif

#if NET
        public override bool SupportsSavepoints => _innerTransaction.SupportsSavepoints;

        public override void Save(string savepointName) => _innerTransaction.Save(savepointName);

        public override void Rollback(string savepointName) => _innerTransaction.Rollback(savepointName);

        public override void Release(string savepointName) => _innerTransaction.Release(savepointName);

        public override Task SaveAsync(string savepointName, CancellationToken cancellationToken = default) => _innerTransaction.SaveAsync(savepointName, cancellationToken);

        public override Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default) => _innerTransaction.RollbackAsync(savepointName, cancellationToken);

        public override Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default) => _innerTransaction.ReleaseAsync(savepointName, cancellationToken);
#endif

        //

        // dispose check
        ~TracingTransaction()
        {
            try
            {
                _traceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
