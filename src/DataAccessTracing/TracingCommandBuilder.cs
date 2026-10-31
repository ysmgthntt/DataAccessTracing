using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using DataAccessTracing.Diagnostics;

namespace DataAccessTracing
{
    internal sealed class TracingCommandBuilder<TDbProviderFactory> : DbCommandBuilder
        where TDbProviderFactory : DbProviderFactory
    {
        private readonly DbCommandBuilder _innerBuilder;
        private readonly CommandBuilderInfo _info;

        internal TracingCommandBuilder(DbCommandBuilder innerBuilder)
        {
            _innerBuilder = innerBuilder;
            _info = new();
        }

        private Action<DbParameter, DataRow, StatementType, bool>? _applyParameterInfo;

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            _applyParameterInfo ??= CreateInnerBuilderMethodDelegate<Action<DbParameter, DataRow, StatementType, bool>>();
            _applyParameterInfo(parameter, row, statementType, whereClause);
        }

        private Func<int, string>? _getParameterNameByOrdinal;

        protected override string GetParameterName(int parameterOrdinal)
        {
            _getParameterNameByOrdinal ??= CreateInnerBuilderMethodDelegate<Func<int, string>>([typeof(int)]);
            return _getParameterNameByOrdinal(parameterOrdinal);
        }

        private Func<string, string>? _getParameterNameByName;

        protected override string GetParameterName(string parameterName)
        {
            _getParameterNameByName ??= CreateInnerBuilderMethodDelegate<Func<string, string>>([typeof(string)]);
            return _getParameterNameByName(parameterName);
        }

        private Func<int, string>? _getParameterPlaceholder;

        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            _getParameterPlaceholder ??= CreateInnerBuilderMethodDelegate<Func<int, string>>();
            return _getParameterPlaceholder(parameterOrdinal);
        }

        private Action<DbDataAdapter>? _setRowUpdatingHandler;

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            if (adapter is TracingDataAdapter tracingDataAdapter)
            {
                tracingDataAdapter.Info.Used();
                adapter = tracingDataAdapter.InnerDataAdapter;
            }
            _setRowUpdatingHandler ??= CreateInnerBuilderMethodDelegate<Action<DbDataAdapter>>();
            _setRowUpdatingHandler(adapter);
        }

        //

        public override CatalogLocation CatalogLocation
        {
            get => _innerBuilder.CatalogLocation;
            set => _innerBuilder.CatalogLocation = value;
        }

        public override string CatalogSeparator
        {
            get => _innerBuilder.CatalogSeparator;
            set => _innerBuilder.CatalogSeparator = value;
        }

        public override ConflictOption ConflictOption
        {
            get => _innerBuilder.ConflictOption;
            set => _innerBuilder.ConflictOption = value;
        }

        private Func<DbCommand, DataTable>? _getSchemaTable;

        protected override DataTable GetSchemaTable(DbCommand sourceCommand)
        {
            if (sourceCommand is TracingCommand tracingCommand)
            {
                tracingCommand.Info.Used();
                sourceCommand = tracingCommand.InnerCommand;
            }
            _getSchemaTable ??= CreateInnerBuilderMethodDelegate<Func<DbCommand, DataTable>>();
            return _getSchemaTable(sourceCommand);
        }

        private Func<DbCommand, DbCommand>? _initializeCommand;

        protected override DbCommand InitializeCommand(DbCommand command)
        {
            if (command is TracingCommand tracingCommand)
            {
                tracingCommand.Info.Used();
                command = tracingCommand.InnerCommand;
            }
            _initializeCommand ??= CreateInnerBuilderMethodDelegate<Func<DbCommand, DbCommand>>();
            return _initializeCommand(command);
        }

        public override string QuoteIdentifier(string unquotedIdentifier) => _innerBuilder.QuoteIdentifier(unquotedIdentifier);

        public override string QuotePrefix
        {
            get => _innerBuilder.QuotePrefix;
            set => _innerBuilder.QuotePrefix = value;
        }

        public override string QuoteSuffix
        {
            get => _innerBuilder.QuoteSuffix;
            set => _innerBuilder.QuoteSuffix = value;
        }

        public override void RefreshSchema() => _innerBuilder.RefreshSchema();

        public override string SchemaSeparator
        {
            get => _innerBuilder.SchemaSeparator;
            set => _innerBuilder.SchemaSeparator = value;
        }

        public override string UnquoteIdentifier(string quotedIdentifier) => _innerBuilder.UnquoteIdentifier(quotedIdentifier);

        //

        private TDelegate CreateInnerBuilderMethodDelegate<TDelegate>([CallerMemberName] string methodName = "")
            where TDelegate : Delegate
        {
            var mi = _innerBuilder.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
#if !NET35
            var d = (TDelegate)mi.CreateDelegate(typeof(TDelegate));
#else
            var d = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), _innerBuilder, mi);
#endif
            return d;
        }

        private TDelegate CreateInnerBuilderMethodDelegate<TDelegate>(Type[] types, [CallerMemberName] string methodName = "")
            where TDelegate : Delegate
        {
            var mi = _innerBuilder.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
#if !NET35
            var d = (TDelegate)mi.CreateDelegate(typeof(TDelegate));
#else
            var d = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), _innerBuilder, mi);
#endif
            return d;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _innerBuilder.Dispose();

            base.Dispose(disposing);

            // dispose check
            if (disposing)
                GC.SuppressFinalize(this);
        }

        // dispose check
        ~TracingCommandBuilder()
        {
            try
            {
                DataAccessTrace.TraceHandler.OnDisposeLeakDetected(_info);
            }
            catch { }
        }

        //

        private static bool _noDeriveParametersMethod;
        private static Action<DbCommand>? _deriveParametersMethodProxy;

        private static Action<DbCommand>? CreateDeriveParametersMethodProxy()
        {
            var innerFactory = TracingProviderFactory<TDbProviderFactory>.Instance.InnerFactory;
            using var cb = innerFactory.CreateCommandBuilder();
            var mi = cb.GetType().GetMethod(nameof(DeriveParameters), BindingFlags.Public | BindingFlags.Static);
            if (mi is null)
            {
                _noDeriveParametersMethod = true;
                return null;
            }

            var parameters = mi.GetParameters();
            if (parameters.Length != 1)
            {
                _noDeriveParametersMethod = true;
                return null;
            }

            // void CallDeriveParameters(DbCommand command) => XxxCommandBuilder.DeriveParameters((XxxCommand)command);
            var inputParam = Expression.Parameter(typeof(DbCommand), "command");
            var targetParamType = parameters[0].ParameterType;
            var castParam = Expression.Convert(inputParam, targetParamType);
            var body = Expression.Call(mi, castParam);
            var lambda = Expression.Lambda<Action<DbCommand>>(body, inputParam);
            var method = lambda.Compile();
            return method;
        }

        public static void DeriveParameters(TracingCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (_deriveParametersMethodProxy is null)
            {
                if (_noDeriveParametersMethod || (_deriveParametersMethodProxy = CreateDeriveParametersMethodProxy()) is null)
                    throw new NotSupportedException($"{typeof(TDbProviderFactory).Name} does not support parameter discovery.");
            }

            _deriveParametersMethodProxy(command.InnerCommand);
        }
    }
}
