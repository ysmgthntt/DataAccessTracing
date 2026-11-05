using System.Data;
using System.Data.Common;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

#pragma warning disable CA2007

namespace DataAccessTracing.Tests
{
    public class TracingCommandTest
    {
        const int DelayLowMargin = 10;
        const int DelayHighMargin = 50;

        [Fact]
        public void SetTracingConnection()
        {
            var traceHandler = new TestTraceHandler();
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            Assert.Null(tracingCommand.Connection);
            Assert.Null(cmd.Connection);
            tracingCommand.Connection = tracingConnection;
            Assert.Equal(tracingConnection, tracingCommand.Connection);
            Assert.Equal(cnn, cmd.Connection);
        }

        [Fact]
        public void SetConnection()
        {
            var traceHandler = new TestTraceHandler();
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            using var cnn = new TestConnection();
            Assert.Null(tracingCommand.Connection);
            Assert.Null(cmd.Connection);
            tracingCommand.Connection = cnn;
            using var tracingConnection = Assert.IsType<TracingConnection>(tracingCommand.Connection);
            Assert.Equal(cnn, tracingConnection.InnerConnection);
            Assert.Equal(cnn, cmd.Connection);
            var th = tracingCommand.GetPrivateFieldValue<DataAccessTraceHandler>("_traceHandler");
            Assert.Equal(traceHandler, th);
            var factory = tracingConnection.GetPrivateFieldValue<TracingProviderFactory>("_factory");
            Assert.NotNull(factory);
        }

        [Fact]
        public void SetTransaction()
        {
            var traceHandler = new TestTraceHandler
            {
                BeginTransaction = (info) => { },
            };
            using var cmd = new TestCommand();
            using var tracingCommand = new TracingCommand(cmd, traceHandler, null, false);
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var trans = new TestTransaction(cnn, IsolationLevel.Chaos);
            using var tracingTrans = new TracingTransaction(trans, traceHandler, tracingConnection);
            Assert.Null(tracingCommand.Transaction);
            Assert.Null(cmd.Transaction);
            tracingCommand.Transaction = trans;
            Assert.Null(tracingCommand.Transaction);
            Assert.Null(cmd.Transaction);
            tracingCommand.Transaction = tracingTrans;
            Assert.Equal(tracingTrans, tracingCommand.Transaction);
            Assert.Equal(trans, cmd.Transaction);
        }

        private const string TestCommandText = "SELECT @P1, @P2 ";

        private static TracingCommand CreateCommandForTest(TracingConnection tracingConnection)
        {
            var tracingCommand = (TracingCommand)tracingConnection.CreateCommand();
            tracingCommand.CommandText = TestCommandText;
            tracingCommand.CommandType = CommandType.Text;
            tracingCommand.Parameters.Add(new TestParameter { ParameterName = "@P1", Value = "value1" });
            tracingCommand.Parameters.Add(new TestParameter { ParameterName = "@P2", Value = "value2" });
            return tracingCommand;
        }

        private static void CheckCommandExecutionInfo(CommandExecutionInfo info)
        {
            Assert.Equal(CommandType.Text, info.CommandType);
            Assert.Equal(TestCommandText, info.CommandText);

            Assert.Equal("SELECT 'value1', 'value2' ", info.GetParameterExpandedCommandText());
            var parameters = info.GetParameters().ToList();
            Assert.Equal(2, parameters.Count);
            Assert.Equal("@P1", parameters[0].ParameterName);
            Assert.Equal("value1", parameters[0].Value);
            Assert.Equal("@P2", parameters[1].ParameterName);
            Assert.Equal("value2", parameters[1].Value);
        }

        private static void CheckExecutingInfo(CommandExecutionInfo info, Guid id, Guid executionId, Guid connectionId, string executionMethodName)
        {
            Assert.Equal(DbObjectKind.Command, info.CommandInfo.Kind);
            Assert.Equal(id, info.CommandInfo.Id);
            Assert.Equal(connectionId, info.ConnectionInfo?.ConnectionId);
            Assert.NotEqual(executionId, info.ExecutionId);
            Assert.Equal(nameof(TracingCommandTest), info.CommandInfo.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommandForTest), info.CommandInfo.CreationMethod?.Name);
            Assert.Equal(executionMethodName, info.ExecutionMethodName);

