using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace DataAccessTracing
{
    internal sealed class TracingCommandBuilder<TDbProviderFactory> : DbCommandBuilder
        where TDbProviderFactory : DbProviderFactory
    {
        private readonly DbCommandBuilder _innerBuilder;

        public TracingCommandBuilder(DbCommandBuilder innerBuilder)
        {
            _innerBuilder = innerBuilder;
        }

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            throw new NotImplementedException();
        }

        protected override string GetParameterName(int parameterOrdinal)
        {
            throw new NotImplementedException();
        }

        protected override string GetParameterName(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            throw new NotImplementedException();
        }

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            throw new NotImplementedException();
        }

        //

        private static bool _noDeriveParametersMethod;
        private static Action<DbCommand>? _deriveParametersMethodProxy;

        private static Action<DbCommand>? CreateDeriveParametersMethodProxy()
        {
            var innerFactory = TracingProviderFactory<TDbProviderFactory>.Instance.InnerFactory;
            var cb = innerFactory.CreateCommandBuilder();
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
