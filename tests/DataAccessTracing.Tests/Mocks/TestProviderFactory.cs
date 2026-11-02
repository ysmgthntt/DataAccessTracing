using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestProviderFactory : DbProviderFactory
    {
        public static readonly TestProviderFactory Instance = new();

        public TestProviderFactory() { }

        public override DbConnection? CreateConnection() => new TestConnection();

        public override DbCommand? CreateCommand() => new TestCommand();

        public override DbDataAdapter CreateDataAdapter() => new TestDataAdapter();

        public bool WithDeriveParameters { get; set; }

        public override DbCommandBuilder? CreateCommandBuilder()
            => WithDeriveParameters ? new TestCommandBuilderWithDeriveParameters() : new TestCommandBuilder();
    }
}
