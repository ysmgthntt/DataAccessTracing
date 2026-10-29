namespace DataAccessTracing.Diagnostics
{
    public sealed class DataReaderInfo : DbObjectInfo
    {
        public Guid ExecutionID { get; }
        public int RowCount { get; internal set; }

        internal DataReaderInfo(Guid executionID) : base(DbObjectKind.DataReader)
        {
            ExecutionID = executionID;
        }
    }
}
