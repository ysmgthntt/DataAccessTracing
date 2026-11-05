using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA2007

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestCommand : DbCommand
    {
#if NET
        [AllowNull]
#endif
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new TestParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        { }

        public int ExecutionDelay;
        public Exception? ThrowException;

        public int ExecuteNonQueryResult;

        public override int ExecuteNonQuery()
        {
            Validate();
            Thread.Sleep(ExecutionDelay);
            if (ThrowException is not null)
                throw ThrowException;
            return ExecuteNonQueryResult;
        }

        public object ExecuteScalarResult = null!;

        public override object ExecuteScalar()
        {
            Validate();
            Thread.Sleep(ExecutionDelay);
            if (ThrowException is not null)
                throw ThrowException;
            return ExecuteScalarResult;
        }

        public override void Prepare()
        { }

        protected override DbParameter CreateDbParameter() => new TestParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Validate();
            Thread.Sleep(ExecutionDelay);
            if (ThrowException is not null)
                throw ThrowException;
            return new TestDataReader();
        }

#if !NET35
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            Validate();
            await Task.Delay(ExecutionDelay, TestContext.Current.CancellationToken);
            if (ThrowException is not null)
                throw ThrowException;
            return ExecuteNonQueryResult;
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            Validate();
            await Task.Delay(ExecutionDelay, TestContext.Current.CancellationToken);
            if (ThrowException is not null)
                throw ThrowException;
            return ExecuteScalarResult;
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            Validate();
            await Task.Delay(ExecutionDelay, TestContext.Current.CancellationToken);
            if (ThrowException is not null)
                throw ThrowException;
            return new TestDataReader();
        }
#endif

        //

        public bool IsDisposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;

            base.Dispose(disposing);
        }

        //

        void Validate()
        {
            if (Connection is not TestConnection || Connection.State != ConnectionState.Open)
                throw new InvalidOperationException();
            (Transaction as TestTransaction)?.CheckAvailable();
        }
    }
}
