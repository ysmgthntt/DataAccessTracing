using System.Data;
using System.Data.Common;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

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

        private sealed class EmptyTraceHandler : DataAccessTraceHandler { }

        [Fact]
        public void ReplaceDbProviderFactoriesWithTracingProviders_ShouldReplaceFactory()
        {
            DataAccessTrace.SetDefaultTraceHandler(new EmptyTraceHandler());
            DataAccessTrace.IsParameterValidationEnabledDefault = true;

            _ = new TracingProviderFactory(new TestProviderFactory());
            _ = TracingProviderFactory<TestProviderFactory>.Instance;

            DataAccessTrace.SetDefaultTraceHandler(new EmptyTraceHandler());
            DataAccessTrace.IsParameterValidationEnabledDefault = true;

#if NET
            DbProviderFactories.RegisterFactory("PrivateProviderFactory", typeof(PrivateProviderFactory).AssemblyQualifiedName!);
#endif
            var before = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(0, PrivateProviderFactoryCreateCount);

            DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
#if NET
            Assert.Equal(1, PrivateProviderFactoryCreateCount);
#else
            Assert.Equal(0, PrivateProviderFactoryCreateCount);
#endif
            Assert.Throws<InvalidOperationException>(() => DataAccessTrace.SetDefaultTraceHandler(new EmptyTraceHandler()));
            Assert.Throws<InvalidOperationException>(() => DataAccessTrace.IsParameterValidationEnabledDefault = true);

            for (int i = 0; i < 3; i++)
            {
                var after = DbProviderFactories.GetFactoryClasses();
                Assert.Equal(before.Rows.Count, after.Rows.Count);
                foreach (DataRow row in before.Rows)
                {
                    var name = row["InvariantName"].ToString()!;
                    var factory = DbProviderFactories.GetFactory(name);
                    var tracingFactory = Assert.IsAssignableFrom<TracingProviderFactory>(factory);
                    var typeName = row["AssemblyQualifiedName"].ToString()!;
                    Type originalType = Type.GetType(typeName)!;
                    Assert.Equal(originalType, tracingFactory.InnerFactory.GetType());
                }
                Assert.Equal(1, PrivateProviderFactoryCreateCount);

#if NET
                DbProviderFactories.RegisterFactory($"TestProviderFactory{i}", TestProviderFactory.Instance);
                foreach (DataRow row in DbProviderFactories.GetFactoryClasses().Rows)
                {
                    if (!before.AsEnumerable().Any(r => r["InvariantName"].ToString() == row["InvariantName"].ToString()))
                    {
                        var newrow = before.NewRow();
                        newrow.ItemArray = row.ItemArray;
                        before.Rows.Add(newrow);
                        break;
                    }
                }
#endif

                DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
                Assert.Equal(1, PrivateProviderFactoryCreateCount);
            }
        }

        [Fact]
        public void SetDefaultTraceHandler_ThrowsArgumentNullException_WhenHandlerIsNull()
        {
            Assert.Throws<ArgumentNullException>("traceHandler", () => DataAccessTrace.SetDefaultTraceHandler(null!));
        }
    }
}
