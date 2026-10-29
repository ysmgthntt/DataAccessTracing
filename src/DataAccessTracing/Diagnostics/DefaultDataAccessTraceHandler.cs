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
            Trace.WriteLine($"[{info.MethodName}]{Environment.NewLine}{info.GetParameterExpandedCommandText()}{caller}");
        }

        protected internal override void OnCommandExecuted(CommandExecutionInfo info)
        {
            Trace.WriteLine($"Command Executed: {info.MethodName}, Duration: {info.Duration}, RecordsAffected: {info.RecordsAffected}");
        }

        protected internal override void OnCommandException(CommandExecutionInfo info)
        {
        }

        protected internal override void OnDataReaderClosed(DataReaderInfo info)
        {
            Trace.WriteLine($"RowCount: {info.RowCount}");
        }

        //

        protected internal override void OnUnusedObjectDetected(DbObjectInfo info)
        {
            Trace.WriteLine($"Unused object detected {info.Kind} ID: {info.ID}, Creation Method: {GetFullMethodName(info.CreationMethod)}");
        }

        protected internal override void OnDisposeLeakDetected(DbObjectInfo info)
        {
            Trace.WriteLine($"Dispose leak detected {info.Kind} ID: {info.ID}, Creation Method: {GetFullMethodName(info.CreationMethod)}");
        }

        protected internal override void OnInvalidParameterDetected(InvalidParameterDetails details)
        {
            Trace.WriteLine($"Invalid parameter detected: {details.Reason}");
            throw new InvalidParameterException(details);
        }
    }
}
