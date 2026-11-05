using System.Data;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

#pragma warning disable CA2007

namespace DataAccessTracing.Tests
{
    public class TracingTransactionTest
    {
        const int DelayLowMargin = 10;
        const int DelayHighMargin = 50;

        [Fact]
#if NETFRAMEWORK
        public void Constructor()
        {
            var traceHandler = new TestTraceHandler
            {
                BeginTransaction = (_) => { },
            };
            using var connection = new TestConnection();
            using var tracingConnection = new TracingConnection(connection, traceHandler, null);
            using var oracleTrans = new System.Data.OracleClient.OracleTransaction(connection, IsolationLevel.Unspecified);
            using var tracingTrans = new TracingTransaction(oracleTrans, traceHandler, tracingConnection);
            Assert.Equal(IsolationLevel.ReadCommitted, tracingTrans.Info.IsolationLevel);
        }
#else
        public void Constructor() { }
#endif

        [Fact]
#if NET35
        public void CommitNet35() => Commit().GetAwaiter().GetResult();
#endif
        public async Task Commit()
        {
            int delay = 500;
            int beginTransCount = 0, commitCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (info) =>
                {
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    beginTransCount++;
                },
                CommitTransaction = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    commitCount++;
                },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            Assert.Equal(0, beginTransCount);
            Assert.Equal(0, commitCount);
            connectionId = tracingConnection.Info.ConnectionId;
            using var trans = tracingConnection.BeginTransaction();
            Assert.Equal(1, beginTransCount);
            Assert.Equal(0, commitCount);
            await Task.Delay(delay, TestContext.Current.CancellationToken);
            trans.Commit();
            Assert.Equal(1, beginTransCount);
            Assert.Equal(1, commitCount);
        }

        [Fact]
#if NET
        public async Task CommitAsync()
        {
            int delay = 600;
            int beginTransCount = 0, commitCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (info) =>
                {
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    beginTransCount++;
                },
                CommitTransaction = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    commitCount++;
                },
            };
            await using var c = new TestConnection();
            await using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            await tracingConnection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, beginTransCount);
            Assert.Equal(0, commitCount);
            connectionId = tracingConnection.Info.ConnectionId;
            await using var trans = await tracingConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, beginTransCount);
            Assert.Equal(0, commitCount);
            await Task.Delay(delay, TestContext.Current.CancellationToken);
            await trans.CommitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, beginTransCount);
            Assert.Equal(1, commitCount);
        }
#else
        public void CommitAsync() { }
#endif

        [Fact]
#if NET35
        public void RollbackNet35() => Rollback().GetAwaiter().GetResult();
#endif
        public async Task Rollback()
        {
            int delay = 700;
            int beginTransCount = 0, rollbackCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (info) =>
                {
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    beginTransCount++;
                },
                RollbackTransaction = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    rollbackCount++;
                },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            Assert.Equal(0, beginTransCount);
            Assert.Equal(0, rollbackCount);
            connectionId = tracingConnection.Info.ConnectionId;
            using var trans = tracingConnection.BeginTransaction();
            Assert.Equal(1, beginTransCount);
            Assert.Equal(0, rollbackCount);
            await Task.Delay(delay, TestContext.Current.CancellationToken);
            trans.Rollback();
            Assert.Equal(1, beginTransCount);
            Assert.Equal(1, rollbackCount);
        }

        [Fact]
#if NET
        public async Task RollbackAsync()
        {
            int delay = 800;
            int beginTransCount = 0, rollbackCount = 0;
            Guid id = default, connectionId = default;
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (info) =>
                {
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.Equal(TimeSpan.Zero, info.Duration);
                    id = info.Id;
                    beginTransCount++;
                },
                RollbackTransaction = (info) =>
                {
                    Assert.Equal(id, info.Id);
                    Assert.Equal(connectionId, info.ConnectionInfo.ConnectionId);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    Thread.Sleep(DelayLowMargin + DelayHighMargin);
                    Assert.InRange(info.Duration.TotalMilliseconds, delay - DelayLowMargin, delay + DelayHighMargin);
                    rollbackCount++;
                },
            };
            await using var c = new TestConnection();
            await using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            await tracingConnection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, beginTransCount);
            Assert.Equal(0, rollbackCount);
            connectionId = tracingConnection.Info.ConnectionId;
            await using var trans = await tracingConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, beginTransCount);
            Assert.Equal(0, rollbackCount);
            await Task.Delay(delay, TestContext.Current.CancellationToken);
            await trans.RollbackAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, beginTransCount);
            Assert.Equal(1, rollbackCount);
        }
#else
        public void RollbackAsync() { }
#endif

        [Fact]
        public void DisposeTest()
        {
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
            };
            using var c = new TestConnection();
            using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            var trans = tracingConnection.BeginTransaction();
            var innerTrans = (TestTransaction)((TracingTransaction)trans).InnerTransaction;
            Assert.False(innerTrans.IsDisposed);
            trans.Dispose();
            Assert.True(innerTrans.IsDisposed);
        }

        [Fact]
#if NET
        public async Task DisposeAsyncTest()
        {
            var traceHandler = new TestTraceHandler
            {
                ConnectionOpened = (_) => { },
                ConnectionClosed = (_) => { },
                BeginTransaction = (_) => { },
            };
            await using var c = new TestConnection();
            await using var tracingConnection = new TracingConnection(c, traceHandler, null);
            tracingConnection.ConnectionString = "test";
            tracingConnection.Open();
            var trans = await tracingConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
            var innerTrans = (TestTransaction)((TracingTransaction)trans).InnerTransaction;
            Assert.False(innerTrans.IsDisposed);
            await trans.DisposeAsync();
            Assert.True(innerTrans.IsDisposed);
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
                Guid id = default, connectionId = default;
                var traceHandler = new TestTraceHandler
                {
                    ConnectionOpened = (_) => { },
                    ConnectionClosed = (_) => { },
                    BeginTransaction = (_) => { },
                    RollbackTransaction = (_) => { },
                    DisposeLeakDetected = (info) =>
                    {
                        Assert.Equal(DbObjectKind.Transaction, info.Kind);
                        Assert.Equal(id, info.Id);
                        Assert.Equal(connectionId, ((TransactionInfo)info).ConnectionInfo.Id);
                        leakCount++;
                    },
                };
                using var innerConnection = new TestConnection();
                using var tracingConnection = new TracingConnection(innerConnection, traceHandler, null);
                connectionId = tracingConnection.Info.Id;
                tracingConnection.ConnectionString = "test";
                tracingConnection.Open();
#pragma warning disable CA2000
                var trans = tracingConnection.BeginTransaction();
#pragma warning restore CA2000
                id = ((TracingTransaction)trans).Info.Id;
                trans.Rollback();
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
