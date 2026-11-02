using System.Reflection;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
{
    public class TracingProviderFactoryTest
    {
        [Fact]
        public void Construtor_ShouldThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new TracingProviderFactory(null!));
            Assert.Equal("innerFactory", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => new TracingProviderFactory(TestProviderFactory.Instance, null!));
            Assert.Equal("traceHandler", ex.ParamName);
        }

        [Fact]
        public void CreateConnection()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var connection = factory.CreateConnection();
            Assert.IsType<TracingConnection>(connection);
            Assert.IsType<TestConnection>(((TracingConnection?)connection)?.InnerConnection);
        }

        [Fact]
        public void CreateCommand()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var command = factory.CreateCommand();
            Assert.IsType<TracingCommand>(command);
            Assert.IsType<TestCommand>(((TracingCommand?)command)?.GetInnerCommand());
        }

        [Fact]
        public void CreateDataAdapter()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var dataAdapter = factory.CreateDataAdapter();
            Assert.IsType<TracingDataAdapter>(dataAdapter);
            Assert.IsType<TestDataAdapter>(((TracingDataAdapter?)dataAdapter)?.GetInnerDataAdapter());
        }

        [Fact]
        public void CreateCommandBuilder()
        {
            var fi = typeof(TracingCommandBuilder).GetField("_innerBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

            var testFactory = new TestProviderFactory();
            var factory = new TracingProviderFactory(testFactory);
            var commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder>(commandBuilder);
            var innerBuilder = fi?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilder>(innerBuilder);

            // cached
            testFactory.WithDeriveParameters = true;
            commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder>(commandBuilder);
            innerBuilder = fi?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilderWithDeriveParameters>(innerBuilder);

            factory = new TracingProviderFactory(testFactory);
            commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder<TestProviderFactory>>(commandBuilder);
            innerBuilder = fi?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilderWithDeriveParameters>(innerBuilder);
        }

        [Fact]
        public void CreateCommandBuilderGeneric()
        {
            var fi = typeof(TracingCommandBuilder).GetField("_innerBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

            var factory = TracingProviderFactory<TestProviderFactory>.Instance;
            var commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder<TestProviderFactory>>(commandBuilder);
            var innerBuilder = fi?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilder>(innerBuilder);
        }
    }
}
