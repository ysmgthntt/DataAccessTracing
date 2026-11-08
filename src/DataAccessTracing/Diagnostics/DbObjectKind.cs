namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Specifies the type of database object.
    /// </summary>
    public enum DbObjectKind
    {
        /// <summary>
        /// A database connection.
        /// </summary>
        Connection,

        /// <summary>
        /// A database transaction.
        /// </summary>
        Transaction,

        /// <summary>
        /// A database command.
        /// </summary>
        Command,

        /// <summary>
        /// A database data reader.
        /// </summary>
        DataReader,

        /// <summary>
        /// A database data adapter.
        /// </summary>
        DataAdapter,

        /// <summary>
        /// A database command builder.
        /// </summary>
        CommandBuilder,
    }
}
