using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace DataAccessTracing.Diagnostics
{
    public sealed class CommandExecutionInfo : IDisposable
    {
        public Guid ExecutionId { get; }
        public CommandInfo CommandInfo { get; }
        public ConnectionInfo? ConnectionInfo { get; }
        public TransactionInfo? TransactionInfo { get; }
        public string MethodName { get; }
        public MethodBase? CallerMethod { get; }

        public string CommandText { get; }
        public CommandType CommandType { get; }

        public TimeSpan Duration => _sw.Elapsed;
        public int RecordsAffected { get; internal set; }

        public Exception? Exception
        {
            get;
            internal set
            {
                field = value;
                _sw.Stop();
            }
        }

        private IDataParameterCollection? _parameterCollection;

        private readonly Stopwatch _sw = new();

        internal CommandExecutionInfo(CommandInfo info, ConnectionInfo? connectionInfo, TransactionInfo? transactionInfo,
            string commandText, CommandType commandType, IDataParameterCollection parameterCollection, string methodName)
        {
            ExecutionId = Guid.NewGuid();
            CommandInfo = info;
            ConnectionInfo = connectionInfo;
            TransactionInfo = transactionInfo;
            MethodName = methodName;
            CallerMethod = DataAccessTrace.GetCallerMethod(5);

            CommandText = commandText;
            CommandType = commandType;
            _parameterCollection = parameterCollection;
        }

        private string? _expandedCommandText;

        public string GetParameterExpandedCommandText()
            => _expandedCommandText ??= CommandType == CommandType.StoredProcedure ? GetStoredProcCommandText() : ExpandCommandText();

        private List<ParameterInfo>? _parameters;

        public IEnumerable<ParameterInfo> GetParameters()
            // 代入はしない。
            => _parameters ?? EnumerateParameters();

        private IEnumerable<ParameterInfo> EnumerateParameters()
        {
            if (_parameterCollection is null)
                yield break;
            foreach (DbParameter parameter in _parameterCollection)
                yield return new ParameterInfo(parameter.ParameterName, parameter.Value, parameter.DbType, parameter.Direction);
        }

        internal void Begin()
        {
            if (DataAccessTrace.IsParameterValidationEnabled)
            {
                // throw されると _parameterCollection は null になるため、 _expandedCommandText, _parameters を作成しておく。
                if (_expandedCommandText is null)
                    GetParameterExpandedCommandText();

                if (_missingParametersInCommandText is not null)
                {
                    var details = new InvalidParameterDetails(this, _missingParametersInCommandText.ToArray(), InvalidParameterReason.MissingInCommandText);
                    DataAccessTrace.TraceHandler.OnInvalidParameterDetected(details);
                }

                var duplicates = GetParameters()
                    .GroupBy(p => p.ParameterName)
                    .Where(g => g.Count() > 1)
                    ;
                if (duplicates.Any())
                {
                    var duplicatedParams = duplicates.SelectMany(g => g);
                    var details = new InvalidParameterDetails(this, duplicatedParams.ToArray(), InvalidParameterReason.DuplicateParameterName);
                    DataAccessTrace.TraceHandler.OnInvalidParameterDetected(details);
                }
            }

            _sw.Start();
        }

        internal void End() => _sw.Stop();

        private List<ParameterInfo>? _missingParametersInCommandText;

        private string ExpandCommandText()
        {
            var parameters = _parameters ??= new(EnumerateParameters());
            if (parameters.Count == 0)
                return CommandText;

            var sb = new StringBuilder(CommandText);

            // 短いパラメータ名が長いパラメータ名を置き換えてしまうため、長いものから処理する。
            foreach (var parameter in parameters.OrderByDescending(p => p.ParameterName.Length))
            {
                if (IndexOf(sb, parameter.ParameterName) < 0)
                {
                    (_missingParametersInCommandText ??= new()).Add(parameter);
                    continue;
                }

                var paramValue = parameter switch
                {
                    { Value: null or DBNull } => "NULL",
                    {
                        DbType:
                            DbType.String or
                            DbType.StringFixedLength or
                            DbType.AnsiString or
                            DbType.AnsiStringFixedLength or
                            DbType.Date or
                            DbType.Time or
                            DbType.DateTime or
                            DbType.DateTime2 or
                            DbType.DateTimeOffset
                    } p => $"'{p.Value}'",
                    { Value: Enum v } => $"{Convert.ToInt32(v, null)}/*{v}*/",
                    var p => p.Value.ToString(),
                };

                sb.Replace(parameter.ParameterName, paramValue);
            }

            var sql = sb.ToString();
            return sql;
        }

        private string GetStoredProcCommandText()
        {
            var parameters = _parameters ??= new(EnumerateParameters());
            if (parameters.Count == 0)
                return CommandText;

            var sb = new StringBuilder(CommandText);
            foreach (var p in parameters)
            {
                string direction = p.Direction == ParameterDirection.Input ? "" : " " + p.Direction;
                sb.AppendLine();
                sb.Append($"{p.ParameterName}({p.DbType}{direction}): {p.Value}");
            }

            var text = sb.ToString();
            return text;
        }

        // https://stackoverflow.com/questions/1359948/why-doesnt-stringbuilder-have-indexof-method
        private static int IndexOf(StringBuilder sb, string value, int startIndex = 0)
        {
            int index;
            int length = value.Length;
            int maxSearchLength = (sb.Length - length) + 1;

            for (int i = startIndex; i < maxSearchLength; ++i)
            {
                if (sb[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (sb[i + index] == value[index]))
                        ++index;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }

        void IDisposable.Dispose()
        {
            _parameterCollection = null;
            Exception = null;
            _sw.Stop();
        }
    }
}