            CheckCommandExecutionInfo(info);

            Assert.Equal(TimeSpan.Zero, info.Duration);
            Assert.Equal(0, info.RecordsAffected);
            Assert.Null(info.Exception);
        }

        private static void CheckExecutedInfo(CommandExecutionInfo info, Guid id, Guid executionId, Guid connectionId, int delay, int recordsAffected)
        {
            Assert.Equal(id, info.CommandInfo.Id);
            Assert.Equal(executionId, info.ExecutionId);
            Assert.Equal(connectionId, info.ConnectionInfo?.ConnectionId);
            Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
            Thread.Sleep(DelayLowMargin + DelayHighMargin);
            Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
            Assert.Equal(recordsAffected, info.RecordsAffected);
            Assert.Null(info.Exception);
        }

        [Fact]
        public void ExecuteNonQueryTest()
        {
            int delay = 500;
            int executingCount = 0, executedCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            int recordsAffected = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    CheckExecutingInfo(info, id, executionId, connectionId, "ExecuteNonQuery");
                    Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(ExecuteNonQueryTest), info.CallerMethod?.Name);

                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    CheckExecutedInfo(info, id, executionId, connectionId, delay, recordsAffected);
                    executedCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, executedCount);
            for (int i = 1; i <= 3; i++)
            {
                cmd.ExecuteNonQueryResult = recordsAffected = 42 * i;
                var result = tracingCommand.ExecuteNonQuery();
                Assert.Equal(i, executingCount);
                Assert.Equal(i, executedCount);
                Assert.Equal(recordsAffected, result);
            }
        }

        [Fact]
#if !NET35
        public async Task ExecuteNonQueryAsyncTest()
        {
            int delay = 600;
            int executingCount = 0, executedCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            int recordsAffected = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    CheckExecutingInfo(info, id, executionId, connectionId, "ExecuteNonQueryAsync");
                    //Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    //Assert.Equal(nameof(ExecuteNonQueryAsyncTest), info.CallerMethod?.Name);

                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    CheckExecutedInfo(info, id, executionId, connectionId, delay, recordsAffected);
                    executedCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            await tracingConnection.OpenAsync(TestContext.Current.CancellationToken);
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, executedCount);
            for (int i = 1; i <= 3; i++)
            {
                cmd.ExecuteNonQueryResult = recordsAffected = 42 * i;
                var result = await tracingCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
                Assert.Equal(i, executingCount);
                Assert.Equal(i, executedCount);
                Assert.Equal(recordsAffected, result);
            }
        }
#else
        public void ExecuteNonQueryAsyncTest() { }
#endif

        [Fact]
        public void ExecuteScalarTest()
        {
            int delay = 700;
            int executingCount = 0, executedCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            object? resultValue;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    CheckExecutingInfo(info, id, executionId, connectionId, "ExecuteScalar");
                    Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(ExecuteScalarTest), info.CallerMethod?.Name);

                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    CheckExecutedInfo(info, id, executionId, connectionId, delay, 0);
                    executedCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, executedCount);
            for (int i = 1; i <= 3; i++)
            {
                cmd.ExecuteScalarResult = resultValue = new object();
                var result = tracingCommand.ExecuteScalar();
                Assert.Equal(i, executingCount);
                Assert.Equal(i, executedCount);
                Assert.Equal(resultValue, result);
            }
        }

        [Fact]
