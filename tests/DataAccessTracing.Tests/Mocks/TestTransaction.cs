using System.Data;
using System.Data.Common;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests.Mocks
{
    internal class TestTransaction(TestConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel { get => isolationLevel; }

        protected override DbConnection? DbConnection { get => connection ?? throw new ArgumentNullException(nameof(connection)); }

        private bool _commit;

        public override void Commit()
        {
            CheckAvailable();
            _commit = true;
        }

        private bool _rollback;

        public override void Rollback()
        {
            CheckAvailable();
            _rollback = true;
        }

        private void CheckAvailable()
        {
            if (_commit)
                throw new InvalidOperationException();
            if (_rollback)
                throw new InvalidOperationException();
            if (IsDisposed)
                throw new InvalidOperationException();
        }

        public bool IsDisposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;

            base.Dispose(disposing);
        }
    }
}
#if NETFRAMEWORK
#pragma warning disable IDE0130
namespace System.Data.OracleClient
{
    internal sealed class OracleTransaction(TestConnection connection, IsolationLevel isolationLevel) : TestTransaction(connection, isolationLevel)
    {
        public override IsolationLevel IsolationLevel => throw new InvalidOperationException();
    }
}
#endif
