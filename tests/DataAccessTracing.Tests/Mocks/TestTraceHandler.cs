using DataAccessTracing.Diagnostics;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestTraceHandler : DataAccessTraceHandler
    {
        public Action<ConnectionInfo>? ConnectionOpened;
        protected internal override void OnConnectionOpened(ConnectionInfo info) => ConnectionOpened!(info);

        public Action<ConnectionInfo>? ConnectionClosed;
        protected internal override void OnConnectionClosed(ConnectionInfo info) => ConnectionClosed!(info);

        public Action<TransactionInfo>? BeginTransaction;
        protected internal override void OnBeginTransaction(TransactionInfo info) => BeginTransaction!(info);

        public Action<TransactionInfo>? CommitTransaction;
        protected internal override void OnCommitTransaction(TransactionInfo info) => CommitTransaction!(info);

        public Action<TransactionInfo>? RollbackTransaction;
        protected internal override void OnRollbackTransaction(TransactionInfo info) => RollbackTransaction!(info);

        public Action<CommandExecutionInfo>? CommandExecuting;
        protected internal override void OnCommandExecuting(CommandExecutionInfo info) => CommandExecuting!(info);

        public Action<CommandExecutionInfo>? CommandExecuted;
        protected internal override void OnCommandExecuted(CommandExecutionInfo info) => CommandExecuted!(info);

        public Action<CommandExecutionInfo>? CommandException;
        protected internal override void OnCommandException(CommandExecutionInfo info) => CommandException!(info);

        public Action<DataReaderInfo>? DataReaderClosed;
        protected internal override void OnDataReaderClosed(DataReaderInfo info) => DataReaderClosed!(info);

        //

        public Action<DbObjectInfo> UnusedObjectDetected = (_) => { };
        protected internal override void OnUnusedObjectDetected(DbObjectInfo info) => UnusedObjectDetected(info);

        public Action<DbObjectInfo>? DisposeLeakDetected;
        protected internal override void OnDisposeLeakDetected(DbObjectInfo info) => DisposeLeakDetected!(info);

        public Action<InvalidParameterDetails>? InvalidParameterDetected;
        protected internal override void OnInvalidParameterDetected(InvalidParameterDetails details) => InvalidParameterDetected!(details);
    }
}
