using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public class DataAccessTraceHandler
    {
        protected internal virtual void OnConnectionOpened(ConnectionInfo info)
        {
            Trace.WriteLine($"Connection Opened ID: {info.ConnectionID}");
        }

        protected internal virtual void OnConnectionClosed(ConnectionInfo info)
        {
            Trace.WriteLine($"Connection Closed ID: {info.ConnectionID}, Duration: {info.Duration}");
        }

        protected internal virtual void OnUnused(DbObjectInfo info)
        {
            Trace.WriteLine($"Unused {info.Kind} ID: {info.ID}");
        }

        protected internal virtual void OnDisposeLeakDetected(DbObjectInfo info)
        {
            Trace.WriteLine($"DisposeLeakDetected {info.Kind} ID: {info.ID}");
        }
    }
}
