namespace DataAccessTracing.Diagnostics
{
    public sealed class InvalidParameterDetails
    {
        public CommandExecutionInfo ExecutionInfo { get; }
        public ParameterInfo[] InvalidParameters { get; }
        public InvalidParameterReason Reason { get; }

        internal InvalidParameterDetails(CommandExecutionInfo info, ParameterInfo[] parameters, InvalidParameterReason reason)
        {
            ExecutionInfo = info;
            InvalidParameters = parameters;
            Reason = reason;
        }
    }
}
