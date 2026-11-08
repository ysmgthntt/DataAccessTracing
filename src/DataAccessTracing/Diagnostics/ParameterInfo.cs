using System.Data;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database command parameter.
    /// </summary>
    public sealed class ParameterInfo
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the database type of the parameter.
        /// </summary>
        public DbType DbType { get; }

        /// <summary>
        /// Gets the direction of the parameter.
        /// </summary>
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
