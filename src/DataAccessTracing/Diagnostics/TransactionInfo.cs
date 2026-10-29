using System.Data;
using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public sealed class TransactionInfo : DbObjectInfo
    {
        public ConnectionInfo ConnectionInfo { get; }
        public TimeSpan Duration => _sw.Elapsed;
        public IsolationLevel IsolationLevel { get; internal set; }

        private readonly Stopwatch _sw = new();

        internal TransactionInfo(ConnectionInfo connectionInfo) : base(DbObjectKind.Transaction)
        {
            ConnectionInfo = connectionInfo;
        }

        internal void Begin() => _sw.Start();

        internal void End() => _sw.Stop();
    }
}
