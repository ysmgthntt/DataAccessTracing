namespace DataAccessTracing.Diagnostics
{
    public sealed class CommandInfo : DbObjectInfo
    {
        public string? CommandText { get; internal set; }

        internal CommandInfo() : base(DbObjectKind.Command) { }
    }
}
