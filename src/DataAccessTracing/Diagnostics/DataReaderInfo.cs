using System.Diagnostics;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database data reader.
    /// </summary>
    public sealed class DataReaderInfo : DbObjectInfo
    {
        /// <summary>
        /// Gets the identifier of the command execution associated with the data reader.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the number of rows read by the data reader.
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// Gets the duration for which the data reader was open.
        /// </summary>
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
