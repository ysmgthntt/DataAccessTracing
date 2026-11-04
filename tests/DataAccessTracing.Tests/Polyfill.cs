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
        }
    }
}
#endif
