using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    internal sealed class TracingCommand : DbCommand
    {
        private readonly DbCommand _innerCommand;
        private readonly DataAccessTraceHandler _traceHandler;
        private TracingConnection? _connection;
        private TracingTransaction? _transaction;
        private readonly bool _isParameterValidationEnabled;
        private readonly CommandInfo _info;

        internal TracingCommand(DbCommand innerCommand, DataAccessTraceHandler traceHandler, TracingConnection? connection, bool isParameterValidationEnabled)
        {
            _innerCommand = innerCommand;
            _traceHandler = traceHandler;
            _connection = connection;
            _isParameterValidationEnabled = isParameterValidationEnabled;
            _info = new();
        }

        //

#if NET
        [AllowNull]
#endif
        public override string CommandText
        {
            get => _innerCommand.CommandText;
            set => _innerCommand.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _innerCommand.CommandTimeout;
            set => _innerCommand.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _innerCommand.CommandType;
            set => _innerCommand.CommandType = value;
        }

        public override bool DesignTimeVisible
        {
            get => _innerCommand.DesignTimeVisible;
            set => _innerCommand.DesignTimeVisible = value;
        }

        public override ISite? Site
        {
            get => _innerCommand.Site;
            set => _innerCommand.Site = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _innerCommand.UpdatedRowSource;
            set => _innerCommand.UpdatedRowSource = value;
        }

        protected override DbConnection? DbConnection
        {
            get => _connection;
            set
            {
                if (value is TracingConnection tracingConnection)
                {
                    _connection = tracingConnection;
                    _innerCommand.Connection = tracingConnection.InnerConnection;
                }
                else if (value is not null)
                {
                    DbProviderFactory? innerFactory;
#if !NET35
                    innerFactory = DbProviderFactories.GetFactory(value);
#else
                    innerFactory = (DbProviderFactory)value.GetType().GetProperty(nameof(DbProviderFactory), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value, null);
#endif
                    var tracingFactory = innerFactory is null ? null : new TracingProviderFactory(innerFactory);
                    _connection = new TracingConnection(value, _traceHandler, tracingFactory);
                    _innerCommand.Connection = value;
                }
                else
                {
                    _connection = null;
                }
            }
        }

        protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

        protected override DbTransaction? DbTransaction
        {
            get => _transaction;
            set
            {
                _transaction = value as TracingTransaction;
                _innerCommand.Transaction = _transaction?.InnerTransaction;

                // MEMO:
                // Glimpse ではここで TracingTransaction 以外の場合、 value をそのまま _innerCommand.Transaction に入れている。
                // TracingTransaction 以外がわたってくることはあるのか？
            }
        }

        public override void Cancel() => _innerCommand.Cancel();

        public override void Prepare() => _innerCommand.Prepare();

        protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

        public override int ExecuteNonQuery()
        {
            using var _ = BeginExecution(out var info);
            int recordsAffected;
            try
            {
                recordsAffected = _innerCommand.ExecuteNonQuery();
                info.RecordsAffected = recordsAffected;
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                _traceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            _traceHandler.OnCommandExecuted(info);
            return recordsAffected;
        }

        public override object? ExecuteScalar()
        {
            using var _ = BeginExecution(out var info);
            object? result;
            try
            {
                result = _innerCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                _traceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            _traceHandler.OnCommandExecuted(info);
            return result;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            using var _ = BeginExecution(out var info);
            DbDataReader innerReader;
            try
            {
                innerReader = _innerCommand.ExecuteReader(behavior);
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                _traceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            _traceHandler.OnCommandExecuted(info);
            return new TracingDataReader(innerReader, _traceHandler, info.ExecutionId, behavior, _connection);
        }

#if !NET35
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            using var _ = BeginExecution(out var info);
            int recordsAffected;
            try
            {
                recordsAffected = await _innerCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                info.RecordsAffected = recordsAffected;
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                _traceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            _traceHandler.OnCommandExecuted(info);
            return recordsAffected;
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            using var _ = BeginExecution(out var info);
            object? result;
            try
            {
                result = await _innerCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                _traceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            _traceHandler.OnCommandExecuted(info);
            return result;
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            using var _ = BeginExecution(out var info);
            DbDataReader innerReader;
            try
            {
                innerReader = await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                _traceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            _traceHandler.OnCommandExecuted(info);
            return new TracingDataReader(innerReader, _traceHandler, info.ExecutionId, behavior, _connection);
        }

#if !NETFRAMEWORK
        public override Task PrepareAsync(CancellationToken cancellationToken = default) => _innerCommand.PrepareAsync(cancellationToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2215")]
        public override async ValueTask DisposeAsync()
        {
            bool anyparam = _innerCommand.Parameters.Cast<object>().Any();
            if (!_info.IsUsed && !anyparam)
                _info.CommandText = _innerCommand.CommandText;
            await _innerCommand.DisposeAsync().ConfigureAwait(false);
            // base.DisposeAsync() を呼ぶと base.Dispose() -> this.Dispose(true) が呼ばれるため、 base.Dispose(true) だけ呼んでおく。
            base.Dispose(true);
            // dispose check
            GC.SuppressFinalize(this);
            if (!_info.IsUsed && !anyparam)
                _traceHandler.OnUnusedObjectDetected(_info);
        }
#endif
#endif

        protected override void Dispose(bool disposing)
        {
            // ループの外側で複数コマンドを定義し、ループ内で対象データがなく使用されないことがある。
            // パラメータがあれば使用されたこととする。
            bool anyparam = false;
            if (disposing)
            {
                anyparam = _innerCommand.Parameters.Cast<object>().Any();
                if (!_info.IsUsed && !anyparam)
                    _info.CommandText = _innerCommand.CommandText;
                _innerCommand.Dispose();
            }

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
            if (!_info.IsUsed && !anyparam)
                _traceHandler.OnUnusedObjectDetected(_info);
        }

        //

        internal void Used()
        {
            _info.Used();
            _connection?.Info.Used();
            _transaction?.Info.Used();
        }

        internal DbCommand GetInnerCommand()
        {
            Used();
            return _innerCommand;
        }

        internal CommandExecutionInfo.Disposer BeginExecution(out CommandExecutionInfo info, [CallerMemberName] string methodName = "")
        {
            Used();
            info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, _innerCommand.CommandText, _innerCommand.CommandType, _innerCommand.Parameters, methodName,
                _traceHandler, _isParameterValidationEnabled);
            var disposer = info.GetDisposer();
            try
            {
                _traceHandler.OnCommandExecuting(info);
                info.Begin();
            }
            catch
            {
                disposer.Dispose();
                throw;
            }
            return disposer;
        }

        // dispose check
        ~TracingCommand()
        {
            try
            {
                _traceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
