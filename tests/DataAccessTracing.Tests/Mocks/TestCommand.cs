using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

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

        public override int ExecuteNonQuery()
        {
            Validate();
            return 0;
        }

        public override object ExecuteScalar()
        {
            Validate();
            return null!;
        }

        public override void Prepare()
        { }

        protected override DbParameter CreateDbParameter() => new TestParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Validate();
            return null!;
        }

        //

        void Validate()
        {
            if (Connection == null || Connection.State != ConnectionState.Open)
                throw new InvalidOperationException();
            //(Transaction as TestTransaction)?.CheckAvailable();
        }
    }
}
