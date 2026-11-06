using System.Data;
using System.Data.Common;
using System.Reflection;
using DataAccessTracing;
using DataAccessTracing.Diagnostics;
using DataAccessTracing.Tests.Mocks;

namespace DataAccessTracing.Tests
{
    public class DataAccessTraceTest
    {
        private static int s_privateProviderFactoryCreateCount;

        private sealed class PrivateProviderFactory : DbProviderFactory
        {
            public static readonly PrivateProviderFactory Instance = new();

            private PrivateProviderFactory()
            {
                s_privateProviderFactoryCreateCount++;
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
            Assert.Equal(0, s_privateProviderFactoryCreateCount);

            DataAccessTrace.ReplaceDbProviderFactoriesWithTracingProviders();
#if NET
            Assert.Equal(1, s_privateProviderFactoryCreateCount);
#else
            Assert.Equal(0, s_privateProviderFactoryCreateCount);
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
#pragma warning disable xUnit2032 // Type assertions based on 'assignable from' are confusingly named
                    var tracingFactory = Assert.IsAssignableFrom<TracingProviderFactory>(factory);
#pragma warning restore xUnit2032 // Type assertions based on 'assignable from' are confusingly named
                    var typeName = row["AssemblyQualifiedName"].ToString()!;
                    Type originalType = Type.GetType(typeName)!;
                    Assert.Equal(originalType, tracingFactory.InnerFactory.GetType());
                }
                Assert.Equal(1, s_privateProviderFactoryCreateCount);

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
                Assert.Equal(1, s_privateProviderFactoryCreateCount);
            }
        }

        [Fact]
        public void SetDefaultTraceHandler_ThrowsArgumentNullException_WhenHandlerIsNull()
        {
            Assert.Throws<ArgumentNullException>("traceHandler", () => DataAccessTrace.SetDefaultTraceHandler(null!));
        }

        [Fact]
        public void StackTraceExcludedNamespacePrefixes()
        {
            MethodBase? callerMethod;
            callerMethod = System.Tests.DataAccessTraceTestInSystemNamespace.GetCallerMethod();
            Assert.Equal(nameof(DataAccessTraceTest), callerMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(StackTraceExcludedNamespacePrefixes), callerMethod?.Name);
            callerMethod = Microsoft.Tests.DataAccessTraceTestInMicrosoftNamespace.GetCallerMethod();
            Assert.Equal(nameof(DataAccessTraceTest), callerMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(StackTraceExcludedNamespacePrefixes), callerMethod?.Name);

            Thread.Sleep(1000);
            if (!CallerMethodFixture.Finished.WaitOne(5000))
                Assert.Fail();

            DataAccessTrace.StackTraceExcludedNamespacePrefixes = [nameof(DataAccessTracing), "Tests"];
            callerMethod = System.Tests.DataAccessTraceTestInSystemNamespace.GetCallerMethod();
            Assert.Equal("DataAccessTraceTestInSystemNamespace", callerMethod?.DeclaringType?.Name);
            Assert.Equal("GetCallerMethodInSystem", callerMethod?.Name);
            callerMethod = Microsoft.Tests.DataAccessTraceTestInMicrosoftNamespace.GetCallerMethod();
            Assert.Equal("DataAccessTraceTestInMicrosoftNamespace", callerMethod?.DeclaringType?.Name);
            Assert.Equal("GetCallerMethodInMicrosoft", callerMethod?.Name);

            DataAccessTrace.StackTraceExcludedNamespacePrefixes = null;
            callerMethod = System.Tests.DataAccessTraceTestInSystemNamespace.GetCallerMethod();
            Assert.Equal("DataAccessTraceTestInSystemNamespace", callerMethod?.DeclaringType?.Name);
            Assert.Equal("GetCallerMethodInSystem", callerMethod?.Name);
            callerMethod = Microsoft.Tests.DataAccessTraceTestInMicrosoftNamespace.GetCallerMethod();
            Assert.Equal("DataAccessTraceTestInMicrosoftNamespace", callerMethod?.DeclaringType?.Name);
            Assert.Equal("GetCallerMethodInMicrosoft", callerMethod?.Name);

            DataAccessTrace.StackTraceExcludedNamespacePrefixes = ["Tests", "System."];
            callerMethod = System.Tests.DataAccessTraceTestInSystemNamespace.GetCallerMethod();
            Assert.Equal(nameof(DataAccessTraceTest), callerMethod?.DeclaringType?.Name);
            Assert.Equal(nameof(StackTraceExcludedNamespacePrefixes), callerMethod?.Name);
            callerMethod = Microsoft.Tests.DataAccessTraceTestInMicrosoftNamespace.GetCallerMethod();
            Assert.Equal("DataAccessTraceTestInMicrosoftNamespace", callerMethod?.DeclaringType?.Name);
            Assert.Equal("GetCallerMethodInMicrosoft", callerMethod?.Name);
        }
    }
}
namespace System.Tests
{
    internal static class DataAccessTraceTestInSystemNamespace
    {
        private static MethodBase? GetCallerMethodInSystem()
            => DataAccessTrace.GetCallerMethod(0);

        public static MethodBase? GetCallerMethod()
            => GetCallerMethodInSystem();
    }
}
namespace Microsoft.Tests
{
    internal static class DataAccessTraceTestInMicrosoftNamespace
    {
        private static MethodBase? GetCallerMethodInMicrosoft()
            => DataAccessTrace.GetCallerMethod(0);

        public static MethodBase? GetCallerMethod()
            => GetCallerMethodInMicrosoft();
    }
}
