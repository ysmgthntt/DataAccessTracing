using System.Reflection;
using DataAccessTracing;
using DataAccessTracing.Tests;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing_Tests
{
    public class TracingProviderFactoryTest
    {
        [Fact]
        public void Construtor_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("innerFactory", () => new TracingProviderFactory(null!));
            Assert.Throws<ArgumentNullException>("traceHandler", () => new TracingProviderFactory(TestProviderFactory.Instance, null!));
        }

        [Fact]
        public void CreateConnection()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var connection = factory.CreateConnection();
            Assert.IsType<TracingConnection>(connection);
            Assert.IsType<TestConnection>(((TracingConnection?)connection)?.InnerConnection);
            var info = ((TracingConnection?)connection)?.Info;
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateConnection), info?.CreationMethod?.Name);
        }

        [Fact]
        public void CreateCommand()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var command = factory.CreateCommand();
            Assert.IsType<TracingCommand>(command);
            Assert.IsType<TestCommand>(((TracingCommand?)command)?.GetInnerCommand());
            CommandInfo? info = (CommandInfo?)command?.GetType().GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(command);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommand), info?.CreationMethod?.Name);
        }

        [Fact]
        public void CreateDataAdapter()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var dataAdapter = factory.CreateDataAdapter();
            Assert.IsType<TracingDataAdapter>(dataAdapter);
            Assert.IsType<TestDataAdapter>(((TracingDataAdapter?)dataAdapter)?.GetInnerDataAdapter());
            DataAdapterInfo? info = (DataAdapterInfo?)dataAdapter?.GetType().GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(dataAdapter);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateDataAdapter), info?.CreationMethod?.Name);
        }

        [Fact]
        public void CreateCommandBuilder()
        {
            var fiBuilder = typeof(TracingCommandBuilder).GetField("_innerBuilder", BindingFlags.NonPublic | BindingFlags.Instance);
            var fiInfo = typeof(TracingCommandBuilder).GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);

            var testFactory = new TestProviderFactory();
            var factory = new TracingProviderFactory(testFactory);
            var commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder>(commandBuilder);
            var innerBuilder = fiBuilder?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilder>(innerBuilder);
            CommandBuilderInfo? info = (CommandBuilderInfo?)fiInfo?.GetValue(commandBuilder);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommandBuilder), info?.CreationMethod?.Name);

            // cached
            testFactory.WithDeriveParameters = true;
            commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder>(commandBuilder);
            innerBuilder = fiBuilder?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilderWithDeriveParameters>(innerBuilder);
            info = (CommandBuilderInfo?)fiInfo?.GetValue(commandBuilder);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommandBuilder), info?.CreationMethod?.Name);

            factory = new TracingProviderFactory(testFactory);
            commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder<TestProviderFactory>>(commandBuilder);
            innerBuilder = fiBuilder?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilderWithDeriveParameters>(innerBuilder);
            info = (CommandBuilderInfo?)fiInfo?.GetValue(commandBuilder);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommandBuilder), info?.CreationMethod?.Name);
        }

        [Fact]
        public void CreateCommandBuilderGeneric()
        {
            var fiBuilder = typeof(TracingCommandBuilder).GetField("_innerBuilder", BindingFlags.NonPublic | BindingFlags.Instance);
            var fiInfo = typeof(TracingCommandBuilder).GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);

            var factory = TracingProviderFactory<TestProviderFactory>.Instance;
            var commandBuilder = factory.CreateCommandBuilder();
            Assert.IsType<TracingCommandBuilder<TestProviderFactory>>(commandBuilder);
            var innerBuilder = fiBuilder?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilder>(innerBuilder);
            CommandBuilderInfo? info = (CommandBuilderInfo?)fiInfo?.GetValue(commandBuilder);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommandBuilderGeneric), info?.CreationMethod?.Name);
        }
    }
}
