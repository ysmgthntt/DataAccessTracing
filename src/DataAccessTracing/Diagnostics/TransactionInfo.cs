using System.Data;
using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public sealed class TransactionInfo : DbObjectInfo
    {
        public ConnectionInfo ConnectionInfo { get; }
        public IsolationLevel IsolationLevel { get; }
        public TimeSpan Duration => _sw.Elapsed;

        private readonly Stopwatch _sw = new();

        internal TransactionInfo(ConnectionInfo connectionInfo, IsolationLevel isolationLevel) : base(DbObjectKind.Transaction)
        {
            ConnectionInfo = connectionInfo;
            IsolationLevel = isolationLevel;
        }

        internal void Begin() => _sw.Start();

        internal void End() => _sw.Stop();
    }
}
