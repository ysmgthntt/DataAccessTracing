using System.Data;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
{
    public class TracingCommandBuilderTest
    {
        [Fact]
        public void GetUpdateCommand()
        {
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            using var da = new TestDataAdapter();
            using var tracingDataAdapter = new TracingDataAdapter(da, traceHandler);
            using var cb = new TestCommandBuilder();
            using var tracingCommandBuilder = new TracingCommandBuilder(cb, traceHandler);

            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Value", typeof(string));
            dt.TableName = "TestTable";
            dt.PrimaryKey = [dt.Columns[0]];
            cmd.ExecuteReaderDataTable = dt;

            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            tracingCommand.Connection = tracingConnection;
            tracingCommand.CommandText = "SELECT Id, Value FROM TestTable";

            tracingDataAdapter.SelectCommand = tracingCommand;
            tracingCommandBuilder.DataAdapter = tracingDataAdapter;

            var updateCommand = tracingCommandBuilder.GetUpdateCommand();
            Assert.NotEmpty(updateCommand.CommandText);
        }

        [Fact]
        public void DisposeTest()
        {
            var traceHandler = new TestTraceHandler();
#pragma warning disable CA2000
            var cb = new TestCommandBuilder();
#pragma warning restore CA2000
            var tracingCommandBuilder = new TracingCommandBuilder(cb, traceHandler);
            Assert.False(cb.IsDisposed);
            tracingCommandBuilder.Dispose();
            Assert.True(cb.IsDisposed);
        }

        [Fact]
        public void DisposeLeak()
        {
            int leakCount = 0;
            void Test()
            {
                Guid id = default;
                var traceHandler = new TestTraceHandler
                {
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.CommandBuilder, info.Kind);
                        Assert.Equal(id, info.Id);
                        Assert.Equal(nameof(TracingCommandBuilderTest), info.CreationMethod?.DeclaringType?.Name);
                        Assert.Equal(nameof(DisposeLeak), info.CreationMethod?.Name);
                        leakCount++;
                    },
                };
#pragma warning disable CA2000
                var cb = new TestCommandBuilder();
                var tracingCommandBuilder = new TracingCommandBuilder(cb, traceHandler);
#pragma warning restore CA2000
                var info = tracingCommandBuilder.GetPrivateFieldValue<CommandBuilderInfo>("_info");
                id = info?.Id ?? default;
                Assert.Equal(0, leakCount);
            }
            Test();
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(1, leakCount);
        }

        [Fact]
        public void Unused()
        {
            int unusedCount = 0;
            Guid id = default;
            var traceHandler = new TestTraceHandler
            {
                UnusedObjectDetected = (info) =>
                {
                    Assert.Equal(DbObjectKind.CommandBuilder, info.Kind);
                    Assert.Equal(id, info.Id);
                    Assert.Equal(nameof(TracingCommandBuilderTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(Unused), info.CreationMethod?.Name);
                    unusedCount++;
                },
            };
#pragma warning disable CA2000
            TracingCommandBuilder CreateTracingCommandBuilder() => new(new TestCommandBuilder(), traceHandler);
#pragma warning restore CA2000
            using (var tracingCommandBuilder = CreateTracingCommandBuilder())
            {
                var info = tracingCommandBuilder.GetPrivateFieldValue<CommandBuilderInfo>("_info");
                id = info?.Id ?? default;
                Assert.Equal(0, unusedCount);
            }
            // no unused check
            Assert.Equal(0, unusedCount);
        }
    }
}
