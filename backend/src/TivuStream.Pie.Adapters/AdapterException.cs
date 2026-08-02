namespace TivuStream.Pie.Adapters;

/// <summary>
/// Failure occurred while communicating with a Data Source or while
/// converting its data.
/// </summary>
/// <remarks>
/// Adapters translate the failures of their Data Source into this exception,
/// so that the rest of the system never handles errors expressed in the
/// vocabulary of a specific backend.
/// <para>
/// Diagnostic details produced by the Data Source must not be carried beyond
/// the Adapter, as they may contain sensitive information.
/// </para>
/// </remarks>
public sealed class AdapterException : Exception
{
    /// <summary>
    /// Creates an exception with no description.
    /// </summary>
    public AdapterException()
    {
    }

    /// <summary>
    /// Creates an exception with a description of the failure.
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    public AdapterException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with a description of the failure and its cause.
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">Failure that caused this one.</param>
    public AdapterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
