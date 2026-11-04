using System.Data;
using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestTransaction : DbTransaction
    {
        public TestTransaction(TestConnection connection, IsolationLevel isolationLevel)
        {
            DbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            IsolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel { get; }

        protected override DbConnection? DbConnection { get; }

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
