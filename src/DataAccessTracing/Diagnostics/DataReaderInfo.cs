namespace DataAccessTracing.Diagnostics
{
    public sealed class DataReaderInfo : DbObjectInfo
    {
        public Guid ExecutionId { get; }
        public int RowCount { get; internal set; }

        internal DataReaderInfo(Guid executionId) : base(DbObjectKind.DataReader)
        {
            ExecutionId = executionId;
        }
    }
}
