using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public class DefaultDataAccessTraceHandler : DataAccessTraceHandler
    {
        protected internal override void OnConnectionOpened(ConnectionInfo info)
        {
            Trace.WriteLine($"Connection Opened ID: {info.ConnectionID}");
        }

        protected internal override void OnConnectionClosed(ConnectionInfo info)
        {
            Trace.WriteLine($"Connection Closed ID: {info.ConnectionID}, Duration: {info.Duration}");
        }

        protected internal override void OnBeginTransaction(TransactionInfo info)
        {
            Trace.WriteLine($"Begin Transaction ID: {info.ID}, ConnectionID: {info.ConnectionInfo.ConnectionID}");
        }

        protected internal override void OnCommitTransaction(TransactionInfo info)
        {
            Trace.WriteLine($"Commit Transaction ID: {info.ID}, ConnectionID: {info.ConnectionInfo.ConnectionID}, Duration: {info.Duration}");
        }

        protected internal override void OnRollbackTransaction(TransactionInfo info)
        {
            Trace.WriteLine($"Rollback Transaction ID: {info.ID}, ConnectionID: {info.ConnectionInfo.ConnectionID}, Duration: {info.Duration}");
        }

        protected internal override void OnCommandExecuting(CommandExecutionInfo info)
        {
            string caller = info.CallerMethod is null ? "" : $"{Environment.NewLine}Caller: {GetFullMethodName(info.CallerMethod)}";
            Trace.WriteLine($"[{info.MethodName}]{Environment.NewLine}sql{caller}");
        }

        protected internal override void OnCommandExecuted(CommandExecutionInfo info)
        {
            Trace.WriteLine($"Command Executed: {info.MethodName}, Duration: {info.Duration}, RecordsAffected: {info.RecordsAffected}");
        }

        protected internal override void OnCommandException(CommandExecutionInfo info)
        {
        }

        protected internal override void OnUnused(DbObjectInfo info)
        {
            Trace.WriteLine($"Unused {info.Kind} ID: {info.ID}");
        }

        protected internal override void OnDisposeLeakDetected(DbObjectInfo info)
        {
            Trace.WriteLine($"DisposeLeakDetected {info.Kind} ID: {info.ID}");
        }
    }
}
