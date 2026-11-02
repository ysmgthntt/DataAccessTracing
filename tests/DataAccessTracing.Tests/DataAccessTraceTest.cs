using System.Data;
using System.Data.Common;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
{
    public class DataAccessTraceTest
    {
        public static int TestProviderFactoryCreateCount;

        [Fact]
        public void ReplaceDbProviderFactoriesWithTracingProviders_ShouldReplaceFactory()
        {
#if NET
            DbProviderFactories.RegisterFactory("TestProviderFactory", typeof(TestProviderFactory).AssemblyQualifiedName!);
#endif
            var before = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(0, TestProviderFactoryCreateCount);
            DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
#if NET
            Assert.Equal(1, TestProviderFactoryCreateCount);
#else
            Assert.Equal(0, TestProviderFactoryCreateCount);
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
                Assert.Equal(1, TestProviderFactoryCreateCount);
                DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
                Assert.Equal(1, TestProviderFactoryCreateCount);
            }
        }
    }
}
