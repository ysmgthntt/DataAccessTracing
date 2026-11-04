using System.Data.Common;
using System.Reflection;
using DataAccessTracing;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

#pragma warning disable CA2007
#pragma warning disable IDE0017
#pragma warning disable IDE0130

namespace DataAccessTracing_Tests
{
    public class TracingConnectionTest
    {
        const int DelayLowMargin = 10;
        const int DelayHighMargin = 50;

        // for CallerMethod
        private static TracingConnection CreateTracingConnection(DbConnection innerConnection, DataAccessTraceHandler traceHandler, TracingProviderFactory? factory)
            => new(innerConnection, traceHandler, factory);

        [Fact]
        public void Constructor()
        {
            int openCount = 0;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    openCount++;
                    Assert.Equal(DbObjectKind.Connection, info.Kind);
                    Assert.Equal(nameof(TracingConnectionTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(Constructor), info.CreationMethod?.Name);
                },
                ConnectionClosed = (_) => { },
            };
            using var closedConnection = new TestConnection();
            using var c1 = CreateTracingConnection(closedConnection, traceHandler, null);
            Assert.Equal(0, openCount);
            Assert.Equal(closedConnection, c1.InnerConnection);

            using var openedConnection = new TestConnection(true);
            using var c2 = CreateTracingConnection(openedConnection, traceHandler, null);
            Assert.Equal(1, openCount);
            Assert.Equal(openedConnection, c2.InnerConnection);
        }

        [Fact]
#if !NET35
        public async Task OpenClose()
#else
        public void OpenClose()
#endif
        {
            int delay = 1000;
            int openCount = 0, closeCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    if (openCount > 0)
                    {
                        Assert.Equal(id, info.Id);
                        Assert.NotEqual(connectionId, info.ConnectionId);
                    }
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    connectionId = info.ConnectionId;
                    openCount++;
                },
                ConnectionClosed = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    closeCount++;
                },
            };

            using var innerConnection = new TestConnection();
            using var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            Assert.Equal(0, openCount);
            Assert.Equal(0, closeCount);

            for (int i = 0; i < 3; i++)
            {
                delay = 500 + (i * 200);
                tracingConnection.Open();
                Assert.Equal(i + 1, openCount);
                Assert.Equal(i, closeCount);

#if !NET35
                await Task.Delay(delay);
#else
                Thread.Sleep(delay);
#endif

                tracingConnection.Close();
                Assert.Equal(i + 1, openCount);
                Assert.Equal(i + 1, closeCount);
            }
        }

        [Fact]
#if !NET35
        public async Task OpenCloseAsync()
        {
            int delay = 1000;
            int openCount = 0, closeCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    if (openCount > 0)
                    {
                        Assert.Equal(id, info.Id);
                        Assert.NotEqual(connectionId, info.ConnectionId);
                    }
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    connectionId = info.ConnectionId;
                    openCount++;
                },
                ConnectionClosed = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    closeCount++;
                },
            };

            using var innerConnection = new TestConnection();
            using var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            Assert.Equal(0, openCount);
            Assert.Equal(0, closeCount);

            for (int i = 0; i < 3; i++)
            {
                delay = 600 + (i * 200);
                await tracingConnection.OpenAsync();
                Assert.Equal(i + 1, openCount);
                Assert.Equal(i, closeCount);

                await Task.Delay(delay);

#if NET
                await tracingConnection.CloseAsync();
#else
                tracingConnection.Close();
#endif
                Assert.Equal(i + 1, openCount);
                Assert.Equal(i + 1, closeCount);
            }
        }
#else
        public void OpenCloseAsync(){}
