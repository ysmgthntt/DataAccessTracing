using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides information about a database command execution.
    /// </summary>
    public sealed class CommandExecutionInfo
    {
        /// <summary>
        /// Gets the unique identifier of the command execution.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets information about the command.
        /// </summary>
        public CommandInfo CommandInfo { get; }

        /// <summary>
        /// Gets information about the connection used to execute the command.
        /// </summary>
        public ConnectionInfo? ConnectionInfo { get; }

        /// <summary>
        /// Gets information about the transaction associated with the command execution.
        /// </summary>
        public TransactionInfo? TransactionInfo { get; }

        /// <summary>
        /// Gets the name of the method used to execute the command.
        /// </summary>
        public string ExecutionMethodName { get; }

        /// <summary>
        /// Gets the calling method.
        /// </summary>
        /// <remarks>
        /// This property may return <see langword="null" /> if the calling method
        /// cannot be determined, including when called from an asynchronous method.
        /// </remarks>
        public MethodBase? CallerMethod { get; }

        /// <summary>
        /// Gets the command text.
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        public CommandType CommandType { get; }

        /// <summary>
        /// Gets the duration of the command execution.
        /// </summary>
        public TimeSpan Duration => _sw.Elapsed;

        /// <summary>
        /// Gets the number of records affected by the command execution.
        /// </summary>
        /// <remarks>
        /// This property contains the return value of <see cref="System.Data.Common.DbCommand.ExecuteNonQuery"/>.
        /// It is always 0 when the command is executed using <see cref="System.Data.Common.DbCommand.ExecuteScalar"/>
        /// or <see cref="System.Data.Common.DbCommand.ExecuteReader()"/>.
        /// </remarks>
        public int RecordsAffected { get; internal set; }

        /// <summary>
        /// Gets the exception associated with the command execution.
        /// </summary>
        /// <remarks>
        /// This property is populated only when <see cref="DataAccessTraceHandler.OnCommandException"/>
        /// is invoked. It is <see langword="null" /> in other cases.
        /// </remarks>
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

        private readonly DataAccessTraceHandler _traceHandler;
        private readonly bool _isParameterValidationEnabled;

        private readonly Stopwatch _sw = new();

        internal CommandExecutionInfo(CommandInfo info, ConnectionInfo? connectionInfo, TransactionInfo? transactionInfo,
            string commandText, CommandType commandType, IDataParameterCollection parameterCollection, string executionMethodName,
            DataAccessTraceHandler traceHandler, bool isParameterValidationEnabled)
        {
            ExecutionId = Guid.NewGuid();
            CommandInfo = info;
            ConnectionInfo = connectionInfo;
            TransactionInfo = transactionInfo;
            ExecutionMethodName = executionMethodName;
            CallerMethod = DataAccessTrace.GetCallerMethod(4);

            CommandText = commandText;
            CommandType = commandType;
            _parameterCollection = parameterCollection;

            _traceHandler = traceHandler;
            _isParameterValidationEnabled = isParameterValidationEnabled;
        }

        private string? _expandedCommandText;

        /// <summary>
        /// Gets the command text with parameter placeholders replaced by their values.
        /// </summary>
        /// <remarks>
        /// Parameter placeholders are replaced with their corresponding values
        /// without parsing the command text.
        /// This method is intended for logging and diagnostic purposes.
        /// </remarks>
        public string GetParameterExpandedCommandText()
            => _expandedCommandText ??= CommandType == CommandType.StoredProcedure ? GetStoredProcCommandText() : ExpandCommandText();

        private List<ParameterInfo>? _parameters;

        /// <summary>
        /// Gets the parameters of the command.
        /// </summary>
        /// <returns>
        /// An enumerable collection of the command's parameters.
        /// </returns>
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

        [SuppressMessage("Style", "IDE0305")]
        internal void Begin()
        {
            if (_isParameterValidationEnabled)
            {
                // throw されると _parameterCollection は null になるため、 _expandedCommandText, _parameters を作成しておく。
                if (_expandedCommandText is null)
                    GetParameterExpandedCommandText();

                var duplicates = GetParameters()
                    .GroupBy(p => p.ParameterName)
                    .Where(g => g.Count() > 1)
                    ;
                if (duplicates.Any())
                {
                    var duplicatedParams = duplicates.SelectMany(g => g);
                    var details = new InvalidParameterDetails(this, duplicatedParams.ToArray(), InvalidParameterReason.DuplicateParameterName);
                    _traceHandler.OnInvalidParameterDetected(details);
                }
                else if (_missingParametersInCommandText is not null)
                {
                    var details = new InvalidParameterDetails(this, _missingParametersInCommandText.ToArray(), InvalidParameterReason.MissingInCommandText);
                    _traceHandler.OnInvalidParameterDetected(details);
                }
            }

            _sw.Start();
        }

        internal void End() => _sw.Stop();

        private List<ParameterInfo>? _missingParametersInCommandText;

        [SuppressMessage("Style", "IDE0028"), SuppressMessage("Style", "IDE0306")]
        private string ExpandCommandText()
        {
            var parameters = _parameters ??= new(EnumerateParameters());
            if (parameters.Count == 0)
                return CommandText;

            var sb = new StringBuilder(CommandText);

            // 短いパラメータ名が長いパラメータ名を置き換えてしまうため、長いものから処理する。
            foreach (var parameter in parameters.OrderByDescending(p => p.ParameterName.Length))
            {
                if (_isParameterValidationEnabled && IndexOf(sb, parameter.ParameterName) < 0)
                {
                    (_missingParametersInCommandText ??= new()).Add(parameter);
                    continue;
                }

                if (parameter.Direction is not (ParameterDirection.Input or ParameterDirection.InputOutput))
                    continue;

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

        [SuppressMessage("Style", "IDE0028"), SuppressMessage("Style", "IDE0306")]
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
                sb.Append(null, $"{p.ParameterName} ({p.DbType}{direction}): {p.Value}");
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

        internal struct Disposer(CommandExecutionInfo info) : IDisposable
        {
            public void Dispose()
            {
                info._parameterCollection = null;
                info.Exception = null;
                info._sw.Stop();
            }
        }

        internal Disposer GetDisposer() => new(this);
    }
}
