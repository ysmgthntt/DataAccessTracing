namespace DataAccessTracing.Tests
{
    public class CallerMethodFixture : IDisposable
    {
        public static readonly ManualResetEvent Finished = new(true);

        private static int _count;

        public CallerMethodFixture()
        {
            if (Interlocked.Increment(ref _count) == 1)
                Finished.Reset();
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _count) == 0)
                Finished.Set();
        }
    }
}
