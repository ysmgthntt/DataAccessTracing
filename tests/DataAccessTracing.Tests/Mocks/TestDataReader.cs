using System.Collections;
using System.Data;
using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestDataReader(DbDataReader dataReader) : DbDataReader
    {
        public override object this[int ordinal] => dataReader[ordinal];

        public override object this[string name] => dataReader[name];

        public override int Depth => dataReader.Depth;

        public override int FieldCount => dataReader.FieldCount;

        public override bool HasRows => dataReader.HasRows;

        public override bool IsClosed => dataReader.IsClosed;

        public override int RecordsAffected => dataReader.RecordsAffected;

        public override bool GetBoolean(int ordinal) => dataReader.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => dataReader.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => dataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => dataReader.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => dataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override string GetDataTypeName(int ordinal) => dataReader.GetDataTypeName(ordinal);
        public override DateTime GetDateTime(int ordinal) => dataReader.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => dataReader.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => dataReader.GetDouble(ordinal);
        public override IEnumerator GetEnumerator() => dataReader.GetEnumerator();
        //[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int ordinal) => dataReader.GetFieldType(ordinal);
        public override float GetFloat(int ordinal) => dataReader.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => dataReader.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => dataReader.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => dataReader.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => dataReader.GetInt64(ordinal);
        public override string GetName(int ordinal) => dataReader.GetName(ordinal);
        public override int GetOrdinal(string name) => dataReader.GetOrdinal(name);
        public override string GetString(int ordinal) => dataReader.GetString(ordinal);
        public override object GetValue(int ordinal) => dataReader.GetValue(ordinal);
        public override int GetValues(object[] values) => dataReader.GetValues(values);
        public override bool IsDBNull(int ordinal) => dataReader.IsDBNull(ordinal);
        public override bool NextResult() => dataReader.NextResult();
        public override bool Read() => dataReader.Read();

        public override DataTable? GetSchemaTable() => dataReader.GetSchemaTable();
        public override void Close() => dataReader?.Close();

        //

        public bool IsDisposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;

            base.Dispose(disposing);
        }
    }
}