#if !NET35
        public async Task ExecuteScalarAsyncTest()
        {
            int delay = 800;
            int executingCount = 0, executedCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            object? resultValue;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    CheckExecutingInfo(info, id, executionId, connectionId, "ExecuteScalarAsync");
                    //Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    //Assert.Equal(nameof(ExecuteScalarAsyncTest), info.CallerMethod?.Name);

                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    CheckExecutedInfo(info, id, executionId, connectionId, delay, 0);
                    executedCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            await tracingConnection.OpenAsync(TestContext.Current.CancellationToken);
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, executedCount);
            for (int i = 1; i <= 3; i++)
            {
                cmd.ExecuteScalarResult = resultValue = new object();
                var result = await tracingCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken);
                Assert.Equal(i, executingCount);
                Assert.Equal(i, executedCount);
                Assert.Equal(resultValue, result);
            }
        }
#else
        public void ExecuteScalarAsyncTest() { }
#endif

        [Fact]
        public void ExecuteReaderTest()
        {
            int delay = 900;
            int executingCount = 0, executedCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    CheckExecutingInfo(info, id, executionId, connectionId, "ExecuteDbDataReader");
                    Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(ExecuteReaderTest), info.CallerMethod?.Name);

                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    CheckExecutedInfo(info, id, executionId, connectionId, delay, 0);
                    executedCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, executedCount);
            for (int i = 1; i <= 3; i++)
            {
                var result = tracingCommand.ExecuteReader();
                Assert.Equal(i, executingCount);
                Assert.Equal(i, executedCount);
                var tracingDataReader = Assert.IsType<TracingDataReader>(result);
                var innerDataReader = tracingDataReader.GetPrivateFieldValue<DbDataReader>("_innerReader");
                Assert.IsType<TestDataReader>(innerDataReader);
            }
        }

        [Fact]
#if !NET35
        public async Task ExecuteReaderAsyncTest()
        {
            int delay = 1000;
            int executingCount = 0, executedCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    CheckExecutingInfo(info, id, executionId, connectionId, "ExecuteDbDataReaderAsync");
                    //Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    //Assert.Equal(nameof(ExecuteReaderAsyncTest), info.CallerMethod?.Name);

                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandExecuted = (info) =>
                {
                    CheckExecutedInfo(info, id, executionId, connectionId, delay, 0);
                    executedCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            await tracingConnection.OpenAsync(TestContext.Current.CancellationToken);
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, executedCount);
            for (int i = 1; i <= 3; i++)
            {
                var result = await tracingCommand.ExecuteReaderAsync(TestContext.Current.CancellationToken);
                Assert.Equal(i, executingCount);
                Assert.Equal(i, executedCount);
                var tracingDataReader = Assert.IsType<TracingDataReader>(result);
                var innerDataReader = tracingDataReader.GetPrivateFieldValue<DbDataReader>("_innerReader");
                Assert.IsType<TestDataReader>(innerDataReader);
            }
        }
#else
        public void ExecuteReaderAsyncTest() { }
#endif

        private static void ExecuteForExceptionTest<TResult>(Func<TResult> func) => func();
        private static async Task ExecuteForExceptionTest<TResult>(Func<Task<TResult>> func) => await func();

        [Fact]
#if NET35
        public void CommandExceptionTest()
#else
        public async Task CommandExceptionTest()
