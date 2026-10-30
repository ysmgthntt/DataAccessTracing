using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    public sealed class DataReaderInfo : DbObjectInfo
    {
        public Guid ExecutionId { get; }
        public int RowCount { get; private set; }
        public TimeSpan Duration => _sw.Elapsed;

        private readonly Stopwatch _sw;

        internal DataReaderInfo(Guid executionId) : base(DbObjectKind.DataReader)
        {
            ExecutionId = executionId;
            _sw = Stopwatch.StartNew();
        }

        internal void Close(int rowCount)
        {
            _sw.Stop();
            RowCount = rowCount;
        }
    }
}
