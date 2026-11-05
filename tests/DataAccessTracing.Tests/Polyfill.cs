#if NETFRAMEWORK
#pragma warning disable IDE0130
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    { }
}
#endif
#if NET35
namespace DataAccessTracing.Tests
{
    internal static class AssertExtensions
    {
        extension(Assert)
        {
            public static Exception Throws<T>(string? paramName, Assert.ThrowsDelegate testCode)
                where T : ArgumentException
            {
                var ex = Assert.Throws<T>(testCode);
                Assert.Equal(paramName, ex.ParamName);
                return ex;
            }

            public static void Fail() => throw new Xunit.Sdk.AssertException("Assert.Fail() Failure");
        }
    }
    internal static class TaskExtensions
    {
        extension(Task)
        {
            public static Task Delay(int dueTime, CancellationToken cancellationToken) => TaskEx.Delay(dueTime, cancellationToken);
        }
    }
    internal sealed class TestContext
    {
        public static TestContext Current = new();

        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
#endif
