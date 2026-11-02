using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestProviderFactory : DbProviderFactory
    {
        public static readonly TestProviderFactory Instance = new();

        private TestProviderFactory()
        {
            DataAccessTraceTest.TestProviderFactoryCreateCount++;
        }
    }
}
