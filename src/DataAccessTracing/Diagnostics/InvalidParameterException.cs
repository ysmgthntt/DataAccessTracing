namespace DataAccessTracing.Diagnostics
{
    public sealed class InvalidParameterException : DataAccessTracingException
    {
        private readonly InvalidParameterDetails _details;

        public CommandExecutionInfo ExecutionInfo => _details.ExecutionInfo;
        public ParameterInfo[] InvalidParameters => _details.InvalidParameters;
        public InvalidParameterReason Reason => _details.Reason;

        internal InvalidParameterException(InvalidParameterDetails details)
        {
            _details = details;
        }
    }
}
