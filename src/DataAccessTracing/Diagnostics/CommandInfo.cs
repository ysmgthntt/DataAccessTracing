namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database command.
    /// </summary>
    public sealed class CommandInfo : DbObjectInfo
    {
        /// <summary>
        /// Gets the command text.
        /// </summary>
        /// <remarks>
        /// This property is populated only when
        /// <see cref="DataAccessTraceHandler.OnUnusedObjectDetected"/> is invoked.
        /// The value represents the command text at the time the command was detected as unused.
        /// </remarks>
        public string? CommandText { get; internal set; }

        internal CommandInfo() : base(DbObjectKind.Command) { }
    }
}
