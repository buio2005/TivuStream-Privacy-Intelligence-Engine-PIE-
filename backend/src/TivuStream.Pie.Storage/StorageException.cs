namespace TivuStream.Pie.Storage;

/// <summary>
/// Failure occurred while reading from or writing to the database.
/// </summary>
/// <remarks>
/// The Storage translates the failures of the database into this exception,
/// so that the rest of the system never handles errors expressed in the
/// vocabulary of a specific storage technology.
/// </remarks>
public sealed class StorageException : Exception
{
    /// <summary>
    /// Creates an exception with no description.
    /// </summary>
    public StorageException()
    {
    }

    /// <summary>
    /// Creates an exception with a description of the failure.
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    public StorageException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with a description of the failure and its cause.
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">Failure that caused this one.</param>
    public StorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
