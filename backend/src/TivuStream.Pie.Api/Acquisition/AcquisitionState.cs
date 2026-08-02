namespace TivuStream.Pie.Api.Acquisition;

/// <summary>
/// Holds the outcome of the most recent acquisition.
/// </summary>
/// <remarks>
/// The Query Flow reads from here and never reaches a Data Source. This is
/// what keeps a request coming from the Frontend separate from the
/// acquisition, as the Architecture Specification requires.
/// <para>
/// The state lives in memory only. Persistence is a later milestone, so
/// restarting the host discards the last acquisition.
/// </para>
/// </remarks>
public sealed class AcquisitionState
{
    private AcquisitionResult? _current;

    /// <summary>
    /// Outcome of the most recent acquisition, when one has taken place.
    /// </summary>
    public AcquisitionResult? Current => Volatile.Read(ref _current);

    /// <summary>
    /// Records the outcome of an acquisition.
    /// </summary>
    /// <param name="result">Outcome to record.</param>
    public void Update(AcquisitionResult result)
    {
        Volatile.Write(ref _current, result);
    }
}
