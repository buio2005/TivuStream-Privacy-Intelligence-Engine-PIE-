namespace TivuStream.Pie.Adapters;

/// <summary>
/// What kind of failure prevented reaching a Data Source.
/// </summary>
/// <remarks>
/// Kept so that whoever is setting up the connection can be told what to
/// change: an address that answers nothing and a token that is refused call
/// for different remedies.
/// </remarks>
public enum AdapterFailure
{
    /// <summary>
    /// Any other failure: an answer that could not be read, a failure the
    /// Data Source reported, a setting missing.
    /// </summary>
    Other = 0,

    /// <summary>
    /// Nothing answered at the address.
    /// </summary>
    Unreachable = 1,

    /// <summary>
    /// Something answered, too late.
    /// </summary>
    NoAnswer = 2,

    /// <summary>
    /// The Data Source answered and refused the credentials.
    /// </summary>
    CredentialsRefused = 3,
}
