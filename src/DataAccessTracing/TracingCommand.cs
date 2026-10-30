using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace DataAccessTracing
{
    using Diagnostics;

    internal sealed class TracingCommand : DbCommand
    {
        private readonly DbCommand _innerCommand;
        private TracingConnection? _connection;
        private TracingTransaction? _transaction;
        private readonly CommandInfo _info;

        internal TracingCommand(DbCommand innerCommand, TracingConnection? connection)
        {
            _innerCommand = innerCommand;
            _connection = connection;
            _info = new CommandInfo();
        }

        internal DbCommand InnerCommand => _innerCommand;

        //

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

        public override ISite Site
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
                    DbProviderFactory innerFactory;
#if !NET35
                    innerFactory = DbProviderFactories.GetFactory(value);
#else
                    innerFactory = (DbProviderFactory)value.GetType().GetProperty(nameof(DbProviderFactory), BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value, null);
#endif
                    _connection = new TracingConnection(value, new TracingProviderFactory(innerFactory));
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
            Used();
            using var info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, this);
            DataAccessTrace.TraceHandler.OnCommandExecuting(info);
            info.Begin();
            int recordsAffected;
            try
            {
                recordsAffected = _innerCommand.ExecuteNonQuery();
                info.RecordsAffected = recordsAffected;
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                DataAccessTrace.TraceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            DataAccessTrace.TraceHandler.OnCommandExecuted(info);
            return recordsAffected;
        }

        public override object ExecuteScalar()
        {
            Used();
            using var info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, this);
            DataAccessTrace.TraceHandler.OnCommandExecuting(info);
            info.Begin();
            object result;
            try
            {
                result = _innerCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                DataAccessTrace.TraceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            DataAccessTrace.TraceHandler.OnCommandExecuted(info);
            return result;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Used();
            using var info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, this);
            DataAccessTrace.TraceHandler.OnCommandExecuting(info);
            info.Begin();
            DbDataReader innerReader;
            try
            {
                innerReader = _innerCommand.ExecuteReader(behavior);
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                DataAccessTrace.TraceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            DataAccessTrace.TraceHandler.OnCommandExecuted(info);
            return new TracingDataReader(innerReader, info.ExecutionId, behavior, _connection);
        }

#if !NET35
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            Used();
            using var info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, this);
            DataAccessTrace.TraceHandler.OnCommandExecuting(info);
            info.Begin();
            int recordsAffected;
            try
            {
                recordsAffected = await _innerCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                info.RecordsAffected = recordsAffected;
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                DataAccessTrace.TraceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            DataAccessTrace.TraceHandler.OnCommandExecuted(info);
            return recordsAffected;
        }

        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            Used();
            using var info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, this);
            DataAccessTrace.TraceHandler.OnCommandExecuting(info);
            info.Begin();
            object result;
            try
            {
                result = await _innerCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                DataAccessTrace.TraceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            DataAccessTrace.TraceHandler.OnCommandExecuted(info);
            return result;
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            Used();
            using var info = new CommandExecutionInfo(_info, _connection?.Info, _transaction?.Info, this);
            DataAccessTrace.TraceHandler.OnCommandExecuting(info);
            info.Begin();
            DbDataReader innerReader;
            try
            {
                innerReader = await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                DataAccessTrace.TraceHandler.OnCommandException(info);
                throw;
            }
            info.End();
            DataAccessTrace.TraceHandler.OnCommandExecuted(info);
            return new TracingDataReader(innerReader, info.ExecutionId, behavior, _connection);
        }

#if !NETFRAMEWORK
        public override Task PrepareAsync(CancellationToken cancellationToken = default) => _innerCommand.PrepareAsync(cancellationToken);

        public override async ValueTask DisposeAsync()
        {
            await _innerCommand.DisposeAsync().ConfigureAwait(false);
            base.Dispose(true);
            // dispose check
            GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnusedObjectDetected(_info);
        }
#endif
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _innerCommand.Dispose();

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                DataAccessTrace.TraceHandler.OnUnusedObjectDetected(_info);
        }

        //

        // protected override object GetService(Type service) => ((IServiceProvider)_innerConnection).GetService(service);

        private void Used()
        {
            _info.Used();
            _connection?.Info.Used();
            _transaction?.Info.Used();
        }

        // dispose check
        ~TracingCommand()
        {
            try
            {
                DataAccessTrace.TraceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
