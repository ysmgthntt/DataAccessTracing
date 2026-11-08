using System.Data.Common;
using System.Reflection;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
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
            var tracingConnection = Assert.IsType<TracingConnection>(connection);
            Assert.IsType<TestConnection>(tracingConnection.InnerConnection);
            var info = tracingConnection.Info;
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateConnection), info?.CreationMethod?.Name);
        }

        [Fact]
        public void CreateCommand()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var command = factory.CreateCommand();
            var tracingCommand = Assert.IsType<TracingCommand>(command);
            Assert.IsType<TestCommand>(tracingCommand.GetInnerCommand());
            var info = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info");
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommand), info?.CreationMethod?.Name);
        }

        [Fact]
        public void CreateDataAdapter()
        {
            var factory = new TracingProviderFactory(TestProviderFactory.Instance);
            var dataAdapter = factory.CreateDataAdapter();
            var tracingDataAdapter = Assert.IsType<TracingDataAdapter>(dataAdapter);
            Assert.IsType<TestDataAdapter>(tracingDataAdapter.GetInnerDataAdapter());
            var info = tracingDataAdapter.GetPrivateFieldValue<DataAdapterInfo>("_info");
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

        private static readonly object s_genericFactoryLock = new();

        [Fact]
        public void CreateCommandBuilderGeneric()
        {
            var fiBuilder = typeof(TracingCommandBuilder).GetField("_innerBuilder", BindingFlags.NonPublic | BindingFlags.Instance);
            var fiInfo = typeof(TracingCommandBuilder).GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);

            var factory = TracingProviderFactory<TestProviderFactory>.Instance;
            DbCommandBuilder? commandBuilder;
            lock (s_genericFactoryLock)
            {
                SetGenericTestProviderFactoryWithDeriveParameters(false);
                commandBuilder = factory.CreateCommandBuilder();
            }
            Assert.IsType<TracingCommandBuilder<TestProviderFactory>>(commandBuilder);
            var innerBuilder = fiBuilder?.GetValue(commandBuilder);
            Assert.IsType<TestCommandBuilder>(innerBuilder);
            CommandBuilderInfo? info = (CommandBuilderInfo?)fiInfo?.GetValue(commandBuilder);
            Assert.Equal(nameof(TracingProviderFactoryTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommandBuilderGeneric), info?.CreationMethod?.Name);
        }

        [Fact]
        public void DeriveParameters()
        {
            var traceHandler = new TestTraceHandler();
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            tracingCommand.CommandText = "test";
            lock (s_genericFactoryLock)
            {
                SetGenericTestProviderFactoryWithDeriveParameters(true);
                TracingCommandBuilder<TestProviderFactory>.DeriveParameters(tracingCommand);
            }
            Assert.Equal("DeriveParametersTest", tracingCommand.CommandText);
            TracingCommandBuilder<TestProviderFactory>.DeriveParameters(tracingCommand);
            Assert.Equal("DeriveParametersTest", tracingCommand.CommandText);
        }

        private static void SetGenericTestProviderFactoryWithDeriveParameters(bool enableDeriveParameters)
        {
            var factory = TracingProviderFactory<TestProviderFactory>.Instance;
            var innerFactory = (TestProviderFactory)factory.InnerFactory;
            innerFactory.WithDeriveParameters = enableDeriveParameters;
            var fi = typeof(TracingProviderFactory).GetField("_noDeriveParametersMethod", BindingFlags.NonPublic | BindingFlags.Instance);
            bool? buulNull = null;
            fi?.SetValue(factory, buulNull);
        }
    }
}
