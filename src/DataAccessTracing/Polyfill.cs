#if NET35
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class CallerMemberNameAttribute : Attribute
    {
        public CallerMemberNameAttribute()
        {
        }
    }
}
#endif
#if !NET
namespace DataAccessTracing
{
    internal static class StringBuilderExtensions
    {
        extension(System.Text.StringBuilder sb)
        {
            public System.Text.StringBuilder Append(IFormatProvider? _, string text) => sb.Append(text);
        }
    }
}
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}
#endif
