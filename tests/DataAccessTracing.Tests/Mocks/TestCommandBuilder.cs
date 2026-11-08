using System.Data;
using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal abstract class TestCommandBuilderBase : DbCommandBuilder
    {
        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            TestParameter p = (TestParameter)parameter;
            Type type = (Type)row[SchemaTableColumn.DataType];

            DbType dbType = DbType.String;
            if (type == typeof(int))
                dbType = DbType.Int32;
            else if (type == typeof(string))
                dbType = DbType.String;
            else if (type == typeof(bool))
                dbType = DbType.Boolean;
            else if (type == typeof(DateTime))
                dbType = DbType.DateTime;
            else if (type == typeof(long))
                dbType = DbType.Int64;
            else if (type == typeof(double))
                dbType = DbType.Double;
            else if (type == typeof(decimal))
                dbType = DbType.Decimal;
            else if (type == typeof(Guid))
                dbType = DbType.Guid;
            else if (type == typeof(byte[]))
                dbType = DbType.Binary;
            else if (type == typeof(short))
                dbType = DbType.Int16;
            else if (type == typeof(byte))
                dbType = DbType.Byte;
            else if (type == typeof(float))
                dbType = DbType.Single;
            else if (type == typeof(char))
                dbType = DbType.StringFixedLength;
            else if (type == typeof(DateTimeOffset))
                dbType = DbType.DateTimeOffset;
            else if (type == typeof(sbyte))
                dbType = DbType.SByte;
            else if (type == typeof(ushort))
                dbType = DbType.UInt16;
            else if (type == typeof(uint))
                dbType = DbType.UInt32;
            else if (type == typeof(ulong))
                dbType = DbType.UInt64;

            p.DbType = dbType;
        }

        protected override string GetParameterName(int parameterOrdinal)
            => $"@p{parameterOrdinal}";

        protected override string GetParameterName(string parameterName)
            => "@" + parameterName;

        protected override string GetParameterPlaceholder(int parameterOrdinal)
            => $"@p{parameterOrdinal}";

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
        }
    }

    internal sealed class TestCommandBuilder : TestCommandBuilderBase
    {
        public bool IsDisposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;

            base.Dispose(disposing);
        }
    }

    internal sealed class TestCommandBuilderWithDeriveParameters : TestCommandBuilderBase
    {
        public static void DeriveParameters(TestCommand command)
        {
            command.CommandText = "DeriveParametersTest";
        }
    }
}
