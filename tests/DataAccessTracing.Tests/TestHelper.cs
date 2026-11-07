using System.Reflection;

namespace DataAccessTracing.Tests
{
    internal static class TestHelper
    {
        extension(object obj)
        {
            public TGet? GetPrivateFieldValue<TGet>(string fieldName)
            {
                var fi = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fi);
                return (TGet?)fi.GetValue(obj);
            }
        }
    }
}
