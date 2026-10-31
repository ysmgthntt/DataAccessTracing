using System.Data;

namespace DataAccessTracing.Diagnostics
{
    public sealed class ParameterInfo
    {
        public string ParameterName { get; }
        public object? Value { get; }
        public DbType DbType { get; }
        public ParameterDirection Direction { get; }

        internal ParameterInfo(string parameterName, object? value, DbType dbType, ParameterDirection direction)
        {
            ParameterName = parameterName;
            Value = value;
            DbType = dbType;
            Direction = direction;
        }
    }
}
