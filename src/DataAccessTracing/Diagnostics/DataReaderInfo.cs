namespace DataAccessTracing.Diagnostics
{
    public sealed class DataReaderInfo : DbObjectInfo
    {
        public CommandInfo CommandInfo { get; }
        public int RowCount { get; internal set; }

        internal DataReaderInfo(CommandInfo commandInfo) : base(DbObjectKind.DataReader)
        {
            CommandInfo = commandInfo;
        }
    }
}
