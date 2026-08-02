namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Operational status of a Data Source.
/// </summary>
/// <remarks>
/// The status reflects what the Adapter observed while communicating with the
/// Data Source, and nothing more. It is the one piece of the description that
/// only the Adapter can establish.
/// </remarks>
public enum DataSourceStatus
{
    /// <summary>
    /// The Data Source could not be reached, or refused the request.
    /// </summary>
    Unreachable = 0,

    /// <summary>
    /// The Data Source responded correctly.
    /// </summary>
    Online = 1,
}
