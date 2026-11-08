namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database command builder.
    /// </summary>
    public sealed class CommandBuilderInfo : DbObjectInfo
    {
        internal CommandBuilderInfo() : base(DbObjectKind.CommandBuilder) { }
    }
}