#endif
        {
            int delay = 100;
            int executingCount = 0, exceptionCount = 0;
            Guid id = default, executionId = default, connectionId = default;
            Exception? exception = null;
            string? executionMethodName = null;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    connectionId = info.ConnectionId;
                },
                ConnectionClosed = (_) => { },
                CommandExecuting = (info) =>
                {
                    executionId = info.ExecutionId;
                    executingCount++;
                },
                CommandException = (info) =>
                {
                    Assert.Equal(DbObjectKind.Command, info.CommandInfo.Kind);
                    Assert.Equal(id, info.CommandInfo.Id);
                    Assert.Equal(executionId, info.ExecutionId);
                    Assert.Equal(connectionId, info.ConnectionInfo?.ConnectionId);

                    Assert.Equal(nameof(TracingCommandTest), info.CommandInfo.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(CreateCommandForTest), info.CommandInfo.CreationMethod?.Name);
                    Assert.Equal(nameof(TracingCommandTest), info.CallerMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(ExecuteForExceptionTest), info.CallerMethod?.Name);
                    Assert.Equal(executionMethodName, info.ExecutionMethodName);

                    CheckCommandExecutionInfo(info);

                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Assert.Equal(0, info.RecordsAffected);

                    Assert.Equal(exception, info.Exception);

                    exceptionCount++;
                },
            };
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            using var tracingCommand = CreateCommandForTest(tracingConnection);
            var cmd = (TestCommand)tracingCommand.GetInnerCommand();
            id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            cmd.ExecutionDelay = delay;

            Assert.Equal(0, executingCount);
            Assert.Equal(0, exceptionCount);
            executionMethodName = "ExecuteNonQuery";
            cmd.ThrowException = exception = new Exception(executionMethodName);
            Assert.Throws<Exception>(() => ExecuteForExceptionTest(tracingCommand.ExecuteNonQuery));
            Assert.Equal(1, executingCount);
            Assert.Equal(1, exceptionCount);
            executionMethodName = "ExecuteScalar";
            cmd.ThrowException = exception = new Exception(executionMethodName);
            Assert.Throws<Exception>(() => ExecuteForExceptionTest(tracingCommand.ExecuteScalar));
            Assert.Equal(2, executingCount);
            Assert.Equal(2, exceptionCount);
            executionMethodName = "ExecuteDbDataReader";
            cmd.ThrowException = exception = new Exception(executionMethodName);
            Assert.Throws<Exception>(() => ExecuteForExceptionTest(tracingCommand.ExecuteReader));
            Assert.Equal(3, executingCount);
            Assert.Equal(3, exceptionCount);

#if !NET35
            executionMethodName = "ExecuteNonQueryAsync";
            cmd.ThrowException = exception = new Exception(executionMethodName);
            await Assert.ThrowsAsync<Exception>(async () => await ExecuteForExceptionTest(tracingCommand.ExecuteNonQueryAsync));
            Assert.Equal(4, executingCount);
            Assert.Equal(4, exceptionCount);
            executionMethodName = "ExecuteScalarAsync";
            cmd.ThrowException = exception = new Exception(executionMethodName);
            await Assert.ThrowsAsync<Exception>(async () => await ExecuteForExceptionTest(tracingCommand.ExecuteScalarAsync));
            Assert.Equal(5, executingCount);
            Assert.Equal(5, exceptionCount);
            executionMethodName = "ExecuteDbDataReaderAsync";
            cmd.ThrowException = exception = new Exception(executionMethodName);
            await Assert.ThrowsAsync<Exception>(async () => await ExecuteForExceptionTest(tracingCommand.ExecuteReaderAsync));
            Assert.Equal(6, executingCount);
            Assert.Equal(6, exceptionCount);
#endif
        }

        [Fact]
        public void DisposeTest()
        {
            var traceHandler = new TestTraceHandler();
#pragma warning disable CA2000
            var innerCommand = new TestCommand();
#pragma warning restore CA2000
            var tracingCommand = new TracingCommand(innerCommand, traceHandler, null, false);
            using var cnn = new TestConnection();
            using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            Assert.False(innerCommand.IsDisposed);
            tracingCommand.Dispose();
            Assert.True(innerCommand.IsDisposed);
        }

        [Fact]
#if NET
        public async Task DisposeAsyncTest()
        {
            var traceHandler = new TestTraceHandler();
#pragma warning disable CA2000
            var innerCommand = new TestCommand();
#pragma warning restore CA2000
            var tracingCommand = new TracingCommand(innerCommand, traceHandler, null, false);
            await using var cnn = new TestConnection();
            await using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
            Assert.False(innerCommand.IsDisposed);
            await tracingCommand.DisposeAsync();
            Assert.True(innerCommand.IsDisposed);
        }
#else
        public void DisposeAsyncTest() { }
#endif

        [Fact]
        public void DisposeLeak()
        {
            int leakCount = 0;
            void Test()
            {
                Guid id = default;
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    CommandExecuting = (_) => { },
                    CommandExecuted = (_) => { },
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.Command, info.Kind);
                        Assert.Equal(id, info.Id);
                        Assert.Equal(nameof(TracingCommandTest), info.CreationMethod?.DeclaringType?.Name);
                        Assert.Equal(nameof(DisposeLeak), info.CreationMethod?.Name);
                        leakCount++;
                    },
                };
