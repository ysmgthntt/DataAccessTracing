using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public sealed class ConnectionInfo : DbObjectInfo
    {
        public Guid ConnectionId { get; private set; }
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
