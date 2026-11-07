using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestDataAdapter : DbDataAdapter
    {
        public bool IsDisposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;

            base.Dispose(disposing);
        }
    }
}
