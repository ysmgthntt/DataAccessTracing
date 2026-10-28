using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public sealed class ConnectionInfo : DbObjectInfo
    {
        public Guid ConnectionID { get; private set; }
        public TimeSpan Duration { get; private set; }

        private readonly Stopwatch _sw = new();

        public ConnectionInfo() : base(DbObjectKind.Connection) { }

        internal void Open()
        {
            ConnectionID = Guid.NewGuid();
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

        internal void Close()
        {
            _sw.Stop();
            Duration = _sw.Elapsed;
        }
    }
}
