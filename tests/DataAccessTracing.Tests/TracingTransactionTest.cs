using DataAccessTracing;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing_Tests
{
    public class TracingTransactionTest
    {
        const int DelayMargin = 50;

        [Fact]
#if !NET35
        public async Task Commit()
#else
        public void Commit()
#endif
        {
            int delay = 1000;
            int beginTransCount = 0;
            int commitCount = 0;
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
                    Assert.InRange(info.Duration.TotalMilliseconds, delay, delay + DelayMargin);
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
#if !NET35
            await Task.Delay(delay);
#else
            Thread.Sleep(delay);
#endif
            trans.Commit();
            Assert.Equal(1, beginTransCount);
            Assert.Equal(1, commitCount);
        }
    }
}
