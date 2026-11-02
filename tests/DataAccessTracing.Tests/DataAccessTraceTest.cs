using System.Data;
using System.Data.Common;

namespace DataAccessTracing.Tests
{
    public class DataAccessTraceTest
    {
        private static int PrivateProviderFactoryCreateCount;

        private sealed class PrivateProviderFactory : DbProviderFactory
        {
            public static readonly PrivateProviderFactory Instance = new();

            private PrivateProviderFactory()
            {
                PrivateProviderFactoryCreateCount++;
            }
        }

        [Fact]
        public void ReplaceDbProviderFactoriesWithTracingProviders_ShouldReplaceFactory()
        {
#if NET
            DbProviderFactories.RegisterFactory("TestProviderFactory", typeof(PrivateProviderFactory).AssemblyQualifiedName!);
#endif
            var before = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(0, PrivateProviderFactoryCreateCount);
            DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
#if NET
            Assert.Equal(1, PrivateProviderFactoryCreateCount);
#else
            Assert.Equal(0, PrivateProviderFactoryCreateCount);
#endif
            for (int i = 0; i < 3; i++)
            {
                var after = DbProviderFactories.GetFactoryClasses();
                Assert.Equal(before.Rows.Count, after.Rows.Count);
                foreach (DataRow row in before.Rows)
                {
                    var name = row["InvariantName"].ToString()!;
                    var factory = DbProviderFactories.GetFactory(name);
                    Assert.IsAssignableFrom<TracingProviderFactory>(factory);
                    var typeName = row["AssemblyQualifiedName"].ToString()!;
                    Type originalType = Type.GetType(typeName)!;
                    Assert.Equal(originalType, ((TracingProviderFactory)factory).InnerFactory.GetType());
                }
                Assert.Equal(1, PrivateProviderFactoryCreateCount);
                DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
                Assert.Equal(1, PrivateProviderFactoryCreateCount);
            }
        }

        [Fact]
        public void SetDefaultTraceHandler_ThrowsArgumentNullException_WhenHandlerIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => DataAccessTrace.SetDefaultTraceHandler(null!));
            Assert.Equal("traceHandler", ex.ParamName);
        }
    }
}
