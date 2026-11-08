namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Provides details about invalid command parameters.
    /// </summary>
    public sealed class InvalidParameterDetails
    {
        /// <summary>
        /// Gets information about the command execution associated with the invalid parameters.
        /// </summary>
        public CommandExecutionInfo ExecutionInfo { get; }

        /// <summary>
        /// Gets the parameters that were detected as invalid.
        /// </summary>
        public ParameterInfo[] InvalidParameters { get; }

        /// <summary>
        /// Gets the reason why the parameters were detected as invalid.
        /// </summary>
        public InvalidParameterReason Reason { get; }

        internal InvalidParameterDetails(CommandExecutionInfo info, ParameterInfo[] parameters, InvalidParameterReason reason)
        {
            ExecutionInfo = info;
            InvalidParameters = parameters;
            Reason = reason;
        }
    }
}
