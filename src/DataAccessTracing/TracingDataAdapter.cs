using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    internal sealed class TracingDataAdapter : DbDataAdapter
    {
        private readonly DbDataAdapter _innerAdapter;
        private readonly DataAccessTraceHandler _traceHandler;
        private readonly DataAdapterInfo _info;

        internal TracingDataAdapter(DbDataAdapter innerAdapter, DataAccessTraceHandler traceHandler)
        {
            _innerAdapter = innerAdapter;
            _traceHandler = traceHandler;
            _info = new();

            // ????
            // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Common/src/System/Data/Common/DataAdapter.cs#L43
            GC.ReRegisterForFinalize(this);
        }

        public override bool ReturnProviderSpecificTypes
        {
            get => _innerAdapter.ReturnProviderSpecificTypes;
            set => _innerAdapter.ReturnProviderSpecificTypes = value;
        }

        public override int UpdateBatchSize
        {
            get => _innerAdapter.UpdateBatchSize;
            set => _innerAdapter.UpdateBatchSize = value;
        }

        public override int Fill(DataSet dataSet)
            => ExecuteWithTrace(() => _innerAdapter.Fill(dataSet));

        public override DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType)
            => ExecuteWithTrace(() => _innerAdapter.FillSchema(dataSet, schemaType));

        public override IDataParameter[] GetFillParameters() => _innerAdapter.GetFillParameters();

        public override bool ShouldSerializeAcceptChangesDuringFill() => _innerAdapter.ShouldSerializeAcceptChangesDuringFill();

        public override bool ShouldSerializeFillLoadOption() => _innerAdapter.ShouldSerializeFillLoadOption();

        //public override string ToString() => _innerAdapter.ToString();

        public override int Update(DataSet dataSet)
        {
            _info.Used();
            CopyTableMappingsToInnerAdapter();
            _innerAdapter.InsertCommand = GetInnerCommand(InsertCommand);
            _innerAdapter.UpdateCommand = GetInnerCommand(UpdateCommand);
            _innerAdapter.DeleteCommand = GetInnerCommand(DeleteCommand);

            return _innerAdapter.Update(dataSet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _innerAdapter.Dispose();

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
            if (!_info.IsUsed)
                _traceHandler.OnUnusedObjectDetected(_info);
        }

        private static DbCommand? GetInnerCommand(DbCommand? command)
            => (command as TracingCommand)?.GetInnerCommand() ?? command;

        //

        internal DbDataAdapter GetInnerDataAdapter()
        {
            _info.Used();
            return _innerAdapter;
        }

        private T ExecuteWithTrace<T>(Func<T> innerAdapterMethod, [CallerMemberName] string methodName = "")
        {
            _info.Used();
            CopyTableMappingsToInnerAdapter();
            var command = SelectCommand;
            if (command is TracingCommand tracingCommand)
            {
                _innerAdapter.SelectCommand = tracingCommand.GetInnerCommand();
                using var _ = tracingCommand.BeginExecution(out var info, methodName);
                T result;
                try
                {
                    result = innerAdapterMethod();
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
            else
            {
                _innerAdapter.SelectCommand = command;
                return innerAdapterMethod();
            }
        }

        private void CopyTableMappingsToInnerAdapter()
        {
            var innerTableMappings = _innerAdapter.TableMappings;
            innerTableMappings.Clear();
            foreach (ICloneable mapping in TableMappings)
                innerTableMappings.Add(mapping.Clone());
        }

        // unused check
        // innerAdapter を呼び出したもの以外は base の DbDataAdapter に直接処理されるため、 override して Used のみ実施しておく。

        protected override int Update(DataRow[] dataRows, DataTableMapping tableMapping)
        {
            _info.Used();
            // DataRowState.Added のみ、などの場合もすべて使用済にする。
            (InsertCommand as TracingCommand)?.Used();
            (UpdateCommand as TracingCommand)?.Used();
            (DeleteCommand as TracingCommand)?.Used();

            return base.Update(dataRows, tableMapping);
        }

        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Common/src/System/Data/Common/DbDataAdapter.cs#L421
        // 以下の 2 つが FillSchemaInternal から最終的に呼ばれる。
        protected override DataTable? FillSchema(DataTable dataTable, SchemaType schemaType, IDataReader dataReader)
        {
            _info.Used();
            return base.FillSchema(dataTable, schemaType, dataReader);
        }

        protected override DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, string srcTable, IDataReader dataReader)
        {
            _info.Used();
            return base.FillSchema(dataSet, schemaType, srcTable, dataReader);
        }

        // dispose check
        ~TracingDataAdapter()
        {
            try
            {
                _traceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }
    }
}
