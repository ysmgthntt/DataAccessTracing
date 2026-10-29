using System.Reflection;

namespace DataAccessTracing.Diagnostics
{
    public abstract class DataAccessTraceHandler
    {
        protected internal virtual void OnConnectionOpened(ConnectionInfo info)
        {
        }

        protected internal virtual void OnConnectionClosed(ConnectionInfo info)
        {
        }

        protected internal virtual void OnBeginTransaction(TransactionInfo info)
        {
        }

        protected internal virtual void OnCommitTransaction(TransactionInfo info)
        {
        }

        protected internal virtual void OnRollbackTransaction(TransactionInfo info)
        {
        }

        protected internal virtual void OnCommandExecuting(CommandExecutionInfo info)
        {
        }

        protected internal virtual void OnCommandExecuted(CommandExecutionInfo info)
        {
        }

        protected internal virtual void OnCommandException(CommandExecutionInfo info)
        {
        }

        protected internal virtual void OnDataReaderClosed(DataReaderInfo info)
        {
        }

        //

        protected internal virtual void OnUnusedObjectDetected(DbObjectInfo info)
        {
        }

        protected internal virtual void OnDisposeLeakDetected(DbObjectInfo info)
        {
        }

        protected internal virtual void OnInvalidParameterDetected(InvalidParameterDetails details)
        {
        }

        //

        protected static string GetFullMethodName(MethodBase? method) => method is null ? "" : $"{method.DeclaringType.FullName}.{method.Name}";
    }
}
