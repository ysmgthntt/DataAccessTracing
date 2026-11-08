using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides a default implementation of <see cref="DataAccessTraceHandler"/>
    /// that outputs tracing information using <see cref="System.Diagnostics.Trace.WriteLine(string)"/>.
    /// </summary>
    public class DefaultDataAccessTraceHandler : DataAccessTraceHandler
    {
        /// <inheritdoc />
        protected internal override void OnConnectionOpened(ConnectionInfo info)
        {
            Trace.WriteLine($"Connection Opened Id: {info.ConnectionId}");
        }

        /// <inheritdoc />
        protected internal override void OnConnectionClosed(ConnectionInfo info)
        {
            Trace.WriteLine($"Connection Closed Id: {info.ConnectionId}, Duration: {info.Duration}");
        }

        /// <inheritdoc />
        protected internal override void OnBeginTransaction(TransactionInfo info)
        {
            Trace.WriteLine($"Begin Transaction Id: {info.Id}, Connection Id: {info.ConnectionInfo.ConnectionId}");
        }

        /// <inheritdoc />
        protected internal override void OnCommitTransaction(TransactionInfo info)
        {
            Trace.WriteLine($"Commit Transaction Id: {info.Id}, Connection Id: {info.ConnectionInfo.ConnectionId}, Duration: {info.Duration}");
        }

        /// <inheritdoc />
        protected internal override void OnRollbackTransaction(TransactionInfo info)
        {
            Trace.WriteLine($"Rollback Transaction Id: {info.Id}, Connection Id: {info.ConnectionInfo.ConnectionId}, Duration: {info.Duration}");
        }

        /// <inheritdoc />
        protected internal override void OnCommandExecuting(CommandExecutionInfo info)
        {
            var nl = Environment.NewLine;
            string caller = info.CallerMethod is null ? "" : $"Caller: {GetFullMethodName(info.CallerMethod)}, ";
            Trace.WriteLine($"[{info.ExecutionMethodName}]{nl}{info.GetParameterExpandedCommandText()}{nl}{caller}Execution Id: {info.ExecutionId}");
        }

        /// <inheritdoc />
        protected internal override void OnCommandExecuted(CommandExecutionInfo info)
        {
            Trace.WriteLine($"Command Executed: {info.ExecutionMethodName}, Duration: {info.Duration}, RecordsAffected: {info.RecordsAffected}, Execution Id: {info.ExecutionId}");
        }

        /// <inheritdoc />
        protected internal override void OnCommandException(CommandExecutionInfo info)
        {
        }

        /// <inheritdoc />
        protected internal override void OnDataReaderClosed(DataReaderInfo info)
        {
            Trace.WriteLine($"RowCount: {info.RowCount}, Execution Id: {info.ExecutionId}");
        }

        //

        /// <inheritdoc />
        protected internal override void OnUnusedObjectDetected(DbObjectInfo info)
        {
            Trace.WriteLine($"Unused object detected {info.Kind} Id: {info.Id}, Creation Method: {GetFullMethodName(info.CreationMethod)}");
            if (info is CommandInfo { CommandText: not null and not "" } commandInfo)
                Trace.WriteLine($"CommandText: {commandInfo.CommandText}");
        }

        /// <inheritdoc />
        protected internal override void OnDisposeLeakDetected(DbObjectInfo info)
        {
            Trace.WriteLine($"Dispose leak detected {info.Kind} Id: {info.Id}, Creation Method: {GetFullMethodName(info.CreationMethod)}");
        }

        /// <inheritdoc />
        protected internal override void OnInvalidParameterDetected(InvalidParameterDetails details)
        {
            Trace.WriteLine($"Invalid parameter detected: {details.Reason}");
            throw new InvalidParameterException(details);
        }
    }
}