#pragma warning disable CA2000
                var innerCommand = new TestCommand();
                var tracingCommand = new TracingCommand(innerCommand, traceHandler, null, false);
#pragma warning restore CA2000
                id = tracingCommand.GetPrivateFieldValue<CommandInfo>("_info")?.Id ?? default;
                using var cnn = new TestConnection();
                using var tracingConnection = new TracingConnection(cnn, traceHandler, null);
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                tracingCommand.Connection = tracingConnection;
                tracingCommand.ExecuteNonQuery();
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
            static TracingConnection CreateOpenConnection(DataAccessTraceHandler traceHandler)
            {
#pragma warning disable CA2000
                var innerConnection = new TestConnection();
#pragma warning restore CA2000
                var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                return tracingConnection;
            }

            DbObjectKind kind = DbObjectKind.CommandBuilder;
            int unusedCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    Assert.Equal(kind, info.Kind);
                    Assert.Equal(nameof(TracingCommandTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(Unused), info.CreationMethod?.Name);
                    unusedCount++;
                },
            };
            {
                var tracingConnection = CreateOpenConnection(traceHandler);
                var trans = tracingConnection.BeginTransaction();
                var tracingCommand = tracingConnection.CreateCommand();
                tracingCommand.Transaction = trans;

                tracingCommand.ExecuteNonQuery();

                Assert.Equal(0, unusedCount);
                tracingCommand.Dispose();
                Assert.Equal(0, unusedCount);
                trans.Dispose();
                Assert.Equal(0, unusedCount);
                tracingConnection.Dispose();
                Assert.Equal(0, unusedCount);
            }
            {
                var tracingConnection = CreateOpenConnection(traceHandler);
                var trans = tracingConnection.BeginTransaction();
                var tracingCommand = tracingConnection.CreateCommand();
                tracingCommand.Transaction = trans;

                Assert.Equal(0, unusedCount);
                kind = DbObjectKind.Command;
                tracingCommand.Dispose();
                Assert.Equal(1, unusedCount);
                kind = DbObjectKind.Transaction;
                trans.Dispose();
                Assert.Equal(2, unusedCount);
                kind = DbObjectKind.Connection;
                tracingConnection.Dispose();
                Assert.Equal(3, unusedCount);
            }
        }

        [Fact]
        public void UnusedWithLoop()
        {
            static TracingConnection CreateOpenConnection(DataAccessTraceHandler traceHandler)
            {
#pragma warning disable CA2000
                var innerConnection = new TestConnection();
#pragma warning restore CA2000
                var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                return tracingConnection;
            }

            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
                CommandExecuting = (_) => { },
                CommandExecuted = (_) => { },
                UnusedObjectDetected = (info) =>
                {
                    Assert.Fail();
                },
            };

            var tracingConnection = CreateOpenConnection(traceHandler);
            var trans = tracingConnection.BeginTransaction();

            var insertCommand = CreateCommandForTest(tracingConnection);
            var updateCommand = CreateCommandForTest(tracingConnection);
            var deleteCommand = CreateCommandForTest(tracingConnection);
            insertCommand.Transaction = trans;
            updateCommand.Transaction = trans;
            deleteCommand.Transaction = trans;

            List<int> updateTargetList = [1];
            foreach (var data in updateTargetList)
            {
                switch (data)
                {
                    case 0:
                        insertCommand.ExecuteNonQuery();
                        Assert.Fail();
                        break;
                    case 1:
                        updateCommand.ExecuteNonQuery();
                        break;
                    case 2:
                        deleteCommand.ExecuteNonQuery();
                        Assert.Fail();
                        break;
                }
            }

            insertCommand.Dispose();
            updateCommand.Dispose();
            deleteCommand.Dispose();
            trans.Dispose();
            tracingConnection.Dispose();
        }
    }
}
