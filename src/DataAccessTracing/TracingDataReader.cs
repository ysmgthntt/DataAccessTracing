using System.Collections;
using System.Data;
using System.Data.Common;

namespace DataAccessTracing
{
    using Diagnostics;

    internal sealed class TracingDataReader : DbDataReader
    {
        private readonly DbDataReader _innerReader;
        private readonly DataReaderInfo _info;
        // dispose check
        private readonly CommandBehavior _behavior;
        private readonly TracingConnection? _connection;

        internal TracingDataReader(DbDataReader innerReader, CommandInfo commandInfo, CommandBehavior behavior, TracingConnection? connection)
        {
            _innerReader = innerReader;
            _info = new DataReaderInfo(commandInfo);
            // dispose check
            _behavior = behavior;
            _connection = connection;
        }

        private int _rowCount;

        //

        public override object this[int ordinal] => _innerReader[ordinal];

        public override object this[string name] => _innerReader[name];

        public override int Depth => _innerReader.Depth;

        public override int FieldCount => _innerReader.FieldCount;

        public override bool HasRows => _innerReader.HasRows;

        public override bool IsClosed => _innerReader.IsClosed;

        public override int RecordsAffected => _innerReader.RecordsAffected;

        public override int VisibleFieldCount => _innerReader.VisibleFieldCount;

        public override void Close()
        {
            _innerReader.Close();
            CloseDataReader();
        }

        public override bool GetBoolean(int ordinal) => _innerReader.GetBoolean(ordinal);

        public override byte GetByte(int ordinal) => _innerReader.GetByte(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => _innerReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        public override char GetChar(int ordinal) => _innerReader.GetChar(ordinal);

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => _innerReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        public override string GetDataTypeName(int ordinal) => _innerReader.GetDataTypeName(ordinal);

        public override DateTime GetDateTime(int ordinal) => _innerReader.GetDateTime(ordinal);

        public override decimal GetDecimal(int ordinal) => _innerReader.GetDecimal(ordinal);

        public override double GetDouble(int ordinal) => _innerReader.GetDouble(ordinal);

        public override IEnumerator GetEnumerator() => _innerReader.GetEnumerator();

        public override Type GetFieldType(int ordinal) => _innerReader.GetFieldType(ordinal);

        public override float GetFloat(int ordinal) => _innerReader.GetFloat(ordinal);

        public override Guid GetGuid(int ordinal) => _innerReader.GetGuid(ordinal);

        public override short GetInt16(int ordinal) => _innerReader.GetInt16(ordinal);

        public override int GetInt32(int ordinal) => _innerReader.GetInt32(ordinal);

        public override long GetInt64(int ordinal) => _innerReader.GetInt64(ordinal);

        public override string GetName(int ordinal) => _innerReader.GetName(ordinal);

        public override int GetOrdinal(string name) => _innerReader.GetOrdinal(name);

        public override DataTable GetSchemaTable() => _innerReader.GetSchemaTable();

        public override string GetString(int ordinal) => _innerReader.GetString(ordinal);

        public override object GetValue(int ordinal) => _innerReader.GetValue(ordinal);

        public override int GetValues(object[] values) => _innerReader.GetValues(values);

        public override bool IsDBNull(int ordinal) => _innerReader.IsDBNull(ordinal);

        public override bool NextResult() => _innerReader.NextResult();

        public override bool Read()
        {
            _info.Used();
            if (_innerReader.Read())
            {
                _rowCount++;
                return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _innerReader.Dispose();

            // Npgsql 対応。
            // DbDataReader.Dispose(true) は Close を呼び出す。
            // this.Close は _innerReadler.Close を呼び出す。
            // _innerReader.Dispose → _innerReader.Close の順に呼び出されると、 Npgsql の動作がおかしくなる。
            // _innerReader.Dispose で Close 処理はされているため、 base.Dispose は呼び出さないようにする。
            //base.Dispose(disposing);

            if (disposing)
            {
                // base.Dispose を呼ばないことで Close が呼ばれないので、ここで CloseDataReader を呼ぶ。
                CloseDataReader();
                // dispose check
                GC.SuppressFinalize(this);
                if (_behavior == CommandBehavior.CloseConnection && _connection is not null)
                    GC.SuppressFinalize(_connection);
            }
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnusedObjectDetected(_info);
        }

        //

        public override Type GetProviderSpecificFieldType(int ordinal) => _innerReader.GetProviderSpecificFieldType(ordinal);

        public override object GetProviderSpecificValue(int ordinal) => _innerReader.GetProviderSpecificValue(ordinal);

        public override int GetProviderSpecificValues(object[] values) => _innerReader.GetProviderSpecificValues(values);

#if !NET35
        public override T GetFieldValue<T>(int ordinal) => _innerReader.GetFieldValue<T>(ordinal);

        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => _innerReader.GetFieldValueAsync<T>(ordinal, cancellationToken);

        public override Stream GetStream(int ordinal) => _innerReader.GetStream(ordinal);

        public override TextReader GetTextReader(int ordinal) => _innerReader.GetTextReader(ordinal);

        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => _innerReader.IsDBNullAsync(ordinal, cancellationToken);

        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => _innerReader.NextResultAsync(cancellationToken);

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            _info.Used();
            if (await _innerReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                _rowCount++;
                return true;
            }
            return false;
        }

#if !NETFRAMEWORK
        public override async Task CloseAsync()
        {
            await _innerReader.CloseAsync().ConfigureAwait(false);
            CloseDataReader();
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerReader.DisposeAsync().ConfigureAwait(false);
            // Npgsql 対応。
            // base.Dispose を呼ぶと同期版の Close が呼ばれるので、ここでは呼ばない。
            // base.Dispose(true);
            // base.Dispose を呼ばないことで Close が呼ばれないので、ここで CloseDataReader を呼ぶ。
            CloseDataReader();
            // dispose check
            GC.SuppressFinalize(this);
            if (_behavior == CommandBehavior.CloseConnection && _connection is not null)
                GC.SuppressFinalize(_connection);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnusedObjectDetected(_info);
        }
#endif
#endif

        private void CloseDataReader()
        {
            _info.RowCount = _rowCount;
            DataAccessTrace.TraceHandler.OnDataReaderClosed(_info);
        }

        // dispose check
        ~TracingDataReader()
        {
            try
            {
                DataAccessTrace.TraceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
