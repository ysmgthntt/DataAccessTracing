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
        }

        internal void Start()
        {
#if !NET35
            _sw.Restart();
#else
            _sw.Reset();
            _sw.Start();
#endif
        }

        internal void Close() => _sw.Stop();
    }
}
