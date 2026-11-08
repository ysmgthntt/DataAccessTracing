using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database connection.
    /// </summary>
    public sealed class ConnectionInfo : DbObjectInfo
    {
        /// <summary>
        /// Gets the unique identifier of the connection.
        /// </summary>
        public Guid ConnectionId { get; private set; }

        /// <summary>
        /// Gets the duration for which the connection was open.
        /// </summary>
        /// <remarks>
        /// This value represents the elapsed time between opening and closing the connection.
        /// </remarks>
        public TimeSpan Duration => _sw.Elapsed;

        private readonly Stopwatch _sw = new();

        internal ConnectionInfo() : base(DbObjectKind.Connection) { }

        internal void Open()
        {
            ConnectionId = Guid.NewGuid();
            _sw.Reset();
        }

        internal void Start() => _sw.Start();

        internal void Close() => _sw.Stop();
    }
}
