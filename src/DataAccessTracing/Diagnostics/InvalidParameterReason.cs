namespace DataAccessTracing.Diagnostics
{
    /// <summary>
    /// Specifies the reason why a command parameter is considered invalid.
    /// </summary>
    public enum InvalidParameterReason
    {
        /// <summary>
        /// A parameter exists in the parameter collection but is not referenced in the command text.
        /// </summary>
        MissingInCommandText,

        /// <summary>
        /// Multiple parameters have the same name.
        /// </summary>
        DuplicateParameterName,
    }
}