#endif

        [Fact]
        public void BeginTransaction()
        {
            int beginTransCount = 0;
            Guid connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (info) =>
                {
                    Assert.Equal(DbObjectKind.Transaction, info.Kind);
                    Assert.Equal(nameof(TracingConnectionTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(BeginTransaction), info.CreationMethod?.Name);
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    beginTransCount++;
                },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            Assert.Equal(0, beginTransCount);
            connectionId = tracingConnection.Info.ConnectionId;
            {
                using var trans = tracingConnection.BeginTransaction();
                Assert.Equal(1, beginTransCount);
                var tracingTrans = Assert.IsType<TracingTransaction>(trans);
                Assert.IsType<TestTransaction>(tracingTrans.InnerTransaction);
                Assert.Equal(System.Data.IsolationLevel.Unspecified, trans.IsolationLevel);
            }
            {
                using var trans = tracingConnection.BeginTransaction(System.Data.IsolationLevel.Snapshot);
                Assert.Equal(2, beginTransCount);
                Assert.Equal(System.Data.IsolationLevel.Snapshot, trans.IsolationLevel);
            }
        }

        [Fact]
#if NET
        public async Task BeginTransactionAsync()
        {
            int beginTransCount = 0;
            Guid connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (info) =>
                {
                    Assert.Equal(DbObjectKind.Transaction, info.Kind);
                    Assert.Equal(nameof(TracingConnectionTest), info.CreationMethod?.DeclaringType?.Name);
                    Assert.Equal(nameof(BeginTransactionAsync), info.CreationMethod?.Name);
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    beginTransCount++;
                },
            };
            await using var c = new TestConnection();
            await using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            await tracingConnection.OpenAsync();
            Assert.Equal(0, beginTransCount);
            connectionId = tracingConnection.Info.ConnectionId;
            {
                await using var trans = await tracingConnection.BeginTransactionAsync();
                Assert.Equal(1, beginTransCount);
                var tracingTrans = Assert.IsType<TracingTransaction>(trans);
                Assert.IsType<TestTransaction>(tracingTrans.InnerTransaction);
                Assert.Equal(System.Data.IsolationLevel.Unspecified, trans.IsolationLevel);
            }
            {
                await using var trans = await tracingConnection.BeginTransactionAsync(System.Data.IsolationLevel.Snapshot);
                Assert.Equal(2, beginTransCount);
                Assert.Equal(System.Data.IsolationLevel.Snapshot, trans.IsolationLevel);
            }
        }
#else
        public void BeginTransactionAsync() { }
#endif

        [Fact]
        public void CreateCommand()
        {
            var traceHandler = new TestTraceHandler();
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            using var command = tracingConnection.CreateCommand();
            var tracingCommand = Assert.IsType<TracingCommand>(command);
            var innerCommand = tracingCommand.GetInnerCommand();
            Assert.IsType<TestCommand>(innerCommand);
            Assert.Equal(c, innerCommand?.Connection);
            CommandInfo? info = (CommandInfo?)tracingCommand.GetType().GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(tracingCommand);
            Assert.Equal(nameof(TracingConnectionTest), info?.CreationMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(CreateCommand), info?.CreationMethod?.Name);
        }

        [Fact]
#if !NET35
        public async Task DisposeTest()
#else
        public void DisposeTest()
#endif
        {
            int delay = 1000;
            int openCount = 0, closeCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    connectionId = info.ConnectionId;
                    openCount++;
                },
                ConnectionClosed = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    closeCount++;
                },
            };

#pragma warning disable CA2000
            var innerConnection = new TestConnection();
#pragma warning restore CA2000
            var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            Assert.Equal(0, openCount);
            Assert.Equal(0, closeCount);

            tracingConnection.Open();
            Assert.Equal(1, openCount);
            Assert.Equal(0, closeCount);

#if !NET35
            await Task.Delay(delay);
#else
            Thread.Sleep(delay);
#endif

            Assert.False(innerConnection.IsDisposed);
            tracingConnection.Dispose();
            Assert.Equal(1, openCount);
            Assert.Equal(1, closeCount);
            Assert.True(innerConnection.IsDisposed);
        }

        [Fact]
#if NET
        public async Task DisposeAsyncTest()
        {
            int delay = 1000;
            int openCount = 0, closeCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (info) =>
                {
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    connectionId = info.ConnectionId;
                    openCount++;
                },
                ConnectionClosed = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    closeCount++;
                },
            };

#pragma warning disable CA2000
            var innerConnection = new TestConnection();
#pragma warning restore CA2000
            var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            Assert.Equal(0, openCount);
            Assert.Equal(0, closeCount);

            await tracingConnection.OpenAsync();
            Assert.Equal(1, openCount);
            Assert.Equal(0, closeCount);

#if !NET35
            await Task.Delay(delay);
#else
            Thread.Sleep(delay);
#endif

            Assert.False(innerConnection.IsDisposed);
            await tracingConnection.DisposeAsync();
            Assert.Equal(1, openCount);
            Assert.Equal(1, closeCount);
            Assert.True(innerConnection.IsDisposed);
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
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.Connection, info.Kind);
                        Assert.Equal(id, info.Id);
                        leakCount++;
                    },
                };
#pragma warning disable CA2000
                var innerConnection = new TestConnection();
                var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
#pragma warning restore CA2000
                id = tracingConnection.Info.Id;
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
                tracingConnection.Close();
                Assert.Equal(0, leakCount);
            }
            Test();
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            Assert.Equal(1, leakCount);
        }
    }
}
