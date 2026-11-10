using System.Data;
using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestDataAdapter : DbDataAdapter
    {
        public bool IsDisposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;

            base.Dispose(disposing);
        }

        private static readonly object s_rowUpdatingEvent = new();

        public event TestRowUpdatingEventHandler RowUpdating
        {
            add => Events.AddHandler(s_rowUpdatingEvent, value);
            remove => Events.RemoveHandler(s_rowUpdatingEvent, value);
        }

        private static readonly object s_rowUpdatedEvent = new();

        public event TestRowUpdatedEventHandler RowUpdated
        {
            add => Events.AddHandler(s_rowUpdatedEvent, value);
            remove => Events.RemoveHandler(s_rowUpdatedEvent, value);
        }

        public void RaiseRowUpdatingEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        {
            var handler = (TestRowUpdatingEventHandler?)Events[s_rowUpdatingEvent];
            handler?.Invoke(this, new TestRowUpdatingEventArgs(dataRow, command, statementType, tableMapping));
        }

        public void RaiseRowUpdatedEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        {
            var handler = (TestRowUpdatedEventHandler?)Events[s_rowUpdatedEvent];
            handler?.Invoke(this, new TestRowUpdatedEventArgs(dataRow, command, statementType, tableMapping));
        }
    }

    internal sealed class TestRowUpdatingEventArgs(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        : RowUpdatingEventArgs(dataRow, command, statementType, tableMapping);

    internal sealed class TestRowUpdatedEventArgs(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        : RowUpdatedEventArgs(dataRow, command, statementType, tableMapping);

    internal delegate void TestRowUpdatingEventHandler(object sender, TestRowUpdatingEventArgs e);
    internal delegate void TestRowUpdatedEventHandler(object sender, TestRowUpdatedEventArgs e);
}
