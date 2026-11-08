namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Represents an exception that occurs when invalid command parameters are detected.
    /// </summary>
    public sealed class InvalidParameterException : DataAccessTracingException
    {
        private readonly InvalidParameterDetails _details;

        /// <summary>
        /// Gets information about the command execution associated with the invalid parameters.
        /// </summary>
        public CommandExecutionInfo ExecutionInfo => _details.ExecutionInfo;

        /// <summary>
        /// Gets the parameters that were detected as invalid.
        /// </summary>
        public ParameterInfo[] InvalidParameters => _details.InvalidParameters;

        /// <summary>
        /// Gets the reason why the parameters were detected as invalid.
        /// </summary>
        public InvalidParameterReason Reason => _details.Reason;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidParameterException"/> class.
        /// </summary>
        /// <param name="details">Details about the invalid parameters.</param>
        public InvalidParameterException(InvalidParameterDetails details)
        {
            ArgumentNullException.ThrowIfNull(details);
            _details = details;
        }
    }
}
