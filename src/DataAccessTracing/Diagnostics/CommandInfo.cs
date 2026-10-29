namespace DataAccessTracing.Diagnostics
{
    public sealed class CommandInfo : DbObjectInfo
    {
        internal CommandInfo() : base(DbObjectKind.Command) { }
    }
}
