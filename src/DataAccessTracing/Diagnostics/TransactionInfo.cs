using System.Data;
using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database transaction.
    /// </summary>
    public sealed class TransactionInfo : DbObjectInfo
    {
        /// <summary>
        /// Gets information about the connection associated with the transaction.
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Gets the isolation level of the transaction.
        /// </summary>
        public IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Gets the duration of the transaction.
        /// </summary>
        /// <remarks>
        /// This value represents the elapsed time between beginning and completing the transaction.
        /// </remarks>
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
