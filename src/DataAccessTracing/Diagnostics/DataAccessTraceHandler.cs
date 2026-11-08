using System.Reflection;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides methods for handling database access tracing events.
    /// </summary>
    /// <remarks>
    /// Derive from this class and override the methods for the events you want to handle.
    /// </remarks>
    public abstract class DataAccessTraceHandler
    {
        /// <summary>
        /// Called when a database connection is opened.
        /// </summary>
        /// <param name="info">Information about the connection.</param>
        protected internal virtual void OnConnectionOpened(ConnectionInfo info)
        {
        }

        /// <summary>
        /// Called when a database connection is closed.
        /// </summary>
        /// <param name="info">Information about the connection.</param>
        protected internal virtual void OnConnectionClosed(ConnectionInfo info)
        {
        }

        /// <summary>
        /// Called when a database transaction begins.
        /// </summary>
        /// <param name="info">Information about the transaction.</param>
        protected internal virtual void OnBeginTransaction(TransactionInfo info)
        {
        }

        /// <summary>
        /// Called when a database transaction is committed.
        /// </summary>
        /// <param name="info">Information about the transaction.</param>
        protected internal virtual void OnCommitTransaction(TransactionInfo info)
        {
        }

        /// <summary>
        /// Called when a database transaction is rolled back.
        /// </summary>
        /// <param name="info">Information about the transaction.</param>
        protected internal virtual void OnRollbackTransaction(TransactionInfo info)
        {
        }

        /// <summary>
        /// Called immediately before a database command is executed.
        /// </summary>
        /// <param name="info">Information about the command execution.</param>
        protected internal virtual void OnCommandExecuting(CommandExecutionInfo info)
        {
        }

        /// <summary>
        /// Called after a database command has been executed successfully.
        /// </summary>
        /// <param name="info">Information about the command execution.</param>
        protected internal virtual void OnCommandExecuted(CommandExecutionInfo info)
        {
        }

        /// <summary>
        /// Called when an exception occurs during command execution.
        /// </summary>
        /// <param name="info">Information about the command execution.</param>
        protected internal virtual void OnCommandException(CommandExecutionInfo info)
        {
        }

        /// <summary>
        /// Called when a data reader is closed or disposed.
        /// </summary>
        /// <param name="info">Information about the data reader.</param>
        protected internal virtual void OnDataReaderClosed(DataReaderInfo info)
        {
        }

        //

        /// <summary>
        /// Called when a database object is disposed without ever having been used.
        /// </summary>
        /// <param name="info">Information about the unused database object.</param>
        protected internal virtual void OnUnusedObjectDetected(DbObjectInfo info)
        {
        }

        /// <summary>
        /// Called when a database object that was not properly disposed is detected by its finalizer.
        /// </summary>
        /// <param name="info">Information about the database object.</param>
        protected internal virtual void OnDisposeLeakDetected(DbObjectInfo info)
        {
        }

        /// <summary>
        /// Called when an invalid command parameter is detected.
        /// </summary>
        /// <param name="details">Details about the invalid parameter.</param>
        /// <remarks>
        /// This method is called only when parameter validation is enabled.
        /// </remarks>
        protected internal virtual void OnInvalidParameterDetected(InvalidParameterDetails details)
        {
        }

        //

        /// <summary>
        /// Gets the method name formatted for tracing and diagnostic purposes.
        /// </summary>
        /// <param name="method">The method whose name is to be formatted.</param>
        /// <returns>
        /// The formatted method name, or the method name if its declaring type
        /// cannot be determined. Returns an empty string if <paramref name="method"/>
        /// is <see langword="null"/>.
        /// </returns>
        protected static string GetFullMethodName(MethodBase? method)
            => method switch
            {
                null => "",
                { DeclaringType: null or { FullName: null or "" } } => method.Name,
                _ => $"{method.DeclaringType.FullName}.{method.Name}"
            };
    }
}
