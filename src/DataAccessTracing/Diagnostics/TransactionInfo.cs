using System.Data;
using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public sealed class TransactionInfo(ConnectionInfo connectionInfo) : DbObjectInfo(DbObjectKind.Transaction)
    {
        public Guid TransactionID { get; } = Guid.NewGuid();
        public ConnectionInfo ConnectionInfo => connectionInfo;
        public TimeSpan Duration { get; private set; }
        public IsolationLevel IsolationLevel { get; internal set; }

        private readonly Stopwatch _sw = new();

        internal void Begin() => _sw.Start();

        internal void End()
        {
            _sw.Stop();
            Duration = _sw.Elapsed;
        }
    }
}
